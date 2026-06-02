using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameUpSDK.Ads
{
    public class AdmobNetwork : MonoBehaviour, IAdNetwork
    {
        [SerializeField] private List<string> testDevices;

        [Header("Ad Unit Configs")]
        public AdUnitConfig interstitialConfig;
        public AdUnitConfig rewardedConfig;
        public AdUnitConfig appOpenConfig;
        public AdUnitConfig bannerConfig;
        public AdUnitConfig nativeAdConfig;

        public bool IsInitialized { get; private set; }

        public Action<IAdNetwork> OnInitialized { get; set; }
        
        public MediationProvider MediationProvider { get; set; } = MediationProvider.Admob;
        public IInterstitialAd InterstitialAd { get; private set; }
        public IRewardedAd RewardedAd { get; private set; }
        public IAppOpenAd AppOpenAd { get; private set; }
        public IBannerAd BannerAd { get; private set; }
        
        public INativeFullScreenAd NativeFullScreenAd { get; private set; }

        public void Initialize()
        {
#if ADMOB_DEPENDENCIES_INSTALLED
            if (IsInitialized) return;
            GoogleMobileAds.Api.RequestConfiguration config = new GoogleMobileAds.Api.RequestConfiguration { TestDeviceIds = testDevices };
            GoogleMobileAds.Api.MobileAds.SetRequestConfiguration(config);
            
            GoogleMobileAds.Api.MobileAds.Initialize(initStatus =>
            {
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    IsInitialized = true;
                    Debug.Log("[GameUp] AdmobNetwork Initialized.");

                    InterstitialAd = new AdmobInterstitialAd(interstitialConfig);
                    RewardedAd = new AdmobRewardedAd(rewardedConfig);
                    AppOpenAd = new AdmobAppOpenAd(appOpenConfig);
                    BannerAd = new AdmobBannerAd(bannerConfig);
                    NativeFullScreenAd = new AdmobNativeFullscreenAd(nativeAdConfig);

                    InterstitialAd.Load();
                    RewardedAd.Load();
                    AppOpenAd.Load();
                    BannerAd.Load();
                    NativeFullScreenAd.Load();
                    
                    OnInitialized?.Invoke(this);
                });
            });
#endif
        }
        
        public void SetConsent(bool isConsent) { }
    }
}