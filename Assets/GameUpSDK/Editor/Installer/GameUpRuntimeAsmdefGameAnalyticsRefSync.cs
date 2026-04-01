using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace GameUpSDK.Installer
{
    /// <summary>
    /// Giữ reference tới assembly <c>GameAnalyticsSDK</c> khớp với file <c>GameAnalyticsSDK.asmdef</c> thực tế trong project.
    /// Khi cài GameUp qua Git/UPM không kèm GameAnalytics, hoặc import GA sau (GUID .meta khác bản dev), Unity không còn báo missing asmdef ref.
    /// </summary>
    [InitializeOnLoad]
    internal static class GameUpRuntimeAsmdefGameAnalyticsRefSync
    {
        private const string RuntimeAsmdefAssetPath = "Assets/GameUpSDK/Scripts/GameUpSDK.Runtime.asmdef";

        /// <summary>GUID từng commit trong repo; xóa khỏi references nếu còn sót.</summary>
        private const string LegacyGameAnalyticsAsmdefGuid = "e7a3f2b81d5c4a9e8f1b2c3d4e5f6789";

        static GameUpRuntimeAsmdefGameAnalyticsRefSync()
        {
            EditorApplication.delayCall += RunWhenEditorIdle;
            CompilationPipeline.compilationFinished += _ => EditorApplication.delayCall += RunWhenEditorIdle;
        }

        [MenuItem("GameUp SDK/Sync Runtime asmdef (GameAnalytics ref)", priority = 22)]
        private static void MenuSyncNow()
        {
            try
            {
                if (SyncGameAnalyticsReference())
                    Debug.Log("[GameUp] Sync Runtime asmdef (GameAnalytics ref): updated.");
                else
                    Debug.Log("[GameUp] Sync Runtime asmdef (GameAnalytics ref): already up to date.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GameUp] Sync Runtime asmdef (GameAnalytics ref) failed: " + e);
            }
        }

        private static void RunWhenEditorIdle()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += RunWhenEditorIdle;
                return;
            }

            try
            {
                SyncGameAnalyticsReference();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GameUp] Auto-sync Runtime asmdef (GameAnalytics ref): " + e.Message);
            }
        }

        /// <returns>true nếu đã ghi file.</returns>
        internal static bool SyncGameAnalyticsReference()
        {
            string dataPath = Application.dataPath;
            if (string.IsNullOrEmpty(dataPath))
                return false;

            string fullPath = Path.Combine(dataPath, "GameUpSDK", "Scripts", "GameUpSDK.Runtime.asmdef");
            if (!File.Exists(fullPath))
                return false;

            string json = File.ReadAllText(fullPath);
            object parsedRoot = SimpleJsonHelper.ParseObject(json);
            if (parsedRoot is not Dictionary<string, object> root)
                return false;

            if (!root.TryGetValue("references", out var refsObj))
                return false;
            if (refsObj is not List<object> rawList)
                return false;

            var refs = rawList.Select(o => o?.ToString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();

            string desiredGaRef = null;
            if (TryGetGameAnalyticsSdkAsmdefGuid(out string gaGuid))
                desiredGaRef = "GUID:" + gaGuid;

            // Gỡ mọi reference trỏ tới GameAnalyticsSDK.asmdef (GUID cũ / import mới) và GUID legacy.
            for (int i = refs.Count - 1; i >= 0; i--)
            {
                var r = refs[i];
                if (!r.StartsWith("GUID:", StringComparison.OrdinalIgnoreCase))
                    continue;
                var id = r.Substring("GUID:".Length).Trim();
                if (id.Equals(LegacyGameAnalyticsAsmdefGuid, StringComparison.OrdinalIgnoreCase))
                {
                    refs.RemoveAt(i);
                    continue;
                }

                string path = AssetDatabase.GUIDToAssetPath(id);
                if (path.EndsWith("GameAnalyticsSDK.asmdef", StringComparison.OrdinalIgnoreCase))
                    refs.RemoveAt(i);
            }

            if (!string.IsNullOrEmpty(desiredGaRef) && !refs.Contains(desiredGaRef))
                refs.Add(desiredGaRef);

            var newRefObjects = refs.Cast<object>().ToList();
            root["references"] = newRefObjects;

            string newJson = SimpleJsonHelper.Serialize(root) + Environment.NewLine;
            if (string.Equals(json.Replace("\r\n", "\n").TrimEnd(), newJson.Replace("\r\n", "\n").TrimEnd(),
                    StringComparison.Ordinal))
                return false;

            File.WriteAllText(fullPath, newJson);
            AssetDatabase.ImportAsset(RuntimeAsmdefAssetPath, ImportAssetOptions.ForceUpdate);
            return true;
        }

        private static bool TryGetGameAnalyticsSdkAsmdefGuid(out string guid)
        {
            guid = null;
            foreach (var g in AssetDatabase.FindAssets("t:asmdef"))
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                if (path.EndsWith("/GameAnalyticsSDK.asmdef", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith("\\GameAnalyticsSDK.asmdef", StringComparison.OrdinalIgnoreCase))
                {
                    guid = g;
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Unity chỉ gọi <see cref="OnPostprocessAllAssets"/> đáng tin khi class kế thừa <see cref="AssetPostprocessor"/>.</summary>
    internal sealed class GameUpRuntimeAsmdefGameAnalyticsAssetProcessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            bool touched = false;
            if (importedAssets != null)
                touched |= importedAssets.Any(p =>
                    p.EndsWith("GameAnalyticsSDK.asmdef", StringComparison.OrdinalIgnoreCase));
            if (deletedAssets != null)
                touched |= deletedAssets.Any(p =>
                    p.EndsWith("GameAnalyticsSDK.asmdef", StringComparison.OrdinalIgnoreCase));
            if (movedAssets != null)
                touched |= movedAssets.Any(p =>
                    p.EndsWith("GameAnalyticsSDK.asmdef", StringComparison.OrdinalIgnoreCase));

            if (!touched)
                return;

            // Ghi asmdef ngay trong pipeline import để reference có trước khi recompile / auto-sync define bật GAMEANALYTICS_*.
            try
            {
                GameUpRuntimeAsmdefGameAnalyticsRefSync.SyncGameAnalyticsReference();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GameUp] Sync Runtime asmdef after GA asmdef change: " + e.Message);
            }
        }
    }
}
