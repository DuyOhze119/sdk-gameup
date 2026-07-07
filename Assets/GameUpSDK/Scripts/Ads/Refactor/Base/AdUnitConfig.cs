using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameUpSDK.Ads
{
    [Serializable]
    public class AdUnitConfig
    {
        [Tooltip("Bật nếu muốn cấu hình ID riêng cho từng vị trí (where). Tắt nếu muốn dùng chung ID mặc định.")]
        public bool useMultiAdUnitIds;

        [Header("Default IDs (Android) - Dùng khi tắt Multi IDs")]
        public string defaultIdAndroid_High;
        public string defaultIdAndroid_Medium;
        [FormerlySerializedAs("defaultIdAndroid")] 
        public string defaultIdAndroid_All; // Giữ lại data cũ trên Inspector

        [Header("Default IDs (iOS) - Dùng khi tắt Multi IDs")]
        public string defaultIdIOS_High;
        public string defaultIdIOS_Medium;
        [FormerlySerializedAs("defaultIdIOS")] 
        public string defaultIdIOS_All; // Giữ lại data cũ trên Inspector

        [Header("Default Banner Settings")]
        public BannerSize defaultBannerSize = BannerSize.Adaptive;
        public BannerFormatType defaultBannerFormat = BannerFormatType.StandardBanner;
        public CollapsibleBannerPlacement defaultCollapsible = CollapsibleBannerPlacement.None;

        [Header("Multi IDs (Cấu hình riêng cho từng Placement)")] 
        public List<AdUnitIdEntry> multiIdsAndroid = new List<AdUnitIdEntry>();
        public List<AdUnitIdEntry> multiIdsIOS = new List<AdUnitIdEntry>();

        // Overload cho các request không truyền Floor (Mặc định sẽ lấy tầng ALL)
        public AdUnitIdEntry GetEntry(AdUnitType type, string where)
        {
            return GetEntry(type, where, EcpmFloor.All);
        }

        // Lấy cấu hình ID dựa theo Loại, Vị trí và Tầng eCPM
        public AdUnitIdEntry GetEntry(AdUnitType type, string where, EcpmFloor floor)
        {
            bool isAndroid = GetRuntimeAdPlatform() == RuntimeAdPlatform.Android;
            var multiIds = isAndroid ? multiIdsAndroid : multiIdsIOS;

            // 1. Chế độ Multi ID
            if (useMultiAdUnitIds && !string.IsNullOrWhiteSpace(where))
            {
                foreach (var entry in multiIds)
                {
                    if (entry != null && entry.AdType == type && entry.IsValid() &&
                        string.Equals(entry.NameId?.Trim(), where.Trim(), StringComparison.OrdinalIgnoreCase) &&
                        entry.Floor == floor) 
                    {
                        return entry;
                    }
                }
            }

            // 2. Chế độ Single ID hoặc Fallback
            string defaultId = GetDefaultId(isAndroid, floor);

            return new AdUnitIdEntry
            {
                Id = defaultId,
                AdType = type,
                NameId = where,
                BannerSize = defaultBannerSize,
                BannerFormat = defaultBannerFormat,
                CollapsiblePlacement = defaultCollapsible,
                Floor = floor
            };
        }

        private string GetDefaultId(bool isAndroid, EcpmFloor floor)
        {
            if (isAndroid)
            {
                switch (floor)
                {
                    case EcpmFloor.High: return defaultIdAndroid_High;
                    case EcpmFloor.Medium: return defaultIdAndroid_Medium;
                    case EcpmFloor.All: default: return defaultIdAndroid_All;
                }
            }
            else
            {
                switch (floor)
                {
                    case EcpmFloor.High: return defaultIdIOS_High;
                    case EcpmFloor.Medium: return defaultIdIOS_Medium;
                    case EcpmFloor.All: default: return defaultIdIOS_All;
                }
            }
        }

        public string WhereByKey(AdUnitType type, string key)
        {
            bool isAndroid = GetRuntimeAdPlatform() == RuntimeAdPlatform.Android;
            var multiIds = isAndroid ? multiIdsAndroid : multiIdsIOS;

            if (useMultiAdUnitIds && !string.IsNullOrWhiteSpace(key))
            {
                foreach (var entry in multiIds)
                {
                    if (entry != null && entry.AdType == type && entry.IsValid() &&
                        string.Equals(entry.Id?.Trim(), key.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        return entry.NameId;
                    }
                }
            }

            return "default";
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
                if (entry != null && entry.IsValid() && !string.IsNullOrWhiteSpace(entry.NameId))
                {
                    string cleanName = entry.NameId.Trim();
                    if (!placements.Contains(cleanName))
                    {
                        placements.Add(cleanName);
                    }
                }
            }

            return placements;
        }

        public List<string> GetAllWhere()
        {
            bool isAndroid = GetRuntimeAdPlatform() == RuntimeAdPlatform.Android;
            var multiIds = isAndroid ? multiIdsAndroid : multiIdsIOS;
            if (useMultiAdUnitIds)
            {
                return multiIds.Select(s => s.NameId).ToList();
            }
            else
            {
                return new List<string> { "default" };
            }
        }

        // Giải quyết chuỗi ID cụ thể (Mặc định là All nếu không truyền floor)
        public string ResolveUnitId(AdUnitType type, string where, EcpmFloor floor = EcpmFloor.All) 
        {
            return GetEntry(type, where, floor).Id;
        }

        private enum RuntimeAdPlatform
        {
            Android,
            IOS
        }

        private RuntimeAdPlatform GetRuntimeAdPlatform()
        {
#if UNITY_ANDROID
            return RuntimeAdPlatform.Android;
#elif UNITY_IOS || UNITY_IPHONE
            return RuntimeAdPlatform.IOS;
#elif UNITY_EDITOR
            return UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS
                ? RuntimeAdPlatform.IOS
                : RuntimeAdPlatform.Android;
#else
            return RuntimeAdPlatform.Android;
#endif
        }
    }
}