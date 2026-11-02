using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;

// 성능/그래픽 Before·After 비교용 벤치마크
// 고정된 시점마다 카메라를 세우고 일정 시간 측정한 뒤, 스크린샷(HUD 포함/미포함)과 수치를 Docs/Report/<라벨>/ 에 저장한다.
// 같은 시드·같은 시점·같은 시간으로 돌리므로 설정 변경 전후를 같은 조건에서 비교할 수 있다.
[DefaultExecutionOrder(-1000)]
public class PerformanceBenchmark : MonoBehaviour
{
    [Serializable]
    public struct Viewpoint
    {
        public string name;
        public Vector3 position;
        public Vector3 eulerAngles;
    }

    [Header("측정 설정")]
    [SerializeField] private string _label = "00_before"; // 결과 폴더 이름
    [SerializeField] private bool _autoRun = true; // Playing 상태가 되면 자동 시작
    [SerializeField] private KeyCode _runKey = KeyCode.F9;
    [SerializeField] private int _randomSeed = 12345; // 좀비 스폰 등 랜덤을 매번 같게
    [SerializeField] private float _warmupSeconds = 1.5f;
    [SerializeField] private float _measureSeconds = 4f;
    [SerializeField] private bool _quitWhenDone = true; // 끝나면 플레이 모드 종료

    [Header("시점")]
    [SerializeField] private Viewpoint[] _viewpoints;

    [Header("HUD")]
    [SerializeField] private bool _showHud = true;

    // 화면 HUD에 표시할 측정값
    private readonly StringBuilder _hudText = new StringBuilder();
    private readonly StringBuilder _report = new StringBuilder();
    private readonly List<float> _frameTimes = new List<float>(1024);
    private readonly FrameTiming[] _frameTimings = new FrameTiming[1];

    private ProfilerRecorder _batchesRecorder;
    private ProfilerRecorder _setPassRecorder;
    private ProfilerRecorder _drawCallsRecorder;
    private ProfilerRecorder _trianglesRecorder;
    private ProfilerRecorder _verticesRecorder;
    private ProfilerRecorder _shadowCastersRecorder;
    private ProfilerRecorder _mainThreadRecorder;
    private ProfilerRecorder _gcAllocRecorder;
    private ProfilerRecorder _totalUsedMemoryRecorder;
    private ProfilerRecorder _textureMemoryRecorder;
    private ProfilerRecorder _meshMemoryRecorder;

    private bool _isRunning = false;
    private bool _hideHudThisFrame = false;
    private GUIStyle _hudStyle;

    public bool IsRunning => _isRunning;

    private void Awake()
    {
        UnityEngine.Random.InitState(_randomSeed);
        FrameTimingManager.CaptureFrameTimings(); // 첫 호출로 프레임 타이밍 수집 시작
    }

    private void OnEnable()
    {
        _batchesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
        _setPassRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        _drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
        _trianglesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        _verticesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
        _shadowCastersRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Shadow Casters Count");
        _mainThreadRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
        _gcAllocRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        _totalUsedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
        _textureMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Texture Memory");
        _meshMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Mesh Memory");
    }

    private void OnDisable()
    {
        _batchesRecorder.Dispose();
        _setPassRecorder.Dispose();
        _drawCallsRecorder.Dispose();
        _trianglesRecorder.Dispose();
        _verticesRecorder.Dispose();
        _shadowCastersRecorder.Dispose();
        _mainThreadRecorder.Dispose();
        _gcAllocRecorder.Dispose();
        _totalUsedMemoryRecorder.Dispose();
        _textureMemoryRecorder.Dispose();
        _meshMemoryRecorder.Dispose();
    }

    private void Update()
    {
        FrameTimingManager.CaptureFrameTimings();

        if (_isRunning) return;
        if (GameManager.Instance.State != GameState.Playing) return;

        if (_autoRun || Input.GetKeyDown(_runKey))
        {
            _autoRun = false;
            StartCoroutine(Run_Coroutine());
        }
    }

    private IEnumerator Run_Coroutine()
    {
        _isRunning = true;

        Camera mainCamera = Camera.main;
        CameraViewSwitcher switcher = mainCamera.GetComponent<CameraViewSwitcher>();
        bool switcherWasEnabled = switcher != null && switcher.enabled;
        if (switcher != null)
        {
            switcher.enabled = false; // 측정 중에는 카메라를 고정 시점에 둔다
        }

        string folder = Path.Combine(Application.dataPath, "..", "Docs", "Report", _label);
        Directory.CreateDirectory(folder);

        _report.Clear();
        _report.AppendLine($"# 벤치마크 결과: {_label}");
        _report.AppendLine();
        _report.AppendLine($"- 측정 시각: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        _report.AppendLine($"- Unity {Application.unityVersion} / {SystemInfo.graphicsDeviceName} / {SystemInfo.processorType}");
        _report.AppendLine($"- 해상도 {Screen.width}x{Screen.height} / Quality: {QualitySettings.names[QualitySettings.GetQualityLevel()]} / 에디터: {Application.isEditor}");
        _report.AppendLine($"- 시점당 워밍업 {_warmupSeconds}s + 측정 {_measureSeconds}s, 랜덤 시드 {_randomSeed}");
        _report.AppendLine();
        _report.AppendLine("| 시점 | 평균 FPS | 평균 프레임(ms) | 1% Low FPS | CPU Main(ms) | CPU Render(ms) | GPU(ms) | Batches | SetPass | Draw Calls | Tris | Verts | Shadow Casters | GC Alloc/프레임(KB) | Total Used Mem(MB) | Texture Mem(MB) | Mesh Mem(MB) |");
        _report.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");

        foreach (Viewpoint viewpoint in _viewpoints)
        {
            mainCamera.transform.SetPositionAndRotation(viewpoint.position, Quaternion.Euler(viewpoint.eulerAngles));

            yield return new WaitForSecondsRealtime(_warmupSeconds);

            // 측정
            _frameTimes.Clear();
            double cpuMainSum = 0, cpuRenderSum = 0, gpuSum = 0;
            int timingCount = 0;
            double batchesSum = 0, setPassSum = 0, drawCallsSum = 0, trisSum = 0, vertsSum = 0, shadowSum = 0, gcSum = 0;
            float elapsed = 0f;

            while (elapsed < _measureSeconds)
            {
                yield return null;
                float dt = Time.unscaledDeltaTime;
                elapsed += dt;
                _frameTimes.Add(dt);

                batchesSum += _batchesRecorder.LastValue;
                setPassSum += _setPassRecorder.LastValue;
                drawCallsSum += _drawCallsRecorder.LastValue;
                trisSum += _trianglesRecorder.LastValue;
                vertsSum += _verticesRecorder.LastValue;
                shadowSum += _shadowCastersRecorder.LastValue;
                gcSum += _gcAllocRecorder.LastValue;

                if (FrameTimingManager.GetLatestTimings(1, _frameTimings) > 0)
                {
                    cpuMainSum += _frameTimings[0].cpuMainThreadFrameTime;
                    cpuRenderSum += _frameTimings[0].cpuRenderThreadFrameTime;
                    gpuSum += _frameTimings[0].gpuFrameTime;
                    timingCount++;
                }

                UpdateHud(viewpoint.name, elapsed);
            }

            int frames = _frameTimes.Count;
            float avgFrameMs = elapsed / frames * 1000f;
            float avgFps = frames / elapsed;
            _frameTimes.Sort();
            float worst1Percent = _frameTimes[Mathf.Min(frames - 1, Mathf.FloorToInt(frames * 0.99f))];
            float lowFps = 1f / worst1Percent;

            // FrameTimingManager가 값을 주지 않는 환경이면 Main Thread 레코더로 대체
            float cpuMain = timingCount > 0 && cpuMainSum > 0 ? (float)(cpuMainSum / timingCount) : (float)(GetAverage(_mainThreadRecorder) * 1e-6);
            float cpuRender = timingCount > 0 ? (float)(cpuRenderSum / timingCount) : 0f;
            float gpu = timingCount > 0 ? (float)(gpuSum / timingCount) : 0f;

            string row =
                $"| {viewpoint.name} | {avgFps:F1} | {avgFrameMs:F2} | {lowFps:F1} | {cpuMain:F2} | {cpuRender:F2} | {(gpu > 0 ? gpu.ToString("F2") : "N/A")} | " +
                $"{batchesSum / frames:F0} | {setPassSum / frames:F0} | {drawCallsSum / frames:F0} | {trisSum / frames:F0} | {vertsSum / frames:F0} | {shadowSum / frames:F0} | " +
                $"{gcSum / frames / 1024.0:F2} | {ToMB(_totalUsedMemoryRecorder.LastValue):F1} | {ToMB(_textureMemoryRecorder.LastValue):F1} | {ToMB(_meshMemoryRecorder.LastValue):F1} |";
            _report.AppendLine(row);

            // HUD에 이 시점의 평균값을 고정 표시하고 스크린샷 2장 (HUD 포함 / 미포함)
            _hudText.Clear();
            _hudText.AppendLine($"[{_label}] {viewpoint.name}  (평균 {_measureSeconds}s)");
            _hudText.AppendLine($"FPS {avgFps:F1}  ({avgFrameMs:F2} ms)   1% Low {lowFps:F1}");
            _hudText.AppendLine($"CPU Main {cpuMain:F2} ms   Render {cpuRender:F2} ms   GPU {(gpu > 0 ? gpu.ToString("F2") + " ms" : "N/A")}");
            _hudText.AppendLine($"Batches {batchesSum / frames:F0}   SetPass {setPassSum / frames:F0}   DrawCalls {drawCallsSum / frames:F0}");
            _hudText.AppendLine($"Tris {trisSum / frames:N0}   Verts {vertsSum / frames:N0}   ShadowCasters {shadowSum / frames:F0}");
            _hudText.AppendLine($"Mem {ToMB(_totalUsedMemoryRecorder.LastValue):F0} MB   Tex {ToMB(_textureMemoryRecorder.LastValue):F0} MB   Mesh {ToMB(_meshMemoryRecorder.LastValue):F0} MB   GC {gcSum / frames / 1024.0:F2} KB/f");

            yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, $"{viewpoint.name}_hud.png"));
            yield return null;
            yield return null;

            _hideHudThisFrame = true;
            yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, $"{viewpoint.name}_clean.png"));
            yield return null;
            yield return null;
            _hideHudThisFrame = false;
        }

        File.WriteAllText(Path.Combine(folder, "results.md"), _report.ToString(), Encoding.UTF8);
        GameLogger.LogWarning($"벤치마크 완료: {folder}");

        if (switcher != null)
        {
            switcher.enabled = switcherWasEnabled;
        }

        _isRunning = false;

#if UNITY_EDITOR
        if (_quitWhenDone)
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
#endif
    }

    private void UpdateHud(string viewpointName, float elapsed)
    {
        _hudText.Clear();
        _hudText.AppendLine($"[{_label}] {viewpointName}  측정 중 {elapsed:F1}/{_measureSeconds}s");
        _hudText.AppendLine($"FPS {1f / Time.unscaledDeltaTime:F0}");
        _hudText.AppendLine($"Batches {_batchesRecorder.LastValue}   SetPass {_setPassRecorder.LastValue}   Tris {_trianglesRecorder.LastValue:N0}");
    }

    private void OnGUI()
    {
        if (!_showHud || _hideHudThisFrame || _hudText.Length == 0) return;

        if (_hudStyle == null)
        {
            _hudStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = Mathf.Max(14, Screen.height / 45),
                alignment = TextAnchor.UpperLeft,
                richText = false
            };
            _hudStyle.normal.textColor = Color.white;
            _hudStyle.padding = new RectOffset(12, 12, 10, 10);
        }

        GUIContent content = new GUIContent(_hudText.ToString().TrimEnd());
        Vector2 size = _hudStyle.CalcSize(content);
        GUI.Box(new Rect(10, 10, size.x, size.y), content, _hudStyle);
    }

    private static double GetAverage(ProfilerRecorder recorder)
    {
        int count = recorder.Count;
        if (count == 0) return 0;

        double sum = 0;
        for (int i = 0; i < count; i++)
        {
            sum += recorder.GetSample(i).Value;
        }

        return sum / count;
    }

    private static float ToMB(long bytes)
    {
        return bytes / (1024f * 1024f);
    }
}
