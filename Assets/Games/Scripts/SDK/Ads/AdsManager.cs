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
        /// Initialize all networks in OrderExecute order.
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

        private void LogAdsEvent(string eventName, string adType, string placement)
        {
            var param = new Dictionary<object, object>
            {
                { "ad_type", adType },
                { "placement", placement ?? "" }
            };
            FirebaseUtils.LogEventsAPI(eventName, param);
        }

        // ---- Show with waterfall ----

        public void ShowBanner(string where)
        {
            const string adType = "banner";
            LogAdsEvent("ads_request", adType, where);
            var network = _ads.FirstOrDefault(a => a.IsBannerAvailable());
            if (network == null)
            {
                Debug.Log("[GameUp] AdsManager ShowBanner: no network available.");
                LogAdsEvent("ads_show_fail", adType, where);
                return;
            }
            LogAdsEvent("ads_available", adType, where);
            try
            {
                network.ShowBanner(where);
                LogAdsEvent("ads_show_success", adType, where);
            }
            catch (Exception e)
            {
                Debug.LogError("[GameUp] AdsManager ShowBanner: " + e);
                LogAdsEvent("ads_show_fail", adType, where);
            }
        }

        public void HideBanner(string where)
        {
            var network = _ads.FirstOrDefault(a => a.IsBannerAvailable());
            network?.HideBanner(where);
        }

        public void ShowInterstitial(string where, Action onSuccess = null, Action onFail = null)
        {
            const string adType = "interstitial";
            LogAdsEvent("ads_request", adType, where);
            var network = _ads.FirstOrDefault(a => a.IsInterstitialAvailable());
            if (network == null)
            {
                Debug.Log("[GameUp] AdsManager ShowInterstitial: no network available.");
                LogAdsEvent("ads_show_fail", adType, where);
                onFail?.Invoke();
                return;
            }
            LogAdsEvent("ads_available", adType, where);
            try
            {
                network.ShowInterstitial(where,
                    () =>
                    {
                        LogAdsEvent("ads_show_success", adType, where);
                        onSuccess?.Invoke();
                    },
                    () =>
                    {
                        LogAdsEvent("ads_show_fail", adType, where);
                        onFail?.Invoke();
                    });
            }
            catch (Exception e)
            {
                Debug.LogError("[GameUp] AdsManager ShowInterstitial: " + e);
                LogAdsEvent("ads_show_fail", adType, where);
                onFail?.Invoke();
            }
        }

        public void ShowRewardedVideo(string where, Action onSuccess = null, Action onFail = null)
        {
            const string adType = "rewarded_video";
            LogAdsEvent("ads_request", adType, where);
            var network = _ads.FirstOrDefault(a => a.IsRewardedVideoAvailable());
            if (network == null)
            {
                Debug.Log("[GameUp] AdsManager ShowRewardedVideo: no network available.");
                LogAdsEvent("ads_show_fail", adType, where);
                onFail?.Invoke();
                return;
            }
            LogAdsEvent("ads_available", adType, where);
            try
            {
                network.ShowRewardedVideo(where,
                    () =>
                    {
                        LogAdsEvent("ads_show_success", adType, where);
                        onSuccess?.Invoke();
                    },
                    () =>
                    {
                        LogAdsEvent("ads_show_fail", adType, where);
                        onFail?.Invoke();
                    });
            }
            catch (Exception e)
            {
                Debug.LogError("[GameUp] AdsManager ShowRewardedVideo: " + e);
                LogAdsEvent("ads_show_fail", adType, where);
                onFail?.Invoke();
            }
        }

        public void ShowAppOpenAds(string where, Action onSuccess = null, Action onFail = null)
        {
            const string adType = "app_open";
            LogAdsEvent("ads_request", adType, where);
            var network = _ads.FirstOrDefault(a => a.IsAppOpenAdsAvailable());
            if (network == null)
            {
                Debug.Log("[GameUp] AdsManager ShowAppOpenAds: no network available.");
                LogAdsEvent("ads_show_fail", adType, where);
                onFail?.Invoke();
                return;
            }
            LogAdsEvent("ads_available", adType, where);
            try
            {
                network.ShowAppOpenAds(where,
                    () =>
                    {
                        LogAdsEvent("ads_show_success", adType, where);
                        onSuccess?.Invoke();
                    },
                    () =>
                    {
                        LogAdsEvent("ads_show_fail", adType, where);
                        onFail?.Invoke();
                    });
            }
            catch (Exception e)
            {
                Debug.LogError("[GameUp] AdsManager ShowAppOpenAds: " + e);
                LogAdsEvent("ads_show_fail", adType, where);
                onFail?.Invoke();
            }
        }

        /// <summary>
        /// Request/load all ad formats on all networks (e.g. after init or after consent).
        /// </summary>
        public void RequestAll()
        {
            foreach (var ad in _ads)
            {
                try
                {
                    ad.RequestBanner();
                    ad.RequestInterstitial();
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
