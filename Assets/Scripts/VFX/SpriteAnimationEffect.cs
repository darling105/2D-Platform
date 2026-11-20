using System.Collections;
using UnityEngine;

namespace Platformer2D.VFX
{
    /// <summary>
    /// Hiệu ứng VFX dạng chuỗi Sprite Frame-by-Frame (One-Shot Sprite Animation):
    /// - Không cần tạo Animator Controller hay State Machine phức tạp cho các hiệu ứng ngắn.
    /// - Phát tuần tự các frame ảnh (Double Jump Dust, Khói bước chân, Tia lửa va chạm) rồi tự hủy.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteAnimationEffect : MonoBehaviour
    {
        [Tooltip("Danh sách các frame ảnh của hiệu ứng")]
        [SerializeField] private Sprite[] _frames;
        [Tooltip("Tốc độ phát (Frames Per Second)")]
        [SerializeField] private float _frameRate = 14f;
        [Tooltip("Tự động hủy GameObject sau khi phát xong chuỗi frame")]
        [SerializeField] private bool _destroyOnFinish = true;

        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            if (_frames != null && _frames.Length > 0)
            {
                StartCoroutine(PlayRoutine());
            }
            else if (_destroyOnFinish)
            {
                Destroy(gameObject);
            }
        }

        public void Initialize(Sprite[] frames, float fps = 14f)
        {
            _frames = frames;
            _frameRate = fps;

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (_frames != null && _frames.Length > 0)
            {
                if (_spriteRenderer != null) _spriteRenderer.sprite = _frames[0];
                StopAllCoroutines();
                StartCoroutine(PlayRoutine());
            }
        }

        /// <summary>
        /// Tạo nhanh hiệu ứng Sprite Frame-by-Frame tại vị trí bất kỳ và tự hủy sau khi phát xong.
        /// </summary>
        public static void SpawnOneShot(
            Vector3 position,
            Sprite[] frames,
            float fps = 16f,
            int sortingOrder = 5,
            float scale = 1f)
        {
            if (frames == null || frames.Length == 0) return;

            GameObject vfxObj = new GameObject("SpriteVfx_OneShot");
            vfxObj.transform.position = position;
            vfxObj.transform.localScale = new Vector3(scale, scale, 1f);

            SpriteRenderer sr = vfxObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder;

            SpriteAnimationEffect effect = vfxObj.AddComponent<SpriteAnimationEffect>();
            effect.Initialize(frames, fps);
        }

        private IEnumerator PlayRoutine()
        {
            float delay = 1f / Mathf.Max(1f, _frameRate);

            for (int i = 0; i < _frames.Length; i++)
            {
                if (_spriteRenderer != null)
                {
                    _spriteRenderer.sprite = _frames[i];
                }
                yield return new WaitForSeconds(delay);
            }

            if (_destroyOnFinish)
            {
                Destroy(gameObject);
            }
        }
    }
}
