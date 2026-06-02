using UnityEngine;

namespace GameUpSDK.Ads
{
    public class CappingTimeCondition : IAdCondition
    {
        private readonly string _cappingGroup;
        private readonly AdUnitType _adUnitType;

        public CappingTimeCondition(AdUnitType adUnitType = AdUnitType.Interstitial, string cappingGroup = "default")
        {
            _cappingGroup = cappingGroup;
            _adUnitType = adUnitType;
        }

        public bool CanShow(AdUnitType adType, string where, out string reason)
        {
            if (adType == _adUnitType)
            {
                if (!AdCappingManager.Instance.IsCappingReady(_cappingGroup))
                {
                    reason = $"capping_not_ready_{_cappingGroup}";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }
    }

    public class MinLevelCondition : IAdCondition
    {
        private readonly System.Func<int> _getCurrentLevelFunc;
        private readonly int _minLevelRequired;

        public MinLevelCondition(int minLevelRequired, System.Func<int> getCurrentLevelFunc)
        {
            _minLevelRequired = minLevelRequired;
            _getCurrentLevelFunc = getCurrentLevelFunc;
        }

        public bool CanShow(AdUnitType adType, string where, out string reason)
        {
            if (adType == AdUnitType.Interstitial)
            {
                var currentLevel = _getCurrentLevelFunc?.Invoke() ?? int.MaxValue;
                if (currentLevel < _minLevelRequired)
                {
                    reason = $"level_too_low_({currentLevel}<{_minLevelRequired})";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }
    }
}