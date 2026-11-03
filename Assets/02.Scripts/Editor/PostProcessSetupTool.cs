using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

// 24장 포스트 프로세싱을 Built-in 렌더 파이프라인(Post Processing v2)으로 적용하는 도구.
// 콘셉트: "빛바랜 폐공장" — 조금 바랜 색, 은은한 대비, 가장자리를 살짝 어둡게.
// Tools/Post Processing/Apply Setup
//  - PostProcessing 레이어를 만들고, 씬의 Global Volume 오브젝트에 PostProcessVolume(Global)을 붙인다.
//  - Main Camera에 PostProcessLayer(SMAA)를 붙인다. MinimapCamera에는 붙이지 않는다.
//  - FactoryPostProfile: Color Grading(ACES, 노출 보정), Bloom, Vignette, Ambient Occlusion(MSVO)
public static class PostProcessSetupTool
{
    private const string LayerName = "PostProcessing";
    private const string VolumeObjectName = "Global Volume";
    private const string ProfilePath = "Assets/01.Scenes/SampleScene/FactoryPostProfile.asset";

    [MenuItem("Tools/Post Processing/Apply Setup")]
    private static void ApplySetup()
    {
        var log = new StringBuilder();
        log.AppendLine("## Post Processing Apply Setup");

        int layer = EnsureLayer(LayerName);
        if (layer < 0)
        {
            Debug.LogError("[PostProcess] 빈 레이어 칸이 없어 PostProcessing 레이어를 만들지 못했다.");
            return;
        }

        // 1. Profile
        PostProcessProfile profile = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<PostProcessProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }

        var grading = GetOrAdd<ColorGrading>(profile);
        grading.gradingMode.Override(GradingMode.HighDefinitionRange);
        grading.tonemapper.Override(Tonemapper.Neutral); // 강한 빛만 부드럽게 누르고 중간 밝기는 그대로 둔다. (ACES는 너무 어두워졌다)
        grading.postExposure.Override(0.45f);           // 굽기 뒤 조금 어두워진 화면을 되돌린다(EV).
        grading.contrast.Override(8f);
        grading.saturation.Override(-10f);              // 빛바랜 느낌
        grading.temperature.Override(4f);               // 아주 살짝 누렇게

        var bloom = GetOrAdd<Bloom>(profile);
        bloom.intensity.Override(1.2f);
        bloom.threshold.Override(1.1f);                  // 하늘·밝은 금속만 번지게
        bloom.softKnee.Override(0.5f);
        bloom.diffusion.Override(6f);
        bloom.fastMode.Override(true);                   // 가벼운 모드

        var vignette = GetOrAdd<Vignette>(profile);
        vignette.intensity.Override(0.22f);
        vignette.smoothness.Override(0.4f);
        vignette.color.Override(Color.black);

        var ao = GetOrAdd<AmbientOcclusion>(profile);
        ao.mode.Override(AmbientOcclusionMode.MultiScaleVolumetricObscurance);
        ao.intensity.Override(0.35f);
        ao.thicknessModifier.Override(1f);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        log.AppendLine($"- Profile: {ProfilePath}");
        log.AppendLine("  - Color Grading: HDR, Neutral, Post Exposure 0.45, Contrast 8, Saturation -10, Temperature 4");
        log.AppendLine("  - Bloom: Intensity 1.2, Threshold 1.1, Soft Knee 0.5, Diffusion 6, Fast Mode");
        log.AppendLine("  - Vignette: Intensity 0.22, Smoothness 0.4");
        log.AppendLine("  - Ambient Occlusion: MSVO, Intensity 0.35");

        // 2. Volume (씬에 원래 있던 빈 Global Volume 오브젝트를 쓴다)
        GameObject volumeGo = GameObject.Find(VolumeObjectName);
        if (volumeGo == null)
        {
            volumeGo = new GameObject(VolumeObjectName);
            Undo.RegisterCreatedObjectUndo(volumeGo, "Post Process Setup");
        }
        Undo.RecordObject(volumeGo, "Post Process Setup");
        volumeGo.layer = layer;
        var volume = volumeGo.GetComponent<PostProcessVolume>();
        if (volume == null) volume = Undo.AddComponent<PostProcessVolume>(volumeGo);
        volume.isGlobal = true;
        volume.priority = 0f;
        volume.weight = 1f;
        volume.sharedProfile = profile;
        log.AppendLine($"- Volume: {VolumeObjectName} (Global, Layer {LayerName})");

        // 3. Main Camera의 PostProcessLayer
        Camera cam = Camera.main;
        var ppLayer = cam.GetComponent<PostProcessLayer>();
        if (ppLayer == null) ppLayer = Undo.AddComponent<PostProcessLayer>(cam.gameObject);
        string[] guids = AssetDatabase.FindAssets("t:PostProcessResources");
        if (guids.Length > 0)
        {
            ppLayer.Init(AssetDatabase.LoadAssetAtPath<PostProcessResources>(AssetDatabase.GUIDToAssetPath(guids[0])));
        }
        ppLayer.volumeTrigger = cam.transform;
        ppLayer.volumeLayer = 1 << layer;
        ppLayer.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
        ppLayer.subpixelMorphologicalAntialiasing.quality = SubpixelMorphologicalAntialiasing.Quality.Medium;
        EditorUtility.SetDirty(ppLayer);
        log.AppendLine($"- Main Camera: PostProcessLayer (Volume Layer {LayerName}, SMAA Medium)");
        log.AppendLine("- MinimapCamera: PostProcessLayer 없음 (미니맵에는 효과가 들지 않는다)");

        EditorSceneManager.MarkSceneDirty(volumeGo.scene);
        System.IO.Directory.CreateDirectory("Docs/Perf");
        System.IO.File.WriteAllText("Docs/Perf/postprocess_apply.md", log.ToString(), new UTF8Encoding(false));
        Debug.Log(log.ToString());
    }

    // Play 중에 Main Camera의 PostProcessLayer를 껐다 켜서 전후를 비교한다. (단축키 F11)
    [MenuItem("Tools/Post Processing/Toggle On Camera _F11")]
    private static void Toggle()
    {
        var ppLayer = Camera.main != null ? Camera.main.GetComponent<PostProcessLayer>() : null;
        if (ppLayer == null) return;
        ppLayer.enabled = !ppLayer.enabled;
        Debug.Log($"[PostProcess] Main Camera PostProcessLayer: {(ppLayer.enabled ? "ON" : "OFF")}");
    }

    private static T GetOrAdd<T>(PostProcessProfile profile) where T : PostProcessEffectSettings
    {
        if (profile.TryGetSettings(out T settings))
        {
            settings.enabled.Override(true);
            return settings;
        }
        settings = (T)profile.AddSettings(typeof(T));
        settings.name = typeof(T).Name;
        settings.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        settings.enabled.Override(true);
        AssetDatabase.AddObjectToAsset(settings, profile);
        return settings;
    }

    // TagManager의 빈 칸(8~31)에 레이어를 만든다. 이미 있으면 그 번호를 돌려준다.
    private static int EnsureLayer(string name)
    {
        int existing = LayerMask.NameToLayer(name);
        if (existing >= 0) return existing;

        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty slot = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = name;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }
        return -1;
    }
}
