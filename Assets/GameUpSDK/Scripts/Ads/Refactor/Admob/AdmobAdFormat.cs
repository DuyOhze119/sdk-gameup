using System;
using System.Collections.Generic;
#if ADMOB_DEPENDENCIES_INSTALLED
using GoogleMobileAds.Api;
using UnityEngine;
#endif

namespace GameUpSDK.Ads
{
    public class AdmobInterstitialAd : BaseAdFormat, IInterstitialAd
    {
#if ADMOB_DEPENDENCIES_INSTALLED
        private readonly Dictionary<string, InterstitialAd> _ads = new Dictionary<string, InterstitialAd>();
#endif
        public AdmobInterstitialAd(AdUnitConfig config) : base(config, AdUnitType.Interstitial, "Admob")
        {
        }

        public override bool IsAvailable(string where = null)
        {
#if ADMOB_DEPENDENCIES_INSTALLED
            return _ads.TryGetValue(GetKey(where), out var ad) && ad != null && ad.CanShowAd();
#endif
            return false;
        }

        protected override void RequestAdInternal(string unitId, string where)
        {
#if ADMOB_DEPENDENCIES_INSTALLED
            var key = GetKey(where);
            if (_ads.TryGetValue(key, out var oldAd) && oldAd != null) oldAd.Destroy();

            InterstitialAd.Load(unitId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    HandleLoadFailed(unitId, where, error?.GetMessage());
                    return;
                }

                ad.OnAdPaid += (adValue) =>
                {
                    if (adValue != null) TrackRevenue(unitId, key, "Interstitial", adValue.Value * 0.000001f);
                };
                _ads[key] = ad;
                HandleLoadSuccess(unitId, where);
            });
#endif
        }

        public void Show(string where, Action onSuccess, Action onFail)
        {
#if ADMOB_DEPENDENCIES_INSTALLED
            var key = GetKey(where);
            if (IsAvailable(where))
            {
                NotifyAdDisplayed(where);
                var ad = _ads[key];
                _ads.Remove(key);
                ad.OnAdFullScreenContentClosed += () => MainThreadDispatcher.Enqueue(() =>
                {
                    Debug.LogError("Interstitial ad dismissed");
                    NotifyAdClosed(where);
                    onSuccess?.Invoke();
                    Load(where);
                });
                ad.OnAdFullScreenContentFailed += (err) => MainThreadDispatcher.Enqueue(() =>
                {
                    NotifyAdDisplayFailed(where, err.GetMessage());
                    onFail?.Invoke();
                    Load(where);
                });
                ad.Show();
            }
            else
            {
                NotifyAdDisplayFailed(where, "not_ready");
                onFail?.Invoke();
                Load(where);
            }
#endif
        }
    }

    public class AdmobRewardedAd : BaseAdFormat, IRewardedAd
    {
#if ADMOB_DEPENDENCIES_INSTALLED
        private readonly Dictionary<string, RewardedAd> _ads = new Dictionary<string, RewardedAd>();
#endif
        public AdmobRewardedAd(AdUnitConfig config) : base(config, AdUnitType.RewardedVideo, "Admob")
        {
        }

        public override bool IsAvailable(string where = null)
        {
#if ADMOB_DEPENDENCIES_INSTALLED
            return _ads.TryGetValue(GetKey(where), out var ad) && ad != null && ad.CanShowAd();
#endif
            return false;
        }

        protected override void RequestAdInternal(string unitId, string where)
        {
#if ADMOB_DEPENDENCIES_INSTALLED
            var key = GetKey(where);
            if (_ads.TryGetValue(key, out var oldAd) && oldAd != null) oldAd.Destroy();

            RewardedAd.Load(unitId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    HandleLoadFailed(unitId, where, error?.GetMessage());
                    return;
                }

                ad.OnAdPaid += (adValue) =>
                {
                    if (adValue != null) TrackRevenue(unitId, key, "Rewarded", adValue.Value * 0.000001f);
                };
                _ads[key] = ad;
                HandleLoadSuccess(unitId, where);
            });
#endif
        }

        public void Show(string where, Action onSuccess, Action onFail)
        {
#if ADMOB_DEPENDENCIES_INSTALLED
            string key = GetKey(where);
            if (IsAvailable(where))
            {
                NotifyAdDisplayed(where);
                var ad = _ads[key];
                _ads.Remove(key);
                bool earned = false;

                ad.OnAdFullScreenContentClosed += () => MainThreadDispatcher.Enqueue(() =>
                {
                    NotifyAdClosed(where);
                    if (!earned) onFail?.Invoke();
                    Load(where);
                });
                ad.OnAdFullScreenContentFailed += (err) => MainThreadDispatcher.Enqueue(() =>
                {
                    NotifyAdDisplayFailed(where, err.GetMessage());
                    onFail?.Invoke();
                    Load(where);
                });
                ad.Show((reward) =>
                {
                    earned = true;
                    MainThreadDispatcher.Enqueue(() => onSuccess?.Invoke());
                });
            }
            else
            {
                NotifyAdDisplayFailed(where, "not_ready");
                onFail?.Invoke();
                Load(where);
            }
#endif
        }
    }

    public class AdmobAppOpenAd : BaseAdFormat, IAppOpenAd
    {
#if ADMOB_DEPENDENCIES_INSTALLED
        private readonly Dictionary<string, AppOpenAd> _ads = new Dictionary<string, AppOpenAd>();
#endif
        private readonly Dictionary<string, DateTime> _expireTimes = new Dictionary<string, DateTime>();

        public AdmobAppOpenAd(AdUnitConfig config) : base(config, AdUnitType.AppOpen, "Admob")
        {
        }

        public override bool IsAvailable(string where = null)
        {
#if ADMOB_DEPENDENCIES_INSTALLED
            string key = GetKey(where);
            return _ads.TryGetValue(key, out var ad) && ad != null && ad.CanShowAd() &&
                   _expireTimes.TryGetValue(key, out var exp) && DateTime.Now < exp;
#endif
            return false;
        }

        protected override void RequestAdInternal(string unitId, string where)
        {
#if ADMOB_DEPENDENCIES_INSTALLED
            string key = GetKey(where);
            if (_ads.TryGetValue(key, out var oldAd) && oldAd != null) oldAd.Destroy();

            AppOpenAd.Load(unitId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    HandleLoadFailed(unitId, where, error?.GetMessage());
                    return;
                }

                ad.OnAdPaid += (adValue) =>
                {
                    if (adValue != null) TrackRevenue(unitId, key, "AppOpen", adValue.Value * 0.000001f);
                };
                _ads[key] = ad;
                _expireTimes[key] = DateTime.Now.AddHours(4);
                HandleLoadSuccess(unitId, where);
            });
#endif
        }

        public void Show(string where, Action onSuccess, Action onFail)
        {
#if ADMOB_DEPENDENCIES_INSTALLED
            string key = GetKey(where);
            if (IsAvailable(where))
            {
                NotifyAdDisplayed(where);
                var ad = _ads[key];
                _ads.Remove(key);
                ad.OnAdFullScreenContentClosed += () => MainThreadDispatcher.Enqueue(() =>
                {
                    NotifyAdClosed(where);
                    onSuccess?.Invoke();
                    Load(where);
                });
                ad.OnAdFullScreenContentFailed += (err) => MainThreadDispatcher.Enqueue(() =>
                {
                    NotifyAdDisplayFailed(where, err.GetMessage());
                    onFail?.Invoke();
                    Load(where);
                });
                ad.Show();
            }
            else
            {
                NotifyAdDisplayFailed(where, "not_ready_or_expired");
                onFail?.Invoke();
                Load(where);
            }
#endif
        }
    }

    public class AdmobBannerAd : BaseAdFormat, IBannerAd
    {
#if ADMOB_DEPENDENCIES_INSTALLED
        private readonly Dictionary<string, BannerView> _banners = new Dictionary<string, BannerView>();
#endif
        private readonly Dictionary<string, bool> _isLoaded = new Dictionary<string, bool>();
        

        public AdmobBannerAd(AdUnitConfig config) : base(config, AdUnitType.Banner, "Admob")
        {
            
        }

        public override bool IsAvailable(string where = null)
        {
#if ADMOB_DEPENDENCIES_INSTALLED
            string key = GetKey(where);
            var available = _isLoaded.TryGetValue(key, out var isLoaded) && isLoaded;
            UnityEngine.Debug.Log($"[GameUp] Banner available: {available} - where: {where}");
            return available;
#endif
            return false;
        }

        protected override void RequestAdInternal(string unitId, string where)
        {
#if ADMOB_DEPENDENCIES_INSTALLED
            string key = GetKey(where);

            // Lấy thẳng Config Entry từ Setup Window
            var entry = _config.GetEntry(_adType, where);

            MainThreadDispatcher.Enqueue(() =>
            {
                if (_banners.TryGetValue(key, out var oldBanner) && oldBanner != null) oldBanner.Destroy();
                _isLoaded[key] = false;

                var pos = entry.CollapsiblePlacement == CollapsibleBannerPlacement.Top
                    ? AdPosition.Top
                    : AdPosition.Bottom;
                var size = GetAdMobBannerSize(entry.BannerSize);

                var banner = new BannerView(unitId, size, pos);
                _banners[key] = banner;

                banner.OnBannerAdLoaded += () =>
                {
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        _isLoaded[key] = true;
                        HandleLoadSuccess(unitId, where);
                        banner.Hide();
                    });
                };

                banner.OnBannerAdLoadFailed += (err) =>
                {
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        _isLoaded[key] = false;
                        HandleLoadFailed(unitId, where, err?.GetMessage());
                    });
                };

                banner.OnAdPaid += (adValue) =>
                {
                    if (adValue != null) TrackRevenue(unitId, key, "Banner", adValue.Value * 0.000001f);
                };

                var request = new AdRequest();
                if (entry.CollapsiblePlacement == CollapsibleBannerPlacement.Top)
                    request.Extras.Add("collapsible", "top");
                else if (entry.CollapsiblePlacement == CollapsibleBannerPlacement.Bottom)
                    request.Extras.Add("collapsible", "bottom");

                banner.LoadAd(request);
            });
#endif
        }

        public void Show(string where)
        {
#if ADMOB_DEPENDENCIES_INSTALLED
            MainThreadDispatcher.Enqueue(() =>
            {
                string key = GetKey(where);
                string unitId = _config.ResolveUnitId(_adType, where);
                if (string.IsNullOrEmpty(unitId))
                {
                    NotifyAdDisplayFailed(where, "empty_id");
                    return;
                }

                if (_isLoaded.TryGetValue(key, out bool loaded) && loaded)
                {
                    NotifyAdDisplayed(where);
                    _banners[key].Show();
                    UnityEngine.Debug.Log($"[GameUp] Banner available: {loaded}");
                }
                else Load(where);
            });
#endif
        }

        public void Hide(string where)
        {
#if ADMOB_DEPENDENCIES_INSTALLED
            string key = GetKey(where);
            MainThreadDispatcher.Enqueue(() =>
            {
                if (_banners.TryGetValue(key, out var banner) && banner != null) banner.Hide();
            });
#endif
        }

        private AdSize GetAdMobBannerSize(BannerSize size)
        {
            switch (size)
            {
                case BannerSize.Banner: return AdSize.Banner;
                case BannerSize.Adaptive:
                    return AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
                case BannerSize.MediumRectangle: return AdSize.MediumRectangle;
                case BannerSize.Leaderboard: return AdSize.Leaderboard;
                case BannerSize.Large:
                default: return new AdSize(320, 100);
            }
        }
    }

    public class AdmobNativeFullscreenAd : BaseAdFormat, INativeFullScreenAd
    {
        private string _bannerId;
        private string _where;
        public AdmobNativeFullscreenAd(AdUnitConfig config) : base(config, AdUnitType.NativeAd, "Admob")
        {
            FullScreenNativeAdManager.Instance.OnAdClosedEvent += OnNativeAdClosed;
            FullScreenNativeAdManager.Instance.OnAdLoadedEvent += OnNativeAdLoaded;
            FullScreenNativeAdManager.Instance.OnAdLoadFailedEvent += OnNativeAdLoadFailed;
        }

        public override bool IsAvailable(string where = null)
        {
            return FullScreenNativeAdManager.Instance.IsAdReady();
        }

        protected override void RequestAdInternal(string unitId, string where)
        {
            _where = where;
            _bannerId = unitId;
            FullScreenNativeAdManager.Instance.RequestAd(unitId);
        }

        public void Show(string where, Action onSuccess, Action onFail)
        {
            _where = where;
            FullScreenNativeAdManager.Instance.ShowFullScreenAd();
        }

        public void Hide()
        {
            FullScreenNativeAdManager.Instance.ForceCloseAd();
        }

        private void OnNativeAdClosed()
        {
            NotifyAdClosed(_where);
            Load(_where);
        }
        
        private void OnNativeAdLoaded()
        {
            HandleLoadSuccess(_bannerId, _where);
        }
        
        private void OnNativeAdLoadFailed(string error)
        {
            HandleLoadFailed(_bannerId, _where, error);
        }
    }
}