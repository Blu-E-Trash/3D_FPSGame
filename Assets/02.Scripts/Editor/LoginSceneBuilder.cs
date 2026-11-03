using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// [에디터 전용 도구] 감시 카메라(B안) 로그인 씬을 만든다.
// 메뉴: Tools/Login/Build Login Scene
// 결과는 평범한 씬(Assets/01.Scenes/LoginScene.unity)이라 만든 뒤에는 Inspector에서 자유롭게 고치면 된다.
// 다시 실행하면 씬을 처음부터 새로 만든다. (손으로 고친 내용은 사라진다)
public static class LoginSceneBuilder
{
    private const string ScenePath = "Assets/01.Scenes/LoginScene.unity";
    private const string GenDir = "Assets/01.Scenes/LoginScene";
    private const string ImgDir = "Assets/04.Images/Login";
    private const string SfxDir = "Assets/06.Sounds/Login";
    private const string FontDir = "Assets/08.Fonts/D2Coding";

    private static readonly Color G = new Color(0.79f, 1f, 0.82f);          // 밝은 녹색 (글자)
    private static readonly Color G2 = new Color(0.61f, 0.91f, 0.65f);      // 녹색 (테두리)
    private static readonly Color Dark = new Color(0.016f, 0.078f, 0.04f);  // 단말기 바탕
    private static readonly Color Red = new Color(1f, 0.3f, 0.3f);

    private static TMP_FontAsset _font;
    private static TMP_FontAsset _fontBold;

    [MenuItem("Tools/Login/Build Login Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Directory.CreateDirectory(GenDir);
        SetupTextures();
        _font = MakeFont($"{FontDir}/D2Coding-Ver1.3.2-20180524.ttf", $"{FontDir}/D2Coding SDF.asset");
        _fontBold = MakeFont($"{FontDir}/D2CodingBold-Ver1.3.2-20180524.ttf", $"{FontDir}/D2Coding Bold SDF.asset");

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---------- 3D: 카메라, 배경, 조명, 좀비, 포스트 프로세싱 ----------
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.85f, 0.88f, 0.9f);
        RenderSettings.skybox = null;

        RenderTexture rt = LoadOrCreate($"{GenDir}/RT_CCTVFeed.renderTexture", () =>
        {
            var r = new RenderTexture(1920, 1080, 24) { antiAliasing = 2, name = "RT_CCTVFeed" };
            return r;
        });

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 40f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 200f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.targetTexture = rt;
        camGo.AddComponent<AudioListener>();

        // 배경 사진: 카메라 화면을 꽉 채우는 판 (60m 앞)
        const float bgDepth = 60f;
        float bgH = 2f * bgDepth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.name = "Background_Map";
        Object.DestroyImmediate(bg.GetComponent<Collider>());
        bg.transform.position = new Vector3(0f, 0f, bgDepth);
        bg.transform.localScale = new Vector3(bgH * 16f / 9f, bgH, 1f);
        var bgMat = LoadOrCreate($"{GenDir}/M_LoginBackground.mat", () => new Material(Shader.Find("Unlit/Texture")));
        bgMat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ImgDir}/IMG_LoginCCTV_BG.png");
        EditorUtility.SetDirty(bgMat);
        var bgRenderer = bg.GetComponent<MeshRenderer>();
        bgRenderer.sharedMaterial = bgMat;
        bgRenderer.shadowCastingMode = ShadowCastingMode.Off;
        bgRenderer.receiveShadows = false;

        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.4f;
        light.color = new Color(0.85f, 0.9f, 1f);
        light.shadows = LightShadows.None;
        lightGo.transform.rotation = Quaternion.Euler(35f, -40f, 0f);

        // 좀비: 원본 모델 + 제자리 걷기 애니메이션, 이동은 LoginZombieWalker가 맡는다.
        var zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Zombie/Prefabs/Zombie1.prefab");
        var zombie = (GameObject)PrefabUtility.InstantiatePrefab(zombiePrefab);
        zombie.name = "CCTV_Zombie";
        string ctrlPath = $"{GenDir}/LoginZombieWalk.controller";
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (ctrl == null)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Zombie/Animations/Z_Walk_InPlace.anim");
            ctrl = AnimatorController.CreateAnimatorControllerAtPathWithClip(ctrlPath, clip);
        }
        var anim = zombie.GetComponentInChildren<Animator>();
        anim.runtimeAnimatorController = ctrl;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        var walker = zombie.AddComponent<LoginZombieWalker>();
        Set(walker, "_camera", cam);
        foreach (var r in zombie.GetComponentsInChildren<SkinnedMeshRenderer>()) r.updateWhenOffscreen = true;

        // 녹색 야간 감시 화면은 UI/CCTVFeed 셰이더가 만든다. (흑백 → 녹색, 렌즈 왜곡, 색 번짐, 가장자리 어둡게)
        var feedMat = LoadOrCreate($"{GenDir}/M_CCTVFeed.mat", () => new Material(Shader.Find("UI/CCTVFeed")));
        feedMat.SetColor("_Mono", new Color(0.62f, 1f, 0.68f));
        feedMat.SetFloat("_Contrast", 1.25f);
        feedMat.SetFloat("_Brightness", 1.45f);
        feedMat.SetFloat("_Distortion", 0.07f);
        feedMat.SetFloat("_Aberration", 0.0015f);
        feedMat.SetFloat("_Vignette", 0.85f);
        EditorUtility.SetDirty(feedMat);

        // ---------- UI ----------
        var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.layer = 5;
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var canvasRt = (RectTransform)canvasGo.transform;

        // 1) 카메라 화면
        var feed = Raw("CCTV_Feed", canvasRt, rt, Color.white);
        feed.material = feedMat;
        Stretch(feed.rectTransform);

        // 2) 찢어짐 띠
        var slicesRoot = UI("GlitchSlices", canvasRt); Stretch(slicesRoot);
        var slices = new List<RawImage>();
        for (int i = 0; i < 4; i++)
        {
            var s = Raw($"Slice_{i}", slicesRoot, rt, Color.white);
            s.material = feedMat;
            Stretch(s.rectTransform);
            s.enabled = false;
            slices.Add(s);
        }

        // 3) 움직임 감지 상자
        var trackerRoot = UI("MotionTracker", canvasRt); Stretch(trackerRoot);
        var box = UI("Box", trackerRoot);
        box.anchorMin = box.anchorMax = Vector2.zero;
        box.pivot = Vector2.zero;
        box.sizeDelta = new Vector2(120, 260);
        BuildCorners(box, Red, 18f, 3f);
        var labels = UI("Labels", box);
        labels.sizeDelta = new Vector2(260, 90);
        var detected = Text("Detected", labels, "MOTION DETECTED", 20, Red, TextAlignmentOptions.TopLeft, _font);
        Place(detected.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0), new Vector2(260, 26));
        var idText = Text("Id", labels, "ID: UNKNOWN", 20, Red, TextAlignmentOptions.TopLeft, _font);
        Place(idText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -26), new Vector2(260, 26));
        var dist = Text("Distance", labels, "DIST 20.0m", 20, Red, TextAlignmentOptions.TopLeft, _font);
        Place(dist.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -52), new Vector2(260, 26));
        var tracker = trackerRoot.gameObject.AddComponent<UI_MotionTracker>();
        Set(tracker, "_camera", cam); Set(tracker, "_target", zombie.transform); Set(tracker, "_walker", walker);
        Set(tracker, "_canvas", canvasRt); Set(tracker, "_box", box); Set(tracker, "_labels", labels);
        Set(tracker, "_detected", detected); Set(tracker, "_distance", dist);

        // 4) 주사선, 노이즈, 흐르는 띠
        var scan = Raw("Scanlines", canvasRt, AssetDatabase.LoadAssetAtPath<Texture2D>($"{ImgDir}/IMG_Scanline.png"), new Color(1, 1, 1, 0.55f));
        Stretch(scan.rectTransform);
        scan.uvRect = new Rect(0, 0, 1920f / 4f, 1080f / 4f);
        var noise = Raw("Noise", canvasRt, AssetDatabase.LoadAssetAtPath<Texture2D>($"{ImgDir}/IMG_Noise.png"), new Color(0.8f, 1f, 0.85f, 0.045f));
        Stretch(noise.rectTransform);
        noise.uvRect = new Rect(0, 0, 1920f / 256f, 1080f / 256f);
        var roll = Raw("RollBar", canvasRt, AssetDatabase.LoadAssetAtPath<Texture2D>($"{ImgDir}/IMG_RollBar.png"), new Color(0.8f, 1f, 0.85f, 0.07f));
        var rollRt = roll.rectTransform;
        rollRt.anchorMin = new Vector2(0, 0); rollRt.anchorMax = new Vector2(1, 0);
        rollRt.pivot = new Vector2(0.5f, 0.5f);
        rollRt.sizeDelta = new Vector2(0, 170);

        // 5) HUD
        var hud = UI("HUD", canvasRt); Stretch(hud);
        BuildCorners(hud, new Color(0.85f, 1f, 0.88f, 0.75f), 72f, 4f, 34f);
        var rec = Text("REC", hud, "● REC", 28, new Color(1f, 0.25f, 0.25f), TextAlignmentOptions.TopLeft, _fontBold);
        Place(rec.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(58, -50), new Vector2(140, 40));
        var camLabel = Text("CamLabel", hud, "CAM 07   SECTOR B — 출입로", 28, G, TextAlignmentOptions.TopLeft, _font);
        Place(camLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(190, -50), new Vector2(700, 40));
        var ts = Text("Timestamp", hud, "2026-11-03  03:14:22", 28, G, TextAlignmentOptions.TopRight, _font);
        Place(ts.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-58, -50), new Vector2(520, 40));
        var sig = Text("Signal", hud, "SIGNAL ▂▃▅▆ 92%", 24, G, TextAlignmentOptions.TopRight, _font);
        Place(sig.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-58, -88), new Vector2(520, 36));
        var bl = Text("Zone", hud, "QUARANTINE ZONE 7 · 생존자 인증 필요", 24, G, TextAlignmentOptions.BottomLeft, _font);
        Place(bl.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(58, 46), new Vector2(900, 36));
        var br = Text("Spec", hud, "1920×1080 · 25FPS · IR ON", 24, G, TextAlignmentOptions.BottomRight, _font);
        Place(br.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-58, 46), new Vector2(600, 36));

        // 6) 제목
        var titleRoot = UI("Title", canvasRt);
        Place(titleRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-20, 300), new Vector2(1000, 190));
        var red = Text("Ghost_Red", titleRoot, "3D FPS", 168, new Color(1f, 0.16f, 0.31f, 0.55f), TextAlignmentOptions.Center, _fontBold); Stretch(red.rectTransform);
        var cyan = Text("Ghost_Cyan", titleRoot, "3D FPS", 168, new Color(0.16f, 0.78f, 1f, 0.55f), TextAlignmentOptions.Center, _fontBold); Stretch(cyan.rectTransform);
        var main = Text("Main", titleRoot, "3D FPS", 168, G, TextAlignmentOptions.Center, _fontBold); Stretch(main.rectTransform);
        foreach (var t in new[] { red, cyan, main }) t.characterSpacing = 6f;
        var cursor = Img("Cursor", main.rectTransform, G);
        cursor.rectTransform.anchorMin = cursor.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        cursor.rectTransform.pivot = new Vector2(0f, 0.5f);
        cursor.rectTransform.sizeDelta = new Vector2(64, 120);
        var glitchTitle = titleRoot.gameObject.AddComponent<UI_GlitchTitle>();
        Set(glitchTitle, "_main", main); Set(glitchTitle, "_red", red); Set(glitchTitle, "_cyan", cyan); Set(glitchTitle, "_cursor", cursor.rectTransform);

        var sub = Text("Subtitle", canvasRt, "SURVIVOR  AUTH  SYSTEM", 24, G2, TextAlignmentOptions.Center, _font);
        sub.characterSpacing = 36f;
        Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 178), new Vector2(900, 40));
        var subGroup = sub.gameObject.AddComponent<CanvasGroup>();

        // 7) 단말기 (로그인 / 회원가입)
        var panel = UI("Terminal", canvasRt);
        Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0, 118), new Vector2(720, 330));
        var panelGroup = panel.gameObject.AddComponent<CanvasGroup>();
        var frame = panel.gameObject.AddComponent<Image>();
        frame.color = new Color(Dark.r, Dark.g, Dark.b, 0.62f);
        Border(panel, new Color(G2.r, G2.g, G2.b, 0.55f));
        var tagBg = Img("TagBg", panel, new Color(0.02f, 0.07f, 0.04f, 1f));
        Place(tagBg.rectTransform, new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(300, 26));
        var tag = Text("Tag", tagBg.rectTransform, "GATE 07 // AUTH REQUIRED", 18, G2, TextAlignmentOptions.Center, _font);
        Stretch(tag.rectTransform);

        var loginView = UI("LoginView", panel); Stretch(loginView);
        var loginGroup = loginView.gameObject.AddComponent<CanvasGroup>();
        var idField = Field("IdField", loginView, "아이디", false, -40);
        var pwField = Field("PasswordField", loginView, "비밀번호", true, -122);
        var loginRow = Row("Buttons", loginView, -222);
        var loginBtn = BracketButton("LoginButton", loginRow, "로그인", 0);
        var toSignupBtn = BracketButton("SignupButton", loginRow, "회원가입", 1);

        var signupView = UI("SignupView", panel); Stretch(signupView);
        var signupGroup = signupView.gameObject.AddComponent<CanvasGroup>();
        Field("IdField", signupView, "아이디", false, -40);
        Field("NicknameField", signupView, "닉네임", false, -122);
        Field("PasswordField", signupView, "비밀번호", true, -204);
        Field("PasswordConfirmField", signupView, "비밀번호 확인", true, -286);
        var signupRow = Row("Buttons", signupView, -386);
        BracketButton("JoinButton", signupRow, "가입하기", 0);
        var backBtn = BracketButton("BackButton", signupRow, "돌아가기", 1);

        var viewSwitch = panel.gameObject.AddComponent<UI_LoginViewSwitch>();
        Set(viewSwitch, "_frame", panel); Set(viewSwitch, "_login", loginGroup); Set(viewSwitch, "_signup", signupGroup);
        UnityEventTools.AddPersistentListener(toSignupBtn.onClick, new UnityAction(viewSwitch.ShowSignup));
        UnityEventTools.AddPersistentListener(backBtn.onClick, new UnityAction(viewSwitch.ShowLogin));
        signupView.gameObject.SetActive(false);

        // 8) 인트로용 검은 화면
        var black = Img("Blackout", canvasRt, Color.black);
        Stretch(black.rectTransform);
        black.raycastTarget = false;
        var blackGroup = black.gameObject.AddComponent<CanvasGroup>();
        blackGroup.blocksRaycasts = false;
        blackGroup.alpha = 0f; // 편집 중에는 투명, 플레이하면 UI_LoginIntro가 덮는다.

        // ---------- 연출 스크립트 연결 ----------
        var feedFx = canvasGo.AddComponent<UI_CCTVFeed>();
        Set(feedFx, "_feed", feed); SetArray(feedFx, "_slices", slices.ToArray());
        Set(feedFx, "_noise", noise); Set(feedFx, "_rollBar", rollRt);
        Set(feedFx, "_rec", rec); Set(feedFx, "_timestamp", ts); Set(feedFx, "_signal", sig); Set(feedFx, "_title", glitchTitle);

        var intro = canvasGo.AddComponent<UI_LoginIntro>();
        Set(intro, "_blackout", blackGroup); Set(intro, "_title", glitchTitle); Set(intro, "_subtitle", subGroup);
        Set(intro, "_panel", panelGroup);
        SetArray(intro, "_items", new Object[]
        {
            idField.GetComponent<CanvasGroup>(), pwField.GetComponent<CanvasGroup>(), loginRow.GetComponent<CanvasGroup>()
        });

        // ---------- 소리 ----------
        var audioGo = new GameObject("LoginAudio");
        var oneShot = audioGo.AddComponent<AudioSource>();
        oneShot.playOnAwake = false;
        var sfx = audioGo.AddComponent<LoginSfx>();
        Set(sfx, "_oneShot", oneShot);
        Set(sfx, "_blip", Clip("SFX_Blip")); Set(sfx, "_type", Clip("SFX_Type")); Set(sfx, "_click", Clip("SFX_Click"));
        Set(sfx, "_glitch", Clip("SFX_Glitch")); Set(sfx, "_boot", Clip("SFX_Boot")); Set(sfx, "_alert", Clip("SFX_Alert"));
        Loop(audioGo, "SFX_Static_Loop", 0.22f);
        Loop(audioGo, "SFX_Hum_Loop", 0.12f);

        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // ---------- 저장 ----------
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        AddToBuildSettingsFirst(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[LoginSceneBuilder] 로그인 씬을 만들었다: {ScenePath}");
    }

    // ===================== 부품 =====================

    private static RectTransform Field(string name, RectTransform parent, string placeholder, bool password, float y)
    {
        var res = new TMP_DefaultControls.Resources();
        var go = TMP_DefaultControls.CreateInputField(res);
        go.name = name;
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        Place(rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(640, 66));
        go.AddComponent<CanvasGroup>();

        var bgImg = go.GetComponent<Image>();
        bgImg.sprite = null;
        bgImg.color = new Color(0f, 0f, 0f, 0.45f);
        var border = Border(rt, new Color(G2.r, G2.g, G2.b, 0.35f));

        var input = go.GetComponent<TMP_InputField>();
        var area = (RectTransform)go.transform.Find("Text Area");
        area.offsetMin = new Vector2(22, 6); area.offsetMax = new Vector2(-22, -6);
        var text = (TextMeshProUGUI)input.textComponent;
        var ph = (TextMeshProUGUI)input.placeholder;
        foreach (var t in new[] { text, ph })
        {
            t.font = _font;
            t.fontSize = 26;
            t.alignment = TextAlignmentOptions.MidlineLeft;
        }
        text.color = new Color(0.84f, 1f, 0.86f);
        ph.text = "> " + placeholder;
        ph.fontStyle = FontStyles.Normal;
        ph.color = new Color(0.75f, 1f, 0.78f, 0.5f);
        input.customCaretColor = true;
        input.caretColor = G;
        input.caretWidth = 3;
        input.selectionColor = new Color(G2.r, G2.g, G2.b, 0.35f);
        if (password)
        {
            input.contentType = TMP_InputField.ContentType.Password;
            input.asteriskChar = '*';
        }
        var term = go.AddComponent<UI_TerminalInputField>();
        Set(term, "_border", border);
        return rt;
    }

    private static RectTransform Row(string name, RectTransform parent, float y)
    {
        var row = UI(name, parent);
        Place(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(640, 72));
        row.gameObject.AddComponent<CanvasGroup>();
        return row;
    }

    private static Button BracketButton(string name, RectTransform row, string label, int index)
    {
        var root = UI(name, row);
        Place(root, new Vector2(index == 0 ? 0f : 1f, 0.5f), new Vector2(index == 0 ? 0f : 1f, 0.5f), Vector2.zero, new Vector2(310, 72));
        var hit = root.gameObject.AddComponent<Image>();
        hit.color = new Color(0, 0, 0, 0.001f); // 클릭 영역 (보이지 않음)
        var btn = root.gameObject.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = hit;

        var visual = UI("Visual", root); Stretch(visual);
        var fill = Img("Fill", visual, G2);
        Stretch(fill.rectTransform);
        fill.rectTransform.pivot = new Vector2(0f, 0.5f);
        fill.rectTransform.localScale = new Vector3(0f, 1f, 1f);
        fill.raycastTarget = false;

        var left = Bracket("BracketLeft", visual, true);
        var right = Bracket("BracketRight", visual, false);
        var rightGroup = right.gameObject.AddComponent<CanvasGroup>();

        var text = Text("Label", visual, label, 28, G, TextAlignmentOptions.Center, _fontBold);
        Stretch(text.rectTransform);

        var bb = root.gameObject.AddComponent<UI_BracketButton>();
        Set(bb, "_visual", visual); Set(bb, "_fill", fill.rectTransform); Set(bb, "_left", left); Set(bb, "_right", right);
        Set(bb, "_rightGroup", rightGroup); Set(bb, "_label", text);
        return btn;
    }

    // [ 또는 ] 모양: 세로선 + 위/아래 짧은 가로선
    private static RectTransform Bracket(string name, RectTransform parent, bool left)
    {
        var b = UI(name, parent);
        float ax = left ? 0f : 1f;
        b.anchorMin = new Vector2(ax, 0f); b.anchorMax = new Vector2(ax, 1f);
        b.pivot = new Vector2(ax, 0.5f);
        b.sizeDelta = new Vector2(22f, 0f);
        b.anchoredPosition = new Vector2(left ? 10f : -10f, 0f);
        const float th = 3f;
        var v = Img("V", b, G2); Place(v.rectTransform, new Vector2(ax, 0.5f), new Vector2(ax, 0.5f), Vector2.zero, new Vector2(th, 0));
        v.rectTransform.anchorMin = new Vector2(ax, 0f); v.rectTransform.anchorMax = new Vector2(ax, 1f); v.rectTransform.sizeDelta = new Vector2(th, 0f);
        var top = Img("Top", b, G2); top.rectTransform.anchorMin = new Vector2(0, 1); top.rectTransform.anchorMax = new Vector2(1, 1);
        top.rectTransform.pivot = new Vector2(0.5f, 1f); top.rectTransform.sizeDelta = new Vector2(0, th); top.rectTransform.anchoredPosition = Vector2.zero;
        var bot = Img("Bottom", b, G2); bot.rectTransform.anchorMin = new Vector2(0, 0); bot.rectTransform.anchorMax = new Vector2(1, 0);
        bot.rectTransform.pivot = new Vector2(0.5f, 0f); bot.rectTransform.sizeDelta = new Vector2(0, th); bot.rectTransform.anchoredPosition = Vector2.zero;
        foreach (var i in new[] { v, top, bot }) i.raycastTarget = false;
        return b;
    }

    // 사각형의 네 모서리에 ㄱ자 표시를 붙인다.
    private static void BuildCorners(RectTransform parent, Color c, float len, float th, float margin = 0f)
    {
        for (int i = 0; i < 4; i++)
        {
            float ax = (i % 2 == 0) ? 0f : 1f;
            float ay = (i < 2) ? 1f : 0f;
            var corner = UI($"Corner_{i}", parent);
            Place(corner, new Vector2(ax, ay), new Vector2(ax, ay),
                new Vector2(ax == 0 ? margin : -margin, ay == 0 ? margin : -margin), new Vector2(len, len));
            var h = Img("H", corner, c);
            Place(h.rectTransform, new Vector2(ax, ay), new Vector2(ax, ay), Vector2.zero, new Vector2(len, th));
            var v = Img("V", corner, c);
            Place(v.rectTransform, new Vector2(ax, ay), new Vector2(ax, ay), Vector2.zero, new Vector2(th, len));
            h.raycastTarget = v.raycastTarget = false;
        }
    }

    // ===================== 에셋 =====================

    private static void SetupTextures()
    {
        Tex($"{ImgDir}/IMG_LoginCCTV_BG.png", TextureWrapMode.Clamp, FilterMode.Bilinear, 2048, true);
        Tex($"{ImgDir}/IMG_Scanline.png", TextureWrapMode.Repeat, FilterMode.Point, 32, false);
        Tex($"{ImgDir}/IMG_Noise.png", TextureWrapMode.Repeat, FilterMode.Point, 256, false);
        Tex($"{ImgDir}/IMG_RollBar.png", TextureWrapMode.Clamp, FilterMode.Bilinear, 128, false);
        BorderSprite($"{ImgDir}/IMG_Border.png");
    }

    private static void Tex(string path, TextureWrapMode wrap, FilterMode filter, int max, bool compress)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogWarning($"[LoginSceneBuilder] 텍스처 없음: {path}"); return; }
        ti.textureType = TextureImporterType.Default;
        ti.wrapMode = wrap;
        ti.filterMode = filter;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.maxTextureSize = max;
        ti.textureCompression = compress ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
        ti.SaveAndReimport();
    }

    private static TMP_FontAsset MakeFont(string ttfPath, string assetPath)
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null) return existing;

        var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        var fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
        fa.name = Path.GetFileNameWithoutExtension(assetPath);
        AssetDatabase.CreateAsset(fa, assetPath);
        fa.atlasTextures[0].name = fa.name + " Atlas";
        AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        fa.material.name = fa.name + " Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        AssetDatabase.SaveAssets();
        return fa;
    }

    // 가운데가 비어 있는 9-slice 테두리 스프라이트
    private static void BorderSprite(string path)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spriteBorder = new Vector4(4, 4, 4, 4);
        ti.filterMode = FilterMode.Point;
        ti.mipmapEnabled = false;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.SaveAndReimport();
    }

    private static Image Border(RectTransform parent, Color c)
    {
        var img = Img("Border", parent, c);
        Stretch(img.rectTransform);
        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ImgDir}/IMG_Border.png");
        img.type = Image.Type.Sliced;
        img.raycastTarget = false;
        return img;
    }

    private static T LoadOrCreate<T>(string path, System.Func<T> make) where T : Object
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a != null) return a;
        a = make();
        AssetDatabase.CreateAsset(a, path);
        return a;
    }

    private static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxDir}/{name}.wav");

    private static void Loop(GameObject go, string clip, float volume)
    {
        var src = go.AddComponent<AudioSource>();
        src.clip = Clip(clip);
        src.loop = true;
        src.playOnAwake = true;
        src.volume = volume;
    }

    private static void AddToBuildSettingsFirst(string path)
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        list.RemoveAll(s => s.path == path);
        list.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    // ===================== UI 도우미 =====================

    private static RectTransform UI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image Img(string name, Transform parent, Color c)
    {
        var rt = UI(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = c;
        return img;
    }

    private static RawImage Raw(string name, Transform parent, Texture tex, Color c)
    {
        var rt = UI(name, parent);
        var raw = rt.gameObject.AddComponent<RawImage>();
        raw.texture = tex;
        raw.color = c;
        raw.raycastTarget = false;
        return raw;
    }

    private static TextMeshProUGUI Text(string name, Transform parent, string s, float size, Color c, TextAlignmentOptions align, TMP_FontAsset font)
    {
        var rt = UI(name, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = font;
        t.text = s;
        t.fontSize = size;
        t.color = c;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void Set(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null) { Debug.LogError($"[LoginSceneBuilder] {target.GetType().Name}.{field} 필드를 찾지 못했다."); return; }
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray(Object target, string field, Object[] values)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
