using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GameUpSDK.Ads
{
    public class AdmobNativeBannerBridge : BaseAdFormat, IBannerAd
    {
        public event Action<string> OnCollapsedNativeBanner = delegate { }; 
#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _nativeManager;
        private AndroidJavaObject _currentActivity;
#endif

#if UNITY_IOS && !UNITY_EDITOR
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

        private Dictionary<string, bool> _isLoaded = new Dictionary<string, bool>();
        private Dictionary<string, bool> _isLoading = new Dictionary<string, bool>();

        // [AUTO-REFRESH VÀO ĐÂY] Quản lý vòng lặp Refresh
        private Dictionary<string, CancellationTokenSource> _refreshTokens = new Dictionary<string, CancellationTokenSource>();
        private readonly int REFRESH_TIME_SECONDS = 30;

#if UNITY_ANDROID || UNITY_EDITOR
        private Dictionary<string, NativeAdCallbackProxy> _proxies = new Dictionary<string, NativeAdCallbackProxy>();
#endif

#if UNITY_IOS && !UNITY_EDITOR
        private static AdmobNativeBannerBridge _instance;
        private string _currentActiveWhere;
#endif

        public AdmobNativeBannerBridge(AdUnitConfig config) : base(config, AdUnitType.NativeAd, "Admob_NativeBridge")
        {
#if UNITY_IOS && !UNITY_EDITOR
            _instance = this;
            NativeBanner_SetCallbacks(OnLoaded_iOS, OnFailed_iOS, OnDisplayed_iOS, OnClosed_iOS, OnClicked_iOS, OnPaid_iOS);
#elif UNITY_ANDROID && !UNITY_EDITOR
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                _currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            }
            using (AndroidJavaClass managerClass = new AndroidJavaClass("com.gameup.ads.NativeBannerManager"))
            {
                _nativeManager = managerClass.CallStatic<AndroidJavaObject>("getInstance");
            }
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

#if UNITY_ANDROID && !UNITY_EDITOR
            var proxy = new NativeAdCallbackProxy(
                onLoaded: () => { MainThreadDispatcher.Enqueue(() => { _isLoading[key] = false; _isLoaded[key] = true; HandleLoadSuccess(unitId, where); }); },
                onFailed: (err) => { MainThreadDispatcher.Enqueue(() => { _isLoading[key] = false; _isLoaded[key] = false; HandleLoadFailed(unitId, where, err); }); },
                onDisplayed: () => { MainThreadDispatcher.Enqueue(() => NotifyAdDisplayed(where)); },
                onClosed: () => { MainThreadDispatcher.Enqueue(() => { 
                    _isLoaded[key] = false; 
                    StopAutoRefresh(where); // Dừng AutoRefresh khi đóng 
                    NotifyAdClosed(where); 
                    OnCollapsedNativeBanner?.Invoke(where);
                }); },
                onClicked: () => { },
                onPaid: (val) => { MainThreadDispatcher.Enqueue(() => TrackRevenue(unitId, where, "NativeBanner_Android", val)); }
            );
            _proxies[key] = proxy;
            _nativeManager.Call("loadAd", _currentActivity, unitId, proxy);
#elif UNITY_IOS && !UNITY_EDITOR
            _currentActiveWhere = where;
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

            if (IsAvailable(where))
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (_proxies.TryGetValue(key, out var proxy))
                {
                    _nativeManager.Call("showAd", _currentActivity, isTop, proxy);
                    _isLoaded[key] = false; 
                    StartAutoRefresh(where); // Kích hoạt AutoRefresh
                }
#elif UNITY_IOS && !UNITY_EDITOR
                _currentActiveWhere = where;
                NativeBanner_ShowAd(isTop);
                _isLoaded[key] = false;
                StartAutoRefresh(where); // Kích hoạt AutoRefresh
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
            StopAutoRefresh(where); // Dừng AutoRefresh ngay lập tức

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

        // ==========================================
        // KHỐI LOGIC AUTO REFRESH CỦA NATIVE
        // ==========================================
        private void StartAutoRefresh(string where)
        {
            string key = GetKey(where);
            StopAutoRefresh(where);
            var cts = new CancellationTokenSource();
            _refreshTokens[key] = cts;
            RunRefreshLoop(where, cts.Token);
        }

        private void StopAutoRefresh(string where)
        {
            string key = GetKey(where);
            if (_refreshTokens.TryGetValue(key, out var cts) && cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                _refreshTokens.Remove(key);
            }
        }

        private async void RunRefreshLoop(string where, CancellationToken token)
        {
            string key = GetKey(where);
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(REFRESH_TIME_SECONDS), token);
                    if (token.IsCancellationRequested) break;

                    MainThreadDispatcher.Enqueue(() =>
                    {
                        // Gọi tải lại. Nếu đang SHOWING, Native OS sẽ tự động Update giao diện (In-place)
                        RequestAdInternal(_config.ResolveUnitId(_adType, where), where);
                    });
                }
            }
            catch (TaskCanceledException) { }
        }

#if UNITY_IOS && !UNITY_EDITOR
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
            _instance.StopAutoRefresh(_instance._currentActiveWhere);
            _instance.NotifyAdClosed(_instance._currentActiveWhere);
            _instance.OnCollapsedNativeBanner.Invoke(_instance._currentActiveWhere);
        });

        [AOT.MonoPInvokeCallback(typeof(Action_Void))]
        private static void OnClicked_iOS() => MainThreadDispatcher.Enqueue(() => {});

        [AOT.MonoPInvokeCallback(typeof(Action_Double))]
        private static void OnPaid_iOS(double value) => MainThreadDispatcher.Enqueue(() => _instance.TrackRevenue("ios_native", _instance._currentActiveWhere, "NativeBanner_iOS", value));
#endif
    }

#if UNITY_ANDROID || UNITY_EDITOR
    public class NativeAdCallbackProxy : AndroidJavaProxy
    {
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