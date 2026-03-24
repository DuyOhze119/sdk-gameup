using System;
using System.Collections.Generic;
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

        private static string PathSDK        => PackageRoot + "/Prefab/SDK.prefab";
        private static string PathAppsFlyer  => PackageRoot + "/Prefab/AppsFlyerObject.prefab";
        private static string PathIronSource => PackageRoot + "/Prefab/IronSourceAds.prefab";
        private static string PathAdMob      => PackageRoot + "/Prefab/AdmobAds.prefab";

        private const string PathGoogleMobileAdsSettings   = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";
        private const string PathLevelPlayMediationSettings = "Assets/LevelPlay/Resources/LevelPlayMediationSettings.asset";

        private int _activeTab;
        #if USE_LEVEL_PLAY_MEDIATION
        private readonly string[] _tabs = { "AppsFlyer", "IronSource Mediation", "AdMob (App Open)", "Firebase RC" };
        #endif
        #if !USE_LEVEL_PLAY_MEDIATION
        private readonly string[] _tabs = { "AppsFlyer", "AdMob (App Open)", "Firebase RC" };
        #endif

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
            LoadFromPrefabs();
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

            _activeTab = GUILayout.Toolbar(_activeTab, _tabs);
            EditorGUILayout.Space(8);

            #if USE_LEVEL_PLAY_MEDIATION
            switch (_activeTab)
            {
                case 0: DrawAppsFlyerSection(); break;
                case 1: DrawIronSourceSection(); break;
                case 2: DrawAdMobSection(); break;
                case 3: DrawFirebaseRemoteConfigSection(); break;
            }
            #endif

            #if !USE_LEVEL_PLAY_MEDIATION
            switch (_activeTab)
            {
                case 0: DrawAppsFlyerSection(); break;
                case 1: DrawAdMobSection(); break;
                case 2: DrawFirebaseRemoteConfigSection(); break;
            }
            #endif

            EditorGUILayout.Space(16);
            if (GUILayout.Button("Save Configuration", GUILayout.Height(32)))
            {
                SaveToPrefabs();
            }
            
            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox("Thêm SDK vào scene hiện tại (sẽ tạo instance từ prefab SDK).", MessageType.None);
            if (GUILayout.Button("Tạo SDK trong Scene hiện tại", GUILayout.Height(28)))
            {
                CreateSDKInCurrentScene();
            }

            EditorGUILayout.EndScrollView();
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
            if (!LoadIronSource()) errors.Add("Prefab not found at: " + PathIronSource);
            if (!LoadAdMob()) errors.Add("Prefab not found at: " + PathAdMob);
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

        private void SaveToPrefabs()
        {
            var errors = new System.Collections.Generic.List<string>();
            if (!SaveAppsFlyer()) errors.Add(PathAppsFlyer);
            if (!SaveAppsFlyerUtils()) errors.Add(PathSDK + " (AppsFlyerUtils)");
            if (!SaveFirebaseRemoteConfigUtils()) errors.Add(PathSDK + " (FirebaseRemoteConfigUtils)");
            if (!SaveAdsManager()) errors.Add(PathSDK + " (AdsManager)");
            #if USE_LEVEL_PLAY_MEDIATION
            if (!SaveIronSource()) errors.Add(PathIronSource);
            #endif
            if (!SaveAdMob()) errors.Add(PathAdMob);
            if (!SaveGoogleMobileAdsSettings()) errors.Add(PathGoogleMobileAdsSettings);
            if (!SaveLevelPlayMediationSettings()) errors.Add(PathLevelPlayMediationSettings);

            if (errors.Count > 0)
                _saveErrors = "Asset/Prefab not found at:\n" + string.Join("\n", errors);
            else
                Debug.Log("[GameUpSDK] Configuration Saved!");
        }

        private bool SaveAppsFlyer()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(PathAppsFlyer);
            if (go == null) return false;
            var type = Type.GetType("AppsFlyerObjectScript, AppsFlyer");
            if (type == null) return false;
            var comp = go.GetComponent(type);
            if (comp == null) return false;
            var so = new SerializedObject(comp);
            Set(so, "devKey", _appsFlyerDevKey);
            Set(so, "appID", _appsFlyerAppId);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(comp);
            PrefabUtility.SavePrefabAsset(go);
            return true;
        }

        private bool SaveAppsFlyerUtils()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(PathSDK);
            if (go == null) return false;
            var comp = go.GetComponent<GameUpSDK.AppsFlyerUtils>();
            if (comp == null) return false;
            var so = new SerializedObject(comp);
            Set(so, "sdkKey", _appsFlyerUtilsSdkKey);
            Set(so, "appId", _appsFlyerUtilsAppId);
            var isDev = so.FindProperty("isDevMode");
            if (isDev != null) isDev.boolValue = _appsFlyerUtilsIsDevMode;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(comp);
            PrefabUtility.SavePrefabAsset(go);
            return true;
        }

        private bool SaveAdsManager()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(PathSDK);
            if (go == null) return false;
            var comp = go.GetComponent<GameUpSDK.AdsManager>();
            if (comp == null) return false;
            var so = new SerializedObject(comp);
            var prop = so.FindProperty("adsBehaviours");
            if (prop == null) return false;

#if USE_LEVEL_PLAY_MEDIATION
            var list = new List<>();
            foreach (var c in go.GetComponentsInChildren<IronSourceAds>(true))
            {
                if (c.gameObject == go) continue;
                list.Add(c);
            }
            
            foreach (var c in go.GetComponentsInChildren<AdmobAds>(true))
            {
                if (c.gameObject == go) continue;
                list.Add(c);
            }
#else
            var list = new List<AdmobAds>();
            foreach (var c in go.GetComponentsInChildren<AdmobAds>(true))
            {
                if (c.gameObject == go) continue;
                list.Add(c);
            }
#endif
            prop.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = list[i];

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(comp);
            PrefabUtility.SavePrefabAsset(go);
            return true;
        }

        private bool SaveFirebaseRemoteConfigUtils()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(PathSDK);
            if (go == null) return false;
            var comp = go.GetComponent<GameUpSDK.FirebaseRemoteConfigUtils>();
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
            PrefabUtility.SavePrefabAsset(go);
            return true;
        }

        private bool SaveIronSource()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(PathIronSource);
            if (go == null) return false;
            var comp = go.GetComponent<GameUpSDK.IronSourceAds>();
            if (comp == null) return false;
            var so = new SerializedObject(comp);
            Set(so, "levelPlayAppKey", _ironSourceAppKey);
            Set(so, "bannerAdUnitId", _ironSourceBannerId);
            Set(so, "interstitialAdUnitId", _ironSourceInterstitialId);
            Set(so, "rewardedVideoAdUnitId", _ironSourceRewardedId);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(comp);
            PrefabUtility.SavePrefabAsset(go);
            return true;
        }

        private bool SaveAdMob()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(PathAdMob);
            if (go == null) return false;
            var comp = go.GetComponent<GameUpSDK.AdmobAds>();
            if (comp == null) return false;
            var so = new SerializedObject(comp);
            Set(so, "bannerAdUnitId", _admobBannerId);
            Set(so, "interstitialAdUnitId", _admobInterstitialId);
            Set(so, "rewardedAdUnitId", _admobRewardedId);
            Set(so, "appOpenAdUnitId", _admobAppOpenId);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(comp);
            PrefabUtility.SavePrefabAsset(go);
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
