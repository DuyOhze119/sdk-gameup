using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace GameUpSDK.Editor
{
    public class GameUpSetupWindow : EditorWindow
    {
        // Đường dẫn được resolve động: hỗ trợ cả Assets/ (dev project) và Packages/ (UPM Git install)
        private static string _packageRoot;

        private static string PackageRoot
        {
            get
            {
                if (_packageRoot != null) return _packageRoot;

                // Thử dùng PackageInfo để tìm đường dẫn chính xác khi cài qua UPM
                try
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    var pkgInfoType = Type.GetType(
                        "UnityEditor.PackageManager.PackageInfo, UnityEditor");
                    if (pkgInfoType != null)
                    {
                        var method = pkgInfoType.GetMethod(
                            "FindForAssembly",
                            BindingFlags.Static | BindingFlags.Public,
                            null, new[] { typeof(Assembly) }, null);
                        if (method != null)
                        {
                            var info = method.Invoke(null, new object[] { assembly });
                            if (info != null)
                            {
                                var assetPathProp = pkgInfoType.GetProperty("assetPath");
                                var path = assetPathProp?.GetValue(info) as string;
                                if (!string.IsNullOrEmpty(path))
                                {
                                    _packageRoot = path;
                                    return _packageRoot;
                                }
                            }
                        }
                    }
                }
                catch { /* fallback below */ }

                // Fallback: project gốc
                _packageRoot = "Assets/GameUpSDK";
                return _packageRoot;
            }
        }

        /// <summary>Bản prefab có thể chỉnh sửa khi SDK cài qua UPM (Packages read-only).</summary>
        private const string WritablePrefabsRoot = "Assets/SDK/Prefabs";

        private static string GetPackagePrefabDirectory()
        {
            return (PackageRoot.Replace('\\', '/') + "/Prefab").Replace("//", "/");
        }

        /// <summary>Ưu tiên bản clone tại Assets/SDK/Prefabs nếu đã có; ngược lại dùng Prefab trong package / Assets.</summary>
        private static string GetPrefabDirectory()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(WritablePrefabsRoot + "/SDK.prefab") != null)
                return WritablePrefabsRoot;
            return GetPackagePrefabDirectory();
        }

        private static string PathSDK        => GetPrefabDirectory() + "/SDK.prefab";
        private static string PathAppsFlyer  => GetPrefabDirectory() + "/AppsFlyerObject.prefab";
        private static string PathIronSource => GetPrefabDirectory() + "/IronSourceAds.prefab";
        private static string PathAdMob      => GetPrefabDirectory() + "/AdmobAds.prefab";

        private const string PathGoogleMobileAdsSettings   = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";
        private const string PathLevelPlayMediationSettings = "Assets/LevelPlay/Resources/LevelPlayMediationSettings.asset";

        private int _activeTab;
        private string[] _tabs;
        private Dictionary<int, Action> _tabDrawers;
        private GameUpSDK.AdsManager.PrimaryMediation _lastPrimaryMediation;

        private enum SetupTab
        {
            AppsFlyer,
            IronSourceMediation,
            AdMobAppOpen,
            FirebaseRemoteConfig,
        }

        // AppsFlyer (AppsFlyerObjectScript on AppsFlyerObject.prefab: devKey, appID)
        private string _appsFlyerDevKey = "";
        private string _appsFlyerAppId = "";

        // AppsFlyerUtils on SDK.prefab (sdkKey, appId, isDevMode)
        private string _appsFlyerUtilsSdkKey = "";
        private string _appsFlyerUtilsAppId = "";
        private bool _appsFlyerUtilsIsDevMode = false;

        // IronSource (IronSourceAds: levelPlayAppKey, bannerAdUnitId, interstitialAdUnitId, rewardedVideoAdUnitId)
        private string _ironSourceAppKey = "";
        private string _ironSourceBannerId = "";
        private string _ironSourceInterstitialId = "";
        private string _ironSourceRewardedId = "";

        // LevelPlay Mediation Settings (LevelPlayMediationSettings.asset)
        private string _levelPlayAndroidAppKey = "";
        private string _levelPlayIOSAppKey = "";

        // AdMob (AdmobAds: bannerAdUnitId, interstitialAdUnitId, rewardedAdUnitId, appOpenAdUnitId)
        private string _admobBannerId = "";
        private string _admobInterstitialId = "";
        private string _admobRewardedId = "";
        private string _admobAppOpenId = "";

        // Google Mobile Ads App IDs (GoogleMobileAdsSettings.asset)
        private string _googleMobileAdsAndroidAppId = "";
        private string _googleMobileAdsIOSAppId = "";

        // FirebaseRemoteConfigUtils on SDK.prefab (default values, sync from Remote at runtime)
        private int _rcInterCappingTime = 120;
        private int _rcInterStartLevel = 3;
        private bool _rcEnableRateApp = false;
        private int _rcLevelStartShowRateApp = 5;
        private bool _rcNoInternetPopupEnable = true;
        private bool _rcEnableBanner = true;

        private Vector2 _scrollPosition;
        private string _loadErrors;
        private string _saveErrors;

        [MenuItem("GameUp SDK/Setup")]
        public static void ShowWindow()
        {
            if (!GameUpSDK.Installer.GameUpDependenciesWindow.AreAllRequiredPackagesInstalled())
            {
                GameUpSDK.Installer.GameUpDependenciesWindow.ShowWindow();
                return;
            }
            var window = GetWindow<GameUpSetupWindow>("GameUp SDK Setup");
            window.minSize = new Vector2(400, 480);
        }

        private void OnEnable()
        {
            LoadFromSceneOrPrefabs();

            _lastPrimaryMediation = GetPrimaryMediationFromDefines();
            BuildTabsForPrimaryMediation(_lastPrimaryMediation, keepActiveTab: false);
        }

        /// <summary>True khi prefab SDK nằm trong Packages (read-only) và chưa có bản clone trong Assets/SDK/Prefabs.</summary>
        private static bool RequiresPrefabCloneBeforeSetup()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(WritablePrefabsRoot + "/SDK.prefab") == null;
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (!string.IsNullOrEmpty(_loadErrors))
            {
                EditorGUILayout.HelpBox(_loadErrors, MessageType.Warning);
                EditorGUILayout.Space(4);
            }
            if (!string.IsNullOrEmpty(_saveErrors))
            {
                EditorGUILayout.HelpBox(_saveErrors, MessageType.Error);
                _saveErrors = null;
                EditorGUILayout.Space(4);
            }

            if (RequiresPrefabCloneBeforeSetup())
            {
                EditorGUILayout.HelpBox(
                    "SDK đang nằm trong Packages (read-only) nên không thể lưu cấu hình vào prefab.\n\n" +
                    "Bạn vẫn có thể cấu hình bình thường. Khi bấm \"Save Configuration\", cấu hình sẽ được lưu " +
                    "trực tiếp lên SDK object hiện có trong Scene (prefab instance overrides).\n\n" +
                    "Nếu bạn muốn có một bản prefab có thể chỉnh sửa trong project, hãy clone prefab từ:\n" +
                    GetPackagePrefabDirectory().Replace('\\', '/') + "\n" +
                    "sang:\n" + WritablePrefabsRoot,
                    MessageType.Info);
                EditorGUILayout.Space(8);
                if (GUILayout.Button("Clone Prefab từ Package → Assets/SDK/Prefabs (tùy chọn)", GUILayout.Height(30)))
                {
                    if (TryClonePackagePrefabsToWritable(out var cloneErr))
                    {
                        LoadFromSceneOrPrefabs();
                        Debug.Log("[GameUpSDK] Đã clone prefab sang " + WritablePrefabsRoot + " — có thể chỉnh sửa và lưu prefab.");
                    }
                    else if (!string.IsNullOrEmpty(cloneErr))
                        _saveErrors = cloneErr;
                }
                EditorGUILayout.Space(6);
            }

            // Nếu user đổi Primary Mediation ở Dependencies window, setup window tự cập nhật tab cho đúng.
            var pm = GetPrimaryMediationFromDefines();
            if (pm != _lastPrimaryMediation)
            {
                _lastPrimaryMediation = pm;
                BuildTabsForPrimaryMediation(pm, keepActiveTab: true);
            }

            if (_tabs == null || _tabs.Length == 0 || _tabDrawers == null || _tabDrawers.Count == 0)
            {
                BuildTabsForPrimaryMediation(pm, keepActiveTab: false);
            }

            _activeTab = GUILayout.Toolbar(_activeTab, _tabs);
            EditorGUILayout.Space(8);

            if (_activeTab < 0) _activeTab = 0;
            if (_activeTab >= _tabs.Length) _activeTab = _tabs.Length - 1;
            if (_tabDrawers.TryGetValue(_activeTab, out var draw))
                draw?.Invoke();

            EditorGUILayout.Space(16);
            if (GUILayout.Button("Save Configuration", GUILayout.Height(32)))
            {
                SaveConfiguration();
            }
            
            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox("Thêm SDK vào scene hiện tại (sẽ tạo instance từ prefab SDK).", MessageType.None);
            if (GUILayout.Button("Tạo SDK trong Scene hiện tại", GUILayout.Height(28)))
            {
                CreateSDKInCurrentScene();
            }

            EditorGUILayout.EndScrollView();
        }

        private static GameUpSDK.AdsManager.PrimaryMediation GetPrimaryMediationFromDefines()
        {
            // Default LevelPlay nếu chưa set gì.
            try
            {
                string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
                if (!string.IsNullOrEmpty(symbols) && symbols.Contains(GameUpSDK.GUDefinetion.PrimaryMediationAdMob))
                    return GameUpSDK.AdsManager.PrimaryMediation.AdMob;
            }
            catch
            {
                // ignore
            }

            return GameUpSDK.AdsManager.PrimaryMediation.LevelPlay;
        }

        private void BuildTabsForPrimaryMediation(GameUpSDK.AdsManager.PrimaryMediation pm, bool keepActiveTab)
        {
            // Preserve current visible tab by name when possible
            string previousTabName = (_tabs != null && _activeTab >= 0 && _activeTab < _tabs.Length)
                ? _tabs[_activeTab]
                : null;

            var tabs = new List<SetupTab>
            {
                SetupTab.AppsFlyer,
            };

            if (pm == GameUpSDK.AdsManager.PrimaryMediation.LevelPlay)
            {
                tabs.Add(SetupTab.IronSourceMediation);
                tabs.Add(SetupTab.AdMobAppOpen);
            }
            else
            {
                tabs.Add(SetupTab.AdMobAppOpen);
            }

            tabs.Add(SetupTab.FirebaseRemoteConfig);

            _tabs = tabs.ConvertAll(GetTabLabel).ToArray();
            _tabDrawers = new Dictionary<int, Action>(_tabs.Length);
            for (int i = 0; i < tabs.Count; i++)
            {
                var t = tabs[i];
                _tabDrawers[i] = () =>
                {
                    switch (t)
                    {
                        case SetupTab.AppsFlyer: DrawAppsFlyerSection(); break;
                        case SetupTab.IronSourceMediation: DrawIronSourceSection(); break;
                        case SetupTab.AdMobAppOpen: DrawAdMobSection(); break;
                        case SetupTab.FirebaseRemoteConfig: DrawFirebaseRemoteConfigSection(); break;
                    }
                };
            }

            if (keepActiveTab && !string.IsNullOrEmpty(previousTabName))
            {
                int idx = Array.IndexOf(_tabs, previousTabName);
                _activeTab = idx >= 0 ? idx : GetDefaultTabIndexFor(pm);
            }
            else
            {
                _activeTab = GetDefaultTabIndexFor(pm);
            }
        }

        private static int GetDefaultTabIndexFor(GameUpSDK.AdsManager.PrimaryMediation pm)
        {
            // Mở tab ads theo PrimaryMediation để user cấu hình nhanh nhất.
            // LevelPlay -> IronSource; AdMob -> AdMob.
            return pm == GameUpSDK.AdsManager.PrimaryMediation.LevelPlay ? 1 : 1;
        }

        private static string GetTabLabel(SetupTab tab)
        {
            switch (tab)
            {
                case SetupTab.AppsFlyer: return "AppsFlyer";
                case SetupTab.IronSourceMediation: return "IronSource Mediation";
                case SetupTab.AdMobAppOpen: return "AdMob (App Open)";
                case SetupTab.FirebaseRemoteConfig: return "Firebase RC";
                default: return tab.ToString();
            }
        }

        private void CreateSDKInCurrentScene()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PathSDK);
            if (prefab == null)
            {
                _saveErrors = "Không tìm thấy prefab SDK tại: " + PathSDK;
                return;
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance != null)
            {
                Selection.activeGameObject = instance;
                EditorGUIUtility.PingObject(instance);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log("[GameUpSDK] Đã thêm SDK vào scene hiện tại.");
            }
        }

        private void DrawAppsFlyerSection()
        {
            EditorGUILayout.LabelField("AppsFlyer Settings", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("AppsFlyerObjectScript on " + PathAppsFlyer, MessageType.None);
            _appsFlyerDevKey = EditorGUILayout.TextField("Dev Key", _appsFlyerDevKey);
            _appsFlyerAppId = EditorGUILayout.TextField("App ID (iOS)", _appsFlyerAppId);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("AppsFlyerUtils (GameUpSDK)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("AppsFlyerUtils on " + PathSDK + " – init SDK, log events, ad revenue.", MessageType.None);
            _appsFlyerUtilsSdkKey = EditorGUILayout.TextField("SDK Key", _appsFlyerUtilsSdkKey);
            _appsFlyerUtilsAppId = EditorGUILayout.TextField("App ID (iOS)", _appsFlyerUtilsAppId);
            _appsFlyerUtilsIsDevMode = EditorGUILayout.Toggle("Dev Mode", _appsFlyerUtilsIsDevMode);
        }

        private void DrawIronSourceSection()
        {
            EditorGUILayout.LabelField("IronSource (LevelPlay) Mediation", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Quảng cáo chạy qua IronSource mediation. AdMob và Unity Ads đã được gộp qua LevelPlay.\n" +
                "Chỉ cần nhập App Key (lấy từ LevelPlay dashboard) để lấy quảng cáo.\n" +
                "Target: IronSourceAds trên " + PathIronSource, MessageType.Info);
            _ironSourceAppKey = EditorGUILayout.TextField("App Key (bắt buộc)", _ironSourceAppKey);
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Ad Unit / Placement IDs (tùy chọn; để trống = dùng DefaultBanner, DefaultInterstitial, DefaultRewardedVideo)", EditorStyles.miniBoldLabel);
            _ironSourceBannerId = EditorGUILayout.TextField("Banner ID", _ironSourceBannerId);
            _ironSourceInterstitialId = EditorGUILayout.TextField("Interstitial ID", _ironSourceInterstitialId);
            _ironSourceRewardedId = EditorGUILayout.TextField("Rewarded ID", _ironSourceRewardedId);

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("LevelPlay Mediation Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("App Key điền vào " + PathLevelPlayMediationSettings, MessageType.None);
            _levelPlayAndroidAppKey = EditorGUILayout.TextField("Android App Key", _levelPlayAndroidAppKey);
            _levelPlayIOSAppKey     = EditorGUILayout.TextField("iOS App Key",     _levelPlayIOSAppKey);
        }

        private void DrawAdMobSection()
        {
            EditorGUILayout.LabelField("AdMob", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "SDK mặc định chỉ dùng IronSource Mediation. Thêm AdmobAds vào adsBehaviours trong prefab SDK nếu cần App Open.\n" +
                "Target: AdmobAds trên " + PathAdMob, MessageType.None);
            EditorGUILayout.LabelField("Ad Unit IDs (chỉ cần nếu dùng App Open)", EditorStyles.miniBoldLabel);
            _admobBannerId = EditorGUILayout.TextField("Banner ID", _admobBannerId);
            _admobInterstitialId = EditorGUILayout.TextField("Interstitial ID", _admobInterstitialId);
            _admobRewardedId = EditorGUILayout.TextField("Rewarded ID", _admobRewardedId);
            _admobAppOpenId = EditorGUILayout.TextField("App Open ID", _admobAppOpenId);

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Google Mobile Ads App ID", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("App ID điền vào " + PathGoogleMobileAdsSettings, MessageType.None);
            _googleMobileAdsAndroidAppId = EditorGUILayout.TextField("Android App ID", _googleMobileAdsAndroidAppId);
            _googleMobileAdsIOSAppId     = EditorGUILayout.TextField("iOS App ID",     _googleMobileAdsIOSAppId);
        }

        private void DrawFirebaseRemoteConfigSection()
        {
            EditorGUILayout.LabelField("Firebase Remote Config (defaults)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("FirebaseRemoteConfigUtils on " + PathSDK + ". Giá trị mặc định khi chưa fetch hoặc key không có trên Remote.", MessageType.None);
            _rcInterCappingTime = EditorGUILayout.IntField("inter_capping_time (giây)", _rcInterCappingTime);
            _rcInterStartLevel = EditorGUILayout.IntField("inter_start_level", _rcInterStartLevel);
            _rcEnableRateApp = EditorGUILayout.Toggle("enable_rate_app", _rcEnableRateApp);
            _rcLevelStartShowRateApp = EditorGUILayout.IntField("level_start_show_rate_app", _rcLevelStartShowRateApp);
            _rcNoInternetPopupEnable = EditorGUILayout.Toggle("no_internet_popup_enable", _rcNoInternetPopupEnable);
            _rcEnableBanner = EditorGUILayout.Toggle("enable_banner", _rcEnableBanner);
        }

        private void LoadFromPrefabs()
        {
            var errors = new System.Collections.Generic.List<string>();
            if (!LoadAppsFlyer()) errors.Add("Prefab not found at: " + PathAppsFlyer);
            LoadAppsFlyerUtils();
            LoadFirebaseRemoteConfigUtils();
            #if USE_LEVEL_PLAY_MEDIATION
            if (!LoadIronSource()) errors.Add("Prefab not found at: " + PathIronSource);
            #endif
            if (!LoadAdMob()) errors.Add("Prefab not found at: " + PathAdMob);
            LoadGoogleMobileAdsSettings();
            LoadLevelPlayMediationSettings();
            _loadErrors = errors.Count > 0 ? string.Join("\n", errors) : null;
        }

        private void LoadFromSceneOrPrefabs()
        {
            _loadErrors = null;
            if (TryGetSdkSceneRoot(out var sdkRoot))
            {
                LoadFromSceneSdk(sdkRoot);
                return;
            }

            // Không có SDK trong Scene → fallback load từ prefab/assets (read-only vẫn load được).
            if (!RequiresPrefabCloneBeforeSetup())
                LoadFromPrefabs();
            else
                LoadFromPrefabs();
        }

        private void LoadFromSceneSdk(GameObject sdkRoot)
        {
            if (sdkRoot == null) return;

            // AppsFlyerObjectScript nằm ở prefab riêng, thường không có trong SDK root của scene.
            // Chỉ load các component nằm trên SDK root/prefab instance.
            var errors = new System.Collections.Generic.List<string>();

            var afUtils = sdkRoot.GetComponent<GameUpSDK.AppsFlyerUtils>();
            if (afUtils != null)
            {
                var so = new SerializedObject(afUtils);
                Assign(so, "sdkKey", ref _appsFlyerUtilsSdkKey);
                Assign(so, "appId", ref _appsFlyerUtilsAppId);
                var isDev = so.FindProperty("isDevMode");
                if (isDev != null) _appsFlyerUtilsIsDevMode = isDev.boolValue;
            }

            var rc = sdkRoot.GetComponent<GameUpSDK.FirebaseRemoteConfigUtils>();
            if (rc != null)
            {
                var so = new SerializedObject(rc);
                AssignInt(so, "inter_capping_time", ref _rcInterCappingTime);
                AssignInt(so, "inter_start_level", ref _rcInterStartLevel);
                AssignBool(so, "enable_rate_app", ref _rcEnableRateApp);
                AssignInt(so, "level_start_show_rate_app", ref _rcLevelStartShowRateApp);
                AssignBool(so, "no_internet_popup_enable", ref _rcNoInternetPopupEnable);
                AssignBool(so, "enable_banner", ref _rcEnableBanner);
            }

            var admob = sdkRoot.GetComponentInChildren<GameUpSDK.AdmobAds>(true);
            if (admob != null)
            {
                var so = new SerializedObject(admob);
                Assign(so, "bannerAdUnitId", ref _admobBannerId);
                Assign(so, "interstitialAdUnitId", ref _admobInterstitialId);
                Assign(so, "rewardedAdUnitId", ref _admobRewardedId);
                Assign(so, "appOpenAdUnitId", ref _admobAppOpenId);
            }

#if USE_LEVEL_PLAY_MEDIATION
            var iron = sdkRoot.GetComponentInChildren<GameUpSDK.IronSourceAds>(true);
            if (iron != null)
            {
                var so = new SerializedObject(iron);
                Assign(so, "levelPlayAppKey", ref _ironSourceAppKey);
                Assign(so, "bannerAdUnitId", ref _ironSourceBannerId);
                Assign(so, "interstitialAdUnitId", ref _ironSourceInterstitialId);
                Assign(so, "rewardedVideoAdUnitId", ref _ironSourceRewardedId);
            }
#endif

            LoadGoogleMobileAdsSettings();
            LoadLevelPlayMediationSettings();

            _loadErrors = errors.Count > 0 ? string.Join("\n", errors) : null;
        }

        private bool LoadAppsFlyer()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(PathAppsFlyer);
            if (go == null) return false;
            var type = Type.GetType("AppsFlyerObjectScript, AppsFlyer");
            if (type == null) return false;
            var comp = go.GetComponent(type);
            if (comp == null) return false;
            var so = new SerializedObject(comp);
            var devKey = so.FindProperty("devKey");
            var appID = so.FindProperty("appID");
            if (devKey != null) _appsFlyerDevKey = devKey.stringValue ?? "";
            if (appID != null) _appsFlyerAppId = appID.stringValue ?? "";
            return true;
        }

        private void LoadAppsFlyerUtils()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(PathSDK);
            if (go == null) return;
            var comp = go.GetComponent<GameUpSDK.AppsFlyerUtils>();
            if (comp == null) return;
            var so = new SerializedObject(comp);
            Assign(so, "sdkKey", ref _appsFlyerUtilsSdkKey);
            Assign(so, "appId", ref _appsFlyerUtilsAppId);
            var isDev = so.FindProperty("isDevMode");
            if (isDev != null) _appsFlyerUtilsIsDevMode = isDev.boolValue;
        }

        private void LoadFirebaseRemoteConfigUtils()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(PathSDK);
            if (go == null) return;
            var comp = go.GetComponent<GameUpSDK.FirebaseRemoteConfigUtils>();
            if (comp == null) return;
            var so = new SerializedObject(comp);
            AssignInt(so, "inter_capping_time", ref _rcInterCappingTime);
            AssignInt(so, "inter_start_level", ref _rcInterStartLevel);
            AssignBool(so, "enable_rate_app", ref _rcEnableRateApp);
            AssignInt(so, "level_start_show_rate_app", ref _rcLevelStartShowRateApp);
            AssignBool(so, "no_internet_popup_enable", ref _rcNoInternetPopupEnable);
            AssignBool(so, "enable_banner", ref _rcEnableBanner);
        }

        private bool LoadIronSource()
        {
            #if USE_LEVEL_PLAY_MEDIATION
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(PathIronSource);
            if (go == null) return false;
            var comp = go.GetComponent<GameUpSDK.IronSourceAds>();
            if (comp == null) return false;
            var so = new SerializedObject(comp);
            Assign(so, "levelPlayAppKey", ref _ironSourceAppKey);
            Assign(so, "bannerAdUnitId", ref _ironSourceBannerId);
            Assign(so, "interstitialAdUnitId", ref _ironSourceInterstitialId);
            Assign(so, "rewardedVideoAdUnitId", ref _ironSourceRewardedId);
            return true;
            #endif
            return false;
        }

        private bool LoadAdMob()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(PathAdMob);
            if (go == null) return false;
            var comp = go.GetComponent<GameUpSDK.AdmobAds>();
            if (comp == null) return false;
            var so = new SerializedObject(comp);
            Assign(so, "bannerAdUnitId", ref _admobBannerId);
            Assign(so, "interstitialAdUnitId", ref _admobInterstitialId);
            Assign(so, "rewardedAdUnitId", ref _admobRewardedId);
            Assign(so, "appOpenAdUnitId", ref _admobAppOpenId);
            return true;
        }

        private void LoadGoogleMobileAdsSettings()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(PathGoogleMobileAdsSettings);
            if (asset == null) return;
            var so = new SerializedObject(asset);
            Assign(so, "adMobAndroidAppId", ref _googleMobileAdsAndroidAppId);
            Assign(so, "adMobIOSAppId",     ref _googleMobileAdsIOSAppId);
        }

        private bool SaveGoogleMobileAdsSettings()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(PathGoogleMobileAdsSettings);
            if (asset == null) return false;
            var so = new SerializedObject(asset);
            Set(so, "adMobAndroidAppId", _googleMobileAdsAndroidAppId);
            Set(so, "adMobIOSAppId",     _googleMobileAdsIOSAppId);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return true;
        }

        private void LoadLevelPlayMediationSettings()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(PathLevelPlayMediationSettings);
            if (asset == null) return;
            var so = new SerializedObject(asset);
            Assign(so, "AndroidAppKey", ref _levelPlayAndroidAppKey);
            Assign(so, "IOSAppKey",     ref _levelPlayIOSAppKey);
        }

        private bool SaveLevelPlayMediationSettings()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(PathLevelPlayMediationSettings);
            if (asset == null) return false;
            var so = new SerializedObject(asset);
            Set(so, "AndroidAppKey", _levelPlayAndroidAppKey);
            Set(so, "IOSAppKey",     _levelPlayIOSAppKey);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return true;
        }

        private static void Assign(SerializedObject so, string propName, ref string target)
        {
            var p = so.FindProperty(propName);
            if (p != null) target = p.stringValue ?? "";
        }

        private static void AssignInt(SerializedObject so, string propName, ref int target)
        {
            var p = so.FindProperty(propName);
            if (p != null) target = p.intValue;
        }

        private static void AssignBool(SerializedObject so, string propName, ref bool target)
        {
            var p = so.FindProperty(propName);
            if (p != null) target = p.boolValue;
        }

        private static string AssetPathToAbsolute(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets/", StringComparison.Ordinal))
                return null;
            var relative = assetPath.Substring("Assets/".Length).Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(Application.dataPath, relative);
        }

        private static void RewirePrefabYamlGuidReferences(string assetPath, Dictionary<string, string> guidMap)
        {
            if (guidMap == null || guidMap.Count == 0)
                return;

            var abs = AssetPathToAbsolute(assetPath);
            if (string.IsNullOrEmpty(abs) || !File.Exists(abs))
                return;

            var text = File.ReadAllText(abs);
            var changed = false;
            foreach (var kv in guidMap)
            {
                if (string.IsNullOrEmpty(kv.Key) || string.IsNullOrEmpty(kv.Value) || kv.Key == kv.Value)
                    continue;
                var needle = "guid: " + kv.Key;
                if (text.IndexOf(needle, StringComparison.Ordinal) < 0)
                    continue;
                text = text.Replace(needle, "guid: " + kv.Value);
                changed = true;
            }

            if (!changed)
                return;

            File.WriteAllText(abs, text);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        /// <summary>Copy mọi prefab trong thư mục Prefab của package sang Assets/SDK/Prefabs và cập nhật guid tham chiếu.</summary>
        private static bool TryClonePackagePrefabsToWritable(out string errorMessage)
        {
            errorMessage = null;
            var srcDir = GetPackagePrefabDirectory().Replace('\\', '/').TrimEnd('/');

            if (!AssetDatabase.IsValidFolder(srcDir))
            {
                errorMessage = "Không tìm thấy thư mục prefab: " + srcDir;
                return false;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(WritablePrefabsRoot + "/SDK.prefab") != null)
                return true;

            if (!AssetDatabase.IsValidFolder("Assets/SDK"))
                AssetDatabase.CreateFolder("Assets", "SDK");
            if (!AssetDatabase.IsValidFolder(WritablePrefabsRoot))
                AssetDatabase.CreateFolder("Assets/SDK", "Prefabs");

            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { srcDir });
            var guidMap = new Dictionary<string, string>();
            var copiedDestPaths = new List<string>();
            var srcPaths = new List<string>();

            foreach (var g in prefabGuids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g).Replace('\\', '/');
                var prefix = srcDir.EndsWith("/", StringComparison.Ordinal) ? srcDir : srcDir + "/";
                if (!p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !string.Equals(p, srcDir, StringComparison.OrdinalIgnoreCase))
                    continue;
                srcPaths.Add(p);
            }

            srcPaths.Sort(StringComparer.Ordinal);

            foreach (var src in srcPaths)
            {
                var fileName = Path.GetFileName(src);
                if (string.IsNullOrEmpty(fileName) || !fileName.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                    continue;

                var dst = WritablePrefabsRoot + "/" + fileName;
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(src) == null)
                    continue;

                var oldGuid = AssetDatabase.AssetPathToGUID(src);
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dst) != null)
                {
                    var existingGuid = AssetDatabase.AssetPathToGUID(dst);
                    if (!string.IsNullOrEmpty(oldGuid) && !string.IsNullOrEmpty(existingGuid))
                        guidMap[oldGuid] = existingGuid;
                    copiedDestPaths.Add(dst);
                    continue;
                }

                if (!AssetDatabase.CopyAsset(src, dst))
                {
                    Debug.LogWarning("[GameUpSDK] Không copy được: " + src + " → " + dst);
                    continue;
                }

                var newGuid = AssetDatabase.AssetPathToGUID(dst);
                if (!string.IsNullOrEmpty(oldGuid) && !string.IsNullOrEmpty(newGuid))
                    guidMap[oldGuid] = newGuid;
                copiedDestPaths.Add(dst);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            foreach (var dstPath in copiedDestPaths)
            {
                if (dstPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                    RewirePrefabYamlGuidReferences(dstPath, guidMap);
            }

            AssetDatabase.Refresh();

            if (AssetDatabase.LoadAssetAtPath<GameObject>(WritablePrefabsRoot + "/SDK.prefab") == null)
            {
                errorMessage = "Clone không tạo được SDK.prefab trong " + WritablePrefabsRoot + ". Xem Console.";
                return false;
            }

            return true;
        }

        private void SaveConfiguration()
        {
            var errors = new System.Collections.Generic.List<string>();

            // Lưu trực tiếp lên SDK instance trong Scene (không lưu prefab asset).
            if (TryGetSdkSceneRoot(out var sdkRoot))
            {
                if (!SaveSceneAppsFlyerUtils(sdkRoot)) errors.Add("SDK in Scene (AppsFlyerUtils)");
                if (!SaveSceneFirebaseRemoteConfigUtils(sdkRoot)) errors.Add("SDK in Scene (FirebaseRemoteConfigUtils)");
                if (!SaveSceneAdsManager(sdkRoot)) errors.Add("SDK in Scene (AdsManager)");
#if USE_LEVEL_PLAY_MEDIATION
                if (!SaveSceneIronSource(sdkRoot)) errors.Add("SDK in Scene (IronSourceAds)");
#endif
                if (!SaveSceneAdMob(sdkRoot)) errors.Add("SDK in Scene (AdmobAds)");

                EditorSceneManager.MarkSceneDirty(sdkRoot.scene);
                EditorSceneManager.SaveOpenScenes();
            }
            else
            {
                errors.Add("Không tìm thấy SDK object trong Scene. Hãy bấm \"Tạo SDK trong Scene hiện tại\" trước.");
            }

            // Các settings asset vẫn lưu như cũ
            if (!SaveGoogleMobileAdsSettings()) errors.Add(PathGoogleMobileAdsSettings);
            if (!SaveLevelPlayMediationSettings()) errors.Add(PathLevelPlayMediationSettings);

            if (errors.Count > 0)
                _saveErrors = "Asset/Prefab not found at:\n" + string.Join("\n", errors);
            else
                Debug.Log("[GameUpSDK] Configuration Saved!");
        }

        private static bool TryGetSdkSceneRoot(out GameObject sdkRoot)
        {
            sdkRoot = null;
            try
            {
                var all = Resources.FindObjectsOfTypeAll<GameUpSDK.AdsManager>();
                foreach (var am in all)
                {
                    if (am == null) continue;
                    if (EditorUtility.IsPersistent(am)) continue; // asset/prefab
                    var go = am.gameObject;
                    if (go == null) continue;
                    // Chỉ lấy object thuộc scene hợp lệ
                    if (!go.scene.IsValid()) continue;
                    sdkRoot = go;
                    return true;
                }
            }
            catch
            {
                // ignore
            }

            return false;
        }

        private bool SaveSceneAppsFlyerUtils(GameObject sdkRoot)
        {
            if (sdkRoot == null) return false;
            var comp = sdkRoot.GetComponent<GameUpSDK.AppsFlyerUtils>();
            if (comp == null) return false;
            var so = new SerializedObject(comp);
            Set(so, "sdkKey", _appsFlyerUtilsSdkKey);
            Set(so, "appId", _appsFlyerUtilsAppId);
            var isDev = so.FindProperty("isDevMode");
            if (isDev != null) isDev.boolValue = _appsFlyerUtilsIsDevMode;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(comp);
            PrefabUtility.RecordPrefabInstancePropertyModifications(comp);
            return true;
        }

        private bool SaveSceneAdsManager(GameObject sdkRoot)
        {
            if (sdkRoot == null) return false;
            var comp = sdkRoot.GetComponent<GameUpSDK.AdsManager>();
            if (comp == null) return false;
            var so = new SerializedObject(comp);
            var prop = so.FindProperty("adsBehaviours");
            if (prop == null) return false;

#if USE_LEVEL_PLAY_MEDIATION
            var list = new List<IronSourceAds>();
            foreach (var c in sdkRoot.GetComponentsInChildren<IronSourceAds>(true))
            {
                if (c.gameObject == sdkRoot) continue;
                list.Add(c);
            }
#else
            var list = new List<AdmobAds>();
            foreach (var c in sdkRoot.GetComponentsInChildren<AdmobAds>(true))
            {
                if (c.gameObject == sdkRoot) continue;
                list.Add(c);
            }
#endif
            prop.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = list[i];

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(comp);
            PrefabUtility.RecordPrefabInstancePropertyModifications(comp);
            return true;
        }

        private bool SaveSceneFirebaseRemoteConfigUtils(GameObject sdkRoot)
        {
            if (sdkRoot == null) return false;
            var comp = sdkRoot.GetComponent<GameUpSDK.FirebaseRemoteConfigUtils>();
            if (comp == null) return false;
            var so = new SerializedObject(comp);
            SetInt(so, "inter_capping_time", _rcInterCappingTime);
            SetInt(so, "inter_start_level", _rcInterStartLevel);
            SetBool(so, "enable_rate_app", _rcEnableRateApp);
            SetInt(so, "level_start_show_rate_app", _rcLevelStartShowRateApp);
            SetBool(so, "no_internet_popup_enable", _rcNoInternetPopupEnable);
            SetBool(so, "enable_banner", _rcEnableBanner);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(comp);
            PrefabUtility.RecordPrefabInstancePropertyModifications(comp);
            return true;
        }

        private bool SaveSceneIronSource(GameObject sdkRoot)
        {
            if (sdkRoot == null) return false;
            var comp = sdkRoot.GetComponentInChildren<GameUpSDK.IronSourceAds>(true);
            if (comp == null) return false;
            var so = new SerializedObject(comp);
            Set(so, "levelPlayAppKey", _ironSourceAppKey);
            Set(so, "bannerAdUnitId", _ironSourceBannerId);
            Set(so, "interstitialAdUnitId", _ironSourceInterstitialId);
            Set(so, "rewardedVideoAdUnitId", _ironSourceRewardedId);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(comp);
            PrefabUtility.RecordPrefabInstancePropertyModifications(comp);
            return true;
        }

        private bool SaveSceneAdMob(GameObject sdkRoot)
        {
            if (sdkRoot == null) return false;
            var comp = sdkRoot.GetComponentInChildren<GameUpSDK.AdmobAds>(true);
            if (comp == null) return false;
            var so = new SerializedObject(comp);
            Set(so, "bannerAdUnitId", _admobBannerId);
            Set(so, "interstitialAdUnitId", _admobInterstitialId);
            Set(so, "rewardedAdUnitId", _admobRewardedId);
            Set(so, "appOpenAdUnitId", _admobAppOpenId);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(comp);
            PrefabUtility.RecordPrefabInstancePropertyModifications(comp);
            return true;
        }

        private static void Set(SerializedObject so, string propName, string value)
        {
            var p = so.FindProperty(propName);
            if (p != null) p.stringValue = value ?? "";
        }

        private static void SetInt(SerializedObject so, string propName, int value)
        {
            var p = so.FindProperty(propName);
            if (p != null) p.intValue = value;
        }

        private static void SetBool(SerializedObject so, string propName, bool value)
        {
            var p = so.FindProperty(propName);
            if (p != null) p.boolValue = value;
        }
    }
}
