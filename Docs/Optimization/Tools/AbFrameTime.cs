using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;

// 플레이 모드에서 실행: 같은 세션 안에서 "최적화 끔(기준 상태 재현)" ↔ "최적화 켬"을 번갈아 측정해
// 에디터 프레임 시간의 세션 간 편차를 상쇄한다.
// 기준 상태 재현 = 오클루전 컬링 끔 + LODGroup 끔 + Mesh LOD 0 강제 + Far 1000/1000
public static class AbFrameTime
{
    class View { public string Name; public Vector3 PlayerXZ; public float Yaw; public bool Quarter; }

    static readonly View[] Views =
    {
        new View { Name = "V1_Spawn",    PlayerXZ = new Vector3(10, 0, 0),      Yaw = 0 },
        new View { Name = "V2_LongRoad", PlayerXZ = new Vector3(78, 0, 0),      Yaw = 270 },
        new View { Name = "V3_TankOccl", PlayerXZ = new Vector3(14.4f, 0, -43), Yaw = 90 },
        new View { Name = "V4_Quarter",  PlayerXZ = new Vector3(10, 0, 0),      Yaw = 45, Quarter = true },
        new View { Name = "V5_Zombies",  PlayerXZ = new Vector3(56, 0, -26.5f), Yaw = 270 },
    };
    static readonly float[] ZombieX = { 53, 51, 46, 41, 36, 31, 26, 21, 16, 12 };
    const int Rounds = 3;
    const int SettleFrames = 30;
    const int SampleFrames = 120;

    static readonly string[] RecNames = { "Triangles Count", "Batches Count", "CPU Main Thread Frame Time", "CPU Render Thread Frame Time", "GPU Frame Time" };
    static ProfilerRecorder[] _recs;
    static List<double>[] _samples;
    static Dictionary<string, List<double>[]> _acc;
    static int _view, _round, _state, _frame, _lastFrame;
    static string _outDir;
    static Transform _player, _camPos;
    static Camera _cam, _minimap;
    static LODGroup[] _lodGroups;
    static Renderer[] _renderers;
    static List<GameObject> _zombies;

    public static string Start(string outDir)
    {
        if (!EditorApplication.isPlaying) return "not playing";
        _outDir = outDir;
        var stopTypes = new HashSet<string> { "CameraViewSwitcher", "CameraRotate", "CameraFollow", "PlayerRotate", "PlayerMove", "ZombieSpawner" };
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (stopTypes.Contains(mb.GetType().Name)) mb.enabled = false;
            if (mb.GetType().Name == "Zombie") mb.gameObject.SetActive(false);
        }
        _player = GameObject.Find("Player").transform;
        _camPos = _player.Find("CameraPosition");
        _cam = Camera.main;
        _minimap = GameObject.Find("MinimapCamera").GetComponent<Camera>();
        _player.GetComponent<CharacterController>().enabled = false;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Monster/Zombie.prefab");
        _zombies = new List<GameObject>();
        for (int i = 0; i < ZombieX.Length; i++)
        {
            var z = Object.Instantiate(prefab, Ground(new Vector3(ZombieX[i], 0, -26.5f + (i % 2 == 0 ? 1 : -1))) + Vector3.up, Quaternion.Euler(0, 90, 0));
            foreach (var mb in z.GetComponents<MonoBehaviour>()) if (mb.GetType().Name == "Zombie") mb.enabled = false;
            if (z.TryGetComponent(out UnityEngine.AI.NavMeshAgent agent)) agent.enabled = false;
            _zombies.Add(z);
        }
        _lodGroups = Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None);
        _renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);

        _recs = RecNames.Select(n => ProfilerRecorder.StartNew(ProfilerCategory.Render, n, 1)).ToArray();
        _samples = RecNames.Select(_ => new List<double>()).ToArray();
        _acc = new Dictionary<string, List<double>[]>();
        _view = 0; _round = 0; _state = 0; _frame = 0; _lastFrame = -1;
        File.Delete(Path.Combine(outDir, "6_AB_FrameTime.done"));
        EditorApplication.update += Tick;
        return "started";
    }

    static Vector3 Ground(Vector3 xz)
    {
        foreach (var h in Physics.RaycastAll(new Vector3(xz.x, 40, xz.z), Vector3.down, 80).OrderBy(h => h.distance))
        {
            if (h.collider.transform.root.name == "Player" || h.collider.GetComponentInParent<UnityEngine.AI.NavMeshAgent>() != null) continue;
            return new Vector3(xz.x, h.point.y, xz.z);
        }
        return xz;
    }

    static void ApplyState(bool optimized)
    {
        _cam.useOcclusionCulling = optimized;
        _minimap.useOcclusionCulling = optimized;
        _cam.farClipPlane = optimized ? 200 : 1000;
        _minimap.farClipPlane = optimized ? 50 : 1000;
        foreach (var g in _lodGroups) if (g != null) g.enabled = optimized;
        foreach (var r in _renderers) if (r != null) r.forceMeshLod = (short)(optimized ? -1 : 0);
    }

    static void ApplyView(View v)
    {
        _player.SetPositionAndRotation(Ground(v.PlayerXZ) + Vector3.up, Quaternion.Euler(0, v.Yaw, 0));
        if (v.Quarter)
        {
            var target = _player.position + Vector3.up * 0.5f;
            var eye = target + Quaternion.Euler(0, v.Yaw, 0) * new Vector3(0, 8, -8.5f);
            _cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(target - eye));
        }
        else
        {
            _cam.transform.SetPositionAndRotation(_camPos.position, Quaternion.Euler(3, v.Yaw, 0));
        }
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) { Finish(); return; }
        if (Time.frameCount == _lastFrame) return;
        _lastFrame = Time.frameCount;

        // 라운드마다 A/B 순서를 뒤집어 순서 효과도 상쇄 (AB, BA, AB)
        bool optimized = ((_state + _round) % 2) == 1;
        ApplyState(optimized);
        ApplyView(Views[_view]);
        _frame++;

        if (_frame > SettleFrames && _frame <= SettleFrames + SampleFrames)
        {
            for (int i = 0; i < _recs.Length; i++) _samples[i].Add(_recs[i].LastValue);
            return;
        }
        if (_frame <= SettleFrames + SampleFrames) return;

        string key = Views[_view].Name + "|" + (optimized ? "after" : "before");
        if (!_acc.TryGetValue(key, out var acc)) _acc[key] = acc = RecNames.Select(_ => new List<double>()).ToArray();
        for (int i = 0; i < _recs.Length; i++)
        {
            var s = _samples[i].OrderBy(x => x).ToList();
            acc[i].Add(s[s.Count / 2]);
            _samples[i].Clear();
        }

        _frame = 0;
        _state++;
        if (_state > 1) { _state = 0; _round++; }
        if (_round >= Rounds) { _round = 0; _view++; }
        if (_view >= Views.Length) Finish();
    }

    static void Finish()
    {
        EditorApplication.update -= Tick;
        foreach (var r in _recs) r.Dispose();
        ApplyState(true);
        foreach (var z in _zombies) if (z != null) Object.Destroy(z);

        var ci = System.Globalization.CultureInfo.InvariantCulture;
        var sb = new StringBuilder("[");
        foreach (var kv in _acc)
        {
            if (sb.Length > 1) sb.Append(',');
            var parts = kv.Key.Split('|');
            sb.Append("{\"view\":\"").Append(parts[0]).Append("\",\"state\":\"").Append(parts[1]).Append('"');
            for (int i = 0; i < RecNames.Length; i++)
            {
                double mean = kv.Value[i].Average();
                if (RecNames[i].Contains("Time")) mean /= 1e6;
                sb.Append(",\"").Append(RecNames[i].Replace(" ", "")).Append("\":").Append(mean.ToString("0.###", ci));
                if (RecNames[i].Contains("Time"))
                {
                    var ms = kv.Value[i].Select(x => (x / 1e6).ToString("0.###", ci));
                    sb.Append(",\"").Append(RecNames[i].Replace(" ", "")).Append("_rounds\":[").Append(string.Join(",", ms)).Append(']');
                }
            }
            sb.Append('}');
        }
        sb.Append(']');
        File.WriteAllText(Path.Combine(_outDir, "6_AB_FrameTime.json"), sb.ToString());
        File.WriteAllText(Path.Combine(_outDir, "6_AB_FrameTime.done"), "ok");
    }
}
