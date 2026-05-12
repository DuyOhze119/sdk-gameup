using UnityEngine;

namespace GameUpSDK.Singletons
{
    public class MonoSingletonSdk<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance) return _instance;
                _instance = FindObjectOfType<T>(true);
                if (_instance) return _instance;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Debug.LogWarning($"[GameUp] {typeof(T).Name}.Instance requested while not in Play Mode. Returning null instead of creating singleton.");
                    return null;
                }
#endif
                _instance = new GameObject(typeof(T).Name + " Singleton").AddComponent<T>();
                return _instance;
            }
        }
    }
}