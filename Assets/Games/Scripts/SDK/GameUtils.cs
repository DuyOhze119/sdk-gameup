using UnityEngine;

namespace GameUpSDK
{
    public static class GameUtils
    {
        public static bool IsAndroid()
        {
            return Application.platform == RuntimePlatform.Android;
        }

        public static bool IsIOS()
        {
            return Application.platform == RuntimePlatform.IPhonePlayer;
        }

        public static bool IsEditor()
        {
            return Application.isEditor;
        }
    }
}
