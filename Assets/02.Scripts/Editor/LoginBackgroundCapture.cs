using System.IO;
using UnityEditor;
using UnityEngine;

// [에디터 전용 도구] 로그인 화면 배경용 스크린샷을 찍는다. (게임 빌드에는 포함되지 않는다)
// Scene 뷰에서 원하는 구도를 잡은 뒤 메뉴 Tools/Login/Capture Background 를 누르면
// 그 시점 그대로 Main Camera 설정(URP, 포스트 프로세싱 포함)으로 1920x1080 렌더링해서
// 프로젝트 폴더의 Docs/LoginBackground/Capture_raw.png 로 저장한다.
public static class LoginBackgroundCapture
{
    private const int Width = 1920;
    private const int Height = 1080;
    private const string Folder = "Docs/LoginBackground";

    [MenuItem("Tools/Login/Capture Background")]
    private static void Capture()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        Camera mainCamera = Camera.main;
        if (sceneView == null || mainCamera == null)
        {
            Debug.LogWarning("[LoginBackgroundCapture] Scene 뷰 또는 Main Camera를 찾지 못했다.");
            return;
        }

        // 1. Main Camera와 같은 설정을 가진 임시 카메라를 Scene 뷰 위치에 만든다.
        GameObject temp = new GameObject("__LoginCaptureCamera");
        temp.hideFlags = HideFlags.HideAndDontSave;
        Camera cam = temp.AddComponent<Camera>();
        EditorUtility.CopySerialized(mainCamera, cam);

        foreach (Component component in mainCamera.GetComponents<Component>())
        {
            // URP 카메라 설정(포스트 프로세싱 켜기 등)도 함께 복사한다.
            // (스크립트가 빠진 컴포넌트는 null이라 건너뛴다)
            if (component != null && component.GetType().Name == "UniversalAdditionalCameraData")
            {
                Component copy = temp.AddComponent(component.GetType());
                EditorUtility.CopySerialized(component, copy);
            }
        }

        Transform view = sceneView.camera.transform;
        temp.transform.SetPositionAndRotation(view.position, view.rotation);
        cam.fieldOfView = sceneView.camera.fieldOfView;

        // 2. 렌더 텍스처에 그린 뒤 PNG로 저장한다.
        RenderTexture rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        rt.antiAliasing = 4;
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        tex.Apply();
        RenderTexture.active = previous;

        Directory.CreateDirectory(Folder);
        string path = Path.Combine(Folder, "Capture_raw.png");
        File.WriteAllBytes(path, tex.EncodeToPNG());

        cam.targetTexture = null;
        Object.DestroyImmediate(tex);
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(temp);

        Debug.Log($"[LoginBackgroundCapture] 저장: {Path.GetFullPath(path)}");
    }

    // 플레이 중인 Main Camera 화면을 그대로(포스트 프로세싱 포함, 화면 UI 제외) 찍는다.
    // 시점은 Tools/Perf/View 메뉴로 시작 지점 방향을 고정한 뒤 찍으면 된다.
    [MenuItem("Tools/Login/Capture From Main Camera")]
    private static void CaptureFromMainCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        RenderTexture rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        RenderTexture prevTarget = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();
        cam.targetTexture = prevTarget;

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        tex.Apply();
        RenderTexture.active = previous;

        Directory.CreateDirectory(Folder);
        string path = Path.Combine(Folder, $"Main_{System.DateTime.Now:HHmmss}.png");
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        rt.Release();
        Object.DestroyImmediate(rt);
        Debug.Log($"[LoginBackgroundCapture] 저장: {Path.GetFullPath(path)}");
    }

    // ---- Scene 뷰 시점 조절 (마우스 없이 구도를 잡기 위한 단축키) ----
    // Ctrl+Alt+Home : Scene 뷰를 Main Camera(플레이어 눈높이) 시점으로 맞춘다.
    // Ctrl+Alt+방향키 : 좌우로 돌기 / 앞뒤로 이동
    // Ctrl+Alt+PageUp/PageDown : 위아래로 이동,  Ctrl+Shift+Alt+Up/Down : 고개 들기/숙이기
    [MenuItem("Tools/Login/View/Align To Main Camera %&HOME")]
    private static void AlignToMainCamera()
    {
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv == null || Camera.main == null) return;
        sv.AlignViewToObject(Camera.main.transform);
    }

    [MenuItem("Tools/Login/View/Turn Left %&LEFT")] private static void TurnLeft() => Rotate(-15f, 0f);
    [MenuItem("Tools/Login/View/Turn Right %&RIGHT")] private static void TurnRight() => Rotate(15f, 0f);
    [MenuItem("Tools/Login/View/Pitch Up %#&UP")] private static void PitchUp() => Rotate(0f, -5f);
    [MenuItem("Tools/Login/View/Pitch Down %#&DOWN")] private static void PitchDown() => Rotate(0f, 5f);
    [MenuItem("Tools/Login/View/Move Forward %&UP")] private static void MoveForward() => Move(new Vector3(0f, 0f, 3f));
    [MenuItem("Tools/Login/View/Move Back %&DOWN")] private static void MoveBack() => Move(new Vector3(0f, 0f, -3f));
    [MenuItem("Tools/Login/View/Move Up %&PGUP")] private static void MoveUp() => Move(new Vector3(0f, 0.5f, 0f));
    [MenuItem("Tools/Login/View/Move Down %&PGDN")] private static void MoveDown() => Move(new Vector3(0f, -0.5f, 0f));

    private static void Rotate(float yaw, float pitch)
    {
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv == null) return;
        Vector3 e = sv.rotation.eulerAngles;
        sv.rotation = Quaternion.Euler(e.x + pitch, e.y + yaw, 0f);
        sv.Repaint();
    }

    // 앞뒤 이동은 바라보는 방향의 수평 성분으로, 위아래는 월드 기준으로 움직인다.
    private static void Move(Vector3 local)
    {
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv == null) return;
        Vector3 forward = sv.rotation * Vector3.forward;
        forward.y = 0f;
        forward.Normalize();
        sv.pivot += forward * local.z + Vector3.up * local.y;
        sv.Repaint();
    }
}
