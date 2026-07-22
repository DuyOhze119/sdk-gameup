using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

public class NativeAdTest : MonoBehaviour
{
    // Delegate định nghĩa callback log từ Native gửi về
    public delegate void NativeLogCallback(string message);

    [MonoPInvokeCallback(typeof(NativeLogCallback))]
    public static void OnNativeLogReceived(string message)
    {
        // In trực tiếp ra Unity Console!
        Debug.Log($"<color=#00FF00>[GameUp-Native]</color> {message}");
    }

    public void LoadTestBanner()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var bannerMgr = new AndroidJavaClass("com.gameup.ads.NativeBannerManager").CallStatic<AndroidJavaObject>("getInstance"))
        {
            // Truyền callback log vào interface của Android
            bannerMgr.Call("setLogListener", new NativeLogProxy());
        }
#elif UNITY_IOS && !UNITY_EDITOR
        NativeBanner_SetLogCallback(OnNativeLogReceived);
#endif
    }
}

// Proxy lắng nghe log dành riêng cho Android
#if UNITY_ANDROID
public class NativeLogProxy : AndroidJavaProxy
{
    public NativeLogProxy() : base("com.gameup.ads.NativeBannerManager$LogListener") { }

    public void onLog(string message)
    {
        Debug.Log($"<color=#00FF00>[GameUp-Native Android]</color> {message}");
    }
}
#endif

#if UNITY_IOS && !UNITY_EDITOR
public static class iOSNativeBridge
{
    [DllImport("__Internal")]
    public static extern void NativeBanner_SetLogCallback(NativeAdTest.NativeLogCallback logCallback);
}
#endif