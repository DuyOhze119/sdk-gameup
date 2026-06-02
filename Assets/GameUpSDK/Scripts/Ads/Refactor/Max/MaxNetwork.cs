using System;
using UnityEngine;

namespace GameUpSDK.Ads
{
    public class MaxNetwork : MonoBehaviour, IAdNetwork
    {
        [SerializeField] private string sdkKey;
        [SerializeField] private bool showMediationDebugger;
        [Header("Ad Unit Configs")] [SerializeField]
        private AdUnitConfig rewardedConfig;

        [SerializeField] private AdUnitConfig interstitialConfig;
        [SerializeField] private AdUnitConfig bannerConfig;
        [SerializeField] private AdUnitConfig appOpenAdConfig;
        [SerializeField] private AdUnitConfig nativeAdConfig;

        public Action<IAdNetwork> OnInitialized { get; set; }
        public MediationProvider MediationProvider { get; set; } = MediationProvider.Max;
        public bool IsInitialized { get; private set; }

        // Expose Providers
        public IRewardedAd RewardedAd { get; private set; }
        public IInterstitialAd InterstitialAd { get; private set; }
        public IBannerAd BannerAd { get; private set; }
        public IAppOpenAd AppOpenAd { get; private set; }
        
        public INativeFullScreenAd NativeFullScreenAd { get; private set; }

        public void Initialize()
        {
            if (IsInitialized) return;

#if MAXSDK_DEPENDENCIES_INSTALLED
            if (!string.IsNullOrEmpty(sdkKey)) MaxSdk.SetSdkKey(sdkKey);

            MaxSdkCallbacks.OnSdkInitializedEvent += (config) =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    IsInitialized = true;
                    if (showMediationDebugger)
                    {
                        MaxSdk.ShowMediationDebugger();
                    }

                    AppOpenAd = new MaxAppOpenAd(appOpenAdConfig);
                    RewardedAd = new MaxRewardedAd(rewardedConfig);
                    InterstitialAd = new MaxInterstitialAd(interstitialConfig);
                    BannerAd = new MaxBannerAd(bannerConfig);
                    NativeFullScreenAd = new DummyNativeFullscreenAd();

                    // Preload
                    AppOpenAd.Load();
                    RewardedAd.Load();
                    InterstitialAd.Load();
                    BannerAd.Load();
                    NativeFullScreenAd.Load();
                    
                    OnInitialized?.Invoke(this);
                });
            };

            MaxSdk.InitializeSdk();
#endif
        }

        public void SetConsent(bool isConsent)
        {
#if MAXSDK_DEPENDENCIES_INSTALLED
            MaxSdk.SetHasUserConsent(isConsent);
            MaxSdk.SetDoNotSell(!isConsent);
#endif
        }
    }
}