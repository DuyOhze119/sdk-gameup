using System;
using System.Collections.Generic;
using GameUpSDK.Singletons;
using UnityEngine;

namespace GameUpSDK.Ads
{
    public class AdCappingManager : MonoSingletonSdk<AdCappingManager>
    {
        private readonly Dictionary<string, float> _cappingLimits = new Dictionary<string, float>();
        private readonly Dictionary<string, float> _currentTimers = new Dictionary<string, float>();
        
        private int _pauseRequests = 0;

        [SerializeField] private float defaultCappingTime = 45f;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
        
        private void Update()
        {
            if (_pauseRequests > 0) return;

            float dt = Time.unscaledDeltaTime;
            var keys = new List<string>(_currentTimers.Keys);
            foreach (var key in keys)
            {
                _currentTimers[key] += dt;
            }
        }
        
        public void SetCappingLimit(string groupId, float limit, float seconds)
        {
            _cappingLimits[groupId] = limit;
            _currentTimers.TryAdd(groupId, seconds);
        }

        public bool IsCappingReady(string groupId = "default")
        {
            float limit = _cappingLimits.GetValueOrDefault(groupId, defaultCappingTime);
            float current = _currentTimers.GetValueOrDefault(groupId, 0f);
            Debug.LogError($"current: {current} - limit: {limit}");
            return current >= limit;
        }

        public void ResetCapping()
        {
            var keys = new List<string>(_currentTimers.Keys);
            foreach (var key in keys)
            {
                _currentTimers[key] = 0;
            }
        }

        public void PauseAllCapping() => _pauseRequests++;
        public void ResumeAllCapping() { _pauseRequests--; if (_pauseRequests < 0) _pauseRequests = 0; }
        public bool IsAnyAdShowing => _pauseRequests > 0;
    }
}