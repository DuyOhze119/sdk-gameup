using System;
using UnityEngine;
#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IPHONE)
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
#endif

namespace GameUpSDK
{
    /// <summary>
    /// AdMob (Google Mobile Ads) implementation of IAds. Handles Banner, Interstitial, Rewarded, and App Open.
    /// </summary>
    public class AdmobAds : MonoBehaviour, IAds, INativeOverlayAds
    {
        [Header("Ad Unit IDs (optional - set via code)")]
        [SerializeField]
        private string bannerAdUnitId;

        [SerializeField] private string interstitialAdUnitId;
        [SerializeField] private string rewardedAdUnitId;
        [SerializeField] private string appOpenAdUnitId;
        [SerializeField] private string nativeOverlayAdUnitId;

        public int OrderExecute { get; set; }

        public event Action OnInterstitialLoaded;
        public event Action<string> OnInterstitialLoadFailed;
        public event Action OnRewardedLoaded;
        public event Action<string> OnRewardedLoadFailed;

        private bool _initialized;

#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IPHONE)
        private BannerView _bannerView;
        private InterstitialAd _interstitialAd;
        private RewardedAd _rewardedAd;
        private AppOpenAd _appOpenAd;
        private NativeOverlayAd _nativeOverlayAd;
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

        public void SetNativeOverlayAdUnitId(string nativeOverlay)
        {
            nativeOverlayAdUnitId = nativeOverlay;
        }

        public void Initialize()
        {
#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IPHONE)
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
                    // Request ads ngay khi SDK sẵn sàng (tránh gọi RequestAll() trước khi init xong).
                    RequestBanner();
                    RequestInterstitial();
                    RequestRewardedVideo();
                    RequestAppOpenAds();
                    RequestNativeOverlay("init");
                });
            });
#else
            _initialized = true;
            Debug.Log("[GameUp] AdmobAds skipped (not mobile platform).");
#endif
        }

        public void SetAfterCheckGDPR()
        {
#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IPHONE)
            // Consent is typically handled by UMP; SDK respects it after init.
            Debug.Log("[GameUp] AdmobAds SetAfterCheckGDPR called.");
            GoogleMobileAds.Mediation.UnityAds.Api.UnityAds.SetConsentMetaData("gdpr.consent", true);
            GoogleMobileAds.Mediation.IronSource.Api.IronSource.SetMetaData("do_not_sell", "true");
#endif
        }

        public void RequestBanner()
        {
#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IPHONE)
            if (!_initialized || string.IsNullOrEmpty(bannerAdUnitId)) return;
            MainThreadDispatcher.Enqueue(() =>
            {
                if (_bannerView != null)
                {
                    _bannerView.Destroy();
                    _bannerView = null;
                }

                // Dùng size chuẩn để có fill. Custom (full width x 150) dễ bị "request doesn't meet size requirements".
                _bannerView = new BannerView(bannerAdUnitId, AdSize.Banner, AdPosition.Bottom);
                _bannerView.OnAdPaid += adValue =>
                {
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        if (adValue == null)
                            return;
                        double value = adValue.Value * 0.000001f;
                        var data = new AdImpressionData
                        {
                            AdNetwork = "Admob",
                            AdUnit = bannerAdUnitId,
                            InstanceName = bannerAdUnitId,
                            AdFormat = "Banner",
                            Revenue = value
                        };
                        AdsEvent.RaiseImpressionDataReady(data);
                    });
                };
                var request = new AdRequest();
                // AdMob BannerView hiển thị mặc định khi load xong (khác LevelPlay SetDisplayOnLoad(false)).
                // Ẩn trước LoadAd để chỉ hiện khi AdsManager.ShowBanner → Show().
                _bannerView.Hide();
                _bannerView.LoadAd(request);
            });
#endif
        }

        public void RequestInterstitial()
        {
#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IPHONE)
            if (!_initialized || string.IsNullOrEmpty(interstitialAdUnitId)) return;
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
#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IPHONE)
            if (!_initialized || string.IsNullOrEmpty(rewardedAdUnitId)) return;
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
                    ad.OnAdPaid += adValue =>
                    {
                        MainThreadDispatcher.Enqueue(() =>
                        {
                            if (adValue == null)
                                return;
                            double value = adValue.Value * 0.000001f;
                            var data = new AdImpressionData
                            {
                                AdNetwork = "Admob",
                                AdUnit = ad.GetAdUnitID(),
                                InstanceName = ad.GetAdUnitID(),
                                AdFormat = "Rewarded",
                                Revenue = value
                            };
                            MainThreadDispatcher.Enqueue(() => AdsEvent.RaiseImpressionDataReady(data));
                        });
                    };
                });
            });
#endif
        }

        public void RequestAppOpenAds()
        {
#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IPHONE)
            if (!_initialized || string.IsNullOrEmpty(appOpenAdUnitId)) return;
            if (_appOpenAd != null)
            {
                _appOpenAd.Destroy();
                _appOpenAd = null;
            }

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

        // ---- Native Overlay Ads (AdMob) ----

        public void RequestNativeOverlay(string where)
        {
#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IPHONE)
            if (!_initialized || string.IsNullOrEmpty(nativeOverlayAdUnitId))
                return;

            MainThreadDispatcher.Enqueue(() =>
            {
                if (_nativeOverlayAd != null)
                {
                    try { _nativeOverlayAd.Destroy(); }
                    catch { /* ignore */ }
                    _nativeOverlayAd = null;
                }

                var adRequest = new AdRequest();

                var options = new NativeAdOptions
                {
                    AdChoicesPlacement = AdChoicesPlacement.TopRightCorner,
                    MediaAspectRatio = MediaAspectRatio.Any
                };

                NativeOverlayAd.Load(nativeOverlayAdUnitId, adRequest, options, (NativeOverlayAd ad, LoadAdError error) =>
                {
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        if (error != null || ad == null)
                        {
                            Debug.LogError("[GameUp] AdmobAds NativeOverlay load failed: " + (error?.GetMessage() ?? "null"));
                            return;
                        }

                        _nativeOverlayAd = ad;
                        RegisterNativeOverlayEvents(ad);
                    });
                });
            });
#endif
        }

        public bool IsNativeOverlayAvailable()
        {
#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IPHONE)
            return _nativeOverlayAd != null;
#else
            return false;
#endif
        }

        public void RenderNativeOverlay(string where, NativeOverlayPlacement placement, NativeOverlayTemplateStyle style = null)
        {
#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IPHONE)
            if (_nativeOverlayAd == null)
                return;

            MainThreadDispatcher.Enqueue(() =>
            {
                // Try exact pixel placement first if provided (depends on plugin version).
                if (placement.PixelX.HasValue && placement.PixelY.HasValue)
                {
                    // Unity screen coords: origin bottom-left. Android view coords: origin top-left.
                    // Using RenderTemplate(style, x, y) has shown device-specific issues; render by anchor first,
                    // then move the template with SetTemplatePosition(x, y).
                    var xPx = placement.PixelX.Value;
                    var yPx = Screen.height - placement.PixelY.Value;

                    var x = PixelsToDp(xPx);
                    var y = PixelsToDp(yPx);

                    _nativeOverlayAd.RenderTemplate(BuildNativeTemplateStyle(style), AdPosition.Center);
                    _nativeOverlayAd.SetTemplatePosition(x, y);
                    return;
                }

                // Fallback to anchor presets.
                var adPosition = MapAnchorToAdPosition(placement.Anchor);
                _nativeOverlayAd.RenderTemplate(BuildNativeTemplateStyle(style), adPosition);
            });
#endif
        }

        public void ShowNativeOverlay(string where)
        {
#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IPHONE)
            MainThreadDispatcher.Enqueue(() =>
            {
                if (_nativeOverlayAd != null)
                    _nativeOverlayAd.Show();
            });
#endif
        }

        public void HideNativeOverlay(string where)
        {
#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IPHONE)
            MainThreadDispatcher.Enqueue(() =>
            {
                if (_nativeOverlayAd != null)
                    _nativeOverlayAd.Hide();
            });
#endif
        }

        public void DestroyNativeOverlay(string where)
        {
#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IPHONE)
            MainThreadDispatcher.Enqueue(() =>
            {
                if (_nativeOverlayAd == null)
                    return;
                try { _nativeOverlayAd.Destroy(); }
                finally { _nativeOverlayAd = null; }
            });
#endif
        }

        public NativeOverlayPlacement BuildPlacementFromRectTransform(RectTransform rectTransform, Canvas canvas = null)
        {
            if (rectTransform == null)
                return new NativeOverlayPlacement { Anchor = NativeOverlayAnchor.Bottom };

            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            var worldCenter = (corners[0] + corners[2]) * 0.5f;

            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera;

            var screen = RectTransformUtility.WorldToScreenPoint(cam, worldCenter);
            return new NativeOverlayPlacement
            {
                Anchor = NativeOverlayAnchor.Center,
                PixelX = Mathf.RoundToInt(screen.x),
                PixelY = Mathf.RoundToInt(screen.y),
            };
        }

#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IPHONE)
        private void RegisterNativeOverlayEvents(NativeOverlayAd ad)
        {
            ad.OnAdPaid += adValue =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    if (adValue == null)
                        return;
                    double value = adValue.Value * 0.000001f;
                    var data = new AdImpressionData
                    {
                        AdNetwork = "Admob",
                        AdUnit = nativeOverlayAdUnitId,
                        InstanceName = nativeOverlayAdUnitId,
                        AdFormat = "NativeOverlay",
                        Revenue = value
                    };
                    AdsEvent.RaiseImpressionDataReady(data);
                });
            };
        }

        private static int PixelsToDp(int px)
        {
#if UNITY_ANDROID
            var density = GetAndroidDensity();
            if (density <= 0f)
                return px;
            return Mathf.RoundToInt(px / density);
#else
            return px;
#endif
        }

#if UNITY_ANDROID
        private static float _androidDensity = -1f;

        private static float GetAndroidDensity()
        {
            if (_androidDensity > 0f)
                return _androidDensity;

            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var resources = activity.Call<AndroidJavaObject>("getResources");
                using var metrics = resources.Call<AndroidJavaObject>("getDisplayMetrics");
                _androidDensity = metrics.Get<float>("density");
            }
            catch
            {
                _androidDensity = 1f;
            }

            if (_androidDensity <= 0f)
                _androidDensity = 1f;

            return _androidDensity;
        }
#endif

        private static NativeTemplateStyle BuildNativeTemplateStyle(NativeOverlayTemplateStyle style)
        {
            var result = new NativeTemplateStyle
            {
                TemplateId = NativeTemplateId.Medium
            };

            if (style == null)
                return result;

            result.TemplateId = style.TemplateId == NativeOverlayTemplateId.Small ? NativeTemplateId.Small : NativeTemplateId.Medium;

            if (style.MainBackgroundColor.HasValue)
                result.MainBackgroundColor = style.MainBackgroundColor.Value;

            if (style.CallToAction != null)
            {
                var cta = new NativeTemplateTextStyle();
                if (style.CallToAction.BackgroundColor.HasValue) cta.BackgroundColor = style.CallToAction.BackgroundColor.Value;
                if (style.CallToAction.FontColor.HasValue) cta.TextColor = style.CallToAction.FontColor.Value;
                if (style.CallToAction.FontSize.HasValue) cta.FontSize = style.CallToAction.FontSize.Value;
                if (style.CallToAction.FontStyle.HasValue)
                {
                    cta.Style = style.CallToAction.FontStyle.Value switch
                    {
                        NativeOverlayFontStyle.Bold => NativeTemplateFontStyle.Bold,
                        NativeOverlayFontStyle.Italic => NativeTemplateFontStyle.Italic,
                        NativeOverlayFontStyle.Monospace => NativeTemplateFontStyle.Monospace,
                        _ => NativeTemplateFontStyle.Normal
                    };
                }
                result.CallToActionText = cta;
            }

            return result;
        }

        private static AdPosition MapAnchorToAdPosition(NativeOverlayAnchor anchor)
        {
            return anchor switch
            {
                NativeOverlayAnchor.Top => AdPosition.Top,
                NativeOverlayAnchor.TopLeft => AdPosition.TopLeft,
                NativeOverlayAnchor.TopRight => AdPosition.TopRight,
                NativeOverlayAnchor.BottomLeft => AdPosition.BottomLeft,
                NativeOverlayAnchor.BottomRight => AdPosition.BottomRight,
                NativeOverlayAnchor.Center => AdPosition.Center,
                _ => AdPosition.Bottom
            };
        }
#endif

#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IPHONE)
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

            ad.OnAdPaid += adValue =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    if (adValue == null)
                        return;
                    double value = adValue.Value * 0.000001f;
                    var data = new AdImpressionData
                    {
                        AdNetwork = "Admob",
                        AdUnit = ad.GetAdUnitID(),
                        InstanceName = ad.GetAdUnitID(),
                        AdFormat = "Interstitial",
                        Revenue = value
                    };
                    MainThreadDispatcher.Enqueue(() => AdsEvent.RaiseImpressionDataReady(data));
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

            ad.OnAdPaid += adValue =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    if (adValue == null)
                        return;
                    double value = adValue.Value * 0.000001f;
                    var data = new AdImpressionData
                    {
                        AdNetwork = "Admob",
                        AdUnit = ad.GetAdUnitID(),
                        InstanceName = ad.GetAdUnitID(),
                        AdFormat = "AppOpenAd",
                        Revenue = value
                    };
                    MainThreadDispatcher.Enqueue(() => AdsEvent.RaiseImpressionDataReady(data));
                });
            };
        }
#endif

        public bool IsBannerAvailable()
        {
#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IPHONE)
            return _bannerView != null;
#else
            return false;
#endif
        }

        public bool IsInterstitialAvailable()
        {
#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IPHONE)
            return _interstitialAd != null && _interstitialAd.CanShowAd();
#else
            return false;
#endif
        }

        public bool IsRewardedVideoAvailable()
        {
#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IPHONE)
            return _rewardedAd != null && _rewardedAd.CanShowAd();
#else
            return false;
#endif
        }

        public bool IsAppOpenAdsAvailable()
        {
#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IPHONE)
            return _appOpenAd != null && _appOpenAd.CanShowAd() && DateTime.Now < _appOpenExpireTime;
#else
            return false;
#endif
        }

        public void ShowBanner(string where)
        {
#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IPHONE)
            MainThreadDispatcher.Enqueue(() =>
            {
                if (_bannerView != null)
                    _bannerView.Show();
            });
#endif
        }

        public void HideBanner(string where)
        {
#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IPHONE)
            MainThreadDispatcher.Enqueue(() => { _bannerView?.Hide(); });
#endif
        }

        public void ShowInterstitial(string where, Action onSuccess, Action onFail)
        {
#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IPHONE)
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
#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IPHONE)
            if (_rewardedAd == null || !_rewardedAd.CanShowAd())
            {
                onFail?.Invoke();
                return;
            }

            AdsRules.BeginInterstitialCappingPause();
            var rewardGranted = false;
            var ad = _rewardedAd;
            _rewardedAd = null;
            ad.OnAdFullScreenContentClosed += () =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    AdsRules.EndInterstitialCappingPause();
                    if (!rewardGranted) onFail?.Invoke();
                    RequestRewardedVideo();
                });
            };
            ad.OnAdFullScreenContentFailed += _ =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    AdsRules.EndInterstitialCappingPause();
                    onFail?.Invoke();
                    RequestRewardedVideo();
                });
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
#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IPHONE)
            if (_appOpenAd == null || !_appOpenAd.CanShowAd() || DateTime.Now >= _appOpenExpireTime)
            {
                onFail?.Invoke();
                return;
            }

            var ad = _appOpenAd;
            _appOpenAd = null;
            ad.OnAdFullScreenContentClosed += () => MainThreadDispatcher.Enqueue(() =>
            {
                onSuccess?.Invoke();
                RequestAppOpenAds();
            });
            ad.OnAdFullScreenContentFailed += _ => MainThreadDispatcher.Enqueue(() =>
            {
                onFail?.Invoke();
                RequestAppOpenAds();
            });
            ad.Show();
#else
            onFail?.Invoke();
#endif
        }

        private void OnDestroy()
        {
#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IPHONE)
            _bannerView?.Destroy();
            _interstitialAd?.Destroy();
            _rewardedAd?.Destroy();
            _appOpenAd?.Destroy();
            try { _nativeOverlayAd?.Destroy(); } catch { /* ignore */ }
#endif
        }
    }
}