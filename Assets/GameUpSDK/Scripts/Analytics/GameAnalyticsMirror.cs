using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace GameUpSDK
{
    /// <summary>
    /// Gọi GameAnalytics qua reflection để GameUpSDK.Runtime không cần assembly reference tới GameAnalyticsSDK.
    /// Hỗ trợ cả UPM (assembly GameAnalyticsSDK) và .unitypackage cổ điển (type trong Assembly-CSharp).
    /// </summary>
    internal static class GameAnalyticsMirror
    {
#if GAMEANALYTICS_DEPENDENCIES_INSTALLED
        private static Type _gaType;
        private static MethodInfo _designStringFloat;
        private static MethodInfo _designStringFloatDict;
        private static bool _resolved;

        private static void EnsureResolved()
        {
            if (_resolved) return;
            _resolved = true;
            _gaType = FindGameAnalyticsType();
            if (_gaType == null) return;

            foreach (var m in _gaType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (m.Name != "NewDesignEvent") continue;
                var p = m.GetParameters();
                if (p.Length == 2 && p[0].ParameterType == typeof(string) && p[1].ParameterType == typeof(float))
                    _designStringFloat = m;
                if (p.Length >= 3 && p[0].ParameterType == typeof(string) && p[1].ParameterType == typeof(float)
                    && typeof(IDictionary<string, object>).IsAssignableFrom(p[2].ParameterType))
                    _designStringFloatDict = m;
            }
        }

        private static Type FindGameAnalyticsType()
        {
            var direct = Type.GetType("GameAnalyticsSDK.GameAnalytics, GameAnalyticsSDK", throwOnError: false, ignoreCase: false);
            if (direct != null) return direct;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var t = asm.GetType("GameAnalyticsSDK.GameAnalytics", throwOnError: false, ignoreCase: false);
                    if (t != null) return t;
                }
                catch
                {
                    /* ignored */
                }
            }

            return null;
        }
#endif

        public static void LogDesign(string eventPath, float value, Dictionary<string, string> stringFields)
        {
#if GAMEANALYTICS_DEPENDENCIES_INSTALLED
            EnsureResolved();
            if (_gaType == null || string.IsNullOrEmpty(eventPath)) return;

            Dictionary<string, object> objFields = null;
            if (stringFields != null && stringFields.Count > 0)
            {
                objFields = new Dictionary<string, object>();
                foreach (var kv in stringFields)
                {
                    if (kv.Value != null) objFields[kv.Key] = kv.Value;
                }
            }

            try
            {
                if (objFields != null && objFields.Count > 0 && _designStringFloatDict != null)
                {
                    var pLen = _designStringFloatDict.GetParameters().Length;
                    if (pLen >= 4)
                        _designStringFloatDict.Invoke(null, new object[] { eventPath, value, objFields, false });
                    else
                        _designStringFloatDict.Invoke(null, new object[] { eventPath, value, objFields });
                }
                else if (_designStringFloat != null)
                {
                    _designStringFloat.Invoke(null, new object[] { eventPath, value });
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GameUpAnalytics] GameAnalytics NewDesignEvent failed: " + e.Message);
            }
#endif
        }
    }
}
