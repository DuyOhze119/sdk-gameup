using UnityEngine;

namespace GameUpSDK
{
    public class FullScreenNativeAdManager : Singletons.MonoSingletonSdk<FullScreenNativeAdManager>
    {
        private AndroidJavaClass bridgeClass;
        private AndroidJavaObject currentActivity;
        private bool _initialized;
        private string _adUnitId;
        
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void _iosLoadNativeAd(string adUnitId);

    [DllImport("__Internal")]
    private static extern bool _iosIsNativeAdReady();

    [DllImport("__Internal")]
    private static extern void _iosShowNativeAd();

    [DllImport("__Internal")]
    private static extern void _iosHideNativeAd();
#endif

        /// <summary>
        /// Hàm gọi tải trước quảng cáo (Nên gọi sớm, ví dụ khi vừa vào sảnh hoặc bắt đầu Level mới)
        /// </summary>
        public void RequestAd(string adUnit)
        {
            if (!_initialized)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            bridgeClass = new AndroidJavaClass("com.plugins.nativebridge.UnityNativeFullScreen");
#endif
            }
            _adUnitId = adUnit;
#if UNITY_ANDROID && !UNITY_EDITOR
        if (bridgeClass != null && currentActivity != null)
        {
            Debug.Log("Unity: Đang tải trước quảng cáo Native Full-Screen...");
            bridgeClass.CallStatic("loadAd", currentActivity, _adUnitId);
        }
#endif
            
#if UNITY_IOS && !UNITY_EDITOR
            _iosLoadNativeAd(_adUnitId);
#endif
        }

        /// <summary>
        /// Hàm kiểm tra xem quảng cáo đã tải xong và sẵn sàng để hiển thị chưa
        /// </summary>
        public bool IsAdReady()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (bridgeClass != null)
        {
            return bridgeClass.CallStatic<bool>("isAdLoaded");
        }
#endif
#if UNITY_IOS && !UNITY_EDITOR
            return _iosIsNativeAdReady();
#endif
            return false;
        }

        /// <summary>
        /// Hàm kiểm tra và hiển thị quảng cáo lên màn hình
        /// </summary>
        public void ShowFullScreenAd()
        {
            if (IsAdReady())
            {
#if UNITY_ANDROID && !UNITY_EDITOR
            Debug.Log("Unity: Quảng cáo đã sẵn sàng, hiển thị ngay.");
            bridgeClass.CallStatic("showAd", currentActivity);
#elif UNITY_IOS
            _iosShowNativeAd();
#endif
            }
            else
            {
                Debug.LogWarning("Unity: Quảng cáo chưa tải xong, tự động kích hoạt tải lại.");
                RequestAd(_adUnitId);
            }
        }

        public void ForceCloseAd()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (bridgeClass != null && currentActivity != null)
        {
            bridgeClass.CallStatic("hideAd", currentActivity);
            // Sau khi tắt quảng cáo cũ, tự động tải trước quảng cáo mới cho lượt sau
            RequestAd(_adUnitId);
        }
#elif UNITY_IOS
        _iosHideNativeAd();
#endif
            RequestAd(_adUnitId);
        }
    }
}