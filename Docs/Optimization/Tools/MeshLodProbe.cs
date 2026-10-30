using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// 플레이 모드에서 실행: Sword/Pistol/DeadPool의 Mesh LOD 자동 선택 vs LOD0 강제를 비교
// (메인 카메라 / 그림자 / 미니맵 기여분 분리 + 근접 시점)
public static class MeshLodProbe
{
    class Config { public string Name; public Action Apply; public bool Shot; }

    const int SettleFrames = 20;
    const int SampleFrames = 60;

    static List<Config> _configs;
    static int _idx, _frame, _lastFrame;
    static ProfilerRecorder _tris;
    static List<long> _samples;
    static StringBuilder _out;
    static string _outDir;
    static Transform _player, _camPos;
    static Camera _cam, _minimap;
    static Renderer _sword, _pistol, _body;
    static Renderer[] _tripo;

    public static string Start(string outDir)
    {
        if (!EditorApplication.isPlaying) return "not playing";
        _outDir = outDir;
        var stopTypes = new HashSet<string> { "CameraViewSwitcher", "CameraRotate", "CameraFollow", "PlayerRotate", "PlayerMove", "ZombieSpawner" };
        foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (stopTypes.Contains(mb.GetType().Name)) mb.enabled = false;
            if (mb.GetType().Name == "Zombie") mb.gameObject.SetActive(false);
        }
        _player = GameObject.Find("Player").transform;
        _camPos = _player.Find("CameraPosition");
        _cam = Camera.main;
        _minimap = GameObject.Find("MinimapCamera").GetComponent<Camera>();
        _player.GetComponent<CharacterController>().enabled = false;
        _player.SetPositionAndRotation(new Vector3(10, 1, 0), Quaternion.identity);

        var all = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        _sword = all.First(r => r.name == "Sword");
        _pistol = all.First(r => r.name == "Pistol");
        _body = all.First(r => r is SkinnedMeshRenderer s && s.sharedMesh != null && s.sharedMesh.name.StartsWith("tripo"));
        _tripo = new[] { _sword, _pistol, _body };

        _configs = new List<Config>();
        foreach (bool forced in new[] { false, true })
        {
            string tag = forced ? "LOD0강제" : "자동";
            // 게임 기본 상태(총 모드)에서는 SwordHolder가 비활성 → 검 근접 시점에서만 켠다
            _configs.Add(new Config { Name = "V1_전체_" + tag, Apply = () => { Sword(false); Fps(); Set(forced, true, true); } });
            _configs.Add(new Config { Name = "V1_미니맵끔_" + tag, Apply = () => { Sword(false); Fps(); Set(forced, false, true); } });
            _configs.Add(new Config { Name = "V1_미니맵끔_그림자끔_" + tag, Apply = () => { Sword(false); Fps(); Set(forced, false, false); } });
            _configs.Add(new Config { Name = "근접_Sword0.6m_" + tag, Shot = true, Apply = () => { Sword(true); Look(_sword, 0.6f); Set(forced, false, true); } });
            _configs.Add(new Config { Name = "근접_Pistol0.4m_" + tag, Shot = true, Apply = () => { Sword(false); Look(_pistol, 0.4f); Set(forced, false, true); } });
            _configs.Add(new Config { Name = "근접_DeadPool2m_" + tag, Shot = true, Apply = () => { Sword(false); Look(_body, 2f); Set(forced, false, true); } });
            _configs.Add(new Config { Name = "원거리_DeadPool15m_" + tag, Apply = () => { Sword(false); Look(_body, 15f); Set(forced, false, true); } });
        }

        _idx = 0; _frame = 0; _lastFrame = -1;
        _samples = new List<long>();
        _out = new StringBuilder("[");
        _tris = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count", 1);
        File.Delete(Path.Combine(outDir, "4_MeshLOD_probe.done"));
        EditorApplication.update += Tick;
        return "started " + _configs.Count + " configs";
    }

    static void Fps()
    {
        _cam.transform.SetPositionAndRotation(_camPos.position, Quaternion.Euler(3, 0, 0));
    }

    static void Sword(bool on)
    {
        _sword.transform.parent.gameObject.SetActive(on);
    }

    static void Look(Renderer r, float dist)
    {
        var c = r.bounds.center;
        var eye = c + _player.forward * dist + Vector3.up * dist * 0.15f;
        _cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(c - eye));
    }

    static void Set(bool forceLod0, bool minimap, bool tripoShadows)
    {
        foreach (var r in _tripo)
        {
            r.forceMeshLod = (short)(forceLod0 ? 0 : -1);
            r.shadowCastingMode = tripoShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }
        _minimap.enabled = minimap;
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) { Finish(); return; }
        if (Time.frameCount == _lastFrame) return;
        _lastFrame = Time.frameCount;

        var cfg = _configs[_idx];
        cfg.Apply();
        _frame++;
        if (_frame > SettleFrames && _frame <= SettleFrames + SampleFrames)
        {
            _samples.Add(_tris.LastValue);
        }
        else if (_frame == SettleFrames + SampleFrames + 1)
        {
            var s = _samples.OrderBy(x => x).ToList();
            if (_out.Length > 1) _out.Append(',');
            _out.Append("{\"config\":\"").Append(cfg.Name).Append("\",\"tris\":").Append(s[s.Count / 2]).Append('}');
            _samples.Clear();
            if (cfg.Shot) ScreenCapture.CaptureScreenshot(Path.Combine(_outDir, "4_MeshLOD_probe_" + cfg.Name + ".png"));
        }
        else if (_frame >= SettleFrames + SampleFrames + 4)
        {
            _frame = 0;
            _idx++;
            if (_idx >= _configs.Count) Finish();
        }
    }

    static void Finish()
    {
        EditorApplication.update -= Tick;
        _tris.Dispose();
        foreach (var r in _tripo)
        {
            r.forceMeshLod = -1;
            r.shadowCastingMode = ShadowCastingMode.On;
        }
        _minimap.enabled = true;
        Sword(false);
        _out.Append(']');
        File.WriteAllText(Path.Combine(_outDir, "4_MeshLOD_probe.json"), _out.ToString());
        File.WriteAllText(Path.Combine(_outDir, "4_MeshLOD_probe.done"), "ok");
    }
}
