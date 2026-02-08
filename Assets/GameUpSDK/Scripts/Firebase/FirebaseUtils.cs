using System;
using UnityEngine;
using Firebase;
using Firebase.Analytics;
using Firebase.Crashlytics;
using Firebase.Extensions;
using System.Collections.Generic;

namespace GameUpSDK
{       
    public class FirebaseUtils : MonoSingleton<FirebaseUtils>
    {
        private bool initialize = false;
        public Action<bool> onInitialized;

        private void Awake()
        {
            FirebaseInit();
        }

        private void FirebaseInit()
        {
            if (!IsEditor())
            {
                FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        Debug.LogError("[Firebase] Init failed: " + (task.Exception?.Message ?? "Unknown"));
                        onInitialized?.Invoke(false);
                        return;
                    }

                    var dependencyStatus = task.Result;
                    if (dependencyStatus == DependencyStatus.Available)
                    {
                        Initialized();
                    }
                    else
                    {
                        Debug.LogError("[Firebase] Could not resolve dependencies: " + dependencyStatus);
                        onInitialized?.Invoke(false);
                    }
                });
            }
            else
            {
                onInitialized?.Invoke(true);
            }
        }

        private void Initialized()
        {
            initialize = true;
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
            Crashlytics.IsCrashlyticsCollectionEnabled = true;
            onInitialized?.Invoke(true);
        }

        private bool IsEditor()
        {
            return Application.platform == RuntimePlatform.OSXEditor ||
                Application.platform == RuntimePlatform.WindowsEditor;
        }

        public static void LogEventsAPI(string eventId, Dictionary<object, object> param = null)
        {
                Instance._LogEvents(eventId, param);
        }

        /// <summary>
        /// Logs a single-parameter event. Pass null or empty paramName/paramValue for no params.
        /// </summary>
        public static void LogEvent(string eventName, string paramName, string paramValue)
        {
            if (string.IsNullOrEmpty(paramName) || paramValue == null)
            {
                Instance._LogEvents(eventName, null);
                return;
            }
            var param = new Dictionary<object, object> { { paramName, paramValue } };
            Instance._LogEvents(eventName, param);
        }

        #region Log Events

        private void _LogEvents(string eventId, Dictionary<object, object> param = null)
        {
            if (!initialize)
            {
                return;
            }
            if (IsEditor())
            {
                Debug.Log("[Firebase] " + eventId);
                return;
            }

            if (param == null)
            {
                FirebaseAnalytics.LogEvent(eventId.ToString());
            }
            else
            {
                var parameters = new List<Parameter>();
                foreach (var p in param)
                {
                    if (p.Value != null)
                    {
                        parameters.Add(new Parameter(p.Key.ToString(), p.Value.ToString()));
                    }
                }

                FirebaseAnalytics.LogEvent(eventId.ToString(), parameters.ToArray());
            }
        }

        public void LogError(string error)
        {
            if (!initialize) return;
            if (IsEditor())
            {
                Debug.Log("[Firebase] " + error);
                return;
            }

            Crashlytics.Log(error);
        }

        public void LogException(Exception e)
        {
            if (!initialize) return;
            if (IsEditor())
            {
                Debug.Log("[Firebase] " + e.Message);
                return;
            }

            Crashlytics.LogException(e);
        }

        #endregion
    }
}