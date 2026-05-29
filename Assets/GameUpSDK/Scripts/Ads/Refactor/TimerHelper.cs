using System;
using System.Collections;
using GameUpSDK.Singletons;
using UnityEngine;

namespace GameUpSDK.Ads
{
    public class TimerHelper : MonoSingletonSdk<TimerHelper>
    {
        public static void Schedule(float time, Action callback)
        {
            Instance.StartCoroutine(Instance.IESchedule(time, callback));
        }

        private IEnumerator IESchedule(float time, Action callback)
        {
            yield return new WaitForSeconds(time);
            callback?.Invoke();
        }
    }
}