using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// 최적화 계획에서 고른 항목을 에셋에 적용하는 도구.
// Tools/Optimization/Apply
//  - GPU: 좀비(14조각 Skinned Mesh)의 스키닝 품질을 4본 → 2본으로, 손·아래팔 조각은 그림자를 끈다.
//  - RAM: 드럼통(Post-Apocalyptic Barrels) 텍스처 최대 크기를 2048 → 1024로 줄인다.
public static class OptimizationApplyTool
{
    private const string ZombiePrefabPath = "Assets/Zombie/Prefabs/Zombie1.prefab";
    private const string BarrelTextureFolder = "Assets/Sector Warzone/Post-Apocalyptic Barrels/Textures";
    private const int BarrelMaxSize = 1024;

    // 화면에서 작게 보여 그림자를 꺼도 티가 나지 않는 조각
    private static readonly string[] NoShadowParts = { "Z_L_ArmPalm", "Z_R_ArmPalm", "Z_L_Forearm", "Z_R_Forearm" };

    [MenuItem("Tools/Optimization/Apply")]
    private static void Apply()
    {
        var log = new StringBuilder();
        log.AppendLine("## Optimization Apply");

        // 1. 좀비 프리팹
        GameObject root = PrefabUtility.LoadPrefabContents(ZombiePrefabPath);
        int parts = 0, noShadow = 0;
        foreach (SkinnedMeshRenderer smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            smr.quality = SkinQuality.Bone2;
            parts++;
            if (System.Array.IndexOf(NoShadowParts, smr.name) >= 0)
            {
                smr.shadowCastingMode = ShadowCastingMode.Off;
                noShadow++;
            }
        }
        PrefabUtility.SaveAsPrefabAsset(root, ZombiePrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        log.AppendLine($"- {ZombiePrefabPath}: Skinned Mesh {parts}개 Skin Weights 2 Bones, 그림자 끈 조각 {noShadow}개 ({string.Join(", ", NoShadowParts)})");

        // 2. 드럼통 텍스처
        int changed = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { BarrelTextureFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer && importer.maxTextureSize > BarrelMaxSize)
            {
                importer.maxTextureSize = BarrelMaxSize;
                importer.SaveAndReimport();
                changed++;
            }
        }
        log.AppendLine($"- {BarrelTextureFolder}: 텍스처 {changed}개 Max Size {BarrelMaxSize}");

        System.IO.Directory.CreateDirectory("Docs/Perf");
        System.IO.File.WriteAllText("Docs/Perf/optimization_apply.md", log.ToString(), new UTF8Encoding(false));
        Debug.Log(log.ToString());
    }
}
