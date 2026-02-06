using UnityEngine;
using System.Collections.Generic;
using AppsFlyerSDK;

namespace GameUpSDK
{
    public class AppsFlyerUtils : MonoSingleton<AppsFlyerUtils>
    {
        [SerializeField] private string sdkKey;
        [SerializeField] private string appId;
        [SerializeField] private bool isDevMode = false;
        private void Awake()
        {
            AppsFlyer.setIsDebug(isDevMode);
            AppsFlyer.initSDK(sdkKey, GameUtils.IsAndroid() ? "" : appId);
            AppsFlyer.startSDK();
        }

        /// <summary>
        /// Gửi ad revenue lên AppsFlyer. Dùng enum MediationNetwork của SDK (GoogleAdMob, IronSource, ApplovinMax, ...).
        /// </summary>
        public static void LogAdRevenue(string monetizationNetwork, MediationNetwork mediationNetwork,
            double eventRevenue, string revenueCurrency, Dictionary<string, string> additionalParameters = null)
        {
            var adRevenueData = new AFAdRevenueData(monetizationNetwork, mediationNetwork, revenueCurrency, eventRevenue);
            AppsFlyer.logAdRevenue(adRevenueData, additionalParameters);
        }

        public static void LogEvents(string eventName, Dictionary<string, string> eventValues = null)
        {
            AppsFlyer.sendEvent(eventName, eventValues);
        }
    }
}