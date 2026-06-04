using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
                        //banner.Hide();
                    });
                };

                banner.OnBannerAdLoadFailed += (err) =>
                {
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        _isLoaded[key] = false;
                        banner.Destroy();
                        _banners.Remove(key);
                        HandleLoadFailed(unitId, where, err?.GetMessage());
                    });
                };

                banner.OnAdPaid += (adValue) =>
                {
                    if (adValue != null) TrackRevenue(unitId, key, "Banner", adValue.Value * 0.000001f);
                };

                var request = new AdRequest();
                switch (entry.CollapsiblePlacement)
                {
                    case CollapsibleBannerPlacement.Top:
                        request.Extras.Add("collapsible", "top");
                        request.Extras.Add("collapsible_request_id", System.Guid.NewGuid().ToString());
                        break;
                    case CollapsibleBannerPlacement.Bottom:
                        request.Extras.Add("collapsible", "bottom");
                        request.Extras.Add("collapsible_request_id", System.Guid.NewGuid().ToString());
                        break;
                }

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
                var entry = _config.GetEntry(_adType, where);

                if (entry.CollapsiblePlacement != CollapsibleBannerPlacement.None)
                {
                    Load(where);
                }
                else
                {
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
                }
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

        public void Restore(string where)
        {
            Show(where);
        }

#if ADMOB_DEPENDENCIES_INSTALLED
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
#endif
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

    public class AdmobNativeExpandBannerAd : BaseAdFormat, IBannerAd
    {
        public Action<string> OnCollapsedNativeBanner = delegate { };
        
        private readonly Dictionary<string, NativeOverlayAd> _expandedAds = new Dictionary<string, NativeOverlayAd>();

        private readonly Dictionary<string, RuntimeCollapsibleUI> _activeUIs =
            new Dictionary<string, RuntimeCollapsibleUI>();

        private readonly Dictionary<string, CancellationTokenSource> _refreshTokens =
            new Dictionary<string, CancellationTokenSource>();

        private readonly int REFRESH_TIME_SECONDS = 30;

        public AdmobNativeExpandBannerAd(AdUnitConfig config, AdUnitType adType = AdUnitType.NativeAd) : base(config,
            adType, "Admob_NativeOverlay")
        {
        }

        public override bool IsAvailable(string where = null) =>
            _expandedAds.ContainsKey(GetKey(where));

        protected override void RequestAdInternal(string unitId, string where)
        {
            LoadSpecificSizeAd(where, GetKey(where), false);
        }

        public void Show(string where)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                var key = GetKey(where);

                var entry = _config.GetEntry(_adType, where);

                var isCollapsible = entry.CollapsiblePlacement != CollapsibleBannerPlacement.None;

                if (isCollapsible)
                {
                    if (_expandedAds.TryGetValue(key, out var eAd) && eAd != null)
                    {
                        NotifyAdDisplayed(where);
                        RenderAdInstance(where, eAd, NativeTemplateId.Medium);
                        StartAutoRefresh(where);
                    }
                    else
                    {
                        LoadSpecificSizeAd(where, key, true);
                    }
                }
            });
        }
        
        public void Hide(string where)
        {
            var key = GetKey(where);
            StopAutoRefresh();

            MainThreadDispatcher.Enqueue(() =>
            {
                if (_expandedAds.TryGetValue(key, out var eAd) && eAd != null) eAd.Hide();
                if (_activeUIs.TryGetValue(key, out var ui) && ui != null) ui.SetVisible(false);
            });
        }

        public void Restore(string where)
        {
            LoadSpecificSizeAd(where, GetKey(where), true);
        }

        private void CloseExpandBanner(string where)
        {
            MainThreadDispatcher.Enqueue(async () =>
            {
                var key = GetKey(where);
                
                HideActiveUI();
                StopAutoRefresh();
                OnCollapsedNativeBanner?.Invoke(where);
                if (_expandedAds.TryGetValue(key, out var ad) && ad != null)
                {
                    _expandedAds.Remove(key);
                    ad.Hide();
                    ad.Destroy();
                    await Task.Delay(100);
                    LoadSpecificSizeAd(where, key, false);
                }
            });
        }

        private void HideActiveUI()
        {
            foreach (var ui in _activeUIs)
            {
                ui.Value.SetVisible(false);
            }
        }

        private void LoadSpecificSizeAd(string where, string key, bool showAfterLoad)
        {
            var unitId = _config.ResolveUnitId(_adType, where);
            var entry = _config.GetEntry(_adType, where);
            var isExpanded = entry.CollapsiblePlacement != CollapsibleBannerPlacement.None;
            var options = new NativeAdOptions { AdChoicesPlacement = AdChoicesPlacement.TopRightCorner };

            NativeOverlayAd.Load(unitId, new AdRequest(), options, (NativeOverlayAd ad, LoadAdError error) =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    if (error != null || ad == null)
                    {
                        if (showAfterLoad) NotifyAdDisplayFailed(where, error?.GetMessage());
                        return;
                    }
                    
                    if (_expandedAds.ContainsKey(key) && _expandedAds[key] != null) _expandedAds[key].Destroy();
                    _expandedAds[key] = ad;

                    HandleLoadSuccess(unitId, where);
                    if (showAfterLoad)
                    {
                        NotifyAdDisplayed(where);
                        RenderAdInstance(where, ad, isExpanded ? NativeTemplateId.Medium : NativeTemplateId.Small);
                        StartAutoRefresh(where);
                    }
                });
            });
        }

        private void RenderAdInstance(string where, NativeOverlayAd ad, string templateId)
        {
            var entry = _config.GetEntry(_adType, where);
            var pos = entry.CollapsiblePlacement == CollapsibleBannerPlacement.Top
                ? AdPosition.Top
                : AdPosition.Bottom;

            var style = new NativeTemplateStyle
            {
                TemplateId = templateId,
                MainBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f)
            };

            ad.RenderTemplate(style, pos);

            var key = GetKey(where);
            EnsureUIExists(key, where);
            _activeUIs[key].SetVisible(true);
        }

        private void EnsureUIExists(string key, string where)
        {
            if (!_activeUIs.ContainsKey(key) || _activeUIs[key] == null)
            {
                _activeUIs[key] =
                    RuntimeCollapsibleUI.Create(() => { CloseExpandBanner(where); });
            }
        }

        private void StartAutoRefresh(string where)
        {
            string key = GetKey(where);
            StopAutoRefresh();
            var cts = new CancellationTokenSource();
            _refreshTokens[key] = cts;
            RunRefreshLoop(where, cts.Token);
        }

        private void StopAutoRefresh()
        {
            var keys = _refreshTokens.Keys;
            foreach (var key in keys)
            {
                if (_refreshTokens.TryGetValue(key, out var cts) && cts != null)
                {
                    cts.Cancel();
                    cts.Dispose();
                }
            }
            
            _refreshTokens.Clear();
        }

        private async void RunRefreshLoop(string where, CancellationToken token)
        {
            string key = GetKey(where);
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(REFRESH_TIME_SECONDS), token);
                    if (token.IsCancellationRequested) break;

                    MainThreadDispatcher.Enqueue(() => { LoadSpecificSizeAd(where, key, true); });
                }
            }
            catch (TaskCanceledException)
            {
            }
        }
    }

    public class AdmobBannerDispatcher : IBannerAd
    {
        private readonly AdmobBannerAd _standardBanner;
        private readonly AdmobNativeExpandBannerAd _nativeExpandBanner;
        private readonly AdUnitConfig _config;

        public AdmobBannerDispatcher(AdUnitConfig config)
        {
            _config = config;
            _standardBanner = new AdmobBannerAd(config);
            _nativeExpandBanner = new AdmobNativeExpandBannerAd(config, AdUnitType.Banner);
            WireUpEvents(_standardBanner);
            WireUpEvents(_nativeExpandBanner);

            _nativeExpandBanner.OnCollapsedNativeBanner += OnCollapsedNativeBanner;
        }

        private void OnCollapsedNativeBanner(string where)
        {
            Debug.Log($"OnCollapsedNativeBanner: {where}");
            var wheres = _config.GetAllWhere();
            foreach (var w in wheres)
            {
                if (_standardBanner.IsAvailable(w))
                {
                    _standardBanner.Show(w);
                }
            }
        }

        public bool IsAvailable(string where = null) => GetTarget(where).IsAvailable(where);

        public event Action<string> OnAdLoaded;
        public event Action<string, string> OnAdLoadFailed;
        public event Action<string> OnAdDisplayed;
        public event Action<string, string> OnAdDisplayFailed;
        public event Action<string> OnAdClosed;

        private void WireUpEvents(IAdFormat adFormat)
        {
            if (adFormat == null) return;

            adFormat.OnAdLoaded += (where) => OnAdLoaded?.Invoke(where);
            adFormat.OnAdLoadFailed += (where, err) => OnAdLoadFailed?.Invoke(where, err);

            adFormat.OnAdDisplayed += (where) => OnAdDisplayed?.Invoke(where);
            adFormat.OnAdDisplayFailed += (where, err) => OnAdDisplayFailed?.Invoke(where, err);

            adFormat.OnAdClosed += (where) => OnAdClosed?.Invoke(where);
        }

        public void Load(string where = "default") => GetTarget(where).Load(where);

        public void LoadAll()
        {
            var placements = _config.GetAllPlacements();
            foreach (var p in placements)
            {
                GetTarget(p).Load(p);
            }
        }

        public void Show(string where = "default") => GetTarget(where).Show(where);

        public void Hide(string where = "default") => GetTarget(where).Hide(where);

        public void Restore(string where)
        {
            GetTarget(where).Restore(where);
        }

        private IBannerAd GetTarget(string where)
        {
            var entry = _config.GetEntry(AdUnitType.Banner, where);
            if (entry != null && entry.BannerFormat == BannerFormatType.NativeOverlay)
            {
                return _nativeExpandBanner;
            }

            return _standardBanner;
        }
    }
}