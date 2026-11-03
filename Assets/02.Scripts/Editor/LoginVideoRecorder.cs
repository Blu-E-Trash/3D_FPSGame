#if LOGIN_RECORDER
using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEngine;

// [에디터 전용 도구] 플레이 중인 Game 화면을 소리와 함께 MP4로 녹화한다.
// 메뉴: Tools/Login/Start Recording (플레이 중, 씬을 처음부터 다시 불러오며 녹화), Tools/Login/Stop Recording
// 저장 위치: 프로젝트 폴더/Docs/LoginVideo/
public static class LoginVideoRecorder
{
    private static RecorderController _controller;

    [MenuItem("Tools/Login/Start Recording")]
    private static void StartRecording()
    {
        if (_controller != null && _controller.IsRecording()) return;

        var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movie.name = "LoginVideo";
        movie.Enabled = true;
        movie.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
        movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = 1920, OutputHeight = 1080 };
        movie.AudioInputSettings.PreserveAudio = true;

        string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/LoginVideo"));
        Directory.CreateDirectory(dir);
        movie.OutputFile = Path.Combine(dir, $"LoginScene_{System.DateTime.Now:HHmmss}");

        settings.AddRecorderSettings(movie);
        settings.SetRecordModeToManual();
        settings.FrameRate = 30f;

        _controller = new RecorderController(settings);
        _controller.PrepareRecording();
        _controller.StartRecording();
        Debug.Log($"[LoginVideoRecorder] 녹화 시작: {movie.OutputFile}.mp4");

        // 인트로부터 찍히도록 지금 씬을 처음부터 다시 불러온다.
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex);
    }

    [MenuItem("Tools/Login/Start Recording", true)]
    private static bool CanStart() => EditorApplication.isPlaying;

    [MenuItem("Tools/Login/Stop Recording")]
    private static void StopRecording()
    {
        if (_controller == null) return;
        _controller.StopRecording();
        _controller = null;
        Debug.Log("[LoginVideoRecorder] 녹화 끝");
    }
}
#endif
