using System.Collections;
using UnityEngine;

namespace Platformer2D.VFX
{
    /// <summary>
    /// Hiệu ứng bóng mờ (Ghost Trail / Afterimage) khi nhân vật Dash hoặc di chuyển tốc độ cao.
    /// Tự động sao chép Sprite hiện tại và mờ dần trong thời gian ngắn.
    /// </summary>
    public class GhostTrail : MonoBehaviour
    {
        [Tooltip("Màu của bóng mờ")]
        [SerializeField] private Color _ghostColor = new Color(1f, 0.4f, 0.7f, 0.6f);
        [Tooltip("Thời gian tồn tại của bóng mờ trước khi tan biến")]
        [SerializeField] private float _ghostFadeDuration = 0.25f;
        [Tooltip("Tần suất tạo bóng mờ (giây)")]
        [SerializeField] private float _spawnInterval = 0.04f;

        private SpriteRenderer _sourceRenderer;
        private Coroutine _trailCoroutine;

        private void Awake()
        {
            _sourceRenderer = GetComponent<SpriteRenderer>();
        }

        public void StartTrail(float totalDuration)
        {
            if (_trailCoroutine != null) StopCoroutine(_trailCoroutine);
            _trailCoroutine = StartCoroutine(EmitTrailRoutine(totalDuration));
        }

        public void StopTrail()
        {
            if (_trailCoroutine != null)
            {
                StopCoroutine(_trailCoroutine);
                _trailCoroutine = null;
            }
        }

        private IEnumerator EmitTrailRoutine(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                SpawnSingleGhost();
                yield return new WaitForSeconds(_spawnInterval);
                elapsed += _spawnInterval;
            }
            _trailCoroutine = null;
        }

        public void SpawnSingleGhost()
        {
            if (_sourceRenderer == null || _sourceRenderer.sprite == null) return;

            GameObject ghostObj = new GameObject("GhostTrail_Afterimage");
            ghostObj.transform.position = transform.position;
            ghostObj.transform.rotation = transform.rotation;
            ghostObj.transform.localScale = transform.localScale;

            SpriteRenderer ghostRenderer = ghostObj.AddComponent<SpriteRenderer>();
            ghostRenderer.sprite = _sourceRenderer.sprite;
            ghostRenderer.color = _ghostColor;
            ghostRenderer.sortingLayerID = _sourceRenderer.sortingLayerID;
            ghostRenderer.sortingOrder = _sourceRenderer.sortingOrder - 1; // Nằm ngay sau nhân vật

            StartCoroutine(FadeAndDestroyRoutine(ghostObj, ghostRenderer));
        }

        private IEnumerator FadeAndDestroyRoutine(GameObject obj, SpriteRenderer renderer)
        {
            float elapsed = 0f;
            Color startColor = renderer.color;

            while (elapsed < _ghostFadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / _ghostFadeDuration);
                renderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }

            Destroy(obj);
        }
    }
}
