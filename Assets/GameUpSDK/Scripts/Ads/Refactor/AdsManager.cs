using System;
using System.Collections.Generic;
using UnityEngine;
using GameUpSDK.Singletons;

namespace GameUpSDK.Ads
{
    [DefaultExecutionOrder(-50)]
    public class AdsManager : MonoSingletonSdk<AdsManager>
    {
        [Header("Waterfall Configuration")]
        [Tooltip(
            "Danh sách ưu tiên mạng quảng cáo. Mạng ở Index 0 là Chính. Rớt xuống Index 1, 2... nếu mạng trên lỗi.")]
        public List<MediationProvider> mediationPriority = new List<MediationProvider>
            { MediationProvider.Max, MediationProvider.Admob, MediationProvider.IronSource };

        [Header("Banner Settings")] [SerializeField]
        private BannerSize bannerSize = BannerSize.Large;

        private readonly Dictionary<MediationProvider, IAdNetwork> _networkDict =
            new Dictionary<MediationProvider, IAdNetwork>();

        private AdsTracker _tracker;

        protected void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _tracker = gameObject.AddComponent<AdsTracker>();

            IAdNetwork[] foundNetworks = GetComponentsInChildren<IAdNetwork>(true);
            foreach (var network in foundNetworks)
            {
                if (_networkDict.TryAdd(network.MediationProvider, network))
                {
                    _tracker.SubscribeToNetwork(network);
                }
            }
        }

        private void Start()
        {
            PrivacyManager.Instance.BeginPrivacyFlow(SetConsent);
            InitializeAll();
        }

        private void OnDestroy()
        {
            AdsEvent.OnImpressionDataReady -= GameUpAnalytics.LogAdImpression;
        }

        private void Update()
        {
            MainThreadDispatcher.ProcessQueue();
        }

        private void InitializeAll()
        {
            AdsEvent.OnImpressionDataReady -= GameUpAnalytics.LogAdImpression;
            AdsEvent.OnImpressionDataReady += GameUpAnalytics.LogAdImpression;

            foreach (var provider in mediationPriority)
            {
                if (provider == MediationProvider.None) continue;
                if (_networkDict.TryGetValue(provider, out var network))
                {
                    if (!network.IsInitialized) network.Initialize();
                }
            }
        }

        private void SetConsent(bool isConsent)
        {
            foreach (var network in _networkDict.Values) network.SetConsent(isConsent);
        }

        private IAdNetwork GetAvailableProvider(AdUnitType adType, string where)
        {
            foreach (var provider in mediationPriority)
            {
                if (provider == MediationProvider.None) continue;

                if (_networkDict.TryGetValue(provider, out var network))
                {
                    bool isAvailable = adType switch
                    {
                        AdUnitType.RewardedVideo => network.RewardedAd != null && network.RewardedAd.IsAvailable(where),
                        AdUnitType.Interstitial => network.InterstitialAd != null &&
                                                   network.InterstitialAd.IsAvailable(where),
                        AdUnitType.AppOpen => network.AppOpenAd != null && network.AppOpenAd.IsAvailable(where),
                        AdUnitType.Banner => network.BannerAd != null && network.BannerAd.IsAvailable(where),
                        _ => false
                    };
                    if (isAvailable) return network;
                }
            }

            return null;
        }

        public bool IsRewardedVideoAvailable(string where = null) =>
            GetAvailableProvider(AdUnitType.RewardedVideo, where) != null;

        public void ShowRewardedVideo(string where, Action onSuccess = null, Action onFail = null) =>
            ShowRewardedVideo(where, 0, onSuccess, onFail);

        public void ShowRewardedVideo(string where, int currentLevel, Action onSuccess = null, Action onFail = null)
        {
            _tracker.LogAdsEventManager(AdsEvent.AdsRequest, AdsEvent.AdTypeRewardedVideo, where);
            var network = GetAvailableProvider(AdUnitType.RewardedVideo, where);

            if (network == null)
            {
                _tracker.LogAdsEventManager(AdsEvent.AdsShowFail, AdsEvent.AdTypeRewardedVideo, where,
                    "no_ads_available");
                onFail?.Invoke();
                return;
            }

            _tracker.LogAdsEventManager(AdsEvent.AdsAvailable, AdsEvent.AdTypeRewardedVideo, where);
            _tracker.RegisterPlacementLevel(where, currentLevel);
            network.RewardedAd.Show(where, onSuccess, onFail);
        }

        public bool IsInterstitialAvailable(string where = null) =>
            GetAvailableProvider(AdUnitType.Interstitial, where) != null;

        public void ShowInterstitial(string where, int currentLevel, Action onSuccess = null,
            Action onFail = null)
        {
            _tracker.LogAdsEventManager(AdsEvent.AdsRequest, AdsEvent.AdTypeInterstitial, where);
            var network = GetAvailableProvider(AdUnitType.Interstitial, where);

            if (network == null)
            {
                _tracker.LogAdsEventManager(AdsEvent.AdsShowFail, AdsEvent.AdTypeInterstitial, where, "network_null");
                onFail?.Invoke();
                return;
            }

            _tracker.LogAdsEventManager(AdsEvent.AdsAvailable, AdsEvent.AdTypeInterstitial, where);
            _tracker.RegisterPlacementLevel(where, currentLevel);
            network.InterstitialAd.Show(where, onSuccess, onFail);
        }

        public bool IsAppOpenAdAvailable(string where = null) =>
            GetAvailableProvider(AdUnitType.AppOpen, where) != null;

        public void ShowAppOpenAds(string where, Action onSuccess = null, Action onFail = null)
        {
            _tracker.LogAdsEventManager(AdsEvent.AdsRequest, AdsEvent.AdTypeAppOpen, where);
            var network = GetAvailableProvider(AdUnitType.AppOpen, where);

            if (network == null)
            {
                _tracker.LogAdsEventManager(AdsEvent.AdsShowFail, AdsEvent.AdTypeAppOpen, where, "network_null");
                onFail?.Invoke();
                return;
            }

            _tracker.LogAdsEventManager(AdsEvent.AdsAvailable, AdsEvent.AdTypeAppOpen, where);
            network.AppOpenAd.Show(where, onSuccess, onFail);
        }

        public bool IsBannerAvailable(string where = null) => GetAvailableProvider(AdUnitType.Banner, where) != null;

        public void ShowBanner(string where)
        {
            if (!AdsRules.IsBannerEnabled()) return;
            _tracker.LogAdsEventManager(AdsEvent.AdsRequest, AdsEvent.AdTypeBanner, where);

            var network = GetAvailableProvider(AdUnitType.Banner, where);
            if (network == null)
            {
                _tracker.LogAdsEventManager(AdsEvent.AdsShowFail, AdsEvent.AdTypeBanner, where, "network_null");
                return;
            }

            _tracker.LogAdsEventManager(AdsEvent.AdsAvailable, AdsEvent.AdTypeBanner, where);
            network.BannerAd.Show(where, CollapsibleBannerPlacement.None);
        }

        public void ShowCollapsibleBanner(string where,
            CollapsibleBannerPlacement placement = CollapsibleBannerPlacement.Bottom)
        {
            if (!AdsRules.IsBannerEnabled()) return;
            _tracker.LogAdsEventManager(AdsEvent.AdsRequest, AdsEvent.AdTypeBanner, where);

            var network = GetAvailableProvider(AdUnitType.Banner, where);
            if (network == null)
            {
                _tracker.LogAdsEventManager(AdsEvent.AdsShowFail, AdsEvent.AdTypeBanner, where, "network_null");
                return;
            }

            _tracker.LogAdsEventManager(AdsEvent.AdsAvailable, AdsEvent.AdTypeBanner, where);
            network.BannerAd.Show(where, placement);
        }

        public void HideBanner(string where)
        {
            foreach (var network in _networkDict.Values) network.BannerAd?.Hide(where);
        }
    }
}