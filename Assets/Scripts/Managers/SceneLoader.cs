using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Platformer2D.Core.Architecture;
using Platformer2D.Core.Singletons;

namespace Platformer2D.Managers
{
    /// <summary>
    /// Quản lý chuyển đổi và tải Scene bất đồng bộ (Async Scene Loading) có hiệu ứng Fade mượt cho dự án.
    /// </summary>
    public class SceneLoader : PersistentSingleton<SceneLoader>
    {
        private const float FadeOutDuration = 0.18f;
        private const float FadeInDuration = 0.20f;

        public bool IsLoading { get; private set; }

        public event Action<float> OnLoadingProgress;
        public event Action OnLoadingStarted;
        public event Action OnLoadingCompleted;

        private CanvasGroup _fadeCanvasGroup;

        protected override void Awake()
        {
            base.Awake();
            if (IsDuplicateInstance) return;

            EnsureFadeOverlay();
        }

        private void EnsureFadeOverlay()
        {
            if (_fadeCanvasGroup != null) return;

            GameObject canvasObj = new GameObject("FadeOverlayCanvas");
            canvasObj.transform.SetParent(transform, false);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            _fadeCanvasGroup = canvasObj.AddComponent<CanvasGroup>();
            _fadeCanvasGroup.alpha = 0f;
            _fadeCanvasGroup.blocksRaycasts = false;
            _fadeCanvasGroup.interactable = false;

            GameObject panelObj = new GameObject("FadeImage");
            panelObj.transform.SetParent(canvasObj.transform, false);
            Image fadeImage = panelObj.AddComponent<Image>();
            fadeImage.color = new Color(0.039f, 0.051f, 0.086f, 1f); // #0A0D16
            fadeImage.raycastTarget = true;

            RectTransform rt = panelObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Tải Scene theo tên một cách bất đồng bộ kèm hiệu ứng chuyển cảnh.
        /// </summary>
        /// <param name="sceneName">Tên scene trong Build Settings</param>
        public void LoadSceneAsync(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning("[SceneLoader] Đang trong quá trình tải scene, bỏ qua yêu cầu mới.");
                return;
            }

            EnsureFadeOverlay();
            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        /// <summary>
        /// Tải lại Scene hiện tại (giữ điểm hiện tại).
        /// </summary>
        public void ReloadCurrentScene()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            LoadSceneAsync(currentSceneName);
        }

        /// <summary>
        /// Đặt lại điểm về 0 rồi tải lại Scene hiện tại (dùng cho nút Restart).
        /// </summary>
        public void ReloadCurrentSceneWithReset()
        {
            if (GameManager.HasInstance)
            {
                GameManager.Instance.ResetScore();
            }
            ReloadCurrentScene();
        }

        /// <summary>
        /// Đặt lại điểm về 0 và quay về Main Menu.
        /// </summary>
        public void LoadMainMenu()
        {
            if (GameManager.HasInstance)
            {
                GameManager.Instance.ResetScore();
            }
            LoadSceneAsync(SceneNames.MainMenu);
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            IsLoading = true;
            Time.timeScale = 1f;
            OnLoadingStarted?.Invoke();

            if (_fadeCanvasGroup != null)
            {
                _fadeCanvasGroup.blocksRaycasts = true;
                yield return Fade(1f, FadeOutDuration);
            }

            AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);
            if (asyncOp == null)
            {
                Debug.LogError($"[SceneLoader] Không thể tải scene '{sceneName}'. Hãy kiểm tra Build Settings!");
                if (_fadeCanvasGroup != null)
                {
                    _fadeCanvasGroup.alpha = 0f;
                    _fadeCanvasGroup.blocksRaycasts = false;
                }
                IsLoading = false;
                yield break;
            }

            // Đảm bảo không nhảy đột ngột trước khi load hoàn tất
            asyncOp.allowSceneActivation = false;

            while (!asyncOp.isDone)
            {
                // asyncOp.progress chạy từ 0 -> 0.9 khi đang tải tài nguyên
                float progress = Mathf.Clamp01(asyncOp.progress / 0.9f);
                OnLoadingProgress?.Invoke(progress);

                if (asyncOp.progress >= 0.9f && !asyncOp.allowSceneActivation)
                {
                    asyncOp.allowSceneActivation = true;
                }

                yield return null;
            }

            OnLoadingProgress?.Invoke(1f);

            if (_fadeCanvasGroup != null)
            {
                yield return Fade(0f, FadeInDuration);
                _fadeCanvasGroup.blocksRaycasts = false;
            }

            OnLoadingCompleted?.Invoke();
            IsLoading = false;
        }

        private IEnumerator Fade(float targetAlpha, float duration)
        {
            if (_fadeCanvasGroup == null) yield break;

            float startAlpha = _fadeCanvasGroup.alpha;
            if (duration <= 0f)
            {
                _fadeCanvasGroup.alpha = targetAlpha;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            _fadeCanvasGroup.alpha = targetAlpha;
        }
    }
}
