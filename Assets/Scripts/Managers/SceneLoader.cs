using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Platformer2D.Core.Singletons;

namespace Platformer2D.Managers
{
    /// <summary>
    /// Quản lý chuyển đổi và tải Scene bất đồng bộ (Async Scene Loading) cho dự án.
    /// </summary>
    public class SceneLoader : PersistentSingleton<SceneLoader>
    {
        public bool IsLoading { get; private set; }

        public event Action<float> OnLoadingProgress;
        public event Action OnLoadingStarted;
        public event Action OnLoadingCompleted;

        /// <summary>
        /// Tải Scene theo tên một cách bất đồng bộ.
        /// </summary>
        /// <param name="sceneName">Tên scene trong Build Settings</param>
        public void LoadSceneAsync(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning("[SceneLoader] Đang trong quá trình tải scene, bỏ qua yêu cầu mới.");
                return;
            }

            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        /// <summary>
        /// Tải lại Scene hiện tại (dùng cho Restart / Replay khi Game Over).
        /// </summary>
        public void ReloadCurrentScene()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            LoadSceneAsync(currentSceneName);
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            IsLoading = true;
            OnLoadingStarted?.Invoke();

            AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);
            if (asyncOp == null)
            {
                Debug.LogError($"[SceneLoader] Không thể tải scene '{sceneName}'. Hãy kiểm tra Build Settings!");
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

                if (asyncOp.progress >= 0.9f)
                {
                    // Hoàn thành tải, cho phép kích hoạt scene
                    asyncOp.allowSceneActivation = true;
                }

                yield return null;
            }

            OnLoadingProgress?.Invoke(1f);
            OnLoadingCompleted?.Invoke();
            IsLoading = false;
        }
    }
}
