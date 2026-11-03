using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

// [에디터 전용 도구] 로그인 화면 영상을 찍기 위해 Unity Recorder 패키지를 설치한다.
// 메뉴: Tools/Login/Install Recorder
// 설치가 끝나면 LOGIN_RECORDER 심볼을 켜서 LoginVideoRecorder 메뉴가 나타나게 한다.
public static class LoginRecorderSetup
{
    private const string Symbol = "LOGIN_RECORDER";
    private static AddRequest _request;

    [MenuItem("Tools/Login/Install Recorder")]
    private static void Install()
    {
        _request = Client.Add("com.unity.recorder");
        EditorApplication.update += Progress;
        Debug.Log("[LoginRecorderSetup] Unity Recorder 설치 중...");
    }

    private static void Progress()
    {
        if (_request == null || !_request.IsCompleted) return;
        EditorApplication.update -= Progress;

        if (_request.Status == StatusCode.Success)
        {
            Debug.Log($"[LoginRecorderSetup] 설치 완료: {_request.Result.packageId}");
            var target = NamedBuildTarget.Standalone;
            string defines = PlayerSettings.GetScriptingDefineSymbols(target);
            if (!defines.Contains(Symbol))
            {
                PlayerSettings.SetScriptingDefineSymbols(target, string.IsNullOrEmpty(defines) ? Symbol : defines + ";" + Symbol);
            }
        }
        else
        {
            Debug.LogError($"[LoginRecorderSetup] 설치 실패: {_request.Error?.message}");
        }
        _request = null;
    }
}
