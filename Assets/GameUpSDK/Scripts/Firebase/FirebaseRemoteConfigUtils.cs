using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.RemoteConfig;

namespace GameUpSDK
{
    /// <summary>
    /// Firebase Remote Config: tên biến trùng với key trên Remote để tự động map (reflection).
    /// Number → int, Boolean → bool.
    /// </summary>
    public class FirebaseRemoteConfigUtils : MonoSingleton<FirebaseRemoteConfigUtils>
    {
        // ---------- Config (tên biến = key trên Remote Config) ----------
        /// <summary>Khoảng thời gian tối thiểu (giây) giữa 2 lần hiển thị Interstitial.</summary>
        public int inter_capping_time = 120;
        /// <summary>Level bắt đầu hiện Interstitial (level tính từ 1).</summary>
        public int inter_start_level = 3;
        /// <summary>Tắt/Bật hiển thị Rate App trong Game.</summary>
        public bool enable_rate_app = false;
        /// <summary>Level hiện Rate App.</summary>
        public int level_start_show_rate_app = 5;
        /// <summary>Tắt/Bật hiển thị Popup yêu cầu Internet.</summary>
        public bool no_internet_popup_enable = true;
        /// <summary>Tắt/Bật hiển thị Banner trong Game.</summary>
        public bool enable_banner = true;

        private bool _remoteConfigReady;
        private FirebaseRemoteConfig _remoteConfig;

        public bool IsRemoteConfigReady => _remoteConfigReady;
        public Action<bool> OnFetchCompleted;

        private void Start()
        {
            InitRemoteConfig();
        }

        private async void InitRemoteConfig()
        {
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.WindowsEditor)
            {
                _remoteConfigReady = true;
                OnFetchCompleted?.Invoke(true);
                return;
            }

            try
            {
                var app = FirebaseApp.DefaultInstance;
                if (app == null)
                {
                    Debug.LogWarning("[GameUp] FirebaseRemoteConfig: FirebaseApp not ready, will retry after Firebase init.");
                    FirebaseUtils.Instance.onInitialized += OnFirebaseInitialized;
                    return;
                }

                await SetupAndFetchAsync(app);
            }
            catch (Exception e)
            {
                Debug.LogError("[GameUp] FirebaseRemoteConfig init error: " + e);
                _remoteConfigReady = true;
                OnFetchCompleted?.Invoke(false);
            }
        }

        private void OnFirebaseInitialized(bool success)
        {
            FirebaseUtils.Instance.onInitialized -= OnFirebaseInitialized;
            if (!success) return;
            var app = FirebaseApp.DefaultInstance;
            if (app != null)
                _ = SetupAndFetchAsync(app);
        }

        private async Task SetupAndFetchAsync(FirebaseApp app)
        {
            _remoteConfig = FirebaseRemoteConfig.GetInstance(app);

            var defaults = new Dictionary<string, object>
            {
                { "inter_capping_time", 120 },
                { "inter_start_level", 3 },
                { "enable_rate_app", false },
                { "level_start_show_rate_app", 5 },
                { "no_internet_popup_enable", true },
                { "enable_banner", true }
            };
            await _remoteConfig.SetDefaultsAsync(defaults);
            await _remoteConfig.EnsureInitializedAsync();

            bool activated = (await _remoteConfig.FetchAndActivateAsync());
            UpdateKeysFromRemote();
            _remoteConfigReady = true;
            OnFetchCompleted?.Invoke(activated);
        }

        private void UpdateKeysFromRemote()
        {
            if (_remoteConfig == null) return;

            Type type = GetType();
            foreach (string k in _remoteConfig.Keys)
            {
                try
                {
                    FieldInfo field = type.GetField(k, BindingFlags.Public | BindingFlags.Instance);
                    if (field == null) continue;

                    if (field.FieldType == typeof(int))
                    {
                        field.SetValue(this, (int)_remoteConfig.GetValue(k).LongValue);
                    }
                    else if (field.FieldType == typeof(bool))
                    {
                        field.SetValue(this, _remoteConfig.GetValue(k).BooleanValue);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[GameUp] RemoteConfig UpdateKeys " + k + ": " + ex.Message);
                }
            }
        }

        /// <summary>Fetch và activate config (gọi lại khi cần refresh).</summary>
        public void FetchAndActivate(Action<bool> onDone = null)
        {
            if (_remoteConfig == null)
            {
                onDone?.Invoke(false);
                return;
            }
            _remoteConfig.FetchAndActivateAsync().ContinueWithOnMainThread(task =>
            {
                bool ok = task.IsCompletedSuccessfully && task.Result;
                if (task.IsFaulted && task.Exception != null)
                    Debug.LogWarning("[GameUp] RemoteConfig FetchAndActivate: " + task.Exception.Message);
                if (ok) UpdateKeysFromRemote();
                onDone?.Invoke(ok);
            });
        }
    }
}
