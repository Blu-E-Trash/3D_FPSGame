using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;

// Play 중에 120프레임 동안 렌더링 수치를 재서 Docs/Perf/<이름>.md 로 저장하고,
// 같은 순간의 게임 화면을 <이름>.png 로 저장한다.
// 메뉴: Tools/Perf/Capture Before(F9), Tools/Perf/Capture After(F12)
// 최적화 단계: Capture Opt Before(Shift+F9), Capture Opt After(Shift+F12)
// 시점: F5~F8 = 시작 지점에서 Yaw 0/90/180/270
// 같은 시점에서 비교하도록 Tools/Perf/View/* 로 플레이어 위치와 방향을 고정할 수 있다.
public static class PerfCapture
{
    private static string _viewName = "free";

    [MenuItem("Tools/Perf/View/Start Yaw 0 _F5")] private static void View0() => LockView(0f);
    [MenuItem("Tools/Perf/View/Start Yaw 90 _F6")] private static void View90() => LockView(90f);
    [MenuItem("Tools/Perf/View/Start Yaw 180 _F7")] private static void View180() => LockView(180f);
    [MenuItem("Tools/Perf/View/Start Yaw 270 _F8")] private static void View270() => LockView(270f);

    [MenuItem("Tools/Perf/View/Start Yaw 0 _F5", true)]
    [MenuItem("Tools/Perf/View/Start Yaw 90 _F6", true)]
    [MenuItem("Tools/Perf/View/Start Yaw 180 _F7", true)]
    [MenuItem("Tools/Perf/View/Start Yaw 270 _F8", true)]
    private static bool CanLockView() => EditorApplication.isPlaying;

    // 마우스 회전 스크립트를 끄고, 플레이어를 시작 지점(10, 1, 0)에 두고 정해진 방향을 보게 한다.
    private static void LockView(float yaw)
    {
        var playerRotate = Object.FindAnyObjectByType<PlayerRotate>();
        var cameraRotate = Object.FindAnyObjectByType<CameraRotate>();
        if (playerRotate == null || cameraRotate == null)
        {
            Debug.LogWarning("[PerfCapture] PlayerRotate / CameraRotate 를 찾지 못했다.");
            return;
        }

        playerRotate.enabled = false;
        cameraRotate.enabled = false;

        Transform player = playerRotate.transform;
        var controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.SetPositionAndRotation(new Vector3(10f, 1f, 0f), Quaternion.Euler(0f, yaw, 0f));
        if (controller != null) controller.enabled = true;

        cameraRotate.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        _viewName = $"yaw{yaw:0}";
        Debug.Log($"[PerfCapture] 시점 고정: {_viewName}");
    }

    private const int SampleFrames = 120;
    private const string Folder = "Docs/Perf";

    private static readonly string[] RenderCounters =
    {
        "Batches Count",
        "SetPass Calls Count",
        "Draw Calls Count",
        "Triangles Count",
        "Vertices Count",
        "Shadow Casters Count",
    };

    // 메모리 카운터: GC Allocated In Frame은 프레임 평균, 나머지는 마지막 값(MB)
    private static readonly string[] MemoryCounters =
    {
        "GC Allocated In Frame",
        "GC Used Memory",
        "System Used Memory",
        "Texture Memory",
        "Mesh Memory",
    };

    private static List<ProfilerRecorder> _memoryRecorders;
    private static double _gcAllocSum;
    private static List<ProfilerRecorder> _recorders;
    private static ProfilerRecorder _mainThread;
    private static double[] _sums;
    private static double _mainSum;
    private static double _gpuSum;
    private static int _gpuFrames;
    private static int _frames;
    private static string _label;

    [MenuItem("Tools/Perf/Capture Before _F9")]
    private static void CaptureBefore() => Begin($"before_{_viewName}");

    [MenuItem("Tools/Perf/Capture After _F12")]
    private static void CaptureAfter() => Begin($"after_{_viewName}");

    // 최적화 단계 전후 (Shift+F9 / Shift+F12)
    [MenuItem("Tools/Perf/Capture Opt Before #F9")]
    private static void CaptureOptBefore() => Begin($"opt_before_{_viewName}");

    [MenuItem("Tools/Perf/Capture Opt After #F12")]
    private static void CaptureOptAfter() => Begin($"opt_after_{_viewName}");

    [MenuItem("Tools/Perf/Capture Before _F9", true)]
    [MenuItem("Tools/Perf/Capture After _F12", true)]
    [MenuItem("Tools/Perf/Capture Opt Before #F9", true)]
    [MenuItem("Tools/Perf/Capture Opt After #F12", true)]
    private static bool CanCapture() => EditorApplication.isPlaying && _recorders == null;

    private static void Begin(string label)
    {
        _label = label;
        _recorders = new List<ProfilerRecorder>();
        foreach (string name in RenderCounters)
        {
            _recorders.Add(ProfilerRecorder.StartNew(ProfilerCategory.Render, name));
        }
        _mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
        _memoryRecorders = new List<ProfilerRecorder>();
        foreach (string name in MemoryCounters)
        {
            _memoryRecorders.Add(ProfilerRecorder.StartNew(ProfilerCategory.Memory, name));
        }
        _gcAllocSum = 0;
        _sums = new double[RenderCounters.Length];
        _mainSum = 0;
        _gpuSum = 0;
        _gpuFrames = 0;
        _frames = -2; // 시작 직후 두 프레임은 버린다.

        Directory.CreateDirectory(Folder);
        ScreenCapture.CaptureScreenshot($"{Folder}/{label}.png");

        EditorApplication.update += Sample;
        Debug.Log($"[PerfCapture] '{label}' 측정 시작 ({SampleFrames}프레임)");
    }

    private static void Sample()
    {
        if (!EditorApplication.isPlaying)
        {
            Stop();
            return;
        }

        _frames++;
        if (_frames <= 0)
        {
            return;
        }

        for (int i = 0; i < _recorders.Count; i++)
        {
            _sums[i] += _recorders[i].LastValue;
        }
        _mainSum += _mainThread.LastValue * 1e-6; // ns → ms
        _gcAllocSum += _memoryRecorders[0].LastValue;

        FrameTimingManager.CaptureFrameTimings();
        FrameTiming[] timings = new FrameTiming[1];
        if (FrameTimingManager.GetLatestTimings(1, timings) > 0 && timings[0].gpuFrameTime > 0)
        {
            _gpuSum += timings[0].gpuFrameTime;
            _gpuFrames++;
        }

        if (_frames >= SampleFrames)
        {
            Write();
            Stop();
        }
    }

    private static void Write()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# PerfCapture: {_label}");
        sb.AppendLine();
        sb.AppendLine($"- 씬: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        sb.AppendLine($"- 시각: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"- 해상도: {Screen.width}x{Screen.height}");
        sb.AppendLine($"- 표본: {SampleFrames}프레임 평균");
        sb.AppendLine();
        sb.AppendLine("| 항목 | 값 |");
        sb.AppendLine("|---|---|");
        double mainMs = _mainSum / SampleFrames;
        sb.AppendLine($"| CPU Main Thread (ms) | {mainMs:F2} |");
        sb.AppendLine($"| FPS (Main Thread 기준) | {(mainMs > 0 ? 1000.0 / mainMs : 0):F1} |");
        sb.AppendLine(_gpuFrames > 0
            ? $"| GPU Frame (ms) | {_gpuSum / _gpuFrames:F2} |"
            : "| GPU Frame (ms) | 측정 안 됨 |");
        for (int i = 0; i < RenderCounters.Length; i++)
        {
            sb.AppendLine($"| {RenderCounters[i]} | {_sums[i] / SampleFrames:F0} |");
        }

        sb.AppendLine($"| GC Allocated In Frame (KB, 평균) | {_gcAllocSum / SampleFrames / 1024.0:F1} |");
        for (int i = 1; i < MemoryCounters.Length; i++)
        {
            sb.AppendLine($"| {MemoryCounters[i]} (MB) | {_memoryRecorders[i].LastValue / (1024.0 * 1024.0):F1} |");
        }

        string path = $"{Folder}/{_label}.md";
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        Debug.Log($"[PerfCapture] 저장: {path}\n{sb}");
    }

    private static void Stop()
    {
        EditorApplication.update -= Sample;
        if (_recorders != null)
        {
            foreach (ProfilerRecorder r in _recorders)
            {
                r.Dispose();
            }
        }
        _mainThread.Dispose();
        if (_memoryRecorders != null)
        {
            foreach (ProfilerRecorder r in _memoryRecorders)
            {
                r.Dispose();
            }
            _memoryRecorders = null;
        }
        _recorders = null;
    }
}
