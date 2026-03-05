using System.Collections.Generic;
using UnityEngine;
#if GAMEUP_SDK_DEPS_READY
using Firebase.Analytics;
using AppsFlyerSDK;
#endif

namespace GameUpSDK
{
    /// <summary>
    /// Game analytics only: logs to Firebase (game events) and/or AppsFlyer (MMP events).
    /// Ad-related logging is handled in Ads flow. Same-purpose events (e.g. level complete) log to both in one call.
    /// </summary>
    public static class GameUpAnalytics
    {
#if GAMEUP_SDK_DEPS_READY
        private static void LogFirebase(string eventName, string paramName = null, string paramValue = null)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            FirebaseUtils.LogEvent(eventName, paramName, paramValue);
        }

        public static void LogFirebaseParams(string eventName, Dictionary<string, string> param)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            if (param == null || param.Count == 0) { FirebaseUtils.LogEventsAPI(eventName, null); return; }
            var fbParam = new Dictionary<object, object>();
            foreach (var p in param)
                if (p.Value != null) fbParam[p.Key] = p.Value;
            FirebaseUtils.LogEventsAPI(eventName, fbParam);
        }

        private static void LogAppsFlyer(string eventName, Dictionary<string, string> eventValues = null)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            AppsFlyerUtils.LogEvents(eventName, eventValues);
        }

        // ---------- Firebase: Virtual currency ----------

        /// <summary> start_level_1 - khi bắt đầu level 1 </summary>
        public static void LogStartLevel1() => LogFirebase(AnalyticsEvent.StartLevel1);
        public static void LogCompleteLevel1() => LogFirebase(AnalyticsEvent.CompleteLevel1);

        /// <summary> earn_virtual_currency: virtual_currency_name, value, source </summary>
        public static void LogEarnVirtualCurrency(string virtualCurrencyName, string value, string source)
        {
            LogFirebaseParams(AnalyticsEvent.EarnVirtualCurrency, new Dictionary<string, string>
            {
                [AnalyticsEvent.ParamVirtualCurrencyName] = virtualCurrencyName ?? "",
                [AnalyticsEvent.ParamValue] = value ?? "",
                [AnalyticsEvent.ParamSource] = source ?? ""
            });
        }

        /// <summary> spend_virtual_currency: virtual_currency_name, value, source </summary>
        public static void LogSpendVirtualCurrency(string virtualCurrencyName, string value, string source)
        {
            LogFirebaseParams(AnalyticsEvent.SpendVirtualCurrency, new Dictionary<string, string>
            {
                [AnalyticsEvent.ParamVirtualCurrencyName] = virtualCurrencyName ?? "",
                [AnalyticsEvent.ParamValue] = value ?? "",
                [AnalyticsEvent.ParamSource] = source ?? ""
            });
        }

        // ---------- Firebase: Loading ----------

        /// <summary> start_loading - khi bắt đầu loading </summary>
        public static void LogStartLoading() => LogFirebase(AnalyticsEvent.StartLoading);

        /// <summary> complete_loading - khi hoàn thành loading, vào màn hình home </summary>
        public static void LogCompleteLoading() => LogFirebase(AnalyticsEvent.CompleteLoading);

        // ---------- Level (Firebase + AppsFlyer af_level_achieved - chung mục đích) ----------

        /// <summary> level_start: level (từ 1), index (lần bắt đầu thứ bao nhiêu) </summary>
        public static void LogLevelStart(int level, int index)
        {
            LogFirebaseParams(AnalyticsEvent.LevelStart, new Dictionary<string, string>
            {
                [AnalyticsEvent.ParamLevel] = level.ToString(),
                [AnalyticsEvent.ParamIndex] = index.ToString()
            });
        }

        /// <summary> level_fail: level, index, time </summary>
        public static void LogLevelFail(int level, int index, float timeSeconds)
        {
            LogFirebaseParams(AnalyticsEvent.LevelFail, new Dictionary<string, string>
            {
                [AnalyticsEvent.ParamLevel] = level.ToString(),
                [AnalyticsEvent.ParamIndex] = index.ToString(),
                [AnalyticsEvent.ParamTime] = timeSeconds.ToString("F0")
            });
        }

        /// <summary> level_complete (Firebase) + af_level_achieved (AppsFlyer): level, index, time; optional af_score. </summary>
        public static void LogLevelComplete(int level, int index, float timeSeconds, int? score = null)
        {
            LogFirebaseParams(AnalyticsEvent.LevelComplete, new Dictionary<string, string>
            {
                [AnalyticsEvent.ParamLevel] = level.ToString(),
                [AnalyticsEvent.ParamIndex] = index.ToString(),
                [AnalyticsEvent.ParamTime] = timeSeconds.ToString("F0")
            });

            var af = new Dictionary<string, string> { [AnalyticsEvent.ParamAfLevel] = level.ToString() };
            if (score.HasValue) af[AnalyticsEvent.ParamAfScore] = score.Value.ToString();
            LogAppsFlyer(AnalyticsEvent.AfLevelAchieved, af);
        }

        // ---------- Firebase: Button ----------

        /// <summary> button_click: source (tên button, bao gồm vị trí) </summary>
        public static void LogButtonClick(string source) => LogFirebase(AnalyticsEvent.ButtonClick, AnalyticsEvent.ParamSource, source ?? "");

        // ---------- Firebase: Wave ----------

        /// <summary> wave_start: level, wave </summary>
        public static void LogWaveStart(int level, int wave)
        {
            LogFirebaseParams(AnalyticsEvent.WaveStart, new Dictionary<string, string>
            {
                [AnalyticsEvent.ParamLevel] = level.ToString(),
                [AnalyticsEvent.ParamWave] = wave.ToString()
            });
        }

        /// <summary> wave_fail: level, wave </summary>
        public static void LogWaveFail(int level, int wave)
        {
            LogFirebaseParams(AnalyticsEvent.WaveFail, new Dictionary<string, string>
            {
                [AnalyticsEvent.ParamLevel] = level.ToString(),
                [AnalyticsEvent.ParamWave] = wave.ToString()
            });
        }

        /// <summary> wave_complete: level, wave </summary>
        public static void LogWaveComplete(int level, int wave)
        {
            LogFirebaseParams(AnalyticsEvent.WaveComplete, new Dictionary<string, string>
            {
                [AnalyticsEvent.ParamLevel] = level.ToString(),
                [AnalyticsEvent.ParamWave] = wave.ToString()
            });
        }

        // ---------- AppsFlyer only ----------

        /// <summary> af_complete_registration: af_registration_method </summary>
        public static void LogCompleteRegistration(string registrationMethod)
        {
            var p = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(registrationMethod))
                p[AnalyticsEvent.ParamAfRegistrationMethod] = registrationMethod;
            LogAppsFlyer(AnalyticsEvent.AfCompleteRegistration, p.Count > 0 ? p : null);
        }

        /// <summary> af_purchase </summary>
        public static void LogPurchase(string currencyCode, int quantity, string contentId, string purchasePrice, string orderId,
            string registrationMethod = null, string customerUserId = null)
        {
            var p = new Dictionary<string, string>
            {
                [AnalyticsEvent.ParamAfCurrencyCode] = currencyCode ?? "",
                [AnalyticsEvent.ParamAfQuantity] = quantity.ToString(),
                [AnalyticsEvent.ParamAfContentId] = contentId ?? "",
                [AnalyticsEvent.ParamAfPurchasePrice] = purchasePrice ?? "",
                [AnalyticsEvent.ParamAfOrderId] = orderId ?? ""
            };
            if (!string.IsNullOrEmpty(registrationMethod)) p[AnalyticsEvent.ParamAfRegistrationMethod] = registrationMethod;
            if (!string.IsNullOrEmpty(customerUserId)) p[AnalyticsEvent.ParamAfCustomerUserId] = customerUserId;
            LogAppsFlyer(AnalyticsEvent.AfPurchase, p);
        }

        /// <summary> af_tutorial_completion </summary>
        public static void LogTutorialCompletion(bool success, string tutorialId = null)
        {
            var p = new Dictionary<string, string> { [AnalyticsEvent.ParamAfSuccess] = success.ToString().ToLowerInvariant() };
            if (!string.IsNullOrEmpty(tutorialId)) p[AnalyticsEvent.ParamAfTutorialId] = tutorialId;
            LogAppsFlyer(AnalyticsEvent.AfTutorialCompletion, p);
        }

        /// <summary> af_achievement_unlocked </summary>
        public static void LogAchievementUnlocked(string contentId, int? level = null)
        {
            var p = new Dictionary<string, string> { [AnalyticsEvent.ParamContentId] = contentId ?? "" };
            if (level.HasValue) p[AnalyticsEvent.ParamAfLevel] = level.Value.ToString();
            LogAppsFlyer(AnalyticsEvent.AfAchievementUnlocked, p);
        }

        // ---------- Firebase: Ad Revenue Measurement (ARM) ----------

        private static MediationNetwork GetMediationNetworkFromAdNetwork(string adNetwork)
        {
            if (string.IsNullOrEmpty(adNetwork)) return MediationNetwork.Custom;
            var n = adNetwork.Trim().ToLowerInvariant();
            if (n.Contains("admob") || n.Contains("google")) return MediationNetwork.GoogleAdMob;
            if (n.Contains("unity")) return MediationNetwork.Unity;
            if (n.Contains("applovin") || n.Contains("max")) return MediationNetwork.ApplovinMax;
            if (n.Contains("meta") || n.Contains("facebook")) return MediationNetwork.Custom;
            if (n.Contains("chartboost")) return MediationNetwork.ChartBoost;
            if (n.Contains("fyber")) return MediationNetwork.Fyber;
            if (n.Contains("appodeal")) return MediationNetwork.Appodeal;
            if (n.Contains("admost")) return MediationNetwork.Admost;
            if (n.Contains("topon")) return MediationNetwork.Topon;
            if (n.Contains("tradplus")) return MediationNetwork.Tradplus;
            if (n.Contains("yandex")) return MediationNetwork.Yandex;
            if (n.Contains("ironsource")) return MediationNetwork.IronSource;
            return MediationNetwork.Custom;
        }

        /// <summary>
        /// Logs ad_impression to Firebase for Ad Revenue Measurement (ARM).
        /// Also logs ad revenue to AppsFlyer via LogAdRevenue.
        /// </summary>
        public static void LogAdImpression(AdImpressionData data)
        {
            if (data == null || !data.Revenue.HasValue) return;

            double revenue = data.Revenue.Value;
            string adNetwork = data.AdNetwork ?? "unknown";

            var parameters = new Parameter[]
            {
                new Parameter(FirebaseAnalytics.ParameterAdPlatform, "ironSource"),
                new Parameter(FirebaseAnalytics.ParameterAdSource, adNetwork),
                new Parameter(FirebaseAnalytics.ParameterAdUnitName, data.AdUnit ?? ""),
                new Parameter(FirebaseAnalytics.ParameterAdFormat, data.InstanceName ?? data.AdFormat ?? ""),
                new Parameter(FirebaseAnalytics.ParameterCurrency, "USD"),
                new Parameter(FirebaseAnalytics.ParameterValue, revenue)
            };

            FirebaseUtils.LogEvent(FirebaseAnalytics.EventAdImpression, parameters);
            AppsFlyerUtils.LogAdRevenue(adNetwork, GetMediationNetworkFromAdNetwork(adNetwork), revenue, "USD");
            Debug.Log($"[GameUpAnalytics] Logged Ad Revenue: {revenue} USD, network: {adNetwork}");
        }
#else
        public static void LogFirebaseParams(string eventName, Dictionary<string, string> param) { }
        public static void LogStartLevel1() { }
        public static void LogCompleteLevel1() { }
        public static void LogEarnVirtualCurrency(string virtualCurrencyName, string value, string source) { }
        public static void LogSpendVirtualCurrency(string virtualCurrencyName, string value, string source) { }
        public static void LogStartLoading() { }
        public static void LogCompleteLoading() { }
        public static void LogLevelStart(int level, int index) { }
        public static void LogLevelFail(int level, int index, float timeSeconds) { }
        public static void LogLevelComplete(int level, int index, float timeSeconds, int? score = null) { }
        public static void LogButtonClick(string source) { }
        public static void LogWaveStart(int level, int wave) { }
        public static void LogWaveFail(int level, int wave) { }
        public static void LogWaveComplete(int level, int wave) { }
        public static void LogCompleteRegistration(string registrationMethod) { }
        public static void LogPurchase(string currencyCode, int quantity, string contentId, string purchasePrice, string orderId,
            string registrationMethod = null, string customerUserId = null) { }
        public static void LogTutorialCompletion(bool success, string tutorialId = null) { }
        public static void LogAchievementUnlocked(string contentId, int? level = null) { }
        public static void LogAdImpression(AdImpressionData data) { }
#endif
    }
}
