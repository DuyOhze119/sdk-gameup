using System;
using UnityEngine;

namespace GameUpSDK.Ads
{
    public class MaxInterstitialAd : BaseAdFormat, IInterstitialAd
    {
        public MaxInterstitialAd(AdUnitConfig config) : base(config, AdUnitType.Interstitial, "MAX")
        {
        }

        public override bool IsAvailable(string where = null)
        {
#if MAXSDK_DEPENDENCIES_INSTALLED
            return MaxSdk.IsInterstitialReady(_config.ResolveUnitId(_adType, where));
#endif
            return false;
        }

        protected override void RequestAdInternal(string unitId, string where)
        {
#if MAXSDK_DEPENDENCIES_INSTALLED
            Action<string, MaxSdkBase.AdInfo> onLoaded = null;
            Action<string, MaxSdkBase.ErrorInfo> onFailed = null;
            Action<string, MaxSdkBase.AdInfo> onRevenue = null;

            onLoaded = (id, info) =>
            {
                if (id == unitId)
                {
                    Unsubscribe();
                    HandleLoadSuccess(unitId, where);
                }
            };
            onFailed = (id, err) =>
            {
                if (id == unitId)
                {
                    Unsubscribe();
                    HandleLoadFailed(unitId, where, err.Message);
                }
            };
            onRevenue = (id, info) =>
            {
                if (id == unitId) TrackRevenue(id, info.NetworkPlacement, "Interstitial", info.Revenue);
            };

            void Unsubscribe()
            {
                MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= onLoaded;
                MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= onFailed;
            }

            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += onLoaded;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += onFailed;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += onRevenue;
            MaxSdk.LoadInterstitial(unitId);
#endif
        }

        public void Show(string where, Action onSuccess, Action onFail)
        {
#if MAXSDK_DEPENDENCIES_INSTALLED
            string unitId = _config.ResolveUnitId(_adType, where);
            if (IsAvailable(where))
            {
                NotifyAdDisplayed(where);
                Action<string, MaxSdkBase.AdInfo> onHidden = null;
                onHidden = (id, info) =>
                {
                    if (id != unitId) return;
                    MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= onHidden;
                    NotifyAdClosed(where);
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        onSuccess?.Invoke();
                        Load(where);
                    });
                };
                MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += onHidden;
                MaxSdk.ShowInterstitial(unitId, where);
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

    public class MaxRewardedAd : BaseAdFormat, IRewardedAd
    {
        public MaxRewardedAd(AdUnitConfig config) : base(config, AdUnitType.RewardedVideo, "MAX")
        {
        }

        public override bool IsAvailable(string where = null)
        {
#if MAXSDK_DEPENDENCIES_INSTALLED
            return MaxSdk.IsRewardedAdReady(_config.ResolveUnitId(_adType, where));
#endif
            return false;
        }

        protected override void RequestAdInternal(string unitId, string where)
        {
#if MAXSDK_DEPENDENCIES_INSTALLED
            Action<string, MaxSdkBase.AdInfo> onLoaded = null;
            Action<string, MaxSdkBase.ErrorInfo> onFailed = null;
            Action<string, MaxSdkBase.AdInfo> onRevenue = null;

            onLoaded = (id, info) =>
            {
                if (id == unitId)
                {
                    Unsubscribe();
                    HandleLoadSuccess(unitId, where);
                }
            };
            onFailed = (id, err) =>
            {
                if (id == unitId)
                {
                    Unsubscribe();
                    HandleLoadFailed(unitId, where, err.Message);
                }
            };
            onRevenue = (id, info) =>
            {
                if (id == unitId) TrackRevenue(id, info.NetworkPlacement, "Rewarded", info.Revenue);
            };

            void Unsubscribe()
            {
                MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= onLoaded;
                MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= onFailed;
            }

            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += onLoaded;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += onFailed;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += onRevenue;
            MaxSdk.LoadRewardedAd(unitId);
#endif
        }

        public void Show(string where, Action onSuccess, Action onFail)
        {
#if MAXSDK_DEPENDENCIES_INSTALLED
            string unitId = _config.ResolveUnitId(_adType, where);
            if (IsAvailable(where))
            {
                NotifyAdDisplayed(where);
                AdsRules.BeginInterstitialCappingPause();
                bool earned = false;
                Action<string, MaxSdkBase.Reward, MaxSdkBase.AdInfo> onReward = null;
                Action<string, MaxSdkBase.AdInfo> onHidden = null;

                onReward = (id, reward, info) =>
                {
                    if (id == unitId)
                    {
                        earned = true;
                    }
                };
                onHidden = (id, info) =>
                {
                    if (id != unitId) return;
                    MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= onReward;
                    MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= onHidden;
                    NotifyAdClosed(where);
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        AdsRules.EndInterstitialCappingPause();
                        if (earned) onSuccess?.Invoke();
                        else onFail?.Invoke();
                        Load(where);
                    });
                };
                MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += onReward;
                MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += onHidden;
                MaxSdk.ShowRewardedAd(unitId, where);
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

    public class MaxAppOpenAd : BaseAdFormat, IAppOpenAd
    {
        public MaxAppOpenAd(AdUnitConfig config) : base(config, AdUnitType.AppOpen, "MAX")
        {
        }

        public override bool IsAvailable(string where = null)
        {
#if MAXSDK_DEPENDENCIES_INSTALLED
            return MaxSdk.IsAppOpenAdReady(_config.ResolveUnitId(_adType, where));
#endif
            return false;
        }

        protected override void RequestAdInternal(string unitId, string where)
        {
#if MAXSDK_DEPENDENCIES_INSTALLED
            Action<string, MaxSdkBase.AdInfo> onLoaded = null;
            Action<string, MaxSdkBase.ErrorInfo> onFailed = null;
            onLoaded = (id, info) =>
            {
                if (id == unitId)
                {
                    MaxSdkCallbacks.AppOpen.OnAdLoadedEvent -= onLoaded;
                    MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent -= onFailed;
                    HandleLoadSuccess(unitId, where);
                }
            };
            onFailed = (id, err) =>
            {
                if (id == unitId)
                {
                    MaxSdkCallbacks.AppOpen.OnAdLoadedEvent -= onLoaded;
                    MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent -= onFailed;
                    HandleLoadFailed(unitId, where, err.Message);
                }
            };
            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += onLoaded;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += onFailed;
            MaxSdk.LoadAppOpenAd(unitId);
#endif
        }

        public void Show(string where, Action onSuccess, Action onFail)
        {
#if MAXSDK_DEPENDENCIES_INSTALLED
            string unitId = _config.ResolveUnitId(_adType, where);
            if (IsAvailable(where))
            {
                NotifyAdDisplayed(where);
                Action<string, MaxSdkBase.AdInfo> onHidden = null;
                onHidden = (id, info) =>
                {
                    if (id == unitId)
                    {
                        MaxSdkCallbacks.AppOpen.OnAdHiddenEvent -= onHidden;
                        NotifyAdClosed(where);
                        MainThreadDispatcher.Enqueue(() =>
                        {
                            onSuccess?.Invoke();
                            Load(where);
                        });
                    }
                };
                MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += onHidden;
                MaxSdk.ShowAppOpenAd(unitId, where);
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

    public class MaxBannerAd : BaseAdFormat, IBannerAd
    {
        public MaxBannerAd(AdUnitConfig config) : base(config, AdUnitType.Banner, "MAX")
        {
        }

        public override bool IsAvailable(string where = null)
        {
            return !string.IsNullOrEmpty(_config.ResolveUnitId(_adType, where));
        }

        protected override void RequestAdInternal(string unitId, string where)
        {
            HandleLoadSuccess(unitId, where);
        }

        public void Show(string where, CollapsibleBannerPlacement placement = CollapsibleBannerPlacement.Bottom)
        {
#if MAXSDK_DEPENDENCIES_INSTALLED

            string unitId = _config.ResolveUnitId(_adType, where);
            if (string.IsNullOrEmpty(unitId)) return;
            var pos = placement == CollapsibleBannerPlacement.Top
                ? MaxSdk.BannerPosition.TopCenter
                : MaxSdk.BannerPosition.BottomCenter;
            MaxSdk.CreateBanner(unitId, pos);
            if (placement == CollapsibleBannerPlacement.None)
                MaxSdk.SetBannerExtraParameter(unitId, "adaptive_banner", "true");
            NotifyAdDisplayed(where);
            MaxSdk.ShowBanner(unitId);

            MaxSdk.ShowBanner(unitId);
#endif
        }

        public void Hide(string where)
        {
#if MAXSDK_DEPENDENCIES_INSTALLED
            string unitId = _config.ResolveUnitId(_adType, where);
            if (!string.IsNullOrEmpty(unitId)) MaxSdk.HideBanner(unitId);
#endif
        }
    }
}