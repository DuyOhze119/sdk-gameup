using System;
using System.Collections;
using UnityEngine;
using GameUpSDK.Singletons;

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
            // Realtime, KHÔNG theo Time.timeScale: retry load ads phải chạy cả khi game đang
            // pause (timeScale = 0), nếu không thì ad sẽ không bao giờ được nạp lại.
            yield return new WaitForSecondsRealtime(time);
            callback?.Invoke();
        }
    }
}