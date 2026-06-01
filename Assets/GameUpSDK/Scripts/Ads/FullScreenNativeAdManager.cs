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

        // Biến lưu Proxy Callback (Android)
        private NativeAdCallbackProxy _callbackProxy;

        public event Action OnAdLoadedEvent;
        public event Action<string> OnAdLoadFailedEvent;
        public event Action OnAdClosedEvent;

#if UNITY_IOS && !UNITY_EDITOR
        // 1. Khai báo kiểu Delegate tương đương con trỏ hàm trong C
        public delegate void NativeAdLoadedDelegate();
        public delegate void NativeAdFailedDelegate(string error);
        public delegate void NativeAdClosedDelegate();

        // 2. Cập nhật chữ ký DllImport
        [DllImport("__Internal")]
        private static extern void _iosLoadNativeAd(string adUnitId, NativeAdLoadedDelegate onLoaded, NativeAdFailedDelegate onFailed, NativeAdClosedDelegate onClosed);

        [DllImport("__Internal")]
        private static extern bool _iosIsNativeAdReady();

        [DllImport("__Internal")]
        private static extern void _iosShowNativeAd();

        [DllImport("__Internal")]
        private static extern void _iosHideNativeAd();

        // 3. Khai báo các hàm Static hứng Callback từ iOS
        [AOT.MonoPInvokeCallback(typeof(NativeAdLoadedDelegate))]
        private static void OnIosAdLoaded()
        {
            if (Instance != null) Instance.HandleAdLoaded();
        }

        [AOT.MonoPInvokeCallback(typeof(NativeAdFailedDelegate))]
        private static void OnIosAdFailed(string error)
        {
            if (Instance != null) Instance.HandleAdFailedToLoad(error);
        }

        [AOT.MonoPInvokeCallback(typeof(NativeAdClosedDelegate))]
        private static void OnIosAdClosed()
        {
            if (Instance != null) Instance.HandleAdClosed();
        }
#endif

        // =======================================================
        // LỚP PROXY: Cầu nối biến Interface Java thành hàm C# (Dành cho Android)
        // =======================================================
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
            Debug.Log("[NativeBridge] Requesting ad: " + _adUnitId);

#if UNITY_ANDROID && !UNITY_EDITOR
            if (bridgeClass != null && currentActivity != null)
            {
                // Truyền Proxy sang cho Java
                bridgeClass.CallStatic("loadAd", currentActivity, _adUnitId, _callbackProxy);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            // Truyền trực tiếp các Static Method (Delegates) sang iOS
            _iosLoadNativeAd(_adUnitId, OnIosAdLoaded, OnIosAdFailed, OnIosAdClosed);
#endif
        }

        public bool IsAdReady()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (bridgeClass != null)
            {
                return bridgeClass.CallStatic<bool>("isAdLoaded");
            }
#elif UNITY_IOS && !UNITY_EDITOR
            return _iosIsNativeAdReady();
#endif
            return false;
        }

        public void ShowFullScreenAd()
        {
            if (IsAdReady())
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                bridgeClass.CallStatic("showAd", currentActivity);
#elif UNITY_IOS && !UNITY_EDITOR
                _iosShowNativeAd();
#endif
            }
            else
            {
                RequestAd(_adUnitId);
            }
        }

        public void ForceCloseAd()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (bridgeClass != null && currentActivity != null)
            {
                bridgeClass.CallStatic("hideAd", currentActivity);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            _iosHideNativeAd();
#endif
        }

        // ==========================================================
        // CÁC HÀM XỬ LÝ NHẬN TỪ PROXY (ANDROID) & DELEGATE (IOS)
        // ==========================================================

        internal void HandleAdLoaded()
        {
            Debug.Log("[NativeBridge] Ad Loaded Success");
            MainThreadDispatcher.Enqueue(() => OnAdLoadedEvent?.Invoke());
        }

        internal void HandleAdFailedToLoad(string error)
        {
            Debug.Log("[NativeBridge] Ad Load Failed. Error: " + error);
            MainThreadDispatcher.Enqueue(() => OnAdLoadFailedEvent?.Invoke(error));
        }

        internal void HandleAdClosed()
        {
            Debug.Log("[NativeBridge] Ad Closed");
            MainThreadDispatcher.Enqueue(() => OnAdClosedEvent?.Invoke());
        }
    }
}