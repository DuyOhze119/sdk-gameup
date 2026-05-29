using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameUpSDK.Ads
{
    [Serializable]
    public class AdUnitConfig
    {
        [Tooltip("Bật để dùng Multi-IDs theo where")]
        public bool useMultiAdUnitIds;
        
        [Header("Default Single IDs")]
        public string defaultIdAndroid;
        public string defaultIdIOS;
        
        [Header("Multi IDs")]
        public List<AdUnitIdEntry> multiIdsAndroid = new List<AdUnitIdEntry>();
        public List<AdUnitIdEntry> multiIdsIOS = new List<AdUnitIdEntry>();

        public string ResolveUnitId(AdUnitType type, string where)
        {
            bool isAndroid = GetRuntimeAdPlatform() == RuntimeAdPlatform.Android;
            var defaultId = isAndroid ? defaultIdAndroid : defaultIdIOS;
            
            if (!useMultiAdUnitIds || string.IsNullOrWhiteSpace(where)) 
                return defaultId;

            var multiIds = isAndroid ? multiIdsAndroid : multiIdsIOS;
            foreach (var entry in multiIds)
            {
                if (entry != null && entry.AdType == type && entry.IsValid() &&
                    string.Equals(entry.NameId?.Trim(), where.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return entry.Id;
                }
            }
            return defaultId; // Fallback
        }

        private enum RuntimeAdPlatform { Android, IOS }
        private RuntimeAdPlatform GetRuntimeAdPlatform()
        {
#if UNITY_ANDROID
            return RuntimeAdPlatform.Android;
#elif UNITY_IOS || UNITY_IPHONE
            return RuntimeAdPlatform.IOS;
#elif UNITY_EDITOR
            return UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS ? RuntimeAdPlatform.IOS : RuntimeAdPlatform.Android;
#else
            return RuntimeAdPlatform.Android;
#endif
        }
    }
}