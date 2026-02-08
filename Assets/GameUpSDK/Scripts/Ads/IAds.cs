namespace GameUpSDK
{
    /// <summary>
    /// Full ads contract: init, request, show, and availability. OrderExecute controls waterfall priority.
    /// </summary>
    public interface IAds : IInitialAds, ICheckValidAds, IShowAds, IRequestAds
    {
        /// <summary>
        /// Lower value = higher priority in AdsManager waterfall.
        /// </summary>
        int OrderExecute { get; set; }

        /// <summary>
        /// Call after GDPR/consent check so the network can apply user consent.
        /// </summary>
        void SetAfterCheckGDPR();
    }
}
