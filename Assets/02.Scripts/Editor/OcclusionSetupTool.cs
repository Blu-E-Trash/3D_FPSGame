using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 22.6 우리 게임 씬에 오클루전 컬링 적용하기를 한 번에 해 주는 에디터 도구.
// 메뉴 Tools > Occlusion > Apply To Map 으로 실행한다.
// - Map_v1 아래에서 큰 건물·벽·바닥(가장 긴 변이 기준 이상): Occluder Static + Occludee Static
// - 작은 소품(상자, 파이프, 팔레트 등): Occludee Static만 (Occluder는 끈다)
// - 파티클과 조명은 건드리지 않는다. 좀비·플레이어·드럼통처럼 움직이는 것은 Map_v1 밖이라 대상이 아니다.
// - MinimapCamera의 Occlusion Culling은 끈다.
// 굽기(Bake)와 성능 비교는 직접 한다.
public static class OcclusionSetupTool
{
    private const string MapRootName = "Map_v1";
    private const string MinimapCameraName = "MinimapCamera";
    private const float OccluderMinSize = 5f;   // Occlusion 창의 Smallest Occluder 기본값(5m)과 맞춘다.

    [MenuItem("Tools/Occlusion/Apply To Map")]
    private static void ApplyToMap()
    {
        GameObject mapRoot = GameObject.Find(MapRootName);
        if (mapRoot == null)
        {
            Debug.LogError($"씬에서 '{MapRootName}'을 찾지 못했다. Map이 켜져 있는지 확인한다.");
            return;
        }

        // 이름(프리팹)별로 몇 개를 어떻게 바꿨는지 모은다.
        var occluders = new Dictionary<string, int>();
        var occludees = new Dictionary<string, int>();

        foreach (MeshRenderer renderer in mapRoot.GetComponentsInChildren<MeshRenderer>(true))
        {
            GameObject go = renderer.gameObject;
            float size = renderer.bounds.size.x;
            size = Mathf.Max(size, renderer.bounds.size.y, renderer.bounds.size.z);
            bool isBig = size >= OccluderMinSize;

            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go);
            flags |= StaticEditorFlags.OccludeeStatic;
            if (isBig)
            {
                flags |= StaticEditorFlags.OccluderStatic;
            }
            else
            {
                flags &= ~StaticEditorFlags.OccluderStatic;
            }

            Undo.RecordObject(go, "Apply Occlusion Flags");
            GameObjectUtility.SetStaticEditorFlags(go, flags);

            string key = GetPrefabName(go);
            var table = isBig ? occluders : occludees;
            table[key] = table.TryGetValue(key, out int count) ? count + 1 : 1;
        }

        // 위에서 내려다보는 미니맵 카메라는 가려지는 것이 거의 없으니 오클루전 컬링을 끈다.
        GameObject minimapCamera = GameObject.Find(MinimapCameraName);
        if (minimapCamera != null && minimapCamera.TryGetComponent(out Camera camera))
        {
            Undo.RecordObject(camera, "Disable Minimap Occlusion");
            camera.useOcclusionCulling = false;
        }

        EditorSceneManager.MarkSceneDirty(mapRoot.scene);
        Debug.Log(BuildReport(occluders, occludees, minimapCamera != null));
    }

    // 프리팹 인스턴스면 프리팹 이름을, 아니면 오브젝트 이름을 쓴다.
    private static string GetPrefabName(GameObject go)
    {
        GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(go);
        GameObject source = root != null ? PrefabUtility.GetCorrespondingObjectFromSource(root) : null;
        return source != null ? source.name : go.name;
    }

    private static string BuildReport(Dictionary<string, int> occluders, Dictionary<string, int> occludees, bool minimapDone)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[Occlusion] Occluder+Occludee {occluders.Values.Sum()}개 / Occludee만 {occludees.Values.Sum()}개 / MinimapCamera Occlusion 끔: {minimapDone}");
        sb.AppendLine("| 표시 | 프리팹 | 개수 |");
        sb.AppendLine("|---|---|---|");
        foreach (var pair in occluders.OrderByDescending(p => p.Value))
        {
            sb.AppendLine($"| Occluder+Occludee | {pair.Key} | {pair.Value} |");
        }
        foreach (var pair in occludees.OrderByDescending(p => p.Value))
        {
            sb.AppendLine($"| Occludee만 | {pair.Key} | {pair.Value} |");
        }
        return sb.ToString();
    }
}
