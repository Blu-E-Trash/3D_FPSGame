using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

// 최적화 대상을 고르기 위한 측정 도구. Play 중에 실행하면 Docs/Perf/<이름>.md 로 남긴다.
// - Skinned  : 켜져 있는 SkinnedMeshRenderer를 루트 오브젝트별로 센다. (GPU 스키닝 비용)
// - Memory   : 메모리 총량, 씬이 쓰는 텍스처 상위 20개, 불러온 오디오 클립. (RAM)
public static class OptimizationReport
{
    [MenuItem("Tools/Perf/Report Skinned Meshes")]
    private static void ReportSkinned()
    {
        var renderers = Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);
        var sb = new StringBuilder();
        sb.AppendLine("## Skinned Mesh Report");
        sb.AppendLine();
        sb.AppendLine($"- 켜진 SkinnedMeshRenderer: {renderers.Length}");
        sb.AppendLine($"- 그중 카메라에 보이는 것(isVisible): {renderers.Count(r => r.isVisible)}");
        sb.AppendLine($"- Update When Offscreen 켜진 것: {renderers.Count(r => r.updateWhenOffscreen)}");
        sb.AppendLine();
        sb.AppendLine("| 루트 오브젝트 | 개수 | 보임 | Animator 켜짐 | 그림자 |");
        sb.AppendLine("|---|---|---|---|---|");
        foreach (var group in renderers.GroupBy(r => r.transform.root.name).OrderByDescending(g => g.Count()))
        {
            int visible = group.Count(r => r.isVisible);
            var animator = group.First().GetComponentInParent<Animator>();
            string anim = animator != null ? $"{animator.enabled} ({animator.cullingMode})" : "없음";
            string shadow = group.First().shadowCastingMode.ToString();
            sb.AppendLine($"| {group.Key} | {group.Count()} | {visible} | {anim} | {shadow} |");
        }
        Save("skinned_report", sb.ToString());
    }

    [MenuItem("Tools/Perf/Report Memory")]
    private static void ReportMemory()
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Memory Report");
        sb.AppendLine();
        sb.AppendLine($"- Total Allocated: {Mb(Profiler.GetTotalAllocatedMemoryLong())} MB");
        sb.AppendLine($"- Total Reserved: {Mb(Profiler.GetTotalReservedMemoryLong())} MB");
        sb.AppendLine($"- Mono Used / Heap: {Mb(Profiler.GetMonoUsedSizeLong())} / {Mb(Profiler.GetMonoHeapSizeLong())} MB");
        sb.AppendLine($"- Graphics Driver: {Mb(Profiler.GetAllocatedMemoryForGraphicsDriver())} MB");
        sb.AppendLine($"- Texture Memory (현재 / 원하는 양): {Mb((long)Texture.currentTextureMemory)} / {Mb((long)Texture.desiredTextureMemory)} MB");

        // 씬의 렌더러가 쓰는 텍스처
        var textures = new HashSet<Texture>();
        foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            foreach (Material m in r.sharedMaterials)
            {
                if (m == null) continue;
                foreach (int id in m.GetTexturePropertyNameIDs())
                {
                    Texture t = m.GetTexture(id);
                    if (t != null) textures.Add(t);
                }
            }
        }
        long texTotal = textures.Sum(t => Profiler.GetRuntimeMemorySizeLong(t));
        sb.AppendLine();
        sb.AppendLine($"### 씬 렌더러가 쓰는 텍스처: {textures.Count}개, {Mb(texTotal)} MB");
        sb.AppendLine();
        sb.AppendLine("| 텍스처 | 크기 | 포맷 | 메모리(MB) | Max Size | 경로 |");
        sb.AppendLine("|---|---|---|---|---|---|");
        foreach (Texture t in textures.OrderByDescending(t => Profiler.GetRuntimeMemorySizeLong(t)).Take(20))
        {
            string path = AssetDatabase.GetAssetPath(t);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            string format = t is Texture2D t2 ? t2.format.ToString() : t.GetType().Name;
            sb.AppendLine($"| {t.name} | {t.width}x{t.height} | {format} | {Mb(Profiler.GetRuntimeMemorySizeLong(t))} | {(importer != null ? importer.maxTextureSize.ToString() : "-")} | {path} |");
        }

        // 크기별 분포
        sb.AppendLine();
        sb.AppendLine("| 가장 긴 변 | 개수 | 메모리(MB) |");
        sb.AppendLine("|---|---|---|");
        foreach (var g in textures.GroupBy(t => Mathf.Max(t.width, t.height)).OrderByDescending(g => g.Key))
        {
            sb.AppendLine($"| {g.Key} | {g.Count()} | {Mb(g.Sum(t => Profiler.GetRuntimeMemorySizeLong(t)))} |");
        }

        // 오디오
        var clips = Resources.FindObjectsOfTypeAll<AudioClip>().Where(c => AssetDatabase.Contains(c)).ToArray();
        sb.AppendLine();
        sb.AppendLine($"### 불러온 오디오 클립: {clips.Length}개, {Mb(clips.Sum(c => Profiler.GetRuntimeMemorySizeLong(c)))} MB");
        sb.AppendLine();
        sb.AppendLine("| 클립 | 길이(초) | 채널 | Load Type | 메모리(MB) |");
        sb.AppendLine("|---|---|---|---|---|");
        foreach (AudioClip c in clips.OrderByDescending(c => Profiler.GetRuntimeMemorySizeLong(c)).Take(15))
        {
            sb.AppendLine($"| {c.name} | {c.length:F1} | {c.channels} | {c.loadType} | {Mb(Profiler.GetRuntimeMemorySizeLong(c))} |");
        }

        Save("memory_report", sb.ToString());
    }

    private static string Mb(long bytes) => (bytes / (1024f * 1024f)).ToString("F1");

    private static void Save(string name, string text)
    {
        Directory.CreateDirectory("Docs/Perf");
        string path = $"Docs/Perf/{name}{(EditorApplication.isPlaying ? "" : "_edit")}.md";
        File.WriteAllText(path, text, new UTF8Encoding(false));
        Debug.Log($"[OptimizationReport] 저장: {path}\n{text}");
    }
}
