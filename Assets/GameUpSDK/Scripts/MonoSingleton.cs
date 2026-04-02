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
                Debug.Log($"Dont has InstanceOf {typeof(T).Name} class");
                _instance = FindObjectOfType<T>(true);
                if (!_instance)
                {
                    _instance = new GameObject(typeof(T).Name + " Singleton").AddComponent<T>();
                }
                return _instance;
            }
        }
    }
}