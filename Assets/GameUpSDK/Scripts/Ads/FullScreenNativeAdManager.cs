using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace GameUpSDK
{
    public class FullScreenNativeAdManager : Singletons.MonoSingletonSdk<FullScreenNativeAdManager>
    {
        private AndroidJavaClass bridgeClass;
        private AndroidJavaObject currentActivity;
        private bool _initialized;
        private string _adUnitId;

        private NativeAdCallbackProxy _callbackProxy;

        public event Action OnAdLoadedEvent;
        public event Action<string> OnAdLoadFailedEvent;
        public event Action OnAdClosedEvent;
        public event Action OnAdDisplayedEvent;
        public event Action<double> OnAdPaidEvent; // <--- MỚI

#if UNITY_IOS && !UNITY_EDITOR
        public delegate void NativeAdLoadedDelegate();
        public delegate void NativeAdFailedDelegate(string error);
        public delegate void NativeAdClosedDelegate();
        public delegate void NativeAdPaidDelegate(double value); // <--- MỚI

        [DllImport("__Internal")]
        private static extern void _iosLoadNativeAd(string adUnitId, NativeAdLoadedDelegate onLoaded, NativeAdFailedDelegate onFailed, NativeAdClosedDelegate onClosed, NativeAdPaidDelegate onPaid);

        [DllImport("__Internal")]
        private static extern bool _iosIsNativeAdReady();

        [DllImport("__Internal")]
        private static extern void _iosShowNativeAd();

        [DllImport("__Internal")]
        private static extern void _iosHideNativeAd();

        [AOT.MonoPInvokeCallback(typeof(NativeAdLoadedDelegate))]
        private static void OnIosAdLoaded() { if (Instance != null) Instance.HandleAdLoaded(); }

        [AOT.MonoPInvokeCallback(typeof(NativeAdFailedDelegate))]
        private static void OnIosAdFailed(string error) { if (Instance != null) Instance.HandleAdFailedToLoad(error); }

        [AOT.MonoPInvokeCallback(typeof(NativeAdClosedDelegate))]
        private static void OnIosAdClosed() { if (Instance != null) Instance.HandleAdClosed(); }

        [AOT.MonoPInvokeCallback(typeof(NativeAdPaidDelegate))]
        private static void OnIosAdPaid(double value) { if (Instance != null) Instance.HandleAdPaid(value); } // <--- MỚI
#endif

        private class NativeAdCallbackProxy : AndroidJavaProxy
        {
            private readonly FullScreenNativeAdManager _manager;

            public NativeAdCallbackProxy(FullScreenNativeAdManager manager) 
                : base("com.plugins.nativebridge.UnityNativeFullScreen$INativeAdCallback")
            {
                _manager = manager;
            }

            public void onAdLoaded() => _manager.HandleAdLoaded();
            public void onAdFailedToLoad(string error) => _manager.HandleAdFailedToLoad(error);
            public void onAdClosed() => _manager.HandleAdClosed();
            public void onAdPaid(double value) => _manager.HandleAdPaid(value); // <--- MỚI
        }

        protected void Awake()
        {
            _callbackProxy = new NativeAdCallbackProxy(this);
            DontDestroyOnLoad(gameObject);
        }

        public void RequestAd(string adUnit)
        {
            if (!_initialized)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                bridgeClass = new AndroidJavaClass("com.plugins.nativebridge.UnityNativeFullScreen");
#endif
                _initialized = true;
            }
            _adUnitId = adUnit;

#if UNITY_ANDROID && !UNITY_EDITOR
            if (bridgeClass != null && currentActivity != null)
            {
                bridgeClass.CallStatic("loadAd", currentActivity, _adUnitId, _callbackProxy);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            // Thêm Delegate Paid vào hàm Load iOS
            _iosLoadNativeAd(_adUnitId, OnIosAdLoaded, OnIosAdFailed, OnIosAdClosed, OnIosAdPaid);
#endif
        }

        public bool IsAdReady()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (bridgeClass != null) return bridgeClass.CallStatic<bool>("isAdLoaded");
#elif UNITY_IOS && !UNITY_EDITOR
            return _iosIsNativeAdReady();
#endif
            return false;
        }

        public void ShowFullScreenAd()
        {
            if (IsAdReady())
            {
                HandleAdDisplayed();
#if UNITY_ANDROID && !UNITY_EDITOR
                bridgeClass.CallStatic("showAd", currentActivity);
#elif UNITY_IOS && !UNITY_EDITOR
                _iosShowNativeAd();
#endif
            }
            else RequestAd(_adUnitId);
        }

        public void ForceCloseAd()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (bridgeClass != null && currentActivity != null) bridgeClass.CallStatic("hideAd", currentActivity);
#elif UNITY_IOS && !UNITY_EDITOR
            _iosHideNativeAd();
#endif
        }

        internal void HandleAdLoaded() => MainThreadDispatcher.Enqueue(() => OnAdLoadedEvent?.Invoke());
        internal void HandleAdFailedToLoad(string error) => MainThreadDispatcher.Enqueue(() => OnAdLoadFailedEvent?.Invoke(error));
        internal void HandleAdClosed() => MainThreadDispatcher.Enqueue(() => OnAdClosedEvent?.Invoke());
        internal void HandleAdDisplayed() => MainThreadDispatcher.Enqueue(() => OnAdDisplayedEvent?.Invoke());
        
        // HÀM HỨNG VÀ BẮN EVENT SANG CHO FILE ADMOB FORMAT
        internal void HandleAdPaid(double value)
        {
            MainThreadDispatcher.Enqueue(() => OnAdPaidEvent?.Invoke(value));
        }
    }
}