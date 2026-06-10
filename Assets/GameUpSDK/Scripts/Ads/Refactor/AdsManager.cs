using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameUpSDK.Singletons;

namespace GameUpSDK.Ads
{
    public enum BannerSize
    {
        /// <summary>320 × 50 – kích thước nhỏ nhất, phổ biến nhất.</summary>
        Banner,

        /// <summary>320 × 90 – lớn hơn BANNER, fill rate tốt. Mặc định.</summary>
        Large,

        /// <summary>
        /// Chiều rộng toàn màn hình, chiều cao tự điều chỉnh theo màn hình.
        /// Fill rate cao nhất – được IronSource/LevelPlay khuyến nghị.
        /// </summary>
        Adaptive,

        /// <summary>300 × 250 – Medium Rectangle (MREC), thường dùng trong content.</summary>
        MediumRectangle,

        /// <summary>728 × 90 – chỉ phù hợp trên iPad / tablet.</summary>
        Leaderboard,
    }
    
    public enum BannerFormatType
    {
        StandardBanner,
        NativeOverlay
    }

    [DefaultExecutionOrder(-50)]
    public class AdsManager : MonoSingletonSdk<AdsManager>
    {
        [Header("Waterfall Configuration")]
        [Tooltip(
            "Danh sách ưu tiên mạng quảng cáo. Mạng ở Index 0 là Chính. Rớt xuống Index 1, 2... nếu mạng trên lỗi.")]
        public List<MediationProvider> mediationPriority = new List<MediationProvider>
            { MediationProvider.Max, MediationProvider.Admob, MediationProvider.IronSource };

        private readonly HashSet<string> _activeBanners = new HashSet<string>();
        private readonly Dictionary<MediationProvider, IAdNetwork> _networkDict =
            new Dictionary<MediationProvider, IAdNetwork>();

        private AdsTracker _tracker;

        private readonly List<IAdCondition> _showConditions = new List<IAdCondition>();

        public static Action<string> OnBannerLoadedEvent = delegate { };
        
        public bool IsInitialized { get; private set; }
        
        protected void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _tracker = gameObject.AddComponent<AdsTracker>();
            IAdNetwork[] foundNetworks = GetComponentsInChildren<IAdNetwork>(true);
            foreach (var provider in mediationPriority)
            {
                var network = foundNetworks.FirstOrDefault(s => s.MediationProvider == provider);
                if (network != null)
                {
                    _networkDict.Add(provider, network);
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
            AdsEvent.OnBannerSwap -= OnBannerSwapped;
        }

        private void Update()
        {
            MainThreadDispatcher.ProcessQueue();
        }

        private void InitializeAll()
        {
            AdsEvent.OnImpressionDataReady -= GameUpAnalytics.LogAdImpression;
            AdsEvent.OnImpressionDataReady += GameUpAnalytics.LogAdImpression;
            
            AdsEvent.OnBannerSwap -= OnBannerSwapped;
            AdsEvent.OnBannerSwap += OnBannerSwapped;

            foreach (var provider in mediationPriority)
            {
                if (provider == MediationProvider.None) continue;
                if (_networkDict.TryGetValue(provider, out var network))
                {
                    if (!network.IsInitialized)
                    {
                        network.OnInitialized += OnInitializedNetwork;
                        network.Initialize();
                    }
                }
            }
        }

        private void OnInitializedNetwork(IAdNetwork network)
        {
            _tracker.SubscribeToNetwork(network);
            WireUpCappingEvents(network);
            if (network.BannerAd != null)
            {
                network.BannerAd.OnAdLoaded += OnBannerLoaded;
            }
            IsInitialized = true;
        }

        private void OnBannerSwapped(string last, string current)
        {
            _activeBanners.Remove(last);
            if (!string.IsNullOrEmpty(current))
            {
                _activeBanners.Add(current);
            }
        }
        
        private void OnBannerLoaded(string where)
        {
            Debug.Log($"OnBannerLoaded: {where}");
            if (!EvaluateConditions(AdUnitType.Banner, where, out var blockReason))
            {
                HideBanner(where);
            }
            
            OnBannerLoadedEvent.Invoke(where);
        }
        
        private void TemporarilyHideBanners()
        {
            foreach (var provider in mediationPriority)
            {
                if (provider == MediationProvider.None) continue;
                if (_networkDict.TryGetValue(provider, out var network) && network.BannerAd != null)
                {
                    foreach (var placement in _activeBanners)
                    {
                        network.BannerAd.Hide(placement);
                    }
                }
            }
        }
        
        private void RestoreBanners()
        {
            foreach (var provider in mediationPriority)
            {
                if (provider == MediationProvider.None) continue;
                if (_networkDict.TryGetValue(provider, out var network) && network.BannerAd != null)
                {
                    foreach (var placement in _activeBanners)
                    {
                        network.BannerAd.Restore(placement);
                    }
                }
            }
        }

        public void SetConsent(bool isConsent)
        {
            foreach (var network in _networkDict.Values) network.SetConsent(isConsent);
        }

        private void WireUpCappingEvents(IAdNetwork network)
        {
            Action<string> pauseAct = (where) =>
            {
                AdCappingManager.Instance.PauseAllCapping();
                TemporarilyHideBanners();
            };

            if (network.InterstitialAd != null)
            {
                network.InterstitialAd.OnAdDisplayed += pauseAct;
                network.InterstitialAd.OnAdClosed += (where) =>
                {
                    AdCappingManager.Instance.ResumeAllCapping();
                    AdCappingManager.Instance.ResetCapping(AdUnitType.Interstitial);
                    RestoreBanners();
                    AdHistoryTracker.MarkAdClosed(AdUnitType.Interstitial);
                };
            }

            if (network.RewardedAd != null)
            {
                network.RewardedAd.OnAdDisplayed += pauseAct;
                network.RewardedAd.OnAdClosed += (where) =>
                {
                    AdCappingManager.Instance.ResumeAllCapping();
                    AdCappingManager.Instance.ResetCapping(AdUnitType.RewardedVideo);
                    RestoreBanners();
                    AdHistoryTracker.MarkAdClosed(AdUnitType.RewardedVideo);
                };
            }

            if (network.AppOpenAd != null)
            {
                network.AppOpenAd.OnAdDisplayed += pauseAct;
                network.AppOpenAd.OnAdClosed += (where) =>
                {
                    AdCappingManager.Instance.ResumeAllCapping();
                    AdCappingManager.Instance.ResetCapping(AdUnitType.AppOpen);
                    RestoreBanners();
                    AdHistoryTracker.MarkAdClosed(AdUnitType.AppOpen);
                };
            }

            if (network.NativeFullScreenAd != null)
            {
                network.NativeFullScreenAd.OnAdDisplayed += pauseAct;
                network.NativeFullScreenAd.OnAdClosed += (where) =>
                {
                    AdCappingManager.Instance.ResumeAllCapping();
                    AdCappingManager.Instance.ResetCapping(AdUnitType.NativeAd);
                    RestoreBanners();
                    AdHistoryTracker.MarkAdClosed(AdUnitType.NativeAd);
                };
            }
        }

        public void AddCondition(IAdCondition condition)
        {
            if (!_showConditions.Contains(condition)) _showConditions.Add(condition);
        }

        private bool EvaluateConditions(AdUnitType adType, string where, out string blockReason)
        {
            foreach (var condition in _showConditions)
            {
                if (!condition.CanShow(adType, where, out blockReason))
                {
                    return false;
                }
            }

            blockReason = string.Empty;
            return true;
        }

        public IAdNetwork GetAvailableProvider(AdUnitType adType, string where)
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
                        AdUnitType.NativeAd => network.NativeFullScreenAd != null &&
                                               network.NativeFullScreenAd.IsAvailable(where),
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
                LoadAd(AdUnitType.RewardedVideo, where);
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
            if (!EvaluateConditions(AdUnitType.Interstitial, where, out var blockReason))
            {
                Debug.Log($"[GameUpSDK] Interstitial block rules: {blockReason}");
                onFail?.Invoke();
                return;
            }

            _tracker.LogAdsEventManager(AdsEvent.AdsRequest, AdsEvent.AdTypeInterstitial, where);
            var network = GetAvailableProvider(AdUnitType.Interstitial, where);

            if (network == null)
            {
                _tracker.LogAdsEventManager(AdsEvent.AdsShowFail, AdsEvent.AdTypeInterstitial, where,
                    "no_ads_available");
                onFail?.Invoke();
                LoadAd(AdUnitType.Interstitial, where);
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
            if (!EvaluateConditions(AdUnitType.AppOpen, where, out var blockReason))
            {
                Debug.Log($"[GameUpSDK] AppOpenAd block rules: {blockReason}");
                onFail?.Invoke();
                return;
            }

            _tracker.LogAdsEventManager(AdsEvent.AdsRequest, AdsEvent.AdTypeAppOpen, where);
            var network = GetAvailableProvider(AdUnitType.AppOpen, where);

            if (network == null)
            {
                _tracker.LogAdsEventManager(AdsEvent.AdsShowFail, AdsEvent.AdTypeAppOpen, where, "no_ads_available");
                onFail?.Invoke();
                LoadAd(AdUnitType.AppOpen, where);
                return;
            }

            _tracker.LogAdsEventManager(AdsEvent.AdsAvailable, AdsEvent.AdTypeAppOpen, where);
            network.AppOpenAd.Show(where, onSuccess, onFail);
        }

        public bool IsBannerAvailable(string where = null) => GetAvailableProvider(AdUnitType.Banner, where) != null;

        public void ShowBanner(string where)
        {
            if (!EvaluateConditions(AdUnitType.Banner, where, out var blockReason))
            {
                Debug.Log($"[GameUpSDK] Banner block rules: {blockReason}");
                return;
            }

            _tracker.LogAdsEventManager(AdsEvent.AdsRequest, AdsEvent.AdTypeBanner, where);

            var network = GetAvailableProvider(AdUnitType.Banner, where);
            if (network == null)
            {
                _tracker.LogAdsEventManager(AdsEvent.AdsShowFail, AdsEvent.AdTypeBanner, where, "no_ads_available");
                LoadAd(AdUnitType.Banner, where);
                return;
            }

            _tracker.LogAdsEventManager(AdsEvent.AdsAvailable, AdsEvent.AdTypeBanner, where);
            _activeBanners.Add(where);
            network.BannerAd.Show(where);
        }

        public void HideBanner(string where)
        {
            _activeBanners.Remove(where);
            foreach (var network in _networkDict.Values) network.BannerAd?.Hide(where);
        }

        public bool IsNativeAdAvailable(string where = null) =>
            GetAvailableProvider(AdUnitType.NativeAd, where) != null;


        public void ShowNativeAd(string where, Action onSuccess, Action onFail)
        {
            if (!EvaluateConditions(AdUnitType.NativeAd, where, out var blockReason))
            {
                Debug.Log($"[GameUpSDK] NativeAd block rules: {blockReason}");
                onFail?.Invoke();
                return;
            }

            _tracker.LogAdsEventManager(AdsEvent.AdsRequest, AdsEvent.AdTypeNativeAd, where);
            var network = GetAvailableProvider(AdUnitType.NativeAd, where);

            if (network == null)
            {
                _tracker.LogAdsEventManager(AdsEvent.AdsShowFail, AdsEvent.AdTypeNativeAd, where, "network_null");
                onFail?.Invoke();
                return;
            }

            _tracker.LogAdsEventManager(AdsEvent.AdsAvailable, AdsEvent.AdTypeNativeAd, where);
            network.NativeFullScreenAd.Show(where, onSuccess, onFail);
        }

        public void HideNativeAd(string where = null)
        {
            foreach (var network in _networkDict)
            {
                network.Value.NativeFullScreenAd.Hide();
            }
        }

        public void LoadAd(AdUnitType adType, string where = null)
        {
            switch (adType)
            {
                case AdUnitType.Banner:
                    foreach (var network in _networkDict)
                    {
                        network.Value.BannerAd?.Load(where);
                    }

                    break;
                case AdUnitType.Interstitial:
                    foreach (var network in _networkDict)
                    {
                        network.Value.InterstitialAd?.Load(where);
                    }

                    break;
                case AdUnitType.RewardedVideo:
                    foreach (var network in _networkDict)
                    {
                        network.Value.RewardedAd?.Load(where);
                    }

                    break;
                case AdUnitType.AppOpen:
                    foreach (var network in _networkDict)
                    {
                        network.Value.AppOpenAd?.Load(where);
                    }

                    break;
                case AdUnitType.NativeAd:
                    foreach (var network in _networkDict)
                    {
                        network.Value.NativeFullScreenAd?.Load(where);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(adType), adType, null);
            }
        }
    }
}