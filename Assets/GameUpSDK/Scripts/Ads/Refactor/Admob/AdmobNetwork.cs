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
            var timeInit = Time.realtimeSinceStartup;
            Debug.LogError($"Initializing Admob Network: {Time.realtimeSinceStartup}");
            GoogleMobileAds.Api.RequestConfiguration config = new GoogleMobileAds.Api.RequestConfiguration { TestDeviceIds = testDevices };
            GoogleMobileAds.Api.MobileAds.SetRequestConfiguration(config);
            
            GoogleMobileAds.Api.MobileAds.Initialize(initStatus =>
            {
                Debug.LogError($"Initialized Admob Network: {Time.realtimeSinceStartup} - Total time initialized: {Time.realtimeSinceStartup - timeInit}");
                foreach (var adapter in initStatus.getAdapterStatusMap())
                {
                    Debug.Log($"Adapter status: {adapter.Key} - {adapter.Value.InitializationState} - {adapter.Value.Latency} - {adapter.Value.Description}");
                }
                
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    IsInitialized = true;
                    Debug.Log("[GameUp] AdmobNetwork Initialized.");

                    InterstitialAd = new AdmobInterstitialAd(interstitialConfig);
                    RewardedAd = new AdmobRewardedAd(rewardedConfig);
                    AppOpenAd = new AdmobAppOpenAd(appOpenConfig);
                    BannerAd = new AdmobBannerAd(bannerConfig);
                    NativeFullScreenAd = new AdmobNativeFullscreenAd(nativeAdConfig);

                    InterstitialAd.LoadAll();
                    RewardedAd.LoadAll();
                    AppOpenAd.LoadAll();
                    BannerAd.LoadAll();
                    NativeFullScreenAd.LoadAll();
                    
                    OnInitialized?.Invoke(this);
                });
            });
#endif
        }
        
        public void SetConsent(bool isConsent) { }
    }
}