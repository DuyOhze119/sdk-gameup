using System;
using System.Collections.Generic;
#if LEVELPLAY_DEPENDENCIES_INSTALLED
using Unity.Services.LevelPlay;
#endif

namespace GameUpSDK.Ads
{
    public class IronSourceInterstitialAd : BaseAdFormat, IInterstitialAd
    {
#if LEVELPLAY_DEPENDENCIES_INSTALLED
        private Dictionary<string, LevelPlayInterstitialAd> _ads = new Dictionary<string, LevelPlayInterstitialAd>();
#endif
        public IronSourceInterstitialAd(AdUnitConfig config) : base(config, AdUnitType.Interstitial, "LevelPlay")
        {
        }

        public override bool IsAvailable(string where = null)
        {
#if LEVELPLAY_DEPENDENCIES_INSTALLED
            return _ads.TryGetValue(GetKey(where), out var ad) && ad != null && ad.IsAdReady();
#endif
            return false;
        }

        protected override void RequestAdInternal(string unitId, string where)
        {
#if LEVELPLAY_DEPENDENCIES_INSTALLED
            string key = GetKey(where);
            if (!_ads.ContainsKey(key))
            {
                var newAd = new LevelPlayInterstitialAd(unitId);
                newAd.OnAdLoaded += (_) => HandleLoadSuccess(unitId, where);
                newAd.OnAdLoadFailed += (err) => HandleLoadFailed(unitId, where, err.ErrorMessage);
                _ads[key] = newAd;
            }

            _ads[key].LoadAd();
#endif
        }

        public void Show(string where, Action onSuccess, Action onFail)
        {
#if LEVELPLAY_DEPENDENCIES_INSTALLED
            string key = GetKey(where);
            if (IsAvailable(where))
            {
                NotifyAdDisplayed(where);
                var ad = _ads[key];
                Action<LevelPlayAdInfo> onClosed = null;
                Action<LevelPlayAdInfo, LevelPlayAdError> onFailed = null;

                onClosed = (_) =>
                {
                    ad.OnAdClosed -= onClosed;
                    ad.OnAdDisplayFailed -= onFailed;
                    NotifyAdClosed(where);
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        onSuccess?.Invoke();
                        Load(where);
                    });
                };
                onFailed = (_, err) =>
                {
                    ad.OnAdClosed -= onClosed;
                    ad.OnAdDisplayFailed -= onFailed;
                    NotifyAdDisplayFailed(where, err.ErrorMessage);
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        onFail?.Invoke();
                        Load(where);
                    });
                };

                ad.OnAdClosed += onClosed;
                ad.OnAdDisplayFailed += onFailed;
                ad.ShowAd(where);
            }
            else
            {
                NotifyAdDisplayFailed(where, "not_ready");
                onFail?.Invoke();
                Load(where);
            }
        }
#endif
    }

    public class IronSourceRewardedAd : BaseAdFormat, IRewardedAd
    {
#if LEVELPLAY_DEPENDENCIES_INSTALLED
        private Dictionary<string, LevelPlayRewardedAd> _ads = new Dictionary<string, LevelPlayRewardedAd>();
#endif
        public IronSourceRewardedAd(AdUnitConfig config) : base(config, AdUnitType.RewardedVideo, "LevelPlay")
        {
        }

        public override bool IsAvailable(string where = null)
        {
#if LEVELPLAY_DEPENDENCIES_INSTALLED
            return _ads.TryGetValue(GetKey(where), out var ad) && ad != null && ad.IsAdReady();
#endif
            return false;
        }

        protected override void RequestAdInternal(string unitId, string where)
        {
#if LEVELPLAY_DEPENDENCIES_INSTALLED
            string key = GetKey(where);
            if (!_ads.ContainsKey(key))
            {
                var newAd = new LevelPlayRewardedAd(unitId);
                newAd.OnAdLoaded += (_) => HandleLoadSuccess(unitId, where);
                newAd.OnAdLoadFailed += (err) => HandleLoadFailed(unitId, where, err.ErrorMessage);
                _ads[key] = newAd;
            }

            _ads[key].LoadAd();
#endif
        }

        public void Show(string where, Action onSuccess, Action onFail)
        {
#if LEVELPLAY_DEPENDENCIES_INSTALLED
            string key = GetKey(where);
            if (IsAvailable(where))
            {
                NotifyAdDisplayed(where);
                AdsRules.BeginInterstitialCappingPause();
                var ad = _ads[key];
                bool earned = false;

                Action<LevelPlayAdInfo> onClosed = null;
                Action<LevelPlayAdInfo, LevelPlayReward> onReward = null;
                Action<LevelPlayAdInfo, LevelPlayAdError> onFailed = null;

                onClosed = (_) =>
                {
                    ad.OnAdClosed -= onClosed;
                    ad.OnAdRewarded -= onReward;
                    ad.OnAdDisplayFailed -= onFailed;
                    NotifyAdClosed(where);
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        AdsRules.EndInterstitialCappingPause();
                        if (earned) onSuccess?.Invoke();
                        else onFail?.Invoke();
                        Load(where);
                    });
                };
                onReward = (_, reward) => { earned = true; };
                onFailed = (_, err) =>
                {
                    ad.OnAdClosed -= onClosed;
                    ad.OnAdRewarded -= onReward;
                    ad.OnAdDisplayFailed -= onFailed;
                    NotifyAdDisplayFailed(where, err.ErrorMessage);
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        AdsRules.EndInterstitialCappingPause();
                        onFail?.Invoke();
                        Load(where);
                    });
                };

                ad.OnAdClosed += onClosed;
                ad.OnAdRewarded += onReward;
                ad.OnAdDisplayFailed += onFailed;
                ad.ShowAd(where);
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

    public class IronSourceBannerAd : BaseAdFormat, IBannerAd
    {
#if LEVELPLAY_DEPENDENCIES_INSTALLED
        private Dictionary<string, LevelPlayBannerAd> _ads = new Dictionary<string, LevelPlayBannerAd>();
#endif
        public IronSourceBannerAd(AdUnitConfig config) : base(config, AdUnitType.Banner, "LevelPlay")
        {
        }

        public override bool IsAvailable(string where = null)
        {
#if LEVELPLAY_DEPENDENCIES_INSTALLED
            return _ads.TryGetValue(GetKey(where), out var ad) && ad != null;
#endif
            return false;
        }

        protected override void RequestAdInternal(string unitId, string where)
        {
            HandleLoadSuccess(unitId, where);
        }

        public void Show(string where, CollapsibleBannerPlacement placement = CollapsibleBannerPlacement.Bottom)
        {
#if LEVELPLAY_DEPENDENCIES_INSTALLED
            string key = GetKey(where);
            string unitId = _config.ResolveUnitId(_adType, where);
            if (string.IsNullOrEmpty(unitId))
            {
                NotifyAdDisplayFailed(where, "empty_id");
                return;
            }

            if (!_ads.ContainsKey(key))
            {
                var bannerConfig = new LevelPlayBannerAd.Config.Builder()
                    .SetSize(LevelPlayAdSize.CreateAdaptiveAdSize())
                    .SetPosition(LevelPlayBannerPosition.BottomCenter)
                    .SetDisplayOnLoad(false)
                    .Build();
                _ads[key] = new LevelPlayBannerAd(unitId, bannerConfig);
            }

            NotifyAdDisplayed(where);
            _ads[key].LoadAd();
            _ads[key].ShowAd();
#endif
        }

        public void Hide(string where)
        {
#if LEVELPLAY_DEPENDENCIES_INSTALLED
            if (_ads.TryGetValue(GetKey(where), out var ad) && ad != null) ad.HideAd();
#endif
        }
    }
}