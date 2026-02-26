using System;
using UnityEngine;
using Unity.Services.LevelPlay;

namespace GameUpSDK
{
    /// <summary>
    /// IronSource (LevelPlay) Mediation implementation of IAds.
    /// Chỉ cần App Key để lấy quảng cáo; AdMob và Unity Ads chạy qua mediation.
    /// Nếu không nhập Ad Unit ID, dùng placement mặc định (DefaultBanner, DefaultInterstitial, DefaultRewardedVideo).
    /// LevelPlay không hỗ trợ App Open; các method App Open no-op / return false.
    /// </summary>
    public class IronSourceAds : MonoBehaviour, IAds
    {
        private const string DefaultBannerId = "DefaultBanner";
        private const string DefaultInterstitialId = "DefaultInterstitial";
        private const string DefaultRewardedId = "DefaultRewardedVideo";

        [Header("LevelPlay App Key (bắt buộc - lấy từ LevelPlay dashboard)")]
        [SerializeField] private string levelPlayAppKey;

        [Header("Ad Unit / Placement IDs (để trống = dùng placement mặc định)")]
        [SerializeField] private string bannerAdUnitId;
        [SerializeField] private string interstitialAdUnitId;
        [SerializeField] private string rewardedVideoAdUnitId;

        public int OrderExecute { get; set; }

        public event Action OnInterstitialLoaded;
        public event Action<string> OnInterstitialLoadFailed;
        public event Action OnRewardedLoaded;
        public event Action<string> OnRewardedLoadFailed;

        private bool _initialized;
        private LevelPlayBannerAd _bannerAd;
        private LevelPlayInterstitialAd _interstitialAd;
        private LevelPlayRewardedAd _rewardedAd;

        public void SetLevelPlayConfig(string appKey, string bannerId, string interstitialId, string rewardedId)
        {
            levelPlayAppKey = appKey;
            bannerAdUnitId = bannerId;
            interstitialAdUnitId = interstitialId;
            rewardedVideoAdUnitId = rewardedId;
        }

        public void Initialize()
        {
            if (_initialized)
            {
                Debug.Log("[CtySDK] IronSourceAds already initialized.");
                return;
            }

            if (string.IsNullOrEmpty(levelPlayAppKey))
            {
                Debug.LogWarning("[CtySDK] IronSourceAds: LevelPlay App Key not set.");
                _initialized = true;
                return;
            }

            LevelPlay.OnInitSuccess += OnLevelPlayInitSuccess;
            LevelPlay.OnInitFailed += OnLevelPlayInitFailed;
            LevelPlay.Init(levelPlayAppKey);
        }

        private void OnLevelPlayInitSuccess(LevelPlayConfiguration config)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                _initialized = true;
                LevelPlay.OnInitSuccess -= OnLevelPlayInitSuccess;
                LevelPlay.OnInitFailed -= OnLevelPlayInitFailed;
                CreateAdUnits();
                SubscribeToAdEvents();
                SubscribeToImpressionData();
                // Request ads ngay khi LevelPlay sẵn sàng (tránh RequestAll() gọi trước khi init xong).
                RequestBanner();
                RequestInterstitial();
                RequestRewardedVideo();
                Debug.Log("[CtySDK] IronSourceAds (LevelPlay) initialized.");
            });
        }

        /// <summary>
        /// Subscribe to LevelPlay impression data (fired after ad is shown with revenue). Forward to AdsEvent for GameUpAnalytics.LogAdImpression.
        /// OnImpressionDataReady runs on background thread → dispatch to main thread then raise.
        /// </summary>
        private void SubscribeToImpressionData()
        {
            LevelPlay.OnImpressionDataReady += OnLevelPlayImpressionDataReady;
        }

        private void OnLevelPlayImpressionDataReady(LevelPlayImpressionData levelPlayData)
        {
            if (levelPlayData == null || !levelPlayData.Revenue.HasValue)
                return;
            var data = new AdImpressionData
            {
                AdNetwork = levelPlayData.AdNetwork,
                AdUnit = levelPlayData.MediationAdUnitName ?? levelPlayData.MediationAdUnitId,
                InstanceName = levelPlayData.InstanceName,
                AdFormat = levelPlayData.AdFormat,
                Revenue = levelPlayData.Revenue
            };
            MainThreadDispatcher.Enqueue(() => AdsEvent.RaiseImpressionDataReady(data));
        }

        private void SubscribeToAdEvents()
        {
            if (_interstitialAd != null)
            {
                _interstitialAd.OnAdLoaded += _ => MainThreadDispatcher.Enqueue(() => OnInterstitialLoaded?.Invoke());
                _interstitialAd.OnAdLoadFailed += (error) => MainThreadDispatcher.Enqueue(() =>
                    OnInterstitialLoadFailed?.Invoke(error?.ErrorMessage ?? error?.ErrorCode.ToString() ?? "unknown"));
            }
            if (_rewardedAd != null)
            {
                _rewardedAd.OnAdLoaded += _ => MainThreadDispatcher.Enqueue(() => OnRewardedLoaded?.Invoke());
                _rewardedAd.OnAdLoadFailed += (error) => MainThreadDispatcher.Enqueue(() =>
                    OnRewardedLoadFailed?.Invoke(error?.ErrorMessage ?? error?.ErrorCode.ToString() ?? "unknown"));
            }
        }

        private void OnLevelPlayInitFailed(LevelPlayInitError error)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                _initialized = true;
                LevelPlay.OnInitSuccess -= OnLevelPlayInitSuccess;
                LevelPlay.OnInitFailed -= OnLevelPlayInitFailed;
                Debug.Log("[CtySDK] IronSourceAds LevelPlay init failed: " + error);
            });
        }

        private void CreateAdUnits()
        {
            var bannerId = string.IsNullOrEmpty(bannerAdUnitId) ? DefaultBannerId : bannerAdUnitId;
            var interId = string.IsNullOrEmpty(interstitialAdUnitId) ? DefaultInterstitialId : interstitialAdUnitId;
            var rewardId = string.IsNullOrEmpty(rewardedVideoAdUnitId) ? DefaultRewardedId : rewardedVideoAdUnitId;

            // Dùng size chuẩn để banner có fill. Custom (width x 150) dễ bị network từ chối.
            // SetDisplayOnLoad(false): chỉ load khi RequestBanner/LoadAd, không tự hiện; chỉ hiện khi AdsManager gọi ShowBanner -> ShowAd().
            var bannerConfig = new LevelPlayBannerAd.Config.Builder()
                .SetSize(LevelPlayAdSize.LARGE) // 320x90 dp, chuẩn và cao hơn BANNER (320x50)
                .SetPosition(LevelPlayBannerPosition.BottomCenter)
                .SetDisplayOnLoad(false)
                .Build();
            _bannerAd = new LevelPlayBannerAd(bannerId, bannerConfig);
            _interstitialAd = new LevelPlayInterstitialAd(interId);
            _rewardedAd = new LevelPlayRewardedAd(rewardId);
        }

        public void SetAfterCheckGDPR()
        {
            LevelPlay.SetConsent(true);
            Debug.Log("[CtySDK] IronSourceAds SetAfterCheckGDPR (consent set).");
        }

        /// <summary>Bridges to LevelPlay's loading: triggers LoadAd on the banner.</summary>
        public void RequestBanner()
        {
            _bannerAd?.LoadAd();
        }

        public void RequestInterstitial()
        {
            _interstitialAd?.LoadAd();
        }

        public void RequestRewardedVideo()
        {
            _rewardedAd?.LoadAd();
        }

        /// <summary>LevelPlay does not support App Open; no-op.</summary>
        public void RequestAppOpenAds() { }

        public bool IsBannerAvailable()
        {
            return _bannerAd != null;
        }

        public bool IsInterstitialAvailable()
        {
            return _interstitialAd != null && _interstitialAd.IsAdReady();
        }

        public bool IsRewardedVideoAvailable()
        {
            return _rewardedAd != null && _rewardedAd.IsAdReady();
        }

        public bool IsAppOpenAdsAvailable()
        {
            return false;
        }

        public void ShowBanner(string where)
        {
            _bannerAd?.ShowAd();
        }

        public void HideBanner(string where)
        {
            _bannerAd?.HideAd();
        }

        public void ShowInterstitial(string where, Action onSuccess, Action onFail)
        {
            if (_interstitialAd == null || !_interstitialAd.IsAdReady())
            {
                onFail?.Invoke();
                return;
            }
            _interstitialAd.OnAdClosed += OnInterstitialClosed;
            _interstitialAd.OnAdDisplayFailed += OnInterstitialDisplayFailed;

            void OnInterstitialClosed(LevelPlayAdInfo _)
            {
                _interstitialAd.OnAdClosed -= OnInterstitialClosed;
                _interstitialAd.OnAdDisplayFailed -= OnInterstitialDisplayFailed;
                MainThreadDispatcher.Enqueue(() => onSuccess?.Invoke());
                RequestInterstitial();
            }

            void OnInterstitialDisplayFailed(LevelPlayAdInfo _, LevelPlayAdError __)
            {
                _interstitialAd.OnAdClosed -= OnInterstitialClosed;
                _interstitialAd.OnAdDisplayFailed -= OnInterstitialDisplayFailed;
                MainThreadDispatcher.Enqueue(() => onFail?.Invoke());
                RequestInterstitial();
            }

            _interstitialAd.ShowAd(where);
        }

        public void ShowRewardedVideo(string where, Action onSuccess, Action onFail)
        {
            if (_rewardedAd == null || !_rewardedAd.IsAdReady())
            {
                onFail?.Invoke();
                return;
            }
            var rewardGranted = false;
            _rewardedAd.OnAdClosed += OnRewardedClosed;
            _rewardedAd.OnAdRewarded += OnRewardedEarned;
            _rewardedAd.OnAdDisplayFailed += OnRewardedDisplayFailed;

            void OnRewardedClosed(LevelPlayAdInfo _)
            {
                _rewardedAd.OnAdClosed -= OnRewardedClosed;
                _rewardedAd.OnAdRewarded -= OnRewardedEarned;
                _rewardedAd.OnAdDisplayFailed -= OnRewardedDisplayFailed;
                if (!rewardGranted)
                    MainThreadDispatcher.Enqueue(() => onFail?.Invoke());
                RequestRewardedVideo();
            }

            void OnRewardedEarned(LevelPlayAdInfo _, LevelPlayReward __)
            {
                rewardGranted = true;
                MainThreadDispatcher.Enqueue(() => onSuccess?.Invoke());
            }

            void OnRewardedDisplayFailed(LevelPlayAdInfo _, LevelPlayAdError __)
            {
                _rewardedAd.OnAdClosed -= OnRewardedClosed;
                _rewardedAd.OnAdRewarded -= OnRewardedEarned;
                _rewardedAd.OnAdDisplayFailed -= OnRewardedDisplayFailed;
                MainThreadDispatcher.Enqueue(() => onFail?.Invoke());
                RequestRewardedVideo();
            }

            _rewardedAd.ShowAd(where);
        }

        public void ShowAppOpenAds(string where, Action onSuccess, Action onFail)
        {
            onFail?.Invoke();
        }

        private void OnDestroy()
        {
            LevelPlay.OnImpressionDataReady -= OnLevelPlayImpressionDataReady;
            _bannerAd?.DestroyAd();
            _bannerAd = null;
            _interstitialAd?.DestroyAd();
            _interstitialAd = null;
            _rewardedAd?.Dispose();
            _rewardedAd = null;
        }
    }
}
