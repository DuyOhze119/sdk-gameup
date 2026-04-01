using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameUpSDK.Installer
{
    /// <summary>
    /// Bản GameAnalytics cài bằng .unitypackage đôi khi không kèm <c>GameAnalyticsSDK.asmdef</c>, khiến script nằm trong
    /// <c>Assembly-CSharp</c> và <c>GameUpSDK.Runtime</c> không thể reference. Đặt asmdef đúng chỗ (cha của
    /// <c>Scripts/</c> và <c>Playmaker/</c> runtime) theo layout chuẩn GA — xem
    /// <see href="https://docs.gameanalytics.com/event-tracking-and-integrations/sdks-and-collection-api/game-engine-sdks/unity/">GameAnalytics Unity</see>.
    /// </summary>
    internal static class GameUpGameAnalyticsAsmdefBootstrap
    {
        private const string RuntimeAsmdefAssetPath = "Assets/GameAnalytics/Plugins/GameAnalyticsSDK.asmdef";
        private const string MarkerScriptPath = "Assets/GameAnalytics/Plugins/Scripts/GameAnalytics.cs";

        private const string RuntimeAsmdefJson =
            "{\n" +
            "    \"name\": \"GameAnalyticsSDK\",\n" +
            "    \"rootNamespace\": \"GameAnalyticsSDK\",\n" +
            "    \"references\": [],\n" +
            "    \"includePlatforms\": [],\n" +
            "    \"excludePlatforms\": [],\n" +
            "    \"allowUnsafeCode\": false,\n" +
            "    \"overrideReferences\": false,\n" +
            "    \"precompiledReferences\": [],\n" +
            "    \"autoReferenced\": true,\n" +
            "    \"defineConstraints\": [],\n" +
            "    \"versionDefines\": [],\n" +
            "    \"noEngineReferences\": false\n" +
            "}\n";

        [MenuItem("GameUp SDK/Ensure GameAnalytics runtime asmdef", priority = 23)]
        private static void MenuEnsure()
        {
            if (TryEnsureRuntimeAsmdef(out string message))
                Debug.Log("[GameUp] " + message);
            else
                Debug.LogWarning("[GameUp] " + message);
        }

        /// <summary>
        /// Tạo <c>GameAnalyticsSDK.asmdef</c> tại <c>Assets/GameAnalytics/Plugins/</c> nếu đã có script GA chuẩn nhưng thiếu asmdef.
        /// </summary>
        internal static bool TryEnsureRuntimeAsmdef(out string message)
        {
            message = null;
            string dataPath = Application.dataPath;
            if (string.IsNullOrEmpty(dataPath))
            {
                message = "Ensure GameAnalytics asmdef: Application.dataPath is empty.";
                return false;
            }

            string markerFull = Path.Combine(dataPath, "GameAnalytics", "Plugins", "Scripts", "GameAnalytics.cs");
            if (!File.Exists(markerFull))
            {
                message =
                    "Ensure GameAnalytics asmdef: không thấy " + MarkerScriptPath + ". Import GameAnalytics SDK hoặc dùng layout tương tự (Plugins/Scripts/GameAnalytics.cs).";
                return false;
            }

            string asmdefFull = Path.Combine(dataPath, "GameAnalytics", "Plugins", "GameAnalyticsSDK.asmdef");
            if (File.Exists(asmdefFull))
            {
                message = "GameAnalytics runtime asmdef đã tồn tại: " + RuntimeAsmdefAssetPath;
                return true;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(asmdefFull) ?? "");
            File.WriteAllText(asmdefFull, RuntimeAsmdefJson);
            AssetDatabase.ImportAsset(RuntimeAsmdefAssetPath, ImportAssetOptions.ForceUpdate);
            message =
                "Đã tạo " + RuntimeAsmdefAssetPath + ". GameUpSDK.Runtime tham chiếu assembly tên GameAnalyticsSDK — hãy đợi Unity recompile.";
            return true;
        }
    }
}
