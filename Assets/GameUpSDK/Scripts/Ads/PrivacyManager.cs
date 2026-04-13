using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using GameUpSDK.Singletons;
#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IOS)
using GoogleMobileAds.Ump.Api;
#endif
#if UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif

namespace GameUpSDK
{
    /// <summary>
    /// Handles privacy workflow: ATT (iOS 14.5+) then UMP consent form.
    /// </summary>
    public class PrivacyManager : MonoSingletonSdk<PrivacyManager>
    {
        private bool _started;
        private bool _completed;
        private bool _consentGranted = true;
        private Action<bool> _onCompleted;

        public bool IsCompleted => _completed;
        public bool ConsentGranted => _consentGranted;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void BeginPrivacyFlow(Action<bool> onCompleted = null)
        {
            if (onCompleted != null)
                _onCompleted += onCompleted;

            if (_completed)
            {
                _onCompleted?.Invoke(_consentGranted);
                _onCompleted = null;
                return;
            }

            if (_started)
                return;

            _started = true;
            StartCoroutine(RunPrivacyFlowCoroutine());
        }

        private IEnumerator RunPrivacyFlowCoroutine()
        {
#if UNITY_IOS && !UNITY_EDITOR
    if (RequiresAttPrompt())
        yield return RequestAttCoroutine();
#endif

            yield return RequestUmpCoroutine();

            _completed = true;

            // QUAN TRỌNG: Gọi hàm này để update trạng thái cho các Ad Network
            // Giả sử AdsManager của bạn là Singleton
            if (AdsManager.Instance != null)
            {
                AdsManager.Instance.SetAfterCheckGDPR(_consentGranted);
            }

            _onCompleted?.Invoke(_consentGranted);
            _onCompleted = null;
        }

#if UNITY_IOS && !UNITY_EDITOR
        private static bool RequiresAttPrompt()
        {
            return TryGetIosVersion(out var version) && version >= 14.5f;
        }

        private static bool TryGetIosVersion(out float version)
        {
            version = 0f;
            var os = SystemInfo.operatingSystem;
            if (string.IsNullOrEmpty(os))
                return false;

            var marker = "iPhone OS ";
            var index = os.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                return false;

            var value = os.Substring(index + marker.Length).Trim();
            var segments = value.Split('.');
            if (segments.Length < 2)
                return false;

            var normalized = segments[0] + "." + segments[1];
            return float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out version);
        }

        private IEnumerator RequestAttCoroutine()
        {
            ATTrackingStatusBinding.AuthorizationTrackingStatus status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
            // NotDetermined = 0 in Unity iOS Support package.
            if (status == 0)
            {
                ATTrackingStatusBinding.RequestAuthorizationTracking();
                const float timeout = 30f;
                float elapsed = 0f;
                while (elapsed < timeout)
                {
                    yield return null;
                    elapsed += Time.unscaledDeltaTime;
                    status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
                    if (status != 0)
                        break;
                }
            }

            Debug.Log("[GameUp] PrivacyManager ATT finished. status=" + status);
        }
#endif

        private IEnumerator RequestUmpCoroutine()
        {
#if ADMOB_DEPENDENCIES_INSTALLED && (UNITY_ANDROID || UNITY_IOS)
            bool done = false;
            bool canRequestAds = true;

            var request = new ConsentRequestParameters();
            ConsentInformation.Update(request, error =>
            {
                if (error != null)
                {
                    Debug.LogWarning("[GameUp] PrivacyManager UMP update failed: " + error.Message);
                    canRequestAds = false;
                    done = true;
                    return;
                }

                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    if (formError != null)
                        Debug.LogWarning("[GameUp] PrivacyManager UMP form failed: " + formError.Message);

                    canRequestAds = ConsentInformation.CanRequestAds();
                    done = true;
                });
            });

            while (!done)
                yield return null;

            _consentGranted = canRequestAds;
            Debug.Log("[GameUp] PrivacyManager UMP finished. consent=" + _consentGranted);
#else
            _consentGranted = true;
            yield break;
#endif
        }
    }
}
