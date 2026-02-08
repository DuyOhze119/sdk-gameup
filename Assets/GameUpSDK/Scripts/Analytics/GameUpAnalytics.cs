using System.Collections.Generic;

namespace GameUpSDK
{
    /// <summary>
    /// Game analytics only: logs to Firebase (game events) and/or AppsFlyer (MMP events).
    /// Ad-related logging is handled in Ads flow. Same-purpose events (e.g. level complete) log to both in one call.
    /// </summary>
    public static class GameUpAnalytics
    {
        private static void LogFirebase(string eventName, string paramName = null, string paramValue = null)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            FirebaseUtils.LogEvent(eventName, paramName, paramValue);
        }

        private static void LogFirebaseParams(string eventName, Dictionary<string, string> param)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            if (param == null || param.Count == 0)
            {
                FirebaseUtils.LogEventsAPI(eventName, null);
                return;
            }
            var fbParam = new Dictionary<object, object>();
            foreach (var p in param)
                if (p.Value != null)
                    fbParam[p.Key] = p.Value;
            FirebaseUtils.LogEventsAPI(eventName, fbParam);
        }

        private static void LogAppsFlyer(string eventName, Dictionary<string, string> eventValues = null)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            AppsFlyerUtils.LogEvents(eventName, eventValues);
        }

        // ---------- Firebase: Virtual currency ----------

        /// <summary> earn_virtual_currency: virtual_currency_name, value, source </summary>
        public static void LogEarnVirtualCurrency(string virtualCurrencyName, string value, string source)
        {
            var p = new Dictionary<string, string>
            {
                [AnalyticsEvent.ParamVirtualCurrencyName] = virtualCurrencyName ?? "",
                [AnalyticsEvent.ParamValue] = value ?? "",
                [AnalyticsEvent.ParamSource] = source ?? ""
            };
            LogFirebaseParams(AnalyticsEvent.EarnVirtualCurrency, p);
        }

        /// <summary> spend_virtual_currency: virtual_currency_name, value, source </summary>
        public static void LogSpendVirtualCurrency(string virtualCurrencyName, string value, string source)
        {
            var p = new Dictionary<string, string>
            {
                [AnalyticsEvent.ParamVirtualCurrencyName] = virtualCurrencyName ?? "",
                [AnalyticsEvent.ParamValue] = value ?? "",
                [AnalyticsEvent.ParamSource] = source ?? ""
            };
            LogFirebaseParams(AnalyticsEvent.SpendVirtualCurrency, p);
        }

        // ---------- Firebase: Loading ----------

        /// <summary> start_loading - khi bắt đầu loading </summary>
        public static void LogStartLoading()
        {
            LogFirebase(AnalyticsEvent.StartLoading, null, null);
        }

        /// <summary> complete_loading - khi hoàn thành loading, vào màn hình home </summary>
        public static void LogCompleteLoading()
        {
            LogFirebase(AnalyticsEvent.CompleteLoading, null, null);
        }

        // ---------- Level (Firebase + AppsFlyer af_level_achieved - chung mục đích) ----------

        /// <summary> level_start: level (từ 1), index (lần bắt đầu thứ bao nhiêu) </summary>
        public static void LogLevelStart(int level, int index)
        {
            var p = new Dictionary<string, string>
            {
                [AnalyticsEvent.ParamLevel] = level.ToString(),
                [AnalyticsEvent.ParamIndex] = index.ToString()
            };
            LogFirebaseParams(AnalyticsEvent.LevelStart, p);
        }

        /// <summary> level_fail: level, index (số lần thua ở level này), time (giây từ lúc bắt đầu đến lúc thua) </summary>
        public static void LogLevelFail(int level, int index, float timeSeconds)
        {
            var p = new Dictionary<string, string>
            {
                [AnalyticsEvent.ParamLevel] = level.ToString(),
                [AnalyticsEvent.ParamIndex] = index.ToString(),
                [AnalyticsEvent.ParamTime] = timeSeconds.ToString("F0")
            };
            LogFirebaseParams(AnalyticsEvent.LevelFail, p);
        }

        /// <summary> level_complete (Firebase) + af_level_achieved (AppsFlyer): level, index, time; optional af_score. </summary>
        public static void LogLevelComplete(int level, int index, float timeSeconds, int? score = null)
        {
            var fb = new Dictionary<string, string>
            {
                [AnalyticsEvent.ParamLevel] = level.ToString(),
                [AnalyticsEvent.ParamIndex] = index.ToString(),
                [AnalyticsEvent.ParamTime] = timeSeconds.ToString("F0")
            };
            LogFirebaseParams(AnalyticsEvent.LevelComplete, fb);

            var af = new Dictionary<string, string> { [AnalyticsEvent.ParamAfLevel] = level.ToString() };
            if (score.HasValue)
                af[AnalyticsEvent.ParamAfScore] = score.Value.ToString();
            LogAppsFlyer(AnalyticsEvent.AfLevelAchieved, af);
        }

        // ---------- Firebase: Button ----------

        /// <summary> button_click: source (tên button, bao gồm vị trí) </summary>
        public static void LogButtonClick(string source)
        {
            LogFirebase(AnalyticsEvent.ButtonClick, AnalyticsEvent.ParamSource, source ?? "");
        }

        // ---------- Firebase: Wave (game có chia wave) ----------

        /// <summary> wave_start: level, wave </summary>
        public static void LogWaveStart(int level, int wave)
        {
            var p = new Dictionary<string, string>
            {
                [AnalyticsEvent.ParamLevel] = level.ToString(),
                [AnalyticsEvent.ParamWave] = wave.ToString()
            };
            LogFirebaseParams(AnalyticsEvent.WaveStart, p);
        }

        /// <summary> wave_fail: level, wave </summary>
        public static void LogWaveFail(int level, int wave)
        {
            var p = new Dictionary<string, string>
            {
                [AnalyticsEvent.ParamLevel] = level.ToString(),
                [AnalyticsEvent.ParamWave] = wave.ToString()
            };
            LogFirebaseParams(AnalyticsEvent.WaveFail, p);
        }

        /// <summary> wave_complete: level, wave </summary>
        public static void LogWaveComplete(int level, int wave)
        {
            var p = new Dictionary<string, string>
            {
                [AnalyticsEvent.ParamLevel] = level.ToString(),
                [AnalyticsEvent.ParamWave] = wave.ToString()
            };
            LogFirebaseParams(AnalyticsEvent.WaveComplete, p);
        }

        // ---------- AppsFlyer only ----------

        /// <summary> af_complete_registration: af_registration_method (e.g. Facebook) </summary>
        public static void LogCompleteRegistration(string registrationMethod)
        {
            var p = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(registrationMethod))
                p[AnalyticsEvent.ParamAfRegistrationMethod] = registrationMethod;
            LogAppsFlyer(AnalyticsEvent.AfCompleteRegistration, p.Count > 0 ? p : null);
        }

        /// <summary> af_purchase: currency, quantity, contentId, price (localized*0.63), orderId; optional af_registration_method, af_customer_user_id </summary>
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
            if (!string.IsNullOrEmpty(registrationMethod))
                p[AnalyticsEvent.ParamAfRegistrationMethod] = registrationMethod;
            if (!string.IsNullOrEmpty(customerUserId))
                p[AnalyticsEvent.ParamAfCustomerUserId] = customerUserId;
            LogAppsFlyer(AnalyticsEvent.AfPurchase, p);
        }

        /// <summary> af_tutorial_completion: af_success (bắt buộc), af_tutorial_id (optional) </summary>
        public static void LogTutorialCompletion(bool success, string tutorialId = null)
        {
            var p = new Dictionary<string, string> { [AnalyticsEvent.ParamAfSuccess] = success.ToString().ToLowerInvariant() };
            if (!string.IsNullOrEmpty(tutorialId))
                p[AnalyticsEvent.ParamAfTutorialId] = tutorialId;
            LogAppsFlyer(AnalyticsEvent.AfTutorialCompletion, p);
        }

        /// <summary> af_achievement_unlocked: content_id, af_level (optional) </summary>
        public static void LogAchievementUnlocked(string contentId, int? level = null)
        {
            var p = new Dictionary<string, string> { [AnalyticsEvent.ParamContentId] = contentId ?? "" };
            if (level.HasValue)
                p[AnalyticsEvent.ParamAfLevel] = level.Value.ToString();
            LogAppsFlyer(AnalyticsEvent.AfAchievementUnlocked, p);
        }
    }
}
