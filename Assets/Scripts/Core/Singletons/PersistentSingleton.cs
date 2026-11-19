using UnityEngine;

namespace Platformer2D.Core.Singletons
{
    /// <summary>
    /// Generic Persistent Singleton MonoBehaviour dành cho các Manager sống xuyên suốt toàn bộ vòng đời game (DontDestroyOnLoad).
    /// </summary>
    /// <typeparam name="T">Component kế thừa PersistentSingleton</typeparam>
    public abstract class PersistentSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();

        public static bool HasInstance => _instance != null;
        protected bool IsDuplicateInstance => _instance != null && _instance != this;

        public static T Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindAnyObjectByType<T>();
                        if (_instance == null)
                        {
                            var singletonObject = new GameObject($"[Persistent_{typeof(T).Name}]");
                            _instance = singletonObject.AddComponent<T>();
                            DontDestroyOnLoad(singletonObject);
                        }
                    }
                    return _instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                enabled = false;
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
