//______________________________________________
// Implemented by using the GPT AI
// Designed, tested and published by:
// ALIyerEdon@gmail.com
// https://assetstore.unity.com/publishers/23606
//______________________________________________

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class UsedAssetsFinderWindow : EditorWindow
{
    private enum AssetCategory
    {
        All,
        Prefabs,
        Models,
        Textures,
        Materials,
        Audio,
        Animations,
        Shaders,
        ScriptableObjects,
        Scripts,
        Other
    }

    [Serializable]
    private class AssetInfo
    {
        public string Path;
        public string FileName;
        public string TypeName;
        public long SizeBytes;
        public bool IsInResources;
        public AssetCategory Category;

        public AssetInfo(string assetPath)
        {
            Path = assetPath;
            FileName = System.IO.Path.GetFileName(assetPath);

            Type assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            TypeName = assetType != null ? assetType.Name : "Unknown";

            IsInResources = ContainsFolder(assetPath, "Resources");
            Category = GetCategory(assetPath, assetType);
            SizeBytes = GetAssetFileSize(assetPath);
        }

        private static long GetAssetFileSize(string assetPath)
        {
            try
            {
                string fullPath = System.IO.Path.GetFullPath(assetPath);

                return File.Exists(fullPath)
                    ? new FileInfo(fullPath).Length
                    : 0L;
            }
            catch
            {
                return 0L;
            }
        }

        private static bool ContainsFolder(string assetPath, string folderName)
        {
            return assetPath.IndexOf(
                $"/{folderName}/",
                StringComparison.OrdinalIgnoreCase
            ) >= 0;
        }

        private static AssetCategory GetCategory(string assetPath, Type assetType)
        {
            string extension = System.IO.Path.GetExtension(assetPath)
                .ToLowerInvariant();

            if (extension == ".prefab")
                return AssetCategory.Prefabs;

            if (extension is ".fbx" or ".obj" or ".blend" or ".dae" or ".3ds" or ".dxf" or ".skp")
                return AssetCategory.Models;

            if (extension is ".png" or ".jpg" or ".jpeg" or ".tga" or ".psd" or ".tif" or ".tiff" or ".bmp" or ".exr" or ".gif" or ".hdr")
                return AssetCategory.Textures;

            if (extension == ".mat" || assetType == typeof(Material))
                return AssetCategory.Materials;

            if (extension is ".wav" or ".mp3" or ".ogg" or ".aiff" or ".aif" or ".mod" or ".it" or ".s3m" or ".xm")
                return AssetCategory.Audio;

            if (extension is ".anim" or ".controller" or ".overridecontroller" ||
                assetType == typeof(AnimationClip) ||
                assetType == typeof(AnimatorController))
            {
                return AssetCategory.Animations;
            }

            if (extension is ".shader" or ".shadergraph" or ".shadersubgraph" or ".compute" ||
                assetType == typeof(Shader))
            {
                return AssetCategory.Shaders;
            }

            if (extension == ".asset")
                return AssetCategory.ScriptableObjects;

            if (extension is ".cs" or ".js" or ".boo")
                return AssetCategory.Scripts;

            return AssetCategory.Other;
        }
    }

    private const int ScanBatchSize = 150;
    private const int ResultsPerPage = 50;

    private readonly List<SceneAsset> selectedScenes = new();
    private readonly List<AssetInfo> usedAssets = new();

    private string[] allAssetPaths;
    private HashSet<string> usedAssetPaths;

    private int currentScanIndex;
    private int currentPage;

    private bool isScanning;
    private bool scanCompleted;
    private bool cancelRequested;

    private Vector2 windowScroll;
    private Vector2 sceneScroll;
    private Vector2 resultsScroll;

    private bool includeDisabledBuildScenes = true;
    private bool includeResourcesAssets;
    private bool includeScripts;
    private bool includeEditorAssets;

    private string searchFilter = string.Empty;
    private AssetCategory selectedCategory = AssetCategory.All;

    [MenuItem("Tools/Project Analysis/Used Assets Finder")]
    public static void Open()
    {
        UsedAssetsFinderWindow window =
            GetWindow<UsedAssetsFinderWindow>("Used Assets Finder");

        window.minSize = new Vector2(620f, 520f);
    }

    private void OnDisable()
    {
        StopScan();
    }

    private void OnGUI()
    {
        windowScroll = EditorGUILayout.BeginScrollView(windowScroll);

        DrawHeader();
        EditorGUILayout.Space(8f);

        DrawSceneSection();
        EditorGUILayout.Space(8f);

        DrawOptions();
        EditorGUILayout.Space(10f);

        DrawScanControls();
        EditorGUILayout.Space(10f);

        if (isScanning)
            DrawScanProgress();

        if (scanCompleted && !isScanning)
            DrawResults();

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField(
            "Used Assets Finder",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "Scans selected scene dependencies and lists all assets referenced by them.\n\n" +
            "Assets loaded through Resources.Load, Addressables, AssetBundles, string paths, " +
            "reflection, or custom runtime systems may not be detected unless they are included " +
            "in the selected scene dependencies.",
            MessageType.Info
        );
    }

    private void DrawSceneSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("Scenes To Scan", EditorStyles.boldLabel);

        GUILayout.FlexibleSpace();

        EditorGUI.BeginDisabledGroup(isScanning);

        if (GUILayout.Button("Add Build Settings Scenes", GUILayout.Width(180f)))
            AddBuildSettingsScenes();

        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        sceneScroll = EditorGUILayout.BeginScrollView(
            sceneScroll,
            GUILayout.Height(120f)
        );

        if (selectedScenes.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No scenes added. Drag scene assets here or add scenes from Build Settings.",
                MessageType.Warning
            );
        }

        for (int i = selectedScenes.Count - 1; i >= 0; i--)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(isScanning);

            SceneAsset updatedScene = (SceneAsset)EditorGUILayout.ObjectField(
                selectedScenes[i],
                typeof(SceneAsset),
                false
            );

            if (updatedScene != selectedScenes[i])
                selectedScenes[i] = updatedScene;

            if (GUILayout.Button("X", GUILayout.Width(28f)))
                selectedScenes.RemoveAt(i);

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space(4f);

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginDisabledGroup(isScanning);

        if (GUILayout.Button("+ Add Scene Slot"))
            selectedScenes.Add(null);

        if (GUILayout.Button("Clear Scenes"))
        {
            selectedScenes.Clear();
            ClearResults();
        }

        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawOptions()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("Scan Options", EditorStyles.boldLabel);

        EditorGUI.BeginDisabledGroup(isScanning);

        includeDisabledBuildScenes = EditorGUILayout.ToggleLeft(
            "Include disabled Build Settings scenes when adding scenes",
            includeDisabledBuildScenes
        );

        includeResourcesAssets = EditorGUILayout.ToggleLeft(
            "Include assets inside Resources folders",
            includeResourcesAssets
        );

        includeScripts = EditorGUILayout.ToggleLeft(
            "Include script files",
            includeScripts
        );

        includeEditorAssets = EditorGUILayout.ToggleLeft(
            "Include assets inside Editor folders",
            includeEditorAssets
        );

        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndVertical();
    }

    private void DrawScanControls()
    {
        bool hasValidScene = selectedScenes.Any(scene => scene != null);

        EditorGUILayout.BeginHorizontal();

        if (isScanning)
        {
            Color previousColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.55f, 0.35f);

            if (GUILayout.Button("Cancel Scan", GUILayout.Height(34f)))
                cancelRequested = true;

            GUI.backgroundColor = previousColor;
        }
        else
        {
            EditorGUI.BeginDisabledGroup(!hasValidScene);

            Color previousColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.35f, 0.75f, 1f);

            if (GUILayout.Button("Scan Project", GUILayout.Height(34f)))
                StartScan();

            GUI.backgroundColor = previousColor;

            EditorGUI.EndDisabledGroup();
        }

        EditorGUILayout.EndHorizontal();

        if (!hasValidScene && !isScanning)
        {
            EditorGUILayout.HelpBox(
                "Add at least one valid scene before scanning.",
                MessageType.Warning
            );
        }
    }

    private void DrawScanProgress()
    {
        if (allAssetPaths == null || allAssetPaths.Length == 0)
            return;

        float progress = (float)currentScanIndex / allAssetPaths.Length;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            $"Scanning: {currentScanIndex:N0} / {allAssetPaths.Length:N0} assets"
        );

        Rect progressRect = GUILayoutUtility.GetRect(1f, 20f);

        EditorGUI.ProgressBar(
            progressRect,
            progress,
            $"{progress * 100f:0.0}%"
        );

        EditorGUILayout.LabelField(
            $"Used assets found so far: {usedAssets.Count:N0}",
            EditorStyles.miniLabel
        );

        EditorGUILayout.EndVertical();
    }

    private void DrawResults()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(
            $"Results: {usedAssets.Count:N0} used assets",
            EditorStyles.boldLabel
        );

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Select Page", GUILayout.Width(95f)))
            SelectCurrentPageAssets();

        if (GUILayout.Button("Export Filtered", GUILayout.Width(110f)))
            ExportFilteredPaths();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6f);

        DrawFilters();

        List<AssetInfo> filteredAssets = GetFilteredAssets();
        long totalSize = filteredAssets.Sum(asset => asset.SizeBytes);

        EditorGUILayout.Space(4f);

        EditorGUILayout.LabelField(
            $"Filtered Results: {filteredAssets.Count:N0} assets | Total Size: {FormatBytes(totalSize)}",
            EditorStyles.miniLabel
        );

        DrawPagination(filteredAssets.Count);

        EditorGUILayout.Space(4f);

        List<AssetInfo> pageAssets = GetCurrentPageAssets(filteredAssets);

        resultsScroll = EditorGUILayout.BeginScrollView(
            resultsScroll,
            GUILayout.Height(320f)
        );

        if (pageAssets.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No assets match the current filter.",
                MessageType.Info
            );
        }
        else
        {
            foreach (AssetInfo assetInfo in pageAssets)
                DrawAssetRow(assetInfo);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawFilters()
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginChangeCheck();

        searchFilter = EditorGUILayout.TextField("Search", searchFilter);

        selectedCategory = (AssetCategory)EditorGUILayout.EnumPopup(
            selectedCategory,
            GUILayout.Width(160f)
        );

        if (EditorGUI.EndChangeCheck())
        {
            currentPage = 0;
            resultsScroll = Vector2.zero;
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawPagination(int filteredCount)
    {
        int pageCount = Mathf.Max(
            1,
            Mathf.CeilToInt(filteredCount / (float)ResultsPerPage)
        );

        currentPage = Mathf.Clamp(currentPage, 0, pageCount - 1);

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginDisabledGroup(currentPage <= 0);

        if (GUILayout.Button("Previous", GUILayout.Width(80f)))
        {
            currentPage--;
            resultsScroll = Vector2.zero;
        }

        EditorGUI.EndDisabledGroup();

        GUILayout.FlexibleSpace();

        EditorGUILayout.LabelField(
            $"Page {currentPage + 1} / {pageCount}",
            EditorStyles.centeredGreyMiniLabel,
            GUILayout.Width(110f)
        );

        GUILayout.FlexibleSpace();

        EditorGUI.BeginDisabledGroup(currentPage >= pageCount - 1);

        if (GUILayout.Button("Next", GUILayout.Width(80f)))
        {
            currentPage++;
            resultsScroll = Vector2.zero;
        }

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawAssetRow(AssetInfo assetInfo)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();

        Texture icon = AssetDatabase.GetCachedIcon(assetInfo.Path);
        GUILayout.Label(icon, GUILayout.Width(20f), GUILayout.Height(20f));

        EditorGUILayout.BeginVertical();

        EditorGUILayout.LabelField(
            assetInfo.FileName,
            EditorStyles.boldLabel
        );

        EditorGUILayout.LabelField(
            $"{assetInfo.Category} | {assetInfo.TypeName} | {FormatBytes(assetInfo.SizeBytes)}",
            EditorStyles.miniLabel
        );

        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Ping", GUILayout.Width(50f)))
            PingAsset(assetInfo.Path);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.SelectableLabel(
            assetInfo.Path,
            EditorStyles.miniLabel,
            GUILayout.Height(EditorGUIUtility.singleLineHeight)
        );

        if (assetInfo.IsInResources)
        {
            EditorGUILayout.LabelField(
                "Info: This asset is inside a Resources folder.",
                EditorStyles.miniLabel
            );
        }

        EditorGUILayout.EndVertical();
    }

    private void StartScan()
    {
        List<string> scenePaths = selectedScenes
            .Where(scene => scene != null)
            .Select(AssetDatabase.GetAssetPath)
            .Where(path => !string.IsNullOrEmpty(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (scenePaths.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "No Scenes Selected",
                "Please add at least one valid scene before scanning.",
                "OK"
            );

            return;
        }

        ClearResults();

        try
        {
            usedAssetPaths = new HashSet<string>(
                AssetDatabase.GetDependencies(scenePaths.ToArray(), true),
                StringComparer.OrdinalIgnoreCase
            );

            foreach (string scenePath in scenePaths)
                usedAssetPaths.Add(scenePath);

            allAssetPaths = AssetDatabase.GetAllAssetPaths();
            currentScanIndex = 0;
            cancelRequested = false;
            isScanning = true;

            EditorApplication.update += ProcessScanBatch;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);

            EditorUtility.DisplayDialog(
                "Scan Failed",
                "An error occurred while collecting asset dependencies. Check the Console for details.",
                "OK"
            );

            StopScan();
        }
    }

    private void ProcessScanBatch()
    {
        if (!isScanning)
            return;

        if (cancelRequested)
        {
            Debug.Log(
                $"[Used Assets Finder] Scan canceled. Found {usedAssets.Count:N0} assets."
            );

            StopScan();
            Repaint();
            return;
        }

        if (allAssetPaths == null)
        {
            StopScan();
            return;
        }

        int endIndex = Mathf.Min(
            currentScanIndex + ScanBatchSize,
            allAssetPaths.Length
        );

        for (int i = currentScanIndex; i < endIndex; i++)
        {
            string assetPath = allAssetPaths[i];

            if (!ShouldCheckAsset(assetPath))
                continue;

            if (!usedAssetPaths.Contains(assetPath))
                continue;

            usedAssets.Add(new AssetInfo(assetPath));
        }

        currentScanIndex = endIndex;

        if (currentScanIndex >= allAssetPaths.Length)
        {
            SortUsedAssetsBySize();

            scanCompleted = true;

            Debug.Log(
                $"[Used Assets Finder] Scan completed. Found {usedAssets.Count:N0} used assets."
            );

            StopScan();
        }

        Repaint();
    }

    private void StopScan()
    {
        EditorApplication.update -= ProcessScanBatch;

        isScanning = false;
        cancelRequested = false;

        allAssetPaths = null;
        usedAssetPaths = null;
        currentScanIndex = 0;
    }

    private void ClearResults()
    {
        usedAssets.Clear();
        scanCompleted = false;
        currentPage = 0;
        resultsScroll = Vector2.zero;
    }

    private void SortUsedAssetsBySize()
    {
        usedAssets.Sort((a, b) =>
        {
            int sizeComparison = b.SizeBytes.CompareTo(a.SizeBytes);

            if (sizeComparison != 0)
                return sizeComparison;

            return string.Compare(
                a.Path,
                b.Path,
                StringComparison.OrdinalIgnoreCase
            );
        });
    }

    private bool ShouldCheckAsset(string assetPath)
    {
        if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            return false;

        if (AssetDatabase.IsValidFolder(assetPath))
            return false;

        if (assetPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            return false;

        if (assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!includeEditorAssets && ContainsFolder(assetPath, "Editor"))
            return false;

        if (!includeResourcesAssets && ContainsFolder(assetPath, "Resources"))
            return false;

        if (!includeScripts && IsScriptFile(assetPath))
            return false;

        return true;
    }

    private static bool IsScriptFile(string assetPath)
    {
        return assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
               assetPath.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
               assetPath.EndsWith(".boo", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsFolder(string assetPath, string folderName)
    {
        return assetPath.IndexOf(
            $"/{folderName}/",
            StringComparison.OrdinalIgnoreCase
        ) >= 0;
    }

    private void AddBuildSettingsScenes()
    {
        HashSet<string> existingPaths = new(
            selectedScenes
                .Where(scene => scene != null)
                .Select(AssetDatabase.GetAssetPath),
            StringComparer.OrdinalIgnoreCase
        );

        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            if (!includeDisabledBuildScenes && !buildScene.enabled)
                continue;

            if (string.IsNullOrEmpty(buildScene.path))
                continue;

            if (existingPaths.Contains(buildScene.path))
                continue;

            SceneAsset sceneAsset =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(buildScene.path);

            if (sceneAsset == null)
                continue;

            selectedScenes.Add(sceneAsset);
            existingPaths.Add(buildScene.path);
        }
    }

    private List<AssetInfo> GetFilteredAssets()
    {
        IEnumerable<AssetInfo> results = usedAssets;

        if (selectedCategory != AssetCategory.All)
        {
            results = results.Where(
                asset => asset.Category == selectedCategory
            );
        }

        if (!string.IsNullOrWhiteSpace(searchFilter))
        {
            results = results.Where(asset =>
                asset.Path.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                asset.FileName.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                asset.TypeName.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0
            );
        }

        return results.ToList();
    }

    private List<AssetInfo> GetCurrentPageAssets(List<AssetInfo> filteredAssets)
    {
        int startIndex = currentPage * ResultsPerPage;

        if (startIndex >= filteredAssets.Count)
            return new List<AssetInfo>();

        return filteredAssets
            .Skip(startIndex)
            .Take(ResultsPerPage)
            .ToList();
    }

    private void SelectCurrentPageAssets()
    {
        List<AssetInfo> filteredAssets = GetFilteredAssets();
        List<AssetInfo> pageAssets = GetCurrentPageAssets(filteredAssets);

        List<UnityEngine.Object> objectsToSelect = new();

        foreach (AssetInfo assetInfo in pageAssets)
        {
            UnityEngine.Object asset =
                AssetDatabase.LoadMainAssetAtPath(assetInfo.Path);

            if (asset != null)
                objectsToSelect.Add(asset);
        }

        Selection.objects = objectsToSelect.ToArray();
    }

    private void PingAsset(string assetPath)
    {
        UnityEngine.Object asset =
            AssetDatabase.LoadMainAssetAtPath(assetPath);

        if (asset == null)
            return;

        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }

    private void ExportFilteredPaths()
    {
        List<AssetInfo> filteredAssets = GetFilteredAssets();

        string savePath = EditorUtility.SaveFilePanel(
            "Export Used Asset Paths",
            Application.dataPath,
            "UsedAssetsReport",
            "txt"
        );

        if (string.IsNullOrEmpty(savePath))
            return;

        List<string> lines = new()
        {
            "Path\tCategory\tType\tSize"
        };

        foreach (AssetInfo assetInfo in filteredAssets)
        {
            lines.Add(
                $"{assetInfo.Path}\t{assetInfo.Category}\t{assetInfo.TypeName}\t{assetInfo.SizeBytes} bytes"
            );
        }

        File.WriteAllLines(savePath, lines);

        EditorUtility.RevealInFinder(savePath);

        Debug.Log(
            $"[Used Assets Finder] Exported {filteredAssets.Count:N0} assets to:\n{savePath}"
        );
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
            return "0 B";

        string[] units = { "B", "KB", "MB", "GB", "TB" };

        double size = bytes;
        int unitIndex = 0;

        while (size >= 1024d && unitIndex < units.Length - 1)
        {
            size /= 1024d;
            unitIndex++;
        }

        return $"{size:0.##} {units[unitIndex]}";
    }
}
