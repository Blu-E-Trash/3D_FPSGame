using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

// 28. 빌드해서 완성하기
// 28.2 Player Settings(이름, 버전, 아이콘, 창 모양, 시작 로고)를 정하고,
// 28.3 개발용 빌드(Development Build)로 Windows 실행 파일을 만든다.
// 메뉴: Tools > Build > ...
public static class BuildTool
{
    // 28.2 이름과 버전
    private const string CompanyName = "MyStudio";
    private const string ProductName = "3D FPS";
    private const string Version = "1.0.0";

    // 28.2 창 모드와 크기
    private const int DefaultScreenWidth = 1280;
    private const int DefaultScreenHeight = 720;

    // 28.2 아이콘 (교안의 GameIcon.png, 512×512)
    private const string IconPath = "Assets/04.Images/GameIcon.png";
    private const string IconSha256 = "8ae4cd49340d59c11561ee6580059174836b3ee24574e56467911bfaaffd2a3c";
    private const string IconDownloadUrl = "https://file.notion.com/f/f/9a359432-3a23-4b23-aa8c-404b36e47a04/4fdfb371-81bc-4b9d-bd2d-1291b29a1bc6/GameIcon.png?table=block&id=620d025d-a087-837a-8d73-81717c78be64&spaceId=9a359432-3a23-4b23-aa8c-404b36e47a04&expirationTimestamp=1793800800000&signature=eeopG00JfqZXx4I9d_tRzeqkaQXoP7Cj1UxMzrlUYS8";

    // 28.3 빌드 폴더: 프로젝트 폴더 안의 Builds/Windows (Assets 등 Unity 폴더 안에는 빌드하지 않는다)
    private const string BuildFolder = "Builds/Windows";

    [MenuItem("Tools/Build/1. Apply Player Settings (28.2)")]
    public static void ApplyPlayerSettings()
    {
        // 이름과 버전. Product Name은 창 제목과 실행 파일 이름(3D FPS.exe)이 된다.
        PlayerSettings.companyName = CompanyName;
        PlayerSettings.productName = ProductName;
        PlayerSettings.bundleVersion = Version;

        // 기본 아이콘: 실행 파일, 게임 창, 작업 표시줄 아이콘
        Texture2D icon = LoadOrDownloadIcon();
        if (icon != null)
        {
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }

        // Resolution and Presentation: 1280×720 창 모드, 창 크기 바꾸기 허용
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = DefaultScreenWidth;
        PlayerSettings.defaultScreenHeight = DefaultScreenHeight;
        PlayerSettings.resizableWindow = true;

        // Splash Image: 시작 로고 끄기
        PlayerSettings.SplashScreen.show = false;

        AssetDatabase.SaveAssets();
        Debug.Log("[BuildTool] Player Settings를 적용했다. (3D FPS 1.0.0, 1280×720 창 모드, 시작 로고 끔)");
    }

    [MenuItem("Tools/Build/2. Windows Development Build (28.3)")]
    public static void BuildWindowsDevelopment()
    {
        ApplyPlayerSettings();

        // Build Profiles > Windows Settings > Development Build 켜기
        EditorUserBuildSettings.development = true;

        // 씬 목록에서 체크된 씬만 빌드한다. (LoginScene 0, LoadingScene 1, 게임 씬 2)
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        string exePath = Path.Combine(BuildFolder, ProductName + ".exe");
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"[BuildTool] 빌드 실패: {report.summary.result} (오류 {report.summary.totalErrors}개)");
            return;
        }

        Debug.Log($"[BuildTool] 빌드 완료: {Path.GetFullPath(exePath)} ({report.summary.totalTime.TotalSeconds:0}초)");
        EditorUtility.RevealInFinder(exePath);
        Process.Start(Path.GetFullPath(exePath));  // 빌드한 게임을 바로 실행한다.
    }

    // 교안의 GameIcon.png가 04.Images에 없으면 내려받아 넣는다.
    private static Texture2D LoadOrDownloadIcon()
    {
        if (!File.Exists(IconPath))
        {
            using UnityWebRequest request = UnityWebRequest.Get(IconDownloadUrl);
            request.SendWebRequest();
            while (!request.isDone)
            {
                EditorUtility.DisplayProgressBar("BuildTool", "GameIcon.png 내려받는 중...", request.downloadProgress);
            }
            EditorUtility.ClearProgressBar();

            byte[] data = request.downloadHandler.data;
            if (request.result != UnityWebRequest.Result.Success || ToSha256(data) != IconSha256)
            {
                Debug.LogWarning($"[BuildTool] GameIcon.png를 내려받지 못했다({request.error}). 교안에서 받아 {IconPath}에 넣어 주세요.");
                return null;
            }

            File.WriteAllBytes(IconPath, data);
            AssetDatabase.ImportAsset(IconPath);
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
    }

    private static string ToSha256(byte[] data)
    {
        if (data == null)
        {
            return string.Empty;
        }

        using SHA256 sha = SHA256.Create();
        return string.Concat(sha.ComputeHash(data).Select(b => b.ToString("x2")));
    }
}
