using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEngine;

namespace GameUpSDK.Installer
{
    /// <summary>
    /// Auto-sync Scripting Define Symbols dựa trên các assemblies đang có trong project.
    /// Mục tiêu: sau khi pull/update từ git hoặc import deps thủ công, symbols vẫn tự cập nhật
    /// mà không cần mở GameUpDependenciesWindow.
    /// </summary>
    [InitializeOnLoad]
    internal static class GameUpDefineSymbolsAutoSync
    {
        private static readonly BuildTargetGroup[] BuildTargetGroups =
        {
            BuildTargetGroup.Android,
            BuildTargetGroup.iOS,
            BuildTargetGroup.Standalone,
        };

        private const string LevelPlayDepsDefine = "LEVELPLAY_DEPENDENCIES_INSTALLED";
        private const string AdMobDepsDefine = "ADMOB_DEPENDENCIES_INSTALLED";
        private const string FirebaseDepsDefine = "FIREBASE_DEPENDENCIES_INSTALLED";
        private const string AppsFlyerDepsDefine = "APPSFLYER_DEPENDENCIES_INSTALLED";
        private const string GameAnalyticsDepsDefine = "GAMEANALYTICS_DEPENDENCIES_INSTALLED";

        private const string SessionThrottleKey = "GameUpSDK_DefinesAutoSync_Throttled";

        static GameUpDefineSymbolsAutoSync()
        {
            // Unity load → schedule 1 lần (đợi domain ổn định)
            EditorApplication.delayCall += TrySyncSoon;

            // Khi compile xong (import package / pull git thường gây recompile)
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
            CompilationPipeline.compilationFinished += OnCompilationFinished;

            // Khi UPM packages thay đổi (nếu deps được cài bằng UPM)
            Events.registeredPackages -= OnRegisteredPackages;
            Events.registeredPackages += OnRegisteredPackages;
        }

        [MenuItem("GameUp SDK/Sync Define Symbols", priority = 21)]
        private static void MenuSyncNow()
        {
            try
            {
                SyncDefines();
                Debug.Log("[GameUp] Sync Define Symbols: done.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GameUp] Sync Define Symbols failed: " + e);
            }
        }

        private static void OnCompilationFinished(object _)
        {
            TrySyncSoon();
        }

        private static void OnRegisteredPackages(PackageRegistrationEventArgs _)
        {
            TrySyncSoon();
        }

        private static void TrySyncSoon()
        {
            // Throttle trong cùng session để tránh loop khi SetDefine trigger recompile.
            if (SessionState.GetBool(SessionThrottleKey, false))
                return;

            SessionState.SetBool(SessionThrottleKey, true);
            EditorApplication.delayCall += () =>
            {
                // Cho phép chạy lại sau 1 nhịp nếu có sự kiện tiếp theo
                SessionState.SetBool(SessionThrottleKey, false);
                if (EditorApplication.isCompiling)
                {
                    // Nếu vẫn đang compile, thử lại ở tick sau.
                    EditorApplication.delayCall += TrySyncSoon;
                    return;
                }

                try
                {
                    SyncDefines();
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[GameUp] Auto-sync define symbols failed: " + e.Message);
                }
            };
        }

        private static void SyncDefines()
        {
            EnsurePrimaryMediationDefines();

            bool levelPlayInstalled = IsAssemblyLoaded("Unity.LevelPlay");
            bool admobInstalled = IsAssemblyLoaded("GoogleMobileAds");
            bool firebaseInstalled = IsAssemblyLoaded("Firebase.App");
            bool appsFlyerInstalled = IsAssemblyLoaded("AppsFlyer");
            bool gameAnalyticsInstalled = GameUpDependenciesWindow.IsGameAnalyticsSdkPresent();

            SetDefine(LevelPlayDepsDefine, levelPlayInstalled);
            SetDefine(AdMobDepsDefine, admobInstalled);
            SetDefine(FirebaseDepsDefine, firebaseInstalled);
            SetDefine(AppsFlyerDepsDefine, appsFlyerInstalled);
            SetDefine(GameAnalyticsDepsDefine, gameAnalyticsInstalled);

            // Backward compat: bật khi có (Firebase hoặc AppsFlyer hoặc GameAnalytics) AND (AdMob hoặc LevelPlay)
            bool hasAnalytics = firebaseInstalled || appsFlyerInstalled || gameAnalyticsInstalled;
            bool hasMediation = admobInstalled || levelPlayInstalled;
            bool sdkEnabled = hasAnalytics && hasMediation;
            GameUpDependenciesWindow.SetDepsReadyDefine(sdkEnabled);
        }

        private static void EnsurePrimaryMediationDefines()
        {
            bool lp = HasDefine(GUDefinetion.PrimaryMediationLevelPlay);
            bool admob = HasDefine(GUDefinetion.PrimaryMediationAdMob);
            if (!lp && !admob)
            {
                SetDefine(GUDefinetion.PrimaryMediationLevelPlay, true);
                return;
            }

            // Nếu lỡ có cả 2, ưu tiên giữ AdMob (giống logic window).
            if (lp && admob)
                SetDefine(GUDefinetion.PrimaryMediationLevelPlay, false);
        }

        private static bool IsAssemblyLoaded(string assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyName)) return false;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Equals(asm.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool HasDefine(string define)
        {
            string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
            return !string.IsNullOrEmpty(symbols) && symbols.Contains(define);
        }

        private static void SetDefine(string define, bool enabled)
        {
            foreach (var group in BuildTargetGroups)
            {
                try
                {
                    string current = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
                    var list = new List<string>(
                        current.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

                    bool changed = false;
                    if (enabled && !list.Contains(define))
                    {
                        list.Add(define);
                        changed = true;
                    }
                    else if (!enabled && list.Remove(define))
                    {
                        changed = true;
                    }

                    if (!changed)
                        continue;

                    // Remove duplicates & normalize order for stability across machines.
                    var normalized = list
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s.Trim())
                        .Distinct()
                        .ToList();

                    PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", normalized));
                }
                catch
                {
                    // group không tồn tại trong project này, bỏ qua
                }
            }
        }
    }
}

