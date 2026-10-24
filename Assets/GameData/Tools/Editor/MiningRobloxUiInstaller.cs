#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace MiningSimulator.Ores.EditorTools
{
    /// <summary>Explicit authoring only: no startup hooks, no save/progression mutations.</summary>
    public static class MiningRobloxUiInstaller
    {
        private const string Fonts = "Assets/Art/UI/Fonts/";

        [MenuItem("Tools/Mining Simulator/UI/Apply Stats and Shop with Fredoka Nunito")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before authoring UI.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var stats = UnityEngine.Object.FindObjectsByType<MiningPlayerStatsPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(x => x.gameObject.scene == scene).ToArray();
            var shops = UnityEngine.Object.FindObjectsByType<MiningShopPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(x => x.gameObject.scene == scene).ToArray();
            if (stats.Length != 1 || shops.Length != 1)
                throw new InvalidOperationException("Expected exactly one Stats presenter and one Shop in the active scene.");

            var heading = EnsureFont("Fredoka-SemiBold");
            var bodyFont = EnsureFont("Nunito-SemiBold");
            if (heading.fallbackFontAssetTable == null)
                heading.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
            if (!heading.fallbackFontAssetTable.Contains(bodyFont))
                heading.fallbackFontAssetTable.Add(bodyFont);
            EditorUtility.SetDirty(heading);
            InvokeStyle(typeof(MiningStatsUiStyleMenu), "target", stats[0], heading);
            InvokeStyle(typeof(MiningSimulator.Ores.Editor.MiningShopUiStyleMenu), "shop", shops[0], heading);
            var statsObject = new SerializedObject(stats[0]);
            var shopObject = new SerializedObject(shops[0]);
            // Author closed: runtime coordinator remains the sole owner of panel visibility.
            ((RectTransform)statsObject.FindProperty("panel").objectReferenceValue).gameObject.SetActive(false);
            ((RectTransform)shopObject.FindProperty("panelRoot").objectReferenceValue).gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Stats and Shop styled with Fredoka/Nunito. Scene left unsaved for review.");
        }

        private static void InvokeStyle(Type type, string field, UnityEngine.Object target, TMP_FontAsset font)
        {
            var window = ScriptableObject.CreateInstance(type);
            try
            {
                type.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(window, target);
                type.GetField("font", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(window, font);
                type.GetMethod("Apply", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(window, null);
            }
            finally { UnityEngine.Object.DestroyImmediate(window); }
        }

        private static TMP_FontAsset EnsureFont(string name)
        {
            string path = Fonts + name + " SDF.asset";
            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (asset != null) return asset;
            var source = AssetDatabase.LoadAssetAtPath<Font>(Fonts + name + ".ttf");
            if (source == null) throw new InvalidOperationException("Missing supplied font " + name);
            asset = TMP_FontAsset.CreateFontAsset(source, 64, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (asset == null) throw new InvalidOperationException("TMP font creation failed for " + name);
            asset.name = name + " SDF";
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            foreach (var texture in asset.atlasTextures) AssetDatabase.AddObjectToAsset(texture, asset);
            var chars = new System.Text.StringBuilder();
            for (int c = 32; c <= 383; c++) chars.Append((char)c);
            for (int c = 0x1EA0; c <= 0x1EF9; c++) chars.Append((char)c);
            chars.Append("•–—×−…");
            asset.TryAddCharacters(chars.ToString(), out string missing);
            EditorUtility.SetDirty(asset);
            return asset;
        }
    }
}
#endif
