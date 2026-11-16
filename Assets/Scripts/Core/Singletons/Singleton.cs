using UnityEngine;

namespace Platformer2D.Core.Singletons
{
    /// <summary>
    /// Generic Singleton MonoBehaviour dành cho các hệ thống tồn tại trong 1 Scene đơn lẻ.
    /// Tự động hủy nếu đã tồn tại instance khác.
    /// </summary>
    /// <typeparam name="T">Component kế thừa Singleton</typeparam>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();

        public static bool HasInstance => _instance != null;

        public static T Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindAnyObjectByType<T>();
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
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[Singleton] Phát hiện instance trùng lặp của {typeof(T).Name} trên GameObject '{gameObject.name}'. Đang tự hủy component...");
                Destroy(this);
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
