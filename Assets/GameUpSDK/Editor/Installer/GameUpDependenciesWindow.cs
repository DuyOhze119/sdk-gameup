using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;

namespace GameUpSDK.Installer
{
    [InitializeOnLoad]
    internal static class GameUpDependenciesWindowAutoRefresh
    {
        private static bool s_hooked;

        static GameUpDependenciesWindowAutoRefresh()
        {
            Hook();
        }

        private static void Hook()
        {
            if (s_hooked) return;
            s_hooked = true;

            AssemblyReloadEvents.afterAssemblyReload -= RefreshAllOpenWindows;
            AssemblyReloadEvents.afterAssemblyReload += RefreshAllOpenWindows;
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
        }

        private static void OnCompilationFinished(object _)
        {
            // compilationFinished có thể bắn trước khi UI repaint; delay để state ổn định rồi mới scan assemblies.
            EditorApplication.delayCall += RefreshAllOpenWindows;
        }

        private static void RefreshAllOpenWindows()
        {
            EditorApplication.delayCall += () =>
            {
                var wins = Resources.FindObjectsOfTypeAll<GameUpDependenciesWindow>();
                if (wins == null || wins.Length == 0) return;
                foreach (var w in wins)
                {
                    if (w == null) continue;
                    w.ForceRefreshFromExternalEvent();
                }
            };
        }
    }

    /// <summary>
    /// Cửa sổ hướng dẫn cài đặt tất cả package phụ thuộc của GameUp SDK.
    /// Tự động xuất hiện khi SDK được cài lần đầu tiên qua Git URL Package.
    /// </summary>
    public class GameUpDependenciesWindow : EditorWindow
    {
        // ─── Định nghĩa các package phụ thuộc ────────────────────────────────────

        private enum InstallMethod
        {
            /// <summary>Cài qua Unity Package Manager bằng Git URL</summary>
            GitUrl,

            /// <summary>Cài qua scoped registry trong manifest.json</summary>
            ScopedRegistry,

            /// <summary>Import .unitypackage đã được bundle trong thư mục Packages~</summary>
            UnityPackage,

            /// <summary>Chỉ mở trang web — cài thủ công</summary>
            OpenUrl,
        }

        private class PackageDef
        {
            public string DisplayName;
            public string Description;
            public bool Required;

            /// <summary>Tên assembly để detect xem package đã cài chưa</summary>
            public string AssemblyName;

            public InstallMethod Method;

            // Git URL (dùng khi Method == GitUrl)
            public string GitUrl;

            // Scoped registry (dùng khi Method == ScopedRegistry)
            public string RegistryName;
            public string RegistryUrl;
            public string[] RegistryScopes;
            public string PackageId;

            /// <summary>
            /// Danh sách file .unitypackage trong thư mục Packages~.
            /// Hỗ trợ subfolder: vd "Firebase/FirebaseAnalytics.unitypackage".
            /// Tất cả file sẽ được import theo thứ tự.
            /// </summary>
            public string[] BundledFileNames;

            /// <summary>
            /// URL để tải từng file tương ứng với BundledFileNames.
            /// Dùng khi file không có trong Packages~ (vd: cài từ .unitypackage).
            /// Index phải khớp 1-1 với BundledFileNames.
            /// </summary>
            public string[] HostedUrls;

            // URL trang tải thủ công (fallback cuối khi cả local lẫn hosted URL đều thất bại)
            public string DownloadUrl;
            public string DownloadLabel;

            // ── Runtime state ──
            public bool IsInstalled;
            public bool IsInstalling;
            public string InstallError;
        }

        // ─── Thay đổi URL ở đây khi cập nhật phiên bản SDK ─────────────────────────
        // Đặt file vào Assets/GameUpSDK/Packages~/ để dùng local (Git URL install).
        // Nếu không có file local, installer tự download từ HostedUrls (unitypackage install).

        private static readonly PackageDef[] s_packages =
        {
            new PackageDef
            {
                DisplayName = "Firebase SDK  (Analytics + Crashlytics + Remote Config)",
                Description = "Bắt buộc. Analytics, crash reporting, remote configuration. Bao gồm EDM4U.",
                Required = false,
                AssemblyName = "Firebase.App",
                Method = InstallMethod.UnityPackage,
                BundledFileNames = new[]
                {
                    "Firebase/FirebaseAnalytics.unitypackage",
                    "Firebase/FirebaseCrashlytics.unitypackage",
                    "Firebase/FirebaseRemoteConfig.unitypackage",
                },
                HostedUrls = new[]
                {
                    "https://github.com/DuyOhze119/sdk-gameup/releases/download/deps/FirebaseAnalytics.unitypackage",
                    "https://github.com/DuyOhze119/sdk-gameup/releases/download/deps/FirebaseCrashlytics.unitypackage",
                    "https://github.com/DuyOhze119/sdk-gameup/releases/download/deps/FirebaseRemoteConfig.unitypackage",
                },
                DownloadUrl = "https://firebase.google.com/docs/unity/setup",
                DownloadLabel = "Tải Firebase Unity SDK →",
            },
            new PackageDef
            {
                DisplayName = "Google Mobile Ads — AdMob",
                Description = "Bắt buộc nếu dùng AdMob standalone (Interstitial/Rewarded/AppOpen) hoặc muốn bắt paid event để log ad_impression.",
                Required = false,
                AssemblyName = "GoogleMobileAds",
                Method = InstallMethod.UnityPackage,
                BundledFileNames = new[] { "GoogleMobileAds-v10.7.0.unitypackage" },
                HostedUrls = new[]
                {
                    "https://github.com/DuyOhze119/sdk-gameup/releases/download/deps/GoogleMobileAds-v10.7.0.unitypackage",
                },
                DownloadUrl = "https://github.com/googlesamples/unity-admob-sdk/releases",
                DownloadLabel = "Tải AdMob Plugin →",
            },
            new PackageDef
            {
                DisplayName = "IronSource LevelPlay SDK",
                Description = "Tùy chọn. Cần nếu bạn chọn Primary Mediation = LevelPlay trong AdsManager.",
                Required = false,
                AssemblyName = "Unity.LevelPlay",
                Method = InstallMethod.UnityPackage,
                BundledFileNames = new[] { "UnityLevelPlay_v9.2.0.unitypackage" },
                HostedUrls = new[]
                {
                    "https://github.com/DuyOhze119/sdk-gameup/releases/download/deps/UnityLevelPlay_v9.2.0.unitypackage",
                },
                DownloadUrl = "https://developers.is.com/ironsource-mobile/unity/unity-plugin/",
                DownloadLabel = "Tải IronSource SDK →",
            },
            new PackageDef
            {
                // Firebase gồm 3 file riêng trong subfolder Firebase/
                // EDM4U (Google.VersionHandler) được bundle kèm trong FirebaseAnalytics
                DisplayName      = "AppsFlyer Attribution SDK",
                Description      = "Tùy chọn. Mobile measurement & attribution.",
                Required         = false,
                AssemblyName     = "AppsFlyer",
                Method           = InstallMethod.UnityPackage,
                BundledFileNames = new[] { "appsflyer-unity-plugin-6.17.81.unitypackage" },
                HostedUrls       = new[]
                {
                    "https://github.com/DuyOhze119/sdk-gameup/releases/download/deps/appsflyer-unity-plugin-6.17.81.unitypackage",
                },
                DownloadUrl      = "https://github.com/AppsFlyerSDK/appsflyer-unity-plugin/releases",
                DownloadLabel    = "Tải AppsFlyer SDK →",
            },

            new PackageDef
            {
                DisplayName      = "Admob Mediation Adapter (Unity + Ironsource)",
                Description      = "Dùng khi sử dụng Admob Mediation",
                Required         = false,
                AssemblyName     = "GoogleMobileAds.Mediation.IronSource.Api",
                Method           = InstallMethod.UnityPackage,
                BundledFileNames = new[]
                {
                    "GoogleMobileAdsUnityAdsMediation.unitypackage",
                    "GoogleMobileAdsIronSourceMediation.unitypackage",
                },
                HostedUrls       = new[]
                {
                    "https://github.com/haopro2911/repo-sdk-importer/releases/download/sdk/GoogleMobileAdsUnityAdsMediation.unitypackage",
                    "https://github.com/haopro2911/repo-sdk-importer/releases/download/sdk/GoogleMobileAdsIronSourceMediation.unitypackage",
                },
                DownloadUrl      = "https://firebase.google.com/docs/unity/setup",
                DownloadLabel    = "Admob Mediation Adapter →",
            },
        };


        // ─── State ────────────────────────────────────────────────────────────────

        private Vector2 _scroll;
        private bool _isRefreshing;
        private bool _isBatchInstalling;

        /// <summary>Package sẽ cài trong lần batch hiện tại (null = toàn bộ s_packages — chỉ dùng nội bộ).</summary>
        private List<PackageDef> _batchScope;
        private bool _wasCompiling;

        // Queue PackageManager (GitUrl / ScopedRegistry)
        private readonly Queue<PackageDef> _installQueue = new Queue<PackageDef>();
        private AddRequest _currentAddRequest;
        private PackageDef _currentInstallingPackage;

        // ── Parallel download state ──
        private class DownloadTask
        {
            public PackageDef Pkg;
            public string FileName;
            public string TempPath;
            public UnityWebRequest Request;
            public bool IsDone;
            public bool HasError;
            public string ErrorMessage;
        }

        private List<DownloadTask> _parallelTasks;
        private Action _parallelDoneCallback;
        private float _downloadProgress;
        private string _downloadStatus;

        // Kept for backward compat with OnDisable / PollDownloadQueue references — sẽ không dùng nữa
        private UnityWebRequest _activeDownload;
        private PackageDef _downloadingPkg;
        private const string LevelPlayDepsDefine = "LEVELPLAY_DEPENDENCIES_INSTALLED";
        private const string AdMobDepsDefine = "ADMOB_DEPENDENCIES_INSTALLED";
        private const string FirebaseDepsDefine = "FIREBASE_DEPENDENCIES_INSTALLED";
        private const string AppsFlyerDepsDefine = "APPSFLYER_DEPENDENCIES_INSTALLED";

        // ─── Static helpers ───────────────────────────────────────────────────────

        [MenuItem("GameUp SDK/Setup Dependencies")]
        public static void ShowWindow()
        {
            var win = GetWindow<GameUpDependenciesWindow>(true, "GameUp SDK — Setup Dependencies");
            win.minSize = new Vector2(560, 520);
            // Đặt kích thước ban đầu nếu window chưa được mở
            if (win.position.width < 560)
                win.position = new Rect(win.position.x, win.position.y, 620, 580);
            win.RefreshStatus();
        }

        /// <summary>
        /// Kiểm tra nhanh (đồng bộ) xem tất cả package bắt buộc đã cài chưa.
        /// Dùng bởi GameUpPackageInstaller để quyết định có mở window không.
        /// </summary>
        public static bool AreAllRequiredPackagesInstalled()
        {
            return s_packages
                .Where(p => p.Required)
                .All(p => IsAssemblyLoaded(p.AssemblyName));
        }

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            RefreshStatus();
            _wasCompiling = EditorApplication.isCompiling;
            EditorApplication.update += EditorUpdateRepaintWhenBusy;

            // Khi đổi Scripting Define Symbols, Unity sẽ trigger compile + domain reload.
            // Rely vào _wasCompiling đôi khi miss edge (window bị recreate sau reload),
            // nên subscribe thêm các events này để luôn refresh UI/state sau khi compile/reload xong.
            AssemblyReloadEvents.afterAssemblyReload -= AfterAssemblyReloadRefresh;
            AssemblyReloadEvents.afterAssemblyReload += AfterAssemblyReloadRefresh;
            CompilationPipeline.compilationFinished -= OnCompilationFinishedRefresh;
            CompilationPipeline.compilationFinished += OnCompilationFinishedRefresh;
        }

        private void OnDisable()
        {
            EditorApplication.update -= EditorUpdateRepaintWhenBusy;
            EditorApplication.update -= PollInstallQueue;
            EditorApplication.update -= PollParallelDownloads;
            AssemblyReloadEvents.afterAssemblyReload -= AfterAssemblyReloadRefresh;
            CompilationPipeline.compilationFinished -= OnCompilationFinishedRefresh;
            if (_parallelTasks != null)
            {
                foreach (var t in _parallelTasks)
                    t.Request?.Dispose();
                _parallelTasks = null;
            }

            _activeDownload?.Dispose();
            _activeDownload = null;
        }

        private void AfterAssemblyReloadRefresh()
        {
            // DelayCall để đảm bảo assemblies đã available đầy đủ trước khi scan IsAssemblyLoaded.
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                RefreshStatus();
            };
        }

        private void OnCompilationFinishedRefresh(object _)
        {
            // compilationFinished có thể bắn khi window vừa được recreate,
            // nên chỉ cần schedule refresh + repaint an toàn.
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                RefreshStatus();
            };
        }

        internal void ForceRefreshFromExternalEvent()
        {
            RefreshStatus();
        }

        /// <summary>Làm mới UI khi đang compile hoặc đang cài để nút bật/tắt đúng lúc compile xong.</summary>
        private void EditorUpdateRepaintWhenBusy()
        {
            bool compiling = EditorApplication.isCompiling;

            // Compile vừa kết thúc → assemblies đã reload, refresh trạng thái package một lần.
            if (_wasCompiling && !compiling)
                RefreshStatus();

            _wasCompiling = compiling;

            if (compiling || IsInstallOrDownloadBusy())
                Repaint();
        }

        private bool IsInstallOrDownloadBusy()
        {
            if (_isBatchInstalling) return true;
            if (_installQueue.Count > 0) return true;
            if (_currentAddRequest != null) return true;
            if (_parallelTasks != null && _parallelTasks.Count > 0) return true;
            foreach (var p in s_packages)
            {
                if (p.IsInstalling) return true;
            }

            return false;
        }

        /// <summary>Khóa mọi thao tác: đang compile hoặc đang cài/tải package.</summary>
        private bool IsInteractionLocked()
        {
            return EditorApplication.isCompiling || IsInstallOrDownloadBusy();
        }

        // ─── GUI ─────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (EditorApplication.isCompiling)
            {
                EditorGUILayout.HelpBox(
                    "Unity đang compile — chờ xong rồi mới thao tác tiếp.",
                    MessageType.Info);
                EditorGUILayout.Space(4);
            }

            DrawHeader();
            DrawMediationInfo();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawPackageList();
            EditorGUILayout.EndScrollView();

            DrawFooter();
        }

        private static bool HasDefine(string define)
        {
            string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
            return symbols.Contains(define);
        }

        private static void SetDefine(string define, bool enabled)
        {
            foreach (var group in s_buildTargetGroups)
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

                    if (changed)
                    {
                        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", list));
                    }
                }
                catch { }
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(6);

            EditorGUILayout.LabelField(
                "GameUp SDK — Setup Dependencies",
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 });

            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Chọn Primary Mediation, rồi dùng nút \"Cài dependency\" trong khung Mediation để cài một lần: Firebase, AppsFlyer và bộ quảng cáo tương ứng (đã có thì bỏ qua).\n" +
                "Khi Unity đang compile hoặc đang cài package, các nút sẽ bị khóa cho tới khi xong.",
                MessageType.Info);

            EditorGUILayout.Space(6);
        }

        private void DrawMediationInfo()
        {
            EditorGUILayout.Space(6);

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("Mediation Settings", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(IsInteractionLocked());
            var current = GetPrimaryMediationFromDefines();
            var next = (AdsManager.PrimaryMediation)EditorGUILayout.EnumPopup("Primary Mediation", current);
            if (next != current)
            {
                SetPrimaryMediationDefines(next);
                RefreshStatus();
            }

            EditorGUI.EndDisabledGroup();

            var pm = GetPrimaryMediationFromDefines();
            var planned = GetPackagesForSdkSetup(pm);
            var missingAuto = planned.Where(p => !p.IsInstalled && CanAutoInstall(p)).ToList();
            var missingManual = planned.Where(p => !p.IsInstalled && !CanAutoInstall(p)).ToList();

            string planDesc = pm == AdsManager.PrimaryMediation.AdMob
                ? "Firebase, AppsFlyer, Google Mobile Ads, AdMob Mediation Adapters."
                : "Firebase, AppsFlyer, IronSource LevelPlay SDK.";

            EditorGUILayout.HelpBox(
                "Một lần bấm sẽ cài (nếu chưa có): " + planDesc,
                MessageType.None);

            if (missingManual.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    "Có package trong bộ này không cài tự động được (thiếu file trong Packages~ và không có URL tải). Cần tải thủ công theo mô tả từng mục trong danh sách bên dưới.",
                    MessageType.Warning);
            }

            EditorGUI.BeginDisabledGroup(IsInteractionLocked() || missingAuto.Count == 0);
            if (GUILayout.Button(
                    missingAuto.Count > 0
                        ? $"⬇ Cài dependency theo Primary Mediation ({missingAuto.Count} chưa có)"
                        : "✓ Đã đủ package (tự động) cho Primary Mediation",
                    GUILayout.Height(28)))
            {
                if (missingAuto.Count > 0)
                    StartBatchInstall(planned);
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.HelpBox(
                "Primary Mediation lưu bằng Scripting Define Symbols (`" + GUDefinetion.PrimaryMediationLevelPlay + "` / `" + GUDefinetion.PrimaryMediationAdMob + "`) — phù hợp khi GameUp SDK cài dạng UPM package (không tạo asset trong Assets/).",
                MessageType.Info
            );

            EditorGUILayout.EndVertical();
        }

        /// <summary>Firebase + AppsFlyer + bộ mediation theo lựa chọn (AdMob: GMA + adapters; LevelPlay: LevelPlay).</summary>
        private static List<PackageDef> GetPackagesForSdkSetup(AdsManager.PrimaryMediation mediation)
        {
            var list = new List<PackageDef>();

            void AddByAssembly(string assemblyName)
            {
                var p = s_packages.FirstOrDefault(x => x.AssemblyName == assemblyName);
                if (p != null && !list.Contains(p))
                    list.Add(p);
            }

            AddByAssembly("Firebase.App");
            AddByAssembly("AppsFlyer");

            if (mediation == AdsManager.PrimaryMediation.AdMob)
            {
                AddByAssembly("GoogleMobileAds");
                AddByAssembly("GoogleMobileAds.Mediation.IronSource.Api");
            }
            else
            {
                AddByAssembly("Unity.LevelPlay");
            }

            return list;
        }

        private static AdsManager.PrimaryMediation GetPrimaryMediationFromDefines()
        {
            if (HasDefine(GUDefinetion.PrimaryMediationAdMob)) return AdsManager.PrimaryMediation.AdMob;
            return AdsManager.PrimaryMediation.LevelPlay;
        }

        private static void SetPrimaryMediationDefines(AdsManager.PrimaryMediation mediation)
        {
            SetDefine(GUDefinetion.PrimaryMediationAdMob, mediation == AdsManager.PrimaryMediation.AdMob);
            SetDefine(GUDefinetion.PrimaryMediationLevelPlay, mediation == AdsManager.PrimaryMediation.LevelPlay);
        }

        /// <summary>Đảm bảo có đúng một define mediation (mặc định LevelPlay nếu chưa có).</summary>
        private static void EnsurePrimaryMediationDefines()
        {
            bool lp = HasDefine(GUDefinetion.PrimaryMediationLevelPlay);
            bool admob = HasDefine(GUDefinetion.PrimaryMediationAdMob);
            if (!lp && !admob)
                SetDefine(GUDefinetion.PrimaryMediationLevelPlay, true);
            else if (lp && admob)
                SetDefine(GUDefinetion.PrimaryMediationAdMob, false);
        }

        private void DrawPackageList()
        {
            bool drewRequired = false, drewOptional = false;

            foreach (var pkg in s_packages)
            {
                // Section headers
                if (pkg.Required && !drewRequired)
                {
                    DrawSectionHeader("BẮT BUỘC");
                    drewRequired = true;
                }

                if (!pkg.Required && !drewOptional)
                {
                    EditorGUILayout.Space(8);
                    DrawSectionHeader("TÙY CHỌN");
                    drewOptional = true;
                }

                DrawPackageRow(pkg);
            }
        }

        private static void DrawSectionHeader(string title)
        {
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(4, 0, 6, 2),
            };
            EditorGUILayout.LabelField(title, style);
        }

        private void DrawPackageRow(PackageDef pkg)
        {
            bool isDownloading = _parallelTasks?.Any(t => t.Pkg == pkg && !t.IsDone) == true;
            bool isInstalling = pkg.IsInstalling
                                || (_isBatchInstalling && _installQueue.Contains(pkg))
                                || isDownloading;
            Color boxColor = pkg.IsInstalled ? new Color(0.18f, 0.45f, 0.18f, 0.3f)
                : isInstalling ? new Color(0.3f, 0.3f, 0.6f, 0.3f)
                : new Color(0.45f, 0.18f, 0.18f, 0.3f);

            // Row background
            var rect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, rect.height + 2), boxColor);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);

            // Status icon
            string icon = pkg.IsInstalled ? "✓" : isInstalling ? "⟳" : "✗";
            var iconStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = pkg.IsInstalled ? Color.green : isInstalling ? Color.yellow : Color.red },
                fixedWidth = 24,
            };
            GUILayout.Label(icon, iconStyle, GUILayout.Width(24));

            // Name + description
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(pkg.DisplayName, EditorStyles.boldLabel);
            var descStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            EditorGUILayout.LabelField(pkg.Description, descStyle);
            if (!string.IsNullOrEmpty(pkg.InstallError))
                EditorGUILayout.HelpBox(pkg.InstallError, MessageType.Error);
            EditorGUILayout.EndVertical();

            // Trạng thái (cài gom qua nút trong Mediation Settings)
            GUILayout.Space(4);
            if (pkg.IsInstalled)
            {
                var greenStyle = new GUIStyle(EditorStyles.miniLabel)
                { normal = { textColor = Color.green }, fontStyle = FontStyle.Bold };
                GUILayout.Label("Đã cài", greenStyle, GUILayout.Width(64));
            }
            else if (isDownloading)
            {
                var pkgTasks = _parallelTasks?.Where(t => t.Pkg == pkg).ToList();
                int total = pkgTasks?.Count ?? 0;
                int done = pkgTasks?.Count(t => t.IsDone) ?? 0;
                float prog = total > 0
                    ? pkgTasks.Average(t => t.IsDone ? 1f : t.Request?.downloadProgress ?? 0f)
                    : 0f;

                EditorGUILayout.BeginVertical(GUILayout.Width(190));
                EditorGUILayout.LabelField(
                    total > 1 ? $"Đang tải... {done}/{total} files" : "Đang tải...",
                    EditorStyles.miniLabel, GUILayout.Width(190));
                var barRect = EditorGUILayout.GetControlRect(GUILayout.Width(190), GUILayout.Height(6));
                EditorGUI.ProgressBar(barRect, prog, "");
                EditorGUILayout.EndVertical();
            }
            else if (isInstalling)
            {
                EditorGUILayout.LabelField("Đang cài...", GUILayout.Width(100));
            }
            else
            {
                var hint = new GUIStyle(EditorStyles.miniLabel)
                {
                    wordWrap = true,
                    normal = { textColor = new Color(0.55f, 0.55f, 0.55f) },
                };
                GUILayout.Label("← nút Cài dependency", hint, GUILayout.Width(118));
            }
            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(2);
        }

        private void DrawFooter()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();

            // Nút refresh thủ công luôn active.
            // Nếu đang compile thì delay để refresh sau khi compile/reload xong.
            if (GUILayout.Button("↻  Làm mới trạng thái", GUILayout.Height(30)))
                RequestManualRefresh();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Manual install hint
            bool hasManualUninstalled = s_packages.Any(p => !p.IsInstalled && p.Method == InstallMethod.OpenUrl);
            if (hasManualUninstalled)
            {
                EditorGUILayout.HelpBox(
                    "Một số package chỉ cài được thủ công: tải .unitypackage từ trang nhà cung cấp, " +
                    "rồi Assets → Import Package → Custom Package…, sau đó \"Làm mới trạng thái\".",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(4);

            // Continue button
            bool allRequiredDone = AreAllRequiredPackagesInstalled();
            if (allRequiredDone)
            {
                EditorGUILayout.HelpBox(
                    "Tất cả package bắt buộc đã được cài đặt! " +
                    "Nhấn bên dưới để mở cửa sổ cấu hình SDK.",
                    MessageType.None);

                // Khi đang compile/cài, không cho bấm (không trigger delay-call) để đúng luồng UX.
                EditorGUI.BeginDisabledGroup(IsInteractionLocked());
                if (GUILayout.Button("→  Mở cấu hình SDK (GameUp SDK Setup)", GUILayout.Height(36)))
                    RequestOpenSetup();
                EditorGUI.EndDisabledGroup();
            }
        }

        private void RequestManualRefresh()
        {
            if (EditorApplication.isCompiling)
            {
                EditorApplication.delayCall += () =>
                {
                    if (this == null) return;
                    RefreshStatus();
                };
                Repaint();
                return;
            }

            RefreshStatus();
        }

        private void RequestOpenSetup()
        {
            if (IsInteractionLocked())
            {
                EditorApplication.delayCall += () =>
                {
                    if (this == null) return;
                    RequestOpenSetup();
                };
                Repaint();
                return;
            }

            GameUpPackageInstaller.MarkSetupComplete();
            Close();
            EditorApplication.ExecuteMenuItem("GameUp SDK/Setup");
        }

        // ─── Install logic ────────────────────────────────────────────────────────

        private void StartInstall(PackageDef pkg)
        {
            if (pkg.IsInstalling) return;
            pkg.IsInstalling = true;
            pkg.InstallError = null;
            Repaint();

            switch (pkg.Method)
            {
                case InstallMethod.GitUrl:
                    EnqueueGitInstall(pkg);
                    break;

                case InstallMethod.ScopedRegistry:
                    AddScopedRegistryAndPackage(pkg);
                    break;
            }
        }

        private void StartBatchInstall(IReadOnlyList<PackageDef> scope)
        {
            _batchScope = scope != null && scope.Count > 0
                ? scope.Distinct().ToList()
                : s_packages.ToList();
            _isBatchInstalling = true;
            _installQueue.Clear();

            IEnumerable<PackageDef> InScope() => _batchScope;

            // 1) Import các UnityPackage đã có file local (đồng bộ, nhanh)
            foreach (var pkg in InScope())
            {
                if (pkg.IsInstalled) continue;
                if (pkg.Method != InstallMethod.UnityPackage) continue;

                var localPaths = GetBundledPackagePaths(pkg.BundledFileNames);
                if (localPaths == null) continue;

                pkg.InstallError = null;
                ImportUnityPackage(pkg, localPaths);
            }

            // 2) Cài GitUrl / ScopedRegistry (bất đồng bộ)
            foreach (var pkg in InScope())
            {
                if (pkg.IsInstalled) continue;
                if (pkg.Method != InstallMethod.GitUrl && pkg.Method != InstallMethod.ScopedRegistry) continue;

                pkg.InstallError = null;
                _installQueue.Enqueue(pkg);
            }

            // 3) Download + import song song các UnityPackage chưa có file local nhưng có HostedUrls
            var downloadPkgs = InScope()
                .Where(p => !p.IsInstalled
                            && p.Method == InstallMethod.UnityPackage
                            && GetBundledPackagePaths(p.BundledFileNames) == null
                            && p.HostedUrls?.Length > 0)
                .ToList();

            void FinishBatch()
            {
                _isBatchInstalling = false;
                _batchScope = null;
                RefreshStatus();
            }

            if (_installQueue.Count > 0)
            {
                // GitUrl chạy trước (bất đồng bộ), download song song sau khi xong
                ProcessNextInQueueThen(() =>
                {
                    if (downloadPkgs.Count > 0)
                        StartParallelDownloadAndImport(downloadPkgs, onAllDone: FinishBatch);
                    else
                        FinishBatch();
                });
            }
            else if (downloadPkgs.Count > 0)
            {
                // Chỉ có download → chạy ngay song song
                StartParallelDownloadAndImport(downloadPkgs, onAllDone: FinishBatch);
            }
            else
            {
                FinishBatch();
            }
        }

        private void EnqueueGitInstall(PackageDef pkg)
        {
            _installQueue.Clear();
            _installQueue.Enqueue(pkg);
            ProcessNextInQueue();
        }

        private Action _onQueueDone;

        private void ProcessNextInQueueThen(Action onDone)
        {
            _onQueueDone = onDone;
            ProcessNextInQueue();
        }

        private void ProcessNextInQueue()
        {
            if (_installQueue.Count == 0)
            {
                _currentInstallingPackage = null;
                _currentAddRequest = null;
                EditorApplication.update -= PollInstallQueue;

                var cb = _onQueueDone;
                _onQueueDone = null;
                if (cb != null) cb();
                else
                {
                    _isBatchInstalling = false;
                    _batchScope = null;
                    RefreshStatus();
                }

                return;
            }

            var pkg = _installQueue.Peek();
            _currentInstallingPackage = pkg;
            pkg.IsInstalling = true;
            Repaint();

            _currentAddRequest = Client.Add(pkg.GitUrl);
            EditorApplication.update += PollInstallQueue;
        }

        private void PollInstallQueue()
        {
            if (_currentAddRequest == null || !_currentAddRequest.IsCompleted) return;

            EditorApplication.update -= PollInstallQueue;

            var pkg = _currentInstallingPackage;
            if (pkg != null)
            {
                pkg.IsInstalling = false;

                if (_currentAddRequest.Status == StatusCode.Success)
                {
                    pkg.IsInstalled = true;
                    pkg.InstallError = null;
                }
                else
                {
                    pkg.InstallError = _currentAddRequest.Error?.message ?? "Cài thất bại.";
                }
            }

            _installQueue.Dequeue();
            _currentAddRequest = null;
            _currentInstallingPackage = null;

            ProcessNextInQueue();
            Repaint();
        }

        // ─── UnityPackage install ─────────────────────────────────────────────────

        /// <summary>
        /// Trả về danh sách đường dẫn tuyệt đối cho các file .unitypackage trong Packages~.
        /// Chỉ trả về file thực sự tồn tại. Trả về null nếu KHÔNG CÓ file nào.
        /// </summary>
        private static List<string> GetBundledPackagePaths(string[] fileNames)
        {
            if (fileNames == null || fileNames.Length == 0) return null;

            string folder = GetPackagesFolder();
            if (string.IsNullOrEmpty(folder)) return null;

            var found = new List<string>();
            foreach (string name in fileNames)
            {
                string full = Path.Combine(folder, name.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(full))
                    found.Add(full);
            }

            return found.Count > 0 ? found : null;
        }

        // Backward compat helper dùng nội bộ để check có ít nhất 1 file
        private static string GetBundledPackagePath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            string folder = GetPackagesFolder();
            if (string.IsNullOrEmpty(folder)) return null;
            string full = Path.Combine(folder, fileName.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(full) ? full : null;
        }

        /// <summary>
        /// Tìm thư mục Packages~ của package này.
        /// Hỗ trợ cả cài via UPM Git URL (resolvedPath) và .unitypackage (Assets/GameUpSDK).
        /// </summary>
        private static string GetPackagesFolder()
        {
            // Thử tìm qua PackageInfo khi cài via UPM
            try
            {
                System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
                Type pkgInfoType = Type.GetType("UnityEditor.PackageManager.PackageInfo, UnityEditor");
                if (pkgInfoType != null)
                {
                    MethodInfo findMethod = pkgInfoType.GetMethod(
                        "FindForAssembly",
                        BindingFlags.Static | BindingFlags.Public,
                        null, new[] { typeof(System.Reflection.Assembly) }, null);

                    object info = findMethod?.Invoke(null, new object[] { asm });
                    if (info != null)
                    {
                        string resolved = pkgInfoType.GetProperty("resolvedPath")
                            ?.GetValue(info) as string;
                        if (!string.IsNullOrEmpty(resolved))
                            return Path.Combine(resolved, "Packages~");
                    }
                }
            }
            catch
            {
            }

            // Fallback: cài via .unitypackage → scripts nằm ở Assets/GameUpSDK
            return Path.Combine(Application.dataPath, "GameUpSDK", "Packages~");
        }

        /// <summary>
        /// Import tất cả file .unitypackage của một package.
        /// interactive=false để không hiện dialog xác nhận cho từng file.
        /// </summary>
        private void ImportUnityPackage(PackageDef pkg, List<string> filePaths)
        {
            pkg.IsInstalling = true;
            pkg.InstallError = null;
            Repaint();

            var errors = new List<string>();
            foreach (string path in filePaths)
            {
                try
                {
                    AssetDatabase.ImportPackage(path, interactive: false);
                    Debug.Log($"[GameUpSDK] Imported: {Path.GetFileName(path)}");
                }
                catch (Exception ex)
                {
                    errors.Add($"{Path.GetFileName(path)}: {ex.Message}");
                    Debug.LogError($"[GameUpSDK] Import {Path.GetFileName(path)} thất bại: {ex.Message}");
                }
            }

            pkg.IsInstalling = false;
            if (errors.Count == 0)
            {
                pkg.IsInstalled = true;
                pkg.InstallError = null;
            }
            else
            {
                pkg.InstallError = "Một số file import thất bại:\n" + string.Join("\n", errors);
            }

            Repaint();
        }

        // ─── Parallel Download & Import ───────────────────────────────────────────

        /// <summary>Bắt đầu download song song + import một package đơn lẻ.</summary>
        private void StartDownloadAndImport(PackageDef pkg)
        {
            StartParallelDownloadAndImport(new List<PackageDef> { pkg }, onAllDone: null);
        }

        /// <summary>
        /// Tải tất cả file của tất cả packages cùng lúc (parallel).
        /// Khi toàn bộ download xong → import từng package theo nhóm → gọi onAllDone.
        /// </summary>
        private void StartParallelDownloadAndImport(List<PackageDef> pkgs, Action onAllDone)
        {
            if (_parallelTasks != null)
            {
                // Đang có download chạy, dừng lại
                foreach (var old in _parallelTasks) old.Request?.Dispose();
                EditorApplication.update -= PollParallelDownloads;
            }

            _parallelTasks = new List<DownloadTask>();
            _parallelDoneCallback = onAllDone;

            foreach (var pkg in pkgs)
            {
                if (pkg.HostedUrls == null || pkg.HostedUrls.Length == 0) continue;

                pkg.IsInstalling = true;
                pkg.InstallError = null;

                for (int i = 0; i < pkg.HostedUrls.Length; i++)
                {
                    string url = pkg.HostedUrls[i];
                    string fileName = pkg.BundledFileNames != null && i < pkg.BundledFileNames.Length
                        ? Path.GetFileName(pkg.BundledFileNames[i])
                        : $"{i}.unitypackage";
                    string tempPath = Path.Combine(Application.temporaryCachePath, fileName);

                    var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
                    req.downloadHandler = new DownloadHandlerFile(tempPath) { removeFileOnAbort = true };
                    req.SendWebRequest();

                    _parallelTasks.Add(new DownloadTask
                    {
                        Pkg = pkg,
                        FileName = fileName,
                        TempPath = tempPath,
                        Request = req,
                    });
                }
            }

            if (_parallelTasks.Count == 0)
            {
                _parallelTasks = null;
                onAllDone?.Invoke();
                return;
            }

            EditorApplication.update += PollParallelDownloads;
            Repaint();
        }

        private void PollParallelDownloads()
        {
            if (_parallelTasks == null) return;

            bool anyRunning = false;
            foreach (var task in _parallelTasks)
            {
                if (task.IsDone) continue;
                if (!task.Request.isDone)
                {
                    anyRunning = true;
                    continue;
                }

                // Request hoàn thành
                task.IsDone = true;
                if (task.Request.result != UnityWebRequest.Result.Success)
                {
                    task.HasError = true;
                    task.ErrorMessage = task.Request.error;
                }

                task.Request.Dispose();
                task.Request = null;
            }

            // Cập nhật overall progress
            float totalProgress = _parallelTasks.Sum(t =>
                t.IsDone ? 1f : t.Request?.downloadProgress ?? 0f);
            _downloadProgress = totalProgress / _parallelTasks.Count;
            int doneCount = _parallelTasks.Count(t => t.IsDone);
            _downloadStatus = $"Đang tải: {doneCount}/{_parallelTasks.Count} files";
            Repaint();

            if (anyRunning) return;

            // ─── Tất cả done → import theo nhóm package ───────────────────────
            EditorApplication.update -= PollParallelDownloads;

            // Group tasks by package
            var byPkg = _parallelTasks
                .GroupBy(t => t.Pkg)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var kv in byPkg)
            {
                PackageDef pkg = kv.Key;
                List<DownloadTask> tasks = kv.Value;
                var successPaths = tasks.Where(t => !t.HasError).Select(t => t.TempPath).ToList();
                var errorMsgs = tasks.Where(t => t.HasError)
                    .Select(t => $"{t.FileName}: {t.ErrorMessage}").ToList();

                pkg.IsInstalling = false;
                if (errorMsgs.Count > 0)
                    pkg.InstallError = "Download thất bại:\n" + string.Join("\n", errorMsgs);

                if (successPaths.Count > 0)
                    ImportUnityPackage(pkg, successPaths);
            }

            _parallelTasks = null;
            _downloadProgress = 0;
            _downloadStatus = null;

            var cb = _parallelDoneCallback;
            _parallelDoneCallback = null;
            cb?.Invoke();
        }

        private void AddScopedRegistryAndPackage(PackageDef pkg)
        {
            // Đọc manifest.json, thêm scoped registry + dependency, ghi lại
            string manifestPath = System.IO.Path.Combine(
                Application.dataPath, "..", "Packages", "manifest.json");

            try
            {
                string json = System.IO.File.ReadAllText(manifestPath);
                var manifest = SimpleJsonHelper.ParseObject(json);

                // Thêm scoped registry nếu chưa có
                if (!string.IsNullOrEmpty(pkg.RegistryUrl))
                {
                    if (!manifest.ContainsKey("scopedRegistries"))
                        manifest["scopedRegistries"] = new List<object>();

                    var registries = (List<object>)manifest["scopedRegistries"];
                    bool found = registries.OfType<Dictionary<string, object>>()
                        .Any(r => r.TryGetValue("url", out var u) && u?.ToString() == pkg.RegistryUrl);

                    if (!found)
                    {
                        registries.Add(new Dictionary<string, object>
                        {
                            ["name"] = pkg.RegistryName,
                            ["url"] = pkg.RegistryUrl,
                            ["scopes"] = pkg.RegistryScopes?.ToList<object>() ?? new List<object>(),
                        });
                    }
                }

                // Thêm dependency
                if (!manifest.ContainsKey("dependencies"))
                    manifest["dependencies"] = new Dictionary<string, object>();

                var deps = (Dictionary<string, object>)manifest["dependencies"];
                if (!deps.ContainsKey(pkg.PackageId))
                    deps[pkg.PackageId] = "latest";

                System.IO.File.WriteAllText(manifestPath, SimpleJsonHelper.Serialize(manifest));
                AssetDatabase.Refresh();

                pkg.IsInstalling = false;
                pkg.IsInstalled = true;
            }
            catch (Exception ex)
            {
                pkg.IsInstalling = false;
                pkg.InstallError = "Lỗi khi sửa manifest.json: " + ex.Message;
            }

            Repaint();
        }

        // ─── Scripting Define Symbol management ──────────────────────────────────

        private static readonly BuildTargetGroup[] s_buildTargetGroups =
        {
            BuildTargetGroup.Android,
            BuildTargetGroup.iOS,
            BuildTargetGroup.Standalone,
        };

        /// <summary>
        /// Thêm hoặc xóa define GAMEUP_SDK_DEPS_READY khỏi Player Settings.
        /// Khi define này tồn tại, GameUpSDK.Runtime và GameUpSDK.Editor sẽ được compile.
        /// </summary>
        internal static void SetDepsReadyDefine(bool enabled)
        {
            foreach (var group in s_buildTargetGroups)
            {
                try
                {
                    string current = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
                    var list = new List<string>(
                        current.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

                    bool changed = false;
                    if (enabled && !list.Contains(GUDefinetion.DepsReadyDefine))
                    {
                        list.Add(GUDefinetion.DepsReadyDefine);
                        changed = true;
                    }
                    else if (!enabled && list.Remove(GUDefinetion.DepsReadyDefine))
                    {
                        changed = true;
                    }

                    if (changed)
                        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", list));
                }
                catch
                {
                    /* group không tồn tại trong project này, bỏ qua */
                }
            }
        }

        internal static bool IsDepsReadyDefined()
        {
            string current = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
            return current.Contains(GUDefinetion.DepsReadyDefine);
        }

        // ─── Status refresh ───────────────────────────────────────────────────────

        private void RefreshStatus()
        {
            EnsurePrimaryMediationDefines();

            foreach (var pkg in s_packages)
            {
                pkg.IsInstalled = IsAssemblyLoaded(pkg.AssemblyName);
                pkg.IsInstalling = false;
                pkg.InstallError = null;
            }

            // Auto set/clear LevelPlay define theo trạng thái package
            bool levelPlayInstalled = IsAssemblyLoaded("Unity.LevelPlay");
            if (levelPlayInstalled && !HasDefine(LevelPlayDepsDefine))
                SetDefine(LevelPlayDepsDefine, true);
            else if (!levelPlayInstalled && HasDefine(LevelPlayDepsDefine))
                SetDefine(LevelPlayDepsDefine, false);

            // Auto set/clear AdMob define theo trạng thái package
            bool admobInstalled = IsAssemblyLoaded("GoogleMobileAds");
            if (admobInstalled && !HasDefine(AdMobDepsDefine))
                SetDefine(AdMobDepsDefine, true);
            else if (!admobInstalled && HasDefine(AdMobDepsDefine))
                SetDefine(AdMobDepsDefine, false);

            // Auto set/clear Firebase define theo trạng thái package
            bool firebaseInstalled = IsAssemblyLoaded("Firebase.App");
            if (firebaseInstalled && !HasDefine(FirebaseDepsDefine))
                SetDefine(FirebaseDepsDefine, true);
            else if (!firebaseInstalled && HasDefine(FirebaseDepsDefine))
                SetDefine(FirebaseDepsDefine, false);

            // Auto set/clear AppsFlyer define theo trạng thái package
            bool appsFlyerInstalled = IsAssemblyLoaded("AppsFlyer");
            if (appsFlyerInstalled && !HasDefine(AppsFlyerDepsDefine))
                SetDefine(AppsFlyerDepsDefine, true);
            else if (!appsFlyerInstalled && HasDefine(AppsFlyerDepsDefine))
                SetDefine(AppsFlyerDepsDefine, false);

            // Tự động set/clear define khi trạng thái thay đổi
            // GAMEUP_SDK_DEPS_READY chỉ còn ý nghĩa "SDK enabled" (backward compat).
            // Bật khi có (Firebase hoặc AppsFlyer) AND (AdMob hoặc LevelPlay).
            // Không dùng define này để include SDK bên thứ 3 nữa.
            bool hasAnalytics = firebaseInstalled || appsFlyerInstalled;
            bool hasMediation = admobInstalled || levelPlayInstalled;
            bool sdkEnabled = hasAnalytics && hasMediation;
            if (sdkEnabled && !IsDepsReadyDefined())
                SetDepsReadyDefine(true);
            else if (!sdkEnabled && IsDepsReadyDefined())
                SetDepsReadyDefine(false);

            Repaint();
        }

        private static bool CanAutoInstall(PackageDef p)
        {
            if (p.Method == InstallMethod.GitUrl || p.Method == InstallMethod.ScopedRegistry)
                return true;
            if (p.Method == InstallMethod.UnityPackage)
                return GetBundledPackagePaths(p.BundledFileNames) != null
                       || (p.HostedUrls?.Length > 0);
            return false;
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
    }

    // ─── Minimal JSON helper (không dùng Newtonsoft/JsonUtility để giữ assembly sạch) ───

    internal static class SimpleJsonHelper
    {
        public static Dictionary<string, object> ParseObject(string json)
        {
            // Dùng Unity built-in JsonUtility không hỗ trợ Dictionary,
            // nên parse thủ công phần dependencies/scopedRegistries cần thiết.
            // Thực tế: dùng regex-free approach với index tracking.
            json = json.Trim();
            int idx = 0;
            return (Dictionary<string, object>)ParseValue(json, ref idx);
        }

        private static object ParseValue(string s, ref int i)
        {
            SkipWhitespace(s, ref i);
            if (i >= s.Length) return null;

            char c = s[i];
            if (c == '{') return ParseObject(s, ref i);
            if (c == '[') return ParseArray(s, ref i);
            if (c == '"') return ParseString(s, ref i);
            if (c == 't')
            {
                i += 4;
                return true;
            }

            if (c == 'f')
            {
                i += 5;
                return false;
            }

            if (c == 'n')
            {
                i += 4;
                return null;
            }

            return ParseNumber(s, ref i);
        }

        private static Dictionary<string, object> ParseObject(string s, ref int i)
        {
            var dict = new Dictionary<string, object>();
            i++; // skip '{'
            SkipWhitespace(s, ref i);
            if (i < s.Length && s[i] == '}')
            {
                i++;
                return dict;
            }

            while (i < s.Length)
            {
                SkipWhitespace(s, ref i);
                string key = ParseString(s, ref i);
                SkipWhitespace(s, ref i);
                i++; // skip ':'
                object val = ParseValue(s, ref i);
                dict[key] = val;
                SkipWhitespace(s, ref i);
                if (i < s.Length && s[i] == ',')
                {
                    i++;
                    continue;
                }

                if (i < s.Length && s[i] == '}')
                {
                    i++;
                    break;
                }
            }

            return dict;
        }

        private static List<object> ParseArray(string s, ref int i)
        {
            var list = new List<object>();
            i++; // skip '['
            SkipWhitespace(s, ref i);
            if (i < s.Length && s[i] == ']')
            {
                i++;
                return list;
            }

            while (i < s.Length)
            {
                list.Add(ParseValue(s, ref i));
                SkipWhitespace(s, ref i);
                if (i < s.Length && s[i] == ',')
                {
                    i++;
                    continue;
                }

                if (i < s.Length && s[i] == ']')
                {
                    i++;
                    break;
                }
            }

            return list;
        }

        private static string ParseString(string s, ref int i)
        {
            i++; // skip opening '"'
            var sb = new System.Text.StringBuilder();
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') break;
                if (c == '\\' && i < s.Length)
                {
                    char esc = s[i++];
                    switch (esc)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        default: sb.Append(esc); break;
                    }
                }
                else sb.Append(c);
            }

            return sb.ToString();
        }

        private static object ParseNumber(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '-' || s[i] == '.' || s[i] == 'e' || s[i] == 'E' ||
                                    s[i] == '+'))
                i++;
            string num = s.Substring(start, i - start);
            if (int.TryParse(num, out int iv)) return iv;
            if (double.TryParse(num, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double dv)) return dv;
            return num;
        }

        private static void SkipWhitespace(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }

        public static string Serialize(object obj, int indent = 0)
        {
            string pad = new string(' ', indent * 2);
            string pad1 = new string(' ', (indent + 1) * 2);

            switch (obj)
            {
                case null: return "null";
                case bool b: return b ? "true" : "false";
                case int iv: return iv.ToString();
                case long lv: return lv.ToString();
                case double dv:
                    return dv.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case string sv:
                    return "\"" + sv.Replace("\\", "\\\\").Replace("\"", "\\\"")
                        .Replace("\n", "\\n").Replace("\r", "\\r")
                        .Replace("\t", "\\t") + "\"";

                case Dictionary<string, object> dict:
                    {
                        if (dict.Count == 0) return "{}";
                        var lines = dict.Select(
                            kv => pad1 + "\"" + kv.Key + "\": " + Serialize(kv.Value, indent + 1));
                        return "{\n" + string.Join(",\n", lines) + "\n" + pad + "}";
                    }

                case List<object> list:
                    {
                        if (list.Count == 0) return "[]";
                        var lines = list.Select(item => pad1 + Serialize(item, indent + 1));
                        return "[\n" + string.Join(",\n", lines) + "\n" + pad + "]";
                    }

                default:
                    return "\"" + obj.ToString() + "\"";
            }
        }
    }
}