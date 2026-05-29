using System;

namespace GameUpSDK.Ads
{
    public enum MediationProvider
    {
        None,
        Admob,
        Max,
        IronSource
    }
    
    public interface IAdNetwork
    {
        MediationProvider MediationProvider { get; set; }
        IInterstitialAd InterstitialAd { get; }
        IRewardedAd RewardedAd { get; }
        IAppOpenAd AppOpenAd { get; }
        IBannerAd BannerAd { get; }
        
        bool IsInitialized { get; }
        void Initialize();
        void SetConsent(bool isConsent);
    }

    public interface IAdFormat
    {
        event Action<string> OnAdLoaded;
        event Action<string, string> OnAdLoadFailed;
        event Action<string> OnAdDisplayed;
        event Action<string, string> OnAdDisplayFailed;
        event Action<string> OnAdClosed;
        void Load(string where = null);
        bool IsAvailable(string where = null);
    }

    public interface IInterstitialAd : IAdFormat
    {
        void Show(string where, Action onSuccess, Action onFail);
    }

    public interface IRewardedAd : IAdFormat
    {
        void Show(string where, Action onSuccess, Action onFail);
    }

    public interface IBannerAd : IAdFormat
    {
        void Show(string where, CollapsibleBannerPlacement placement = CollapsibleBannerPlacement.Bottom);
        void Hide(string where);
    }

    public interface IAppOpenAd : IAdFormat
    {
        void Show(string where, Action onSuccess, Action onFail);
    }
}