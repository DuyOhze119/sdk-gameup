using System.Runtime.InteropServices;
using UnityEngine;

public static class NativeAdConfigBridge
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void NativeBanner_SetCtaRate(int rate);

    [DllImport("__Internal")]
    private static extern void _iosSetNativeFullScreenCtaRate(int rate);
#endif

    // Gọi hàm này khi nhận được giá trị X từ Firebase Remote Config
    public static void SetGlobalCtaClickRate(int ratePercent)
    {
        int safeRate = Mathf.Clamp(ratePercent, 0, 100);
        Debug.Log($"[GameUp Ads] Set Native CTA Click Rate to: {safeRate}%");

#if UNITY_ANDROID && !UNITY_EDITOR
        try 
        {
            using (var bannerClass = new AndroidJavaClass("com.gameup.ads.NativeBannerManager")) 
            {
                bannerClass.CallStatic("setCtaClickRate", safeRate);
            }
            using (var fsClass = new AndroidJavaClass("com.plugins.nativebridge.UnityNativeFullScreen")) 
            {
                fsClass.CallStatic("setCtaClickRate", safeRate);
            }
        } 
        catch (System.Exception e) 
        {
            Debug.LogError($"[GameUp Ads] Failed to set Android CTA rate: {e.Message}");
        }
#elif UNITY_IOS && !UNITY_EDITOR
        NativeBanner_SetCtaRate(safeRate);
        _iosSetNativeFullScreenCtaRate(safeRate);
#endif
    }
}