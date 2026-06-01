using System;

namespace GameUpSDK.Ads
{
    public enum AdUnitType
    {
        Banner,
        Interstitial,
        RewardedVideo,
        AppOpen,
        NativeAd
    }

    public enum CollapsibleBannerPlacement
    {
        None,
        Top,
        Bottom
    }

    [Serializable]
    public class AdUnitIdEntry
    {
        public AdUnitType AdType;
        public string NameId; // placement "where"
        public string Id;     // Unit ID thực tế
        public int intId;
        public BannerSize BannerSize = BannerSize.Adaptive;
        public CollapsibleBannerPlacement CollapsiblePlacement = CollapsibleBannerPlacement.None;

        public bool IsValid() => !string.IsNullOrEmpty(Id);
    }
}