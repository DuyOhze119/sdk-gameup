using UnityEngine;
using System.Collections.Generic;
using GameUpSDK.Singletons;
#if APPSFLYER_DEPENDENCIES_INSTALLED
using AppsFlyerSDK;
#endif

namespace GameUpSDK
{
    /// <summary>
    /// Gọi event / ad revenue AppsFlyer. SDK được khởi tạo bởi AppsFlyerObject (AppsFlyerObjectScript) — devKey và appID cấu hình trên object đó.
    /// </summary>
    public class AppsFlyerUtils : MonoSingletonSdk<AppsFlyerUtils>
    {
#if APPSFLYER_DEPENDENCIES_INSTALLED
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
#else
        public static void LogAdRevenue(string monetizationNetwork, int mediationNetwork,
            double eventRevenue, string revenueCurrency, Dictionary<string, string> additionalParameters = null) { }

        public static void LogEvents(string eventName, Dictionary<string, string> eventValues = null) { }
#endif
    }
}
