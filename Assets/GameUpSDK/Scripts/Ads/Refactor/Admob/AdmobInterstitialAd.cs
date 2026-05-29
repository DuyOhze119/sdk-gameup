using System;
using System.Collections.Generic;
#if ADMOB_DEPENDENCIES_INSTALLED
using GoogleMobileAds.Api;
#endif
using UnityEngine;

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
                AdsRules.BeginInterstitialCappingPause();
                var ad = _ads[key];
                _ads.Remove(key);
                bool earned = false;

                ad.OnAdFullScreenContentClosed += () => MainThreadDispatcher.Enqueue(() =>
                {
                    AdsRules.EndInterstitialCappingPause();
                    NotifyAdClosed(where);
                    if (!earned) onFail?.Invoke();
                    Load(where);
                });
                ad.OnAdFullScreenContentFailed += (err) => MainThreadDispatcher.Enqueue(() =>
                {
                    AdsRules.EndInterstitialCappingPause();
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
        private Dictionary<string, BannerView> _banners = new Dictionary<string, BannerView>();
#endif
        public AdmobBannerAd(AdUnitConfig config) : base(config, AdUnitType.Banner, "Admob")
        {
        }

        public override bool IsAvailable(string where = null)
        {
#if ADMOB_DEPENDENCIES_INSTALLED
            string key = GetKey(where);
            return _banners.TryGetValue(key, out var banner) && banner != null;
#endif
            return false;
        }

        protected override void RequestAdInternal(string unitId, string where)
        {
            HandleLoadSuccess(unitId, where);
        }

        public void Show(string where, CollapsibleBannerPlacement placement = CollapsibleBannerPlacement.Bottom)
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

                if (_banners.TryGetValue(key, out var oldBanner) && oldBanner != null) oldBanner.Destroy();
                var pos = placement == CollapsibleBannerPlacement.Top ? AdPosition.Top : AdPosition.Bottom;
                var size = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
                var banner = new BannerView(unitId, size, pos);
                var request = new AdRequest();
                if (placement == CollapsibleBannerPlacement.Top) request.Extras.Add("collapsible", "top");
                else if (placement == CollapsibleBannerPlacement.Bottom) request.Extras.Add("collapsible", "bottom");

                banner.OnAdPaid += (adValue) =>
                {
                    if (adValue != null) TrackRevenue(unitId, key, "Banner", adValue.Value * 0.000001f);
                };
                banner.LoadAd(request);
                NotifyAdDisplayed(where);
                banner.Show();
                _banners[key] = banner;
            });
#endif
        }

        public void Hide(string where)
        {
#if ADMOB_DEPENDENCIES_INSTALLED
            if (_banners.TryGetValue(GetKey(where), out var banner) && banner != null) banner.Hide();
#endif
        }
    }
}