using System.Collections;
using UnityEngine;

namespace Platformer2D.Core.ObjectPool
{
    /// <summary>
    /// Component hỗ trợ tự động hoàn trả đối tượng vào ObjectPoolManager sau khoảng thời gian định sẵn (thường dùng cho VFX, đạn bay quá xa, text sát thương,...).
    /// </summary>
    public class AutoReturnToPool : MonoBehaviour, IPoolable
    {
        [Tooltip("Thời gian tồn tại (giây) trước khi tự động trả về pool.")]
        [SerializeField] private float _lifeTime = 1.5f;

        private Coroutine _returnCoroutine;

        public void SetLifeTime(float time)
        {
            _lifeTime = time;
        }

        public void OnSpawnFromPool()
        {
            if (_returnCoroutine != null)
            {
                StopCoroutine(_returnCoroutine);
            }
            _returnCoroutine = StartCoroutine(ReturnRoutine());
        }

        public void OnReturnToPool()
        {
            if (_returnCoroutine != null)
            {
                StopCoroutine(_returnCoroutine);
                _returnCoroutine = null;
            }
        }

        private IEnumerator ReturnRoutine()
        {
            yield return new WaitForSeconds(_lifeTime);
            ObjectPoolManager.Instance.ReturnToPool(gameObject);
        }
    }
}
