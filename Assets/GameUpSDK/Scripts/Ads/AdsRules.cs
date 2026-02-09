using System;
using UnityEngine;

namespace GameUpSDK
{
    /// <summary>
    /// Quy tắc hiển thị quảng cáo: inter_capping_time, inter_start_level, và mở rộng sau này.
    /// Dùng Firebase Remote Config cho giá trị; class này quản lý logic (capping, level).
    /// </summary>
    public static class AdsRules
    {
        private static double _lastInterstitialShowTime;

        /// <summary>
        /// Kiểm tra có được phép hiển thị Interstitial tại level hiện tại không.
        /// Điều kiện: level >= inter_start_level và đã qua ít nhất inter_capping_time (giây) kể từ lần show trước.
        /// </summary>
        /// <param name="currentLevel">Level hiện tại (tính từ 1).</param>
        /// <returns>True nếu được phép show interstitial.</returns>
        public static bool CanShowInterstitial(int currentLevel)
        {
            if (FirebaseRemoteConfigUtils.Instance == null)
                return true;

            int startLevel = FirebaseRemoteConfigUtils.Instance.inter_start_level;
            if (currentLevel < startLevel)
                return false;

            int cappingSeconds = FirebaseRemoteConfigUtils.Instance.inter_capping_time;
            if (cappingSeconds <= 0)
                return true;

            double now = GetCurrentTimeSeconds();
            double elapsed = now - _lastInterstitialShowTime;
            return elapsed >= cappingSeconds;
        }

        /// <summary>
        /// Gọi sau khi đã hiển thị Interstitial thành công để cập nhật capping.
        /// </summary>
        public static void RecordInterstitialShown()
        {
            _lastInterstitialShowTime = GetCurrentTimeSeconds();
        }

        /// <summary>
        /// Kiểm tra có được phép hiển thị Banner không (theo Remote Config enable_banner).
        /// </summary>
        public static bool IsBannerEnabled()
        {
            if (FirebaseRemoteConfigUtils.Instance == null)
                return true;
            return FirebaseRemoteConfigUtils.Instance.enable_banner;
        }

        private static double GetCurrentTimeSeconds()
        {
            return (double)DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond;
        }

        /// <summary>
        /// Reset thời gian capping (test hoặc khi cần bỏ qua capping). Không gọi trong production.
        /// </summary>
        public static void ResetInterstitialCappingForTest()
        {
            _lastInterstitialShowTime = 0;
        }
    }
}
