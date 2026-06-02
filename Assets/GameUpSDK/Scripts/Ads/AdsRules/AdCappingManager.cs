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

        private void Start()
        {
            SetCappingLimit("default", defaultCappingTime);
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
        
        public void SetCappingLimit(string groupId, float seconds)
        {
            _cappingLimits[groupId] = seconds;
            _currentTimers.TryAdd(groupId, seconds);
        }

        public bool IsCappingReady(string groupId = "default")
        {
            float limit = _cappingLimits.GetValueOrDefault(groupId, defaultCappingTime);
            float current = _currentTimers.GetValueOrDefault(groupId, 0f);
            return current >= limit;
        }

        public void ResetCapping(string groupId = "default")
        {
            if (_currentTimers.ContainsKey(groupId)) _currentTimers[groupId] = 0f;
        }

        public void PauseAllCapping() => _pauseRequests++;
        public void ResumeAllCapping() { _pauseRequests--; if (_pauseRequests < 0) _pauseRequests = 0; }
    }
}