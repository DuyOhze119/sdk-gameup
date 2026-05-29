using System;

namespace GameUpSDK.Ads
{
    public class DummyAppOpenAd : BaseAdFormat, IAppOpenAd
    {
        public DummyAppOpenAd() : base()
        {
            
        }
        
        public DummyAppOpenAd(AdUnitConfig config, AdUnitType adType, string networkName) : base(config, adType,
            networkName)
        {
        }

        public override bool IsAvailable(string where = null)
        {
            return false;
        }

        public void Show(string where, Action onSuccess, Action onFail)
        {
            
        }

        protected override void RequestAdInternal(string unitId, string where)
        {
        }
    }
}