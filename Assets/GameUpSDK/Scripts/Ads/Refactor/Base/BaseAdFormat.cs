using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameUpSDK.Ads
{
    public abstract class BaseAdFormat : IAdFormat
    {
        protected readonly AdUnitConfig _config;
        protected readonly AdUnitType _adType;
        protected readonly string _networkName;
        
        public event Action<string> OnAdLoaded;
        public event Action<string, string> OnAdLoadFailed;
        public event Action<string> OnAdDisplayed;
        public event Action<string, string> OnAdDisplayFailed;
        public event Action<string> OnAdClosed;
        
        protected void NotifyAdDisplayed(string where) => OnAdDisplayed?.Invoke(where);
        protected void NotifyAdDisplayFailed(string where, string error) => OnAdDisplayFailed?.Invoke(where, error);
        protected void NotifyAdClosed(string where) => OnAdClosed?.Invoke(where);
        
        // Quản lý trạng thái Loading và Retry độc lập theo Placement (where)
        private readonly Dictionary<string, bool> _isLoadingByWhere = new Dictionary<string, bool>();
        private readonly Dictionary<string, int> _retryAttemptsByWhere = new Dictionary<string, int>();

        protected const int LoadRetryExponentCap = 6; // Tối đa delay 2^6 = 64s

        protected BaseAdFormat()
        {
            
        }
        
        protected BaseAdFormat(AdUnitConfig config, AdUnitType adType, string networkName)
        {
            _config = config;
            _adType = adType;
            _networkName = networkName;
        }

        protected string GetKey(string where) => _config.ResolveUnitId(_adType, where);

        public virtual void Load(string where = null)
        {
            string unitId = _config.ResolveUnitId(_adType, where);
            if (string.IsNullOrEmpty(unitId)) return;

            string key = GetKey(where);

            if (IsLoading(key))
            {
                LogTrace("request_skipped", unitId, where, "reason=already_loading");
                return;
            }

            SetLoadingState(key, true);
            LogTrace("request", unitId, where);
            
            RequestAdInternal(unitId, where);
        }

        public void LoadAll()
        {
            var placements = _config.GetAllPlacements();
            foreach (var placement in placements)
            {
                Load(placement);
            }
        }

        public abstract bool IsAvailable(string where = null);

        protected abstract void RequestAdInternal(string unitId, string where);

        private bool IsLoading(string key)
        {
            return _isLoadingByWhere.TryGetValue(key, out bool loading) && loading;
        }

        private void SetLoadingState(string key, bool isLoading)
        {
            _isLoadingByWhere[key] = isLoading;
        }
        
        protected void HandleLoadFailed(string unitId, string where, string error)
        {
            string key = GetKey(where);
            SetLoadingState(key, false);

            int currentRetry = _retryAttemptsByWhere.TryGetValue(key, out int attempts) ? attempts : 0;
            currentRetry++;
            _retryAttemptsByWhere[key] = currentRetry;

            float retryDelay = (float)Math.Pow(2, Math.Min(LoadRetryExponentCap, currentRetry));
            OnAdLoadFailed?.Invoke(unitId, where);
            LogTrace("load_failed_retry", unitId, where, $"delay={retryDelay}s, attempt={currentRetry}, error={error}");
            MainThreadDispatcher.Enqueue(() =>
            {
                TimerHelper.Schedule(retryDelay, () => Load(where)); 
            });
        }

        protected void HandleLoadSuccess(string unitId, string where)
        {
            string key = GetKey(where);
            SetLoadingState(key, false);
            _retryAttemptsByWhere[key] = 0;
            OnAdLoaded?.Invoke(where);
            LogTrace("load_success", unitId, where);
        }
        
        protected void LogTrace(string phase, string unitId, string where, string extra = null)
        {
            var msg = $"[GameUp] {_adType} {phase} | where={where ?? "null"} | unitId={unitId ?? "null"}";
            if (!string.IsNullOrEmpty(extra)) msg += $" | {extra}";
            Debug.Log(msg);
        }
        
        protected void TrackRevenue(string adUnitId, string placement, string adFormat, double revenue)
        {
            var data = new AdImpressionData
            {
                AdNetwork = _networkName,
                AdUnit = adUnitId,
                InstanceName = placement,
                AdFormat = adFormat,
                Revenue = revenue
            };
            MainThreadDispatcher.Enqueue(() => AdsEvent.RaiseImpressionDataReady(data));
        }
    }
}