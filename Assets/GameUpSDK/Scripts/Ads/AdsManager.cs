using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameUpSDK
{
    /// <summary>
    /// Mediator for all ad networks. Initializes networks by OrderExecute, uses waterfall for show (first available wins).
    /// Logs ads_request, ads_available, ads_show_success, ads_show_fail to Firebase with ad_type and placement.
    /// </summary>
    public class AdsManager : MonoSingleton<AdsManager>
    {
        [SerializeField] private List<MonoBehaviour> adsBehaviours = new List<MonoBehaviour>();

        private List<IAds> _ads = new List<IAds>();
        private bool _initialized;

        private void Awake()
        {
            BuildAdsList();
        }

        private void Update()
        {
            MainThreadDispatcher.ProcessQueue();
        }

        private void BuildAdsList()
        {
            _ads.Clear();
            foreach (var mb in adsBehaviours)
            {
                if (mb is IAds iads)
                    _ads.Add(iads);
            }
            _ads = _ads.OrderBy(a => a.OrderExecute).ToList();
        }

        /// <summary>
        /// Register ad networks (e.g. AdmobAds, IronSourceAds). Call before Initialize.
        /// </summary>
        public void SetAds(IEnumerable<IAds> ads)
        {
            _ads = ads?.OrderBy(a => a.OrderExecute).ToList() ?? new List<IAds>();
        }

        /// <summary>
        /// Initialize all networks in OrderExecute order and subscribe to load events for logging.
        /// </summary>
        public void Initialize()
        {
            if (_initialized)
            {
                Debug.Log("[GameUp] AdsManager already initialized.");
                return;
            }

            foreach (var ad in _ads)
            {
                try
                {
                    ad.OnInterstitialLoaded += () => LogAdsEvent(AdsEvent.InterCompleteLoad, null, null);
                    ad.OnInterstitialLoadFailed += (error) => LogAdsEvent(AdsEvent.InterLoadFail, null, error ?? "unknown");
                    ad.OnRewardedLoaded += () => LogAdsEvent(AdsEvent.RewardCompleteLoad, null, null);
                    ad.OnRewardedLoadFailed += (error) => LogAdsEvent(AdsEvent.RewardLoadFail, null, error ?? "unknown");
                    ad.Initialize();
                }
                catch (Exception e)
                {
                    Debug.LogError("[GameUp] AdsManager Init failed for " + ad.GetType().Name + ": " + e);
                }
            }
            _initialized = true;
            Debug.Log("[GameUp] AdsManager Initialize called for " + _ads.Count + " networks.");
        }

        /// <summary>
        /// Call after GDPR/consent flow. Forwards to all networks.
        /// </summary>
        public void SetAfterCheckGDPR()
        {
            foreach (var ad in _ads)
            {
                try
                {
                    ad.SetAfterCheckGDPR();
                }
                catch (Exception e)
                {
                    Debug.LogError("[GameUp] AdsManager SetAfterCheckGDPR failed for " + ad.GetType().Name + ": " + e);
                }
            }
        }

        /// <summary>
        /// Returns true if all registered networks have been initialized (no runtime check of SDK state).
        /// </summary>
        public bool CheckInitialized()
        {
            return _initialized && _ads.Count > 0;
        }

        /// <summary>
        /// Centralized logging: Firebase + AppsFlyer (when appsFlyerEventName is set). Maps 'where' to af_level for AppsFlyer.
        /// </summary>
        private void LogAdsEvent(string eventName, string paramWhere = null, string paramSource = null, string appsFlyerEventName = null)
        {
            if (!string.IsNullOrEmpty(paramWhere))
                FirebaseUtils.LogEvent(eventName, AdsEvent.ParamWhere, paramWhere);
            else if (!string.IsNullOrEmpty(paramSource))
                FirebaseUtils.LogEvent(eventName, AdsEvent.ParamSource, paramSource);
            else
                FirebaseUtils.LogEvent(eventName, null, null);

            if (!string.IsNullOrEmpty(appsFlyerEventName) && !string.IsNullOrEmpty(paramWhere))
                AppsFlyerUtils.LogEvents(appsFlyerEventName, new Dictionary<string, string> { { AdsEvent.ParamAfLevel, paramWhere } });
        }

        private void LogAdsEventManager(string eventName, string adType, string placement)
        {
            var param = new Dictionary<object, object>
            {
                { AdsEvent.ParamAdType, adType },
                { AdsEvent.ParamPlacement, placement ?? "" }
            };
            FirebaseUtils.LogEventsAPI(eventName, param);
        }

        // ---- Show with waterfall ----

        public void ShowBanner(string where)
        {
            LogAdsEventManager(AdsEvent.AdsRequest, AdsEvent.AdTypeBanner, where);
            var network = _ads.FirstOrDefault(a => a.IsBannerAvailable());
            if (network == null)
            {
                Debug.Log("[GameUp] AdsManager ShowBanner: no network available.");
                LogAdsEventManager(AdsEvent.AdsShowFail, AdsEvent.AdTypeBanner, where);
                return;
            }
            LogAdsEventManager(AdsEvent.AdsAvailable, AdsEvent.AdTypeBanner, where);
            try
            {
                network.ShowBanner(where);
                LogAdsEventManager(AdsEvent.AdsShowSuccess, AdsEvent.AdTypeBanner, where);
            }
            catch (Exception e)
            {
                Debug.LogError("[GameUp] AdsManager ShowBanner: " + e);
                LogAdsEventManager(AdsEvent.AdsShowFail, AdsEvent.AdTypeBanner, where);
            }
        }

        public void HideBanner(string where)
        {
            var network = _ads.FirstOrDefault(a => a.IsBannerAvailable());
            network?.HideBanner(where);
        }

        public void ShowInterstitial(string where, Action onSuccess = null, Action onFail = null)
        {
            LogAdsEventManager(AdsEvent.AdsRequest, AdsEvent.AdTypeInterstitial, where);
            var network = _ads.FirstOrDefault(a => a.IsInterstitialAvailable());
            if (network == null)
            {
                Debug.Log("[GameUp] AdsManager ShowInterstitial: no network available.");
                LogAdsEventManager(AdsEvent.AdsShowFail, AdsEvent.AdTypeInterstitial, where);
                onFail?.Invoke();
                return;
            }
            LogAdsEventManager(AdsEvent.AdsAvailable, AdsEvent.AdTypeInterstitial, where);
            try
            {
                LogAdsEvent(AdsEvent.InterShow, where, null, AdsEvent.AfInterShow);
                Action wrappedSuccess = () =>
                {
                    LogAdsEvent(AdsEvent.InterShowComplete, where, null, AdsEvent.AfInterDisplayed);
                    onSuccess?.Invoke();
                };
                Action wrappedFail = () => onFail?.Invoke();
                network.ShowInterstitial(where, wrappedSuccess, wrappedFail);
            }
            catch (Exception e)
            {
                Debug.LogError("[GameUp] AdsManager ShowInterstitial: " + e);
                LogAdsEventManager(AdsEvent.AdsShowFail, AdsEvent.AdTypeInterstitial, where);
                onFail?.Invoke();
            }
        }

        public void ShowRewardedVideo(string where, Action onSuccess = null, Action onFail = null)
        {
            LogAdsEventManager(AdsEvent.AdsRequest, AdsEvent.AdTypeRewardedVideo, where);
            var network = _ads.FirstOrDefault(a => a.IsRewardedVideoAvailable());
            if (network == null)
            {
                Debug.Log("[GameUp] AdsManager ShowRewardedVideo: no network available.");
                LogAdsEventManager(AdsEvent.AdsShowFail, AdsEvent.AdTypeRewardedVideo, where);
                onFail?.Invoke();
                return;
            }
            LogAdsEventManager(AdsEvent.AdsAvailable, AdsEvent.AdTypeRewardedVideo, where);
            try
            {
                LogAdsEvent(AdsEvent.RewardShow, where, null, AdsEvent.AfRewardShow);
                Action wrappedSuccess = () =>
                {
                    LogAdsEvent(AdsEvent.RewardShowComplete, where, null, AdsEvent.AfRewardDisplayed);
                    onSuccess?.Invoke();
                };
                Action wrappedFail = () => onFail?.Invoke();
                network.ShowRewardedVideo(where, wrappedSuccess, wrappedFail);
            }
            catch (Exception e)
            {
                Debug.LogError("[GameUp] AdsManager ShowRewardedVideo: " + e);
                LogAdsEventManager(AdsEvent.AdsShowFail, AdsEvent.AdTypeRewardedVideo, where);
                onFail?.Invoke();
            }
        }

        public void ShowAppOpenAds(string where, Action onSuccess = null, Action onFail = null)
        {
            LogAdsEventManager(AdsEvent.AdsRequest, AdsEvent.AdTypeAppOpen, where);
            var network = _ads.FirstOrDefault(a => a.IsAppOpenAdsAvailable());
            if (network == null)
            {
                Debug.Log("[GameUp] AdsManager ShowAppOpenAds: no network available.");
                LogAdsEventManager(AdsEvent.AdsShowFail, AdsEvent.AdTypeAppOpen, where);
                onFail?.Invoke();
                return;
            }
            LogAdsEventManager(AdsEvent.AdsAvailable, AdsEvent.AdTypeAppOpen, where);
            try
            {
                network.ShowAppOpenAds(where,
                    () =>
                    {
                        LogAdsEventManager(AdsEvent.AdsShowSuccess, AdsEvent.AdTypeAppOpen, where);
                        onSuccess?.Invoke();
                    },
                    () =>
                    {
                        LogAdsEventManager(AdsEvent.AdsShowFail, AdsEvent.AdTypeAppOpen, where);
                        onFail?.Invoke();
                    });
            }
            catch (Exception e)
            {
                Debug.LogError("[GameUp] AdsManager ShowAppOpenAds: " + e);
                LogAdsEventManager(AdsEvent.AdsShowFail, AdsEvent.AdTypeAppOpen, where);
                onFail?.Invoke();
            }
        }

        /// <summary>
        /// Request/load all ad formats on all networks (e.g. after init or after consent).
        /// Logs InterStartLoad / RewardStartLoad before each request.
        /// </summary>
        public void RequestAll()
        {
            foreach (var ad in _ads)
            {
                try
                {
                    ad.RequestBanner();
                    LogAdsEvent(AdsEvent.InterStartLoad, null, null);
                    ad.RequestInterstitial();
                    LogAdsEvent(AdsEvent.RewardStartLoad, null, null);
                    ad.RequestRewardedVideo();
                    ad.RequestAppOpenAds();
                }
                catch (Exception e)
                {
                    Debug.LogError("[GameUp] AdsManager RequestAll failed for " + ad.GetType().Name + ": " + e);
                }
            }
        }
    }
}
