using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameUpSDK.Ads
{
    [Serializable]
    public class AdUnitConfig
    {
        public bool useMultiAdUnitIds;
        
        [Header("Default Single IDs")]
        public string defaultIdAndroid;
        public string defaultIdIOS;
        
        // Bổ sung cấu hình mặc định khi không dùng Multi Ids
        public BannerSize defaultBannerSize = BannerSize.Adaptive;
        public CollapsibleBannerPlacement defaultCollapsible = CollapsibleBannerPlacement.None;
        
        [Header("Multi IDs")]
        public List<AdUnitIdEntry> multiIdsAndroid = new List<AdUnitIdEntry>();
        public List<AdUnitIdEntry> multiIdsIOS = new List<AdUnitIdEntry>();

        public AdUnitIdEntry GetEntry(AdUnitType type, string where)
        {
            bool isAndroid = GetRuntimeAdPlatform() == RuntimeAdPlatform.Android;
            var multiIds = isAndroid ? multiIdsAndroid : multiIdsIOS;
            
            if (useMultiAdUnitIds && !string.IsNullOrWhiteSpace(where)) 
            {
                foreach (var entry in multiIds)
                {
                    if (entry != null && entry.AdType == type && entry.IsValid() &&
                        string.Equals(entry.NameId?.Trim(), where.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        return entry;
                    }
                }
            }
            
            // Fallback (Hoặc dành cho chế độ Default ID)
            return new AdUnitIdEntry {
                Id = isAndroid ? defaultIdAndroid : defaultIdIOS,
                AdType = type,
                NameId = where,
                BannerSize = defaultBannerSize,
                CollapsiblePlacement = defaultCollapsible
            };
        }

        public List<string> GetAllPlacements()
        {
            var placements = new List<string>();
            
            if (!useMultiAdUnitIds)
            {
                placements.Add("default");
                return placements;
            }

            bool isAndroid = GetRuntimeAdPlatform() == RuntimeAdPlatform.Android;
            var multiIds = isAndroid ? multiIdsAndroid : multiIdsIOS;

            foreach (var entry in multiIds)
            {
                if (entry != null&& entry.IsValid() && !string.IsNullOrWhiteSpace(entry.NameId))
                {
                    string cleanName = entry.NameId.Trim();
                    if (!placements.Contains(cleanName))
                    {
                        placements.Add(cleanName);
                    }
                }
            }

            if (placements.Count == 0) placements.Add("default"); 

            return placements;
        }
        
        public string ResolveUnitId(AdUnitType type, string where) => GetEntry(type, where).Id;

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