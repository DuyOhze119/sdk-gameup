using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GameUpSDK.Ads
{
    public class AdmobNativeBannerBridge : BaseAdFormat, IBannerAd
    {
        public event Action<string> OnCollapsedNativeBanner;
        // =========================================================
        // KHAI BÁO BIẾN DÀNH CHO ANDROID
        // =========================================================
#if UNITY_ANDROID && !UNITY_EDITOR && ADMOB_DEPENDENCIES_INSTALLED
        private AndroidJavaObject _nativeManager;
        private AndroidJavaObject _currentActivity;
#endif

        // =========================================================
        // KHAI BÁO IMPORT DÀNH CHO IOS (OBJECTIVE-C)
        // =========================================================
#if UNITY_IOS && !UNITY_EDITOR && ADMOB_DEPENDENCIES_INSTALLED
        [DllImport("__Internal")]
        private static extern void NativeBanner_LoadAd(string adUnitId);

        [DllImport("__Internal")]
        private static extern void NativeBanner_ShowAd(bool isTop);

        [DllImport("__Internal")]
        private static extern void NativeBanner_HideAd();

        [DllImport("__Internal")]
        private static extern void NativeBanner_SetCallbacks(
            Action_Void onLoaded, Action_String onFailed, 
            Action_Void onDisplayed, Action_Void onClosed, 
            Action_Void onClicked, Action_Double onPaid);

        delegate void Action_Void();
        delegate void Action_String(string error);
        delegate void Action_Double(double value);
#endif

        // =========================================================
        // BIẾN QUẢN LÝ TRẠNG THÁI CHUNG
        // =========================================================
        private Dictionary<string, bool> _isLoaded = new Dictionary<string, bool>();
        private Dictionary<string, bool> _isLoading = new Dictionary<string, bool>();

        // (Android) Chống dọn rác GC
        private Dictionary<string, NativeAdCallbackProxy> _proxies = new Dictionary<string, NativeAdCallbackProxy>();

        // (iOS) Singleton ẩn để hứng Callback tĩnh từ Objective-C
        private static AdmobNativeBannerBridge _instance;
        private string _currentActiveWhere;

        public AdmobNativeBannerBridge(AdUnitConfig config) : base(config, AdUnitType.Banner, "Admob_NativeBridge")
        {
            _instance = this;

#if UNITY_ANDROID && !UNITY_EDITOR && ADMOB_DEPENDENCIES_INSTALLED
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                _currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            }
            using (AndroidJavaClass managerClass = new AndroidJavaClass("com.gameup.ads.NativeBannerManager"))
            {
                _nativeManager = managerClass.CallStatic<AndroidJavaObject>("getInstance");
            }
#elif UNITY_IOS && !UNITY_EDITOR
            // Đăng ký các hàm Callback tĩnh cho iOS
            NativeBanner_SetCallbacks(
                OnLoaded_iOS, OnFailed_iOS, OnDisplayed_iOS, 
                OnClosed_iOS, OnClicked_iOS, OnPaid_iOS
            );
#endif
        }

        public override bool IsAvailable(string where = null)
        {
            string key = GetKey(where);
            return _isLoaded.TryGetValue(key, out bool loaded) && loaded;
        }

        protected override void RequestAdInternal(string unitId, string where)
        {
            string key = GetKey(where);
            if (_isLoading.TryGetValue(key, out bool loading) && loading) return;

            _isLoading[key] = true;
            _isLoaded[key] = false;
            _currentActiveWhere = where; // Lưu vết vị trí đang xử lý

#if UNITY_ANDROID && !UNITY_EDITOR && ADMOB_DEPENDENCIES_INSTALLED
            var proxy = new NativeAdCallbackProxy(
                onLoaded: () => { MainThreadDispatcher.Enqueue(() => { _isLoading[key] = false; _isLoaded[key] = true; HandleLoadSuccess(unitId, where); }); },
                onFailed: (err) => { MainThreadDispatcher.Enqueue(() => { _isLoading[key] = false; _isLoaded[key] = false; HandleLoadFailed(unitId, where, err); }); },
                onDisplayed: () => { MainThreadDispatcher.Enqueue(() => NotifyAdDisplayed(where)); },
                onClosed: () => { MainThreadDispatcher.Enqueue(() => { _isLoaded[key] = false; NotifyAdClosed(where); OnCollapsedNativeBanner?.Invoke(where);}); },
                onClicked: () => { MainThreadDispatcher.Enqueue(() => { }); },
                onPaid: (val) => { MainThreadDispatcher.Enqueue(() => TrackRevenue(unitId, where, "NativeBanner_Android", val)); }
            );

            _proxies[key] = proxy;
            _nativeManager.Call("loadAd", _currentActivity, unitId, proxy);

#elif UNITY_IOS && !UNITY_EDITOR && ADMOB_DEPENDENCIES_INSTALLED
            NativeBanner_LoadAd(unitId);
#else
            Debug.Log($"[Bridge] Fake Loading in Editor for {where}");
#endif
        }

        public void Show(string where)
        {
            string key = GetKey(where);
            var entry = _config.GetEntry(_adType, where);
            bool isTop = entry.CollapsiblePlacement == CollapsibleBannerPlacement.Top;
            _currentActiveWhere = where;

            if (IsAvailable(where))
            {
#if UNITY_ANDROID && !UNITY_EDITOR && ADMOB_DEPENDENCIES_INSTALLED
                if (_proxies.TryGetValue(key, out var proxy))
                {
                    _nativeManager.Call("showAd", _currentActivity, isTop, proxy);
                    _isLoaded[key] = false; 
                }
#elif UNITY_IOS && !UNITY_EDITOR && ADMOB_DEPENDENCIES_INSTALLED
                NativeBanner_ShowAd(isTop);
                _isLoaded[key] = false;
#else
                Debug.Log($"[Bridge] Fake Show in Editor for {where}");
#endif
            }
            else Load(where);
        }

        public void Hide(string where)
        {
            string key = GetKey(where);
            _isLoaded[key] = false;
            _isLoading[key] = false;

#if UNITY_ANDROID && !UNITY_EDITOR && ADMOB_DEPENDENCIES_INSTALLED
            _nativeManager.Call("hideAd", _currentActivity);
#elif UNITY_IOS && !UNITY_EDITOR && ADMOB_DEPENDENCIES_INSTALLED
            NativeBanner_HideAd();
#endif
        }

        public void Restore(string where)
        {
            Show(where);
        }

        // =========================================================
        // KHU VỰC HỨNG CALLBACK TỪ NỀN TẢNG iOS (CẦN [AOT.MonoPInvokeCallback])
        // =========================================================
#if UNITY_IOS && !UNITY_EDITOR && ADMOB_DEPENDENCIES_INSTALLED
        [AOT.MonoPInvokeCallback(typeof(Action_Void))]
        private static void OnLoaded_iOS() => MainThreadDispatcher.Enqueue(() => {
            string key = _instance.GetKey(_instance._currentActiveWhere);
            _instance._isLoading[key] = false; _instance._isLoaded[key] = true;
            _instance.HandleLoadSuccess("ios_native", _instance._currentActiveWhere);
        });

        [AOT.MonoPInvokeCallback(typeof(Action_String))]
        private static void OnFailed_iOS(string error) => MainThreadDispatcher.Enqueue(() => {
            string key = _instance.GetKey(_instance._currentActiveWhere);
            _instance._isLoading[key] = false; _instance._isLoaded[key] = false;
            _instance.HandleLoadFailed("ios_native", _instance._currentActiveWhere, error);
        });

        [AOT.MonoPInvokeCallback(typeof(Action_Void))]
        private static void OnDisplayed_iOS() => MainThreadDispatcher.Enqueue(() => _instance.NotifyAdDisplayed(_instance._currentActiveWhere));

        [AOT.MonoPInvokeCallback(typeof(Action_Void))]
        private static void OnClosed_iOS() => MainThreadDispatcher.Enqueue(() => {
            string key = _instance.GetKey(_instance._currentActiveWhere);
            _instance._isLoaded[key] = false;
            _instance.NotifyAdClosed(_instance._currentActiveWhere);
            OnCollapsedNativeBanner?.Invoke(_instance._currentActiveWhere);
        });

        [AOT.MonoPInvokeCallback(typeof(Action_Void))]
        private static void OnClicked_iOS() => MainThreadDispatcher.Enqueue(() => {});

        [AOT.MonoPInvokeCallback(typeof(Action_Double))]
        private static void OnPaid_iOS(double value) => MainThreadDispatcher.Enqueue(() => _instance.TrackRevenue("ios_native", _instance._currentActiveWhere, "NativeBanner_iOS", value));
#endif
    }

#if UNITY_ANDROID || UNITY_EDITOR
    // Lớp Proxy cho Android (Giữ nguyên)
    public class NativeAdCallbackProxy : AndroidJavaProxy
    {
        // ... (Code Proxy Android giữ nguyên như bản trước) ...
        private readonly Action _onLoaded;
        private readonly Action<string> _onFailed;
        private readonly Action _onDisplayed;
        private readonly Action _onClosed;
        private readonly Action _onClicked;
        private readonly Action<double> _onPaid;

        public NativeAdCallbackProxy(Action onLoaded, Action<string> onFailed, Action onDisplayed, Action onClosed, Action onClicked, Action<double> onPaid) 
            : base("com.gameup.ads.NativeBannerManager$AdCallback")
        {
            _onLoaded = onLoaded; _onFailed = onFailed; _onDisplayed = onDisplayed; _onClosed = onClosed; _onClicked = onClicked; _onPaid = onPaid;
        }

        public void onLoaded() => _onLoaded?.Invoke();
        public void onFailed(string error) => _onFailed?.Invoke(error);
        public void onDisplayed() => _onDisplayed?.Invoke();
        public void onClosed() => _onClosed?.Invoke();
        public void onClicked() => _onClicked?.Invoke();
        public void onPaid(double value) => _onPaid?.Invoke(value);
    }
#endif
}