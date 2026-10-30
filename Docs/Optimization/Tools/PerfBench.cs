using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;

// 플레이 모드에서 실행: 고정 시점 5곳 x (전체 / tripo 제외) 렌더 카운터 + 프레임 시간 측정
public static class PerfBench
{
    class View { public string Name; public Vector3 PlayerXZ; public float Yaw; public float Pitch; public bool Quarter; }

    static readonly View[] Views =
    {
        new View { Name = "V1_Spawn",     PlayerXZ = new Vector3(10, 0, 0),     Yaw = 0,   Pitch = 3 },
        new View { Name = "V2_LongRoad",  PlayerXZ = new Vector3(78, 0, 0),     Yaw = 270, Pitch = 3 },
        new View { Name = "V3_TankOccl",  PlayerXZ = new Vector3(14.4f, 0, -43), Yaw = 90,  Pitch = 3 },
        new View { Name = "V4_Quarter",   PlayerXZ = new Vector3(10, 0, 0),     Yaw = 45,  Pitch = 0, Quarter = true },
        new View { Name = "V5_Zombies",   PlayerXZ = new Vector3(56, 0, -26.5f), Yaw = 270, Pitch = 3 },
    };

    static readonly float[] ZombieX = { 53, 51, 46, 41, 36, 31, 26, 21, 16, 12 };
    const float ZombieZ = -26.5f;
    const int SettleFrames = 30;
    const int SampleFrames = 90;

    static string _stage, _outDir;
    static int _viewIdx, _modeIdx, _frameInPhase, _lastFrame;
    static bool _setupDone;
    static Transform _player, _camPos;
    static Camera _cam;
    static List<Renderer> _tripo;
    static List<GameObject> _zombies;
    static ProfilerRecorder[] _recs;
    static readonly string[] RecNames =
    {
        "Triangles Count", "Vertices Count", "Batches Count", "SetPass Calls Count", "Draw Calls Count",
        "Shadow Casters Count", "Visible Skinned Meshes Count",
        "CPU Main Thread Frame Time", "CPU Render Thread Frame Time", "GPU Frame Time",
    };
    static List<double>[] _samples;
    static StringBuilder _json;

    public static string Start(string stage, string outDir)
    {
        if (!EditorApplication.isPlaying) return "not playing";
        _stage = stage; _outDir = outDir;
        Directory.CreateDirectory(outDir);
        _viewIdx = 0; _modeIdx = 0; _frameInPhase = 0; _lastFrame = -1; _setupDone = false;
        File.Delete(Path.Combine(outDir, stage + ".done"));
        _json = new StringBuilder();
        _recs = RecNames.Select(n => ProfilerRecorder.StartNew(ProfilerCategory.Render, n, 1)).ToArray();
        EditorApplication.update += Tick;
        return "started; valid=" + string.Join(",", _recs.Select((r, i) => RecNames[i] + ":" + r.Valid));
    }

    static void Setup()
    {
        // 측정 중에는 입력/AI/카메라 스크립트를 멈춰 시점을 고정한다
        var stopTypes = new HashSet<string> { "CameraViewSwitcher", "CameraRotate", "CameraFollow", "PlayerRotate", "PlayerMove", "ZombieSpawner" };
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (stopTypes.Contains(mb.GetType().Name)) mb.enabled = false;
            if (mb.GetType().Name == "Zombie") mb.gameObject.SetActive(false); // 스포너가 이미 꺼낸 좀비는 숨김 (측정 일관성)
        }

        _player = GameObject.Find("Player").transform;
        _camPos = _player.Find("CameraPosition");
        _cam = Camera.main;
        _player.GetComponent<CharacterController>().enabled = false;

        _tripo = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(r => (r is SkinnedMeshRenderer s && s.sharedMesh != null && s.sharedMesh.name.StartsWith("tripo"))
                     || (r.TryGetComponent(out MeshFilter mf) && mf.sharedMesh != null && mf.sharedMesh.name.StartsWith("tripo")))
            .ToList();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Monster/Zombie.prefab");
        _zombies = new List<GameObject>();
        for (int i = 0; i < ZombieX.Length; i++)
        {
            var p = Ground(new Vector3(ZombieX[i], 0, ZombieZ + (i % 2 == 0 ? 1 : -1))) + Vector3.up;
            var z = Object.Instantiate(prefab, p, Quaternion.Euler(0, 90, 0));
            z.name = "__BenchZombie" + i;
            foreach (var mb in z.GetComponents<MonoBehaviour>()) if (mb.GetType().Name == "Zombie") mb.enabled = false;
            if (z.TryGetComponent(out UnityEngine.AI.NavMeshAgent agent)) agent.enabled = false;
            _zombies.Add(z);
        }
        _samples = RecNames.Select(_ => new List<double>()).ToArray();
        _json.Append("{\"stage\":\"").Append(_stage).Append("\",\"tripoRenderers\":").Append(_tripo.Count)
             .Append(",\"mainFar\":").Append(_cam.farClipPlane)
             .Append(",\"screen\":\"").Append(Screen.width).Append('x').Append(Screen.height).Append('"')
             .Append(",\"results\":[");
    }

    static Vector3 Ground(Vector3 xz)
    {
        var hits = Physics.RaycastAll(new Vector3(xz.x, 40, xz.z), Vector3.down, 80);
        float y = 0;
        foreach (var h in hits.OrderBy(h => h.distance))
        {
            if (h.collider.transform.root.name == "Player" || h.collider.name.StartsWith("__Bench")) continue;
            y = h.point.y; break;
        }
        return new Vector3(xz.x, y, xz.z);
    }

    static void ApplyView(View v, bool includeTripo)
    {
        var g = Ground(v.PlayerXZ);
        _player.SetPositionAndRotation(g + Vector3.up, Quaternion.Euler(0, v.Yaw, 0));
        if (v.Quarter)
        {
            var target = _player.position + Vector3.up * 0.5f;
            var eye = target + Quaternion.Euler(0, v.Yaw, 0) * new Vector3(0, 8, -8.5f);
            _cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(target - eye));
        }
        else
        {
            _cam.transform.SetPositionAndRotation(_camPos.position, Quaternion.Euler(v.Pitch, v.Yaw, 0));
        }
        foreach (var r in _tripo) r.enabled = includeTripo;
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) { Finish(false); return; }
        if (Time.frameCount == _lastFrame) return;
        _lastFrame = Time.frameCount;

        if (!_setupDone) { Setup(); _setupDone = true; ApplyView(Views[0], true); _frameInPhase = 0; return; }

        var view = Views[_viewIdx];
        bool includeTripo = _modeIdx == 0;
        ApplyView(view, includeTripo); // 매 프레임 재적용 (애니메이션 등으로 흐트러지지 않게)
        _frameInPhase++;

        if (_frameInPhase > SettleFrames && _frameInPhase <= SettleFrames + SampleFrames)
        {
            for (int i = 0; i < _recs.Length; i++) _samples[i].Add(_recs[i].LastValue);
        }
        else if (_frameInPhase == SettleFrames + SampleFrames + 1)
        {
            AppendResult(view.Name, includeTripo ? "all" : "excl_tripo");
            if (includeTripo)
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(_outDir, _stage + "_" + view.Name + ".png"));
            }
        }
        else if (_frameInPhase >= SettleFrames + SampleFrames + 4)
        {
            _frameInPhase = 0;
            _modeIdx++;
            if (_modeIdx > 1) { _modeIdx = 0; _viewIdx++; }
            if (_viewIdx >= Views.Length) Finish(true);
        }
    }

    static void AppendResult(string view, string mode)
    {
        if (_json[_json.Length - 1] == '}') _json.Append(',');
        _json.Append("{\"view\":\"").Append(view).Append("\",\"mode\":\"").Append(mode).Append('"');
        for (int i = 0; i < RecNames.Length; i++)
        {
            var s = _samples[i].OrderBy(x => x).ToList();
            double med = s.Count == 0 ? 0 : s[s.Count / 2];
            double avg = s.Count == 0 ? 0 : s.Average();
            bool isTime = RecNames[i].Contains("Time");
            string key = RecNames[i].Replace(" ", "");
            if (isTime) { med /= 1e6; avg /= 1e6; } // ns → ms
            _json.Append(",\"").Append(key).Append("\":").Append(med.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
            if (isTime) _json.Append(",\"").Append(key).Append("_avg\":").Append(avg.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
            _samples[i].Clear();
        }
        _json.Append('}');
    }

    static void Finish(bool ok)
    {
        EditorApplication.update -= Tick;
        if (_recs != null) foreach (var r in _recs) r.Dispose();
        if (_tripo != null) foreach (var r in _tripo) if (r != null) r.enabled = true;
        if (_zombies != null) foreach (var z in _zombies) if (z != null) Object.Destroy(z);
        _json.Append("]}");
        File.WriteAllText(Path.Combine(_outDir, _stage + ".json"), _json.ToString());
        File.WriteAllText(Path.Combine(_outDir, _stage + ".done"), ok ? "ok" : "aborted");
    }
}
