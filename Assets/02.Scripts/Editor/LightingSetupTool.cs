using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

// 23장 빛 최적화를 Built-in 렌더 파이프라인의 SampleScene에 적용하는 도구.
// 1. Analyze      : 지금 씬의 빛, 맵 렌더러, 라이트맵 UV 상태를 Console에 표로 남긴다.
// 2. Apply Setup  : 해는 Mixed, 맵의 보조 해는 Baked, 맵은 라이트맵으로 굽게 바꾸고
//                   FactoryLighting 설정, 라이트 프로브, 리플렉션 프로브, 그림자 거리를 맞춘다.
// 3. Bake         : Generate Lighting (비동기)
public static class LightingSetupTool
{
    private const string MapRootName = "Map_v1";
    private const string SunName = "Directional Light";
    private const string LightingAssetPath = "Assets/01.Scenes/SampleScene/FactoryLighting.lighting";
    private const string ProbeGroupName = "LightProbes_Factory";
    private const string ReflectionProbeName = "ReflectionProbe_Factory";

    private const float SmallObjectSize = 1.0f;   // 이보다 작은 물체는 라이트맵 대신 프로브로 빛을 받는다.
    private const float ProbeSpacing = 6f;        // 라이트 프로브 격자 간격(m)
    private const float ShadowDistance = 30f;     // 실시간 그림자 거리(m), 그 너머는 구운 그림자(Shadowmask)

    [MenuItem("Tools/Lighting/1. Analyze")]
    private static void Analyze()
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Lighting Analyze");
        sb.AppendLine();
        sb.AppendLine("| Light | Type | Mode | Shadows | Intensity | Active |");
        sb.AppendLine("|---|---|---|---|---|---|");
        foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            sb.AppendLine($"| {GetPath(light.transform)} | {light.type} | {light.lightmapBakeType} | {light.shadows} | {light.intensity} | {light.isActiveAndEnabled} |");
        }

        GameObject map = GameObject.Find(MapRootName);
        if (map != null)
        {
            MeshRenderer[] renderers = map.GetComponentsInChildren<MeshRenderer>();
            int noUv2 = 0, small = 0, gi = 0;
            var importersWithoutUv = new HashSet<string>();
            foreach (MeshRenderer r in renderers)
            {
                var filter = r.GetComponent<MeshFilter>();
                Mesh mesh = filter != null ? filter.sharedMesh : null;
                if (mesh != null && !HasLightmapUv(mesh, importersWithoutUv)) noUv2++;
                if (r.bounds.size.magnitude < SmallObjectSize) small++;
                if (GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject, StaticEditorFlags.ContributeGI)) gi++;
            }
            sb.AppendLine();
            sb.AppendLine($"- {MapRootName} MeshRenderer: {renderers.Length}");
            sb.AppendLine($"- Contribute GI 켜진 렌더러: {gi}");
            sb.AppendLine($"- 라이트맵 UV(UV2)가 없는 렌더러: {noUv2} (모델 파일 {importersWithoutUv.Count}개)");
            sb.AppendLine($"- {SmallObjectSize}m보다 작은 렌더러: {small}");
        }

        sb.AppendLine();
        sb.AppendLine($"- Lighting Settings: {(Lightmapping.TryGetLightingSettings(out LightingSettings current) ? AssetDatabase.GetAssetPath(current) : "없음")}");
        sb.AppendLine($"- Quality: {QualitySettings.names[QualitySettings.GetQualityLevel()]}, Shadow Distance {QualitySettings.shadowDistance}, Cascades {QualitySettings.shadowCascades}, Shadowmask {QualitySettings.shadowmaskMode}");
        Save("lighting_analyze", sb.ToString());
    }

    [MenuItem("Tools/Lighting/2. Apply Setup")]
    private static void ApplySetup()
    {
        var log = new StringBuilder();
        log.AppendLine("## Lighting Apply Setup");

        GameObject map = GameObject.Find(MapRootName);
        if (map == null)
        {
            Debug.LogError($"[Lighting] {MapRootName} 을 찾지 못했다.");
            return;
        }

        // 1. 빛: 씬 루트의 해는 Mixed(실시간 그림자 + 구운 간접광), 맵 안의 빛은 Baked.
        Light sun = null;
        foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            Undo.RecordObject(light, "Lighting Setup");
            bool isRootSun = light.type == LightType.Directional && light.transform.parent == null && light.name == SunName;
            if (isRootSun)
            {
                sun = light;
                light.lightmapBakeType = LightmapBakeType.Mixed;
                light.shadows = LightShadows.Soft;
                log.AppendLine($"- 해 {GetPath(light.transform)}: Mixed, Soft Shadows");
            }
            else if (light.transform.IsChildOf(map.transform))
            {
                light.lightmapBakeType = LightmapBakeType.Baked;
                log.AppendLine($"- 맵 빛 {GetPath(light.transform)}: {light.type} → Baked (실시간 계산·그림자 없음)");
            }
        }
        if (sun != null) RenderSettings.sun = sun;

        // 2. 맵 렌더러: 굽기에 넣고, 큰 물체는 라이트맵, 작은 물체는 라이트 프로브로 받는다.
        int lightmapCount = 0, probeCount = 0;
        var importersWithoutUv = new HashSet<string>();
        foreach (MeshRenderer r in map.GetComponentsInChildren<MeshRenderer>())
        {
            GameObject go = r.gameObject;
            Undo.RecordObject(go, "Lighting Setup");
            Undo.RecordObject(r, "Lighting Setup");
            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go);
            GameObjectUtility.SetStaticEditorFlags(go, flags | StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic);

            var filter = r.GetComponent<MeshFilter>();
            bool hasUv = filter != null && filter.sharedMesh != null && HasLightmapUv(filter.sharedMesh, importersWithoutUv);
            if (r.bounds.size.magnitude >= SmallObjectSize && hasUv)
            {
                r.receiveGI = ReceiveGI.Lightmaps;
                lightmapCount++;
            }
            else
            {
                r.receiveGI = ReceiveGI.LightProbes;
                probeCount++;
            }
        }
        log.AppendLine($"- 맵 렌더러: 라이트맵 {lightmapCount}개, 라이트 프로브 {probeCount}개 (UV2 없는 모델 {importersWithoutUv.Count}개)");

        // 3. 굽기 설정: Shadowmask, Progressive GPU, 해상도 20
        LightingSettings settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingAssetPath);
        if (settings == null)
        {
            settings = new LightingSettings { name = "FactoryLighting" };
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(LightingAssetPath));
            AssetDatabase.CreateAsset(settings, LightingAssetPath);
        }
        settings.bakedGI = true;
        settings.realtimeGI = false;
        settings.mixedBakeMode = MixedLightingMode.Shadowmask;
        settings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
        settings.lightmapResolution = 20;
        settings.lightmapMaxSize = 2048;
        settings.directionalityMode = LightmapsMode.NonDirectional;
        settings.ao = false;
        settings.lightmapCompression = LightmapCompression.NormalQuality;
        EditorUtility.SetDirty(settings);
        Lightmapping.lightingSettings = settings;
        log.AppendLine($"- Lighting Settings: {LightingAssetPath} (Shadowmask, Progressive GPU, Resolution 20, Max 2048, Non-Directional)");

        // 4. 라이트 프로브: NavMesh가 있는 곳(플레이어·좀비가 다니는 곳)에 격자로 두 층.
        Bounds mapBounds = GetBounds(map);
        var positions = new List<Vector3>();
        for (float x = mapBounds.min.x; x <= mapBounds.max.x; x += ProbeSpacing)
        {
            for (float z = mapBounds.min.z; z <= mapBounds.max.z; z += ProbeSpacing)
            {
                var probe = new Vector3(x, mapBounds.center.y, z);
                if (NavMesh.SamplePosition(probe, out NavMeshHit hit, mapBounds.extents.y + 2f, NavMesh.AllAreas)
                    && Mathf.Abs(hit.position.x - x) < ProbeSpacing * 0.5f
                    && Mathf.Abs(hit.position.z - z) < ProbeSpacing * 0.5f)
                {
                    positions.Add(hit.position + Vector3.up * 0.5f);
                    positions.Add(hit.position + Vector3.up * 2.5f);
                }
            }
        }
        GameObject probeGo = GameObject.Find(ProbeGroupName);
        if (probeGo == null)
        {
            probeGo = new GameObject(ProbeGroupName);
            Undo.RegisterCreatedObjectUndo(probeGo, "Lighting Setup");
        }
        probeGo.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        var group = probeGo.GetComponent<LightProbeGroup>();
        if (group == null) group = probeGo.AddComponent<LightProbeGroup>();
        Undo.RecordObject(group, "Lighting Setup");
        group.probePositions = positions.ToArray();
        log.AppendLine($"- Light Probe Group: 프로브 {positions.Count}개 (간격 {ProbeSpacing}m, 높이 0.5m·2.5m)");

        // 5. 리플렉션 프로브: 맵 전체를 덮는 Baked 프로브 하나 (실시간 아님)
        GameObject reflGo = GameObject.Find(ReflectionProbeName);
        if (reflGo == null)
        {
            reflGo = new GameObject(ReflectionProbeName);
            Undo.RegisterCreatedObjectUndo(reflGo, "Lighting Setup");
        }
        reflGo.transform.position = new Vector3(mapBounds.center.x, mapBounds.min.y + 3f, mapBounds.center.z);
        var refl = reflGo.GetComponent<ReflectionProbe>();
        if (refl == null) refl = reflGo.AddComponent<ReflectionProbe>();
        Undo.RecordObject(refl, "Lighting Setup");
        refl.mode = ReflectionProbeMode.Baked;
        refl.resolution = 128;
        refl.size = mapBounds.size + Vector3.one;
        refl.center = new Vector3(0f, mapBounds.center.y - reflGo.transform.position.y, 0f);
        log.AppendLine($"- Reflection Probe: Baked, 128, 크기 {refl.size}");

        // 6. 그림자: 실시간 그림자는 30m까지, 그 너머는 Shadowmask로 구운 그림자.
        //    QualitySettings는 프로젝트 설정이라 모든 Quality 단계에 똑같이 적용한다.
        int current = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.shadowDistance = ShadowDistance;
            QualitySettings.shadowmaskMode = ShadowmaskMode.DistanceShadowmask;
        }
        QualitySettings.SetQualityLevel(current, false);
        log.AppendLine($"- Quality(모든 단계): Shadow Distance {ShadowDistance}, Shadowmask Mode Distance Shadowmask");

        EditorSceneManager.MarkSceneDirty(map.scene);
        Save("lighting_apply", log.ToString());
    }

    [MenuItem("Tools/Lighting/3. Bake")]
    private static void Bake()
    {
        if (Lightmapping.BakeAsync())
        {
            Debug.Log("[Lighting] Generate Lighting 시작");
        }
    }

    // Console과 Docs/Perf/<이름>.md 에 함께 남긴다.
    private static void Save(string name, string text)
    {
        System.IO.Directory.CreateDirectory("Docs/Perf");
        System.IO.File.WriteAllText($"Docs/Perf/{name}.md", text, new UTF8Encoding(false));
        Debug.Log(text);
    }

    private static bool HasLightmapUv(Mesh mesh, HashSet<string> importersWithoutUv)
    {
        string path = AssetDatabase.GetAssetPath(mesh);
        if (AssetImporter.GetAtPath(path) is ModelImporter importer)
        {
            if (importer.generateSecondaryUV) return true;
            // 모델에 UV2가 들어 있는지는 메시를 읽어야 알 수 있다.
            var uvs = new List<Vector2>();
            mesh.GetUVs(1, uvs);
            if (uvs.Count > 0) return true;
            importersWithoutUv.Add(path);
            return false;
        }
        var list = new List<Vector2>();
        mesh.GetUVs(1, list);
        return list.Count > 0;
    }

    private static Bounds GetBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
