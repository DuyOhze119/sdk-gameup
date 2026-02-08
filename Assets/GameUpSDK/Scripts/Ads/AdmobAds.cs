using System;
using UnityEngine;
#if UNITY_ANDROID || UNITY_IPHONE
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
#endif

namespace GameUpSDK
{
    /// <summary>
    /// AdMob (Google Mobile Ads) implementation of IAds. Handles Banner, Interstitial, Rewarded, and App Open.
    /// </summary>
    public class AdmobAds : MonoBehaviour, IAds
    {
        [Header("Ad Unit IDs (optional - set via code)")]
        [SerializeField] private string bannerAdUnitId;
        [SerializeField] private string interstitialAdUnitId;
        [SerializeField] private string rewardedAdUnitId;
        [SerializeField] private string appOpenAdUnitId;

        public int OrderExecute { get; set; }

        public event Action OnInterstitialLoaded;
        public event Action<string> OnInterstitialLoadFailed;
        public event Action OnRewardedLoaded;
        public event Action<string> OnRewardedLoadFailed;

        private bool _initialized;

#if UNITY_ANDROID || UNITY_IPHONE
        private BannerView _bannerView;
        private InterstitialAd _interstitialAd;
        private RewardedAd _rewardedAd;
        private AppOpenAd _appOpenAd;
        private DateTime _appOpenExpireTime = DateTime.MinValue;
        private const int AppOpenTimeoutHours = 4;
#endif

        public void SetAdUnitIds(string banner, string interstitial, string rewarded, string appOpen)
        {
            bannerAdUnitId = banner;
            interstitialAdUnitId = interstitial;
            rewardedAdUnitId = rewarded;
            appOpenAdUnitId = appOpen;
        }

        public void Initialize()
        {
#if UNITY_ANDROID || UNITY_IPHONE
            if (_initialized)
            {
                Debug.Log("[GameUp] AdmobAds already initialized.");
                return;
            }

            MobileAds.Initialize(initStatus =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    _initialized = true;
                    Debug.Log("[GameUp] AdmobAds initialized.");
                });
            });
#else
            _initialized = true;
            Debug.Log("[GameUp] AdmobAds skipped (not mobile platform).");
#endif
        }

        public void SetAfterCheckGDPR()
        {
#if UNITY_ANDROID || UNITY_IPHONE
            // Consent is typically handled by UMP; SDK respects it after init.
            Debug.Log("[GameUp] AdmobAds SetAfterCheckGDPR called.");
#endif
        }

        public void RequestBanner()
        {
#if UNITY_ANDROID || UNITY_IPHONE
            if (string.IsNullOrEmpty(bannerAdUnitId)) return;
            MainThreadDispatcher.Enqueue(() =>
            {
                if (_bannerView != null) { _bannerView.Destroy(); _bannerView = null; }
                _bannerView = new BannerView(bannerAdUnitId, AdSize.Banner, AdPosition.Bottom);
                var request = new AdRequest();
                _bannerView.LoadAd(request);
            });
#endif
        }

        public void RequestInterstitial()
        {
#if UNITY_ANDROID || UNITY_IPHONE
            if (string.IsNullOrEmpty(interstitialAdUnitId)) return;
            var request = new AdRequest();
            InterstitialAd.Load(interstitialAdUnitId, request, (ad, error) =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    if (error != null || ad == null)
                    {
                        var source = error?.GetMessage() ?? (error != null ? error.GetCode().ToString() : "unknown");
                        OnInterstitialLoadFailed?.Invoke(source);
                        return;
                    }
                    if (_interstitialAd != null) _interstitialAd.Destroy();
                    _interstitialAd = ad;
                    RegisterInterstitialEvents(ad);
                    OnInterstitialLoaded?.Invoke();
                });
            });
#endif
        }

        public void RequestRewardedVideo()
        {
#if UNITY_ANDROID || UNITY_IPHONE
            if (string.IsNullOrEmpty(rewardedAdUnitId)) return;
            var request = new AdRequest();
            RewardedAd.Load(rewardedAdUnitId, request, (ad, error) =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    if (error != null || ad == null)
                    {
                        var source = error?.GetMessage() ?? (error != null ? error.GetCode().ToString() : "unknown");
                        OnRewardedLoadFailed?.Invoke(source);
                        return;
                    }
                    if (_rewardedAd != null) _rewardedAd.Destroy();
                    _rewardedAd = ad;
                    OnRewardedLoaded?.Invoke();
                });
            });
#endif
        }

        public void RequestAppOpenAds()
        {
#if UNITY_ANDROID || UNITY_IPHONE
            if (string.IsNullOrEmpty(appOpenAdUnitId)) return;
            if (_appOpenAd != null) { _appOpenAd.Destroy(); _appOpenAd = null; }
            var request = new AdRequest();
            AppOpenAd.Load(appOpenAdUnitId, request, (ad, error) =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    if (error != null || ad == null)
                    {
                        Debug.Log("[GameUp] AdmobAds AppOpen load failed: " + (error?.GetMessage() ?? "null"));
                        return;
                    }
                    _appOpenAd = ad;
                    _appOpenExpireTime = DateTime.Now + TimeSpan.FromHours(AppOpenTimeoutHours);
                    RegisterAppOpenEvents(ad);
                });
            });
#endif
        }

#if UNITY_ANDROID || UNITY_IPHONE
        private void RegisterInterstitialEvents(InterstitialAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    _interstitialAd?.Destroy();
                    _interstitialAd = null;
                    RequestInterstitial();
                });
            };
            ad.OnAdFullScreenContentFailed += _ =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    _interstitialAd?.Destroy();
                    _interstitialAd = null;
                    RequestInterstitial();
                });
            };
        }

        private void RegisterAppOpenEvents(AppOpenAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    _appOpenAd?.Destroy();
                    _appOpenAd = null;
                    RequestAppOpenAds();
                });
            };
            ad.OnAdFullScreenContentFailed += _ =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    _appOpenAd?.Destroy();
                    _appOpenAd = null;
                    RequestAppOpenAds();
                });
            };
        }
#endif

        public bool IsBannerAvailable()
        {
#if UNITY_ANDROID || UNITY_IPHONE
            return _bannerView != null;
#else
            return false;
#endif
        }

        public bool IsInterstitialAvailable()
        {
#if UNITY_ANDROID || UNITY_IPHONE
            return _interstitialAd != null && _interstitialAd.CanShowAd();
#else
            return false;
#endif
        }

        public bool IsRewardedVideoAvailable()
        {
#if UNITY_ANDROID || UNITY_IPHONE
            return _rewardedAd != null && _rewardedAd.CanShowAd();
#else
            return false;
#endif
        }

        public bool IsAppOpenAdsAvailable()
        {
#if UNITY_ANDROID || UNITY_IPHONE
            return _appOpenAd != null && _appOpenAd.CanShowAd() && DateTime.Now < _appOpenExpireTime;
#else
            return false;
#endif
        }

        public void ShowBanner(string where)
        {
#if UNITY_ANDROID || UNITY_IPHONE
            MainThreadDispatcher.Enqueue(() =>
            {
                if (_bannerView != null)
                    _bannerView.Show();
            });
#endif
        }

        public void HideBanner(string where)
        {
#if UNITY_ANDROID || UNITY_IPHONE
            MainThreadDispatcher.Enqueue(() =>
            {
                _bannerView?.Hide();
            });
#endif
        }

        public void ShowInterstitial(string where, Action onSuccess, Action onFail)
        {
#if UNITY_ANDROID || UNITY_IPHONE
            if (_interstitialAd == null || !_interstitialAd.CanShowAd())
            {
                onFail?.Invoke();
                return;
            }
            var ad = _interstitialAd;
            _interstitialAd = null;
            ad.OnAdFullScreenContentClosed += () => MainThreadDispatcher.Enqueue(() => onSuccess?.Invoke());
            ad.OnAdFullScreenContentFailed += _ => MainThreadDispatcher.Enqueue(() => onFail?.Invoke());
            ad.Show();
#else
            onFail?.Invoke();
#endif
        }

        public void ShowRewardedVideo(string where, Action onSuccess, Action onFail)
        {
#if UNITY_ANDROID || UNITY_IPHONE
            if (_rewardedAd == null || !_rewardedAd.CanShowAd())
            {
                onFail?.Invoke();
                return;
            }
            var rewardGranted = false;
            var ad = _rewardedAd;
            _rewardedAd = null;
            ad.OnAdFullScreenContentClosed += () =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    if (!rewardGranted) onFail?.Invoke();
                    RequestRewardedVideo();
                });
            };
            ad.OnAdFullScreenContentFailed += _ =>
            {
                MainThreadDispatcher.Enqueue(() => { onFail?.Invoke(); RequestRewardedVideo(); });
            };
            ad.Show(reward =>
            {
                rewardGranted = true;
                MainThreadDispatcher.Enqueue(() => onSuccess?.Invoke());
            });
#else
            onFail?.Invoke();
#endif
        }

        public void ShowAppOpenAds(string where, Action onSuccess, Action onFail)
        {
#if UNITY_ANDROID || UNITY_IPHONE
            if (_appOpenAd == null || !_appOpenAd.CanShowAd() || DateTime.Now >= _appOpenExpireTime)
            {
                onFail?.Invoke();
                return;
            }
            var ad = _appOpenAd;
            _appOpenAd = null;
            ad.OnAdFullScreenContentClosed += () => MainThreadDispatcher.Enqueue(() => { onSuccess?.Invoke(); RequestAppOpenAds(); });
            ad.OnAdFullScreenContentFailed += _ => MainThreadDispatcher.Enqueue(() => { onFail?.Invoke(); RequestAppOpenAds(); });
            ad.Show();
#else
            onFail?.Invoke();
#endif
        }

        private void OnDestroy()
        {
#if UNITY_ANDROID || UNITY_IPHONE
            _bannerView?.Destroy();
            _interstitialAd?.Destroy();
            _rewardedAd?.Destroy();
            _appOpenAd?.Destroy();
#endif
        }
    }
}
