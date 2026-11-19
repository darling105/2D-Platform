using System.Collections.Generic;
using UnityEngine;
using Platformer2D.Core.Singletons;

namespace Platformer2D.Core.ObjectPool
{
    [System.Serializable]
    public class PoolConfig
    {
        public string Tag;
        public GameObject Prefab;
        public int InitialSize = 10;
        public bool AllowGrowth = true;
    }

    /// <summary>
    /// Hệ thống Object Pooling trung tâm, loại bỏ tình trạng Garbage Collection (GC) gây giật lag
    /// khi sinh ra và hủy nhiều đạn, VFX, particle trong game 2D.
    /// </summary>
    public class ObjectPoolManager : PersistentSingleton<ObjectPoolManager>
    {
        [Header("Predefined Pools")]
        [SerializeField] private List<PoolConfig> _predefinedPools = new List<PoolConfig>();

        private readonly Dictionary<GameObject, Queue<GameObject>> _prefabPools = new Dictionary<GameObject, Queue<GameObject>>();
        private readonly Dictionary<string, Queue<GameObject>> _tagPools = new Dictionary<string, Queue<GameObject>>();
        private readonly Dictionary<GameObject, GameObject> _instanceToPrefabMap = new Dictionary<GameObject, GameObject>();
        private readonly Dictionary<GameObject, string> _instanceToTagMap = new Dictionary<GameObject, string>();
        private readonly Dictionary<GameObject, IPoolable[]> _cachedPoolables = new Dictionary<GameObject, IPoolable[]>();

        protected override void Awake()
        {
            base.Awake();
            if (IsDuplicateInstance) return;
            InitializePredefinedPools();
        }

        private void InitializePredefinedPools()
        {
            foreach (var config in _predefinedPools)
            {
                if (config.Prefab != null && !string.IsNullOrEmpty(config.Tag))
                {
                    CreatePool(config.Tag, config.Prefab, config.InitialSize);
                }
            }
        }

        /// <summary>
        /// Tạo một pool mới theo Tag.
        /// </summary>
        public void CreatePool(string tag, GameObject prefab, int initialSize)
        {
            if (string.IsNullOrEmpty(tag) || prefab == null) return;

            if (!_tagPools.ContainsKey(tag))
            {
                _tagPools[tag] = new Queue<GameObject>();
            }

            var poolQueue = _tagPools[tag];
            var parent = GetOrCreateParentTransform(tag);

            for (int i = 0; i < initialSize; i++)
            {
                var instance = Instantiate(prefab, parent);
                instance.SetActive(false);
                _instanceToTagMap[instance] = tag;
                _cachedPoolables[instance] = instance.GetComponentsInChildren<IPoolable>(true);
                poolQueue.Enqueue(instance);
            }
        }

        /// <summary>
        /// Lấy đối tượng từ pool theo Prefab. Tự động khởi tạo thêm nếu pool rỗng.
        /// </summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;

            if (!_prefabPools.TryGetValue(prefab, out var poolQueue))
            {
                poolQueue = new Queue<GameObject>();
                _prefabPools[prefab] = poolQueue;
            }

            GameObject instance;
            if (poolQueue.Count > 0)
            {
                instance = poolQueue.Dequeue();
            }
            else
            {
                var rootParent = GetOrCreateParentTransform(prefab.name);
                instance = Instantiate(prefab, rootParent);
                _instanceToPrefabMap[instance] = prefab;
                _cachedPoolables[instance] = instance.GetComponentsInChildren<IPoolable>(true);
            }

            if (instance != null)
            {
                instance.transform.SetPositionAndRotation(position, rotation);
                if (parent != null)
                {
                    instance.transform.SetParent(parent);
                }

                instance.SetActive(true);

                if (_cachedPoolables.TryGetValue(instance, out var poolables))
                {
                    for (int i = 0; i < poolables.Length; i++)
                    {
                        poolables[i].OnSpawnFromPool();
                    }
                }
            }

            return instance;
        }

        /// <summary>
        /// Lấy đối tượng từ pool theo Tag đã định nghĩa trước.
        /// </summary>
        public GameObject Spawn(string tag, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (string.IsNullOrEmpty(tag) || !_tagPools.TryGetValue(tag, out var poolQueue) || poolQueue.Count == 0)
            {
                Debug.LogWarning($"[ObjectPoolManager] Không tìm thấy pool khả dụng với tag: {tag}");
                return null;
            }

            var instance = poolQueue.Dequeue();
            if (instance != null)
            {
                instance.transform.SetPositionAndRotation(position, rotation);
                if (parent != null)
                {
                    instance.transform.SetParent(parent);
                }

                instance.SetActive(true);

                if (_cachedPoolables.TryGetValue(instance, out var poolables))
                {
                    for (int i = 0; i < poolables.Length; i++)
                    {
                        poolables[i].OnSpawnFromPool();
                    }
                }
            }

            return instance;
        }

        /// <summary>
        /// Hoàn trả đối tượng về pool tương ứng.
        /// </summary>
        public void ReturnToPool(GameObject instance)
        {
            if (instance == null) return;

            if (_cachedPoolables.TryGetValue(instance, out var poolables))
            {
                for (int i = 0; i < poolables.Length; i++)
                {
                    poolables[i].OnReturnToPool();
                }
            }

            instance.SetActive(false);

            if (_instanceToPrefabMap.TryGetValue(instance, out var prefab))
            {
                instance.transform.SetParent(GetOrCreateParentTransform(prefab.name));
                _prefabPools[prefab].Enqueue(instance);
            }
            else if (_instanceToTagMap.TryGetValue(instance, out var tag))
            {
                instance.transform.SetParent(GetOrCreateParentTransform(tag));
                _tagPools[tag].Enqueue(instance);
            }
            else
            {
                Debug.LogWarning($"[ObjectPoolManager] Đối tượng '{instance.name}' không thuộc quyền quản lý của Pool. Đang Destroy mặc định.");
                Destroy(instance);
            }
        }

        private Transform GetOrCreateParentTransform(string poolName)
        {
            var child = transform.Find($"Pool_{poolName}");
            if (child == null)
            {
                var newParent = new GameObject($"Pool_{poolName}");
                newParent.transform.SetParent(transform);
                return newParent.transform;
            }
            return child;
        }
    }
}
