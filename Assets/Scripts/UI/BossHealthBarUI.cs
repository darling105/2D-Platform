using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Platformer2D.Core.Architecture;

namespace Platformer2D.UI
{
    /// <summary>
    /// Thanh Máu Boss (Boss Health Bar UI):
    /// - Tự động hiển thị ở đỉnh màn hình khi người chơi chạm trán Boss.
    /// - Thanh máu dài uy lực màu đỏ tím, thanh ghost bar vàng cam rút từ từ tạo độ mượt.
    /// - Tự động mờ dần và biến mất khi Boss bị tiêu diệt.
    /// </summary>
    public class BossHealthBarUI : MonoBehaviour
    {
        [Header("UI References (Tùy chọn)")]
        [SerializeField] private GameObject _container;
        [SerializeField] private Text _bossNameText;
        [SerializeField] private Image _healthFill;
        [SerializeField] private Image _ghostFill;
        [SerializeField] private Text _healthValueText;

        private int _currentHp;
        private int _maxHp;
        private Coroutine _ghostBarCoroutine;
        private Coroutine _fadeCoroutine;

        private void Awake()
        {
            EnsureHierarchy();
            if (_container != null)
            {
                _container.SetActive(false);
            }
        }

        private void Start()
        {
            EventBus.Subscribe<BossSpawnedEvent>(OnBossSpawned);
            EventBus.Subscribe<BossHealthChangedEvent>(OnBossHealthChanged);
            EventBus.Subscribe<BossDefeatedEvent>(OnBossDefeated);
            EventBus.Subscribe<BossHealthBarHideEvent>(OnBossHealthBarHide);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<BossSpawnedEvent>(OnBossSpawned);
            EventBus.Unsubscribe<BossHealthChangedEvent>(OnBossHealthChanged);
            EventBus.Unsubscribe<BossDefeatedEvent>(OnBossDefeated);
            EventBus.Unsubscribe<BossHealthBarHideEvent>(OnBossHealthBarHide);
        }

        private void OnBossHealthBarHide(BossHealthBarHideEvent evt)
        {
            if (_ghostBarCoroutine != null) StopCoroutine(_ghostBarCoroutine);
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            if (_container != null)
            {
                _container.SetActive(false);
            }
        }

        private void OnBossSpawned(BossSpawnedEvent evt)
        {
            _currentHp = evt.CurrentHealth;
            _maxHp = evt.MaxHealth;

            EnsureHierarchy();
            if (_container != null)
            {
                _container.SetActive(true);
            }

            if (_bossNameText != null)
            {
                _bossNameText.text = evt.BossName.ToUpper();
            }

            UpdateBars(1f, 1f);
            UpdateHpText(_currentHp, _maxHp);
        }

        private void OnBossHealthChanged(BossHealthChangedEvent evt)
        {
            _currentHp = evt.CurrentHealth;
            _maxHp = evt.MaxHealth;

            float targetFill = _maxHp > 0 ? Mathf.Clamp01((float)_currentHp / _maxHp) : 0f;

            if (_healthFill != null)
            {
                _healthFill.fillAmount = targetFill;
            }

            UpdateHpText(_currentHp, _maxHp);

            if (_ghostBarCoroutine != null) StopCoroutine(_ghostBarCoroutine);
            _ghostBarCoroutine = StartCoroutine(GhostBarRoutine(targetFill));
        }

        private IEnumerator GhostBarRoutine(float targetFill)
        {
            yield return new WaitForSeconds(0.2f);
            if (_ghostFill != null)
            {
                float start = _ghostFill.fillAmount;
                float elapsed = 0f;
                const float dur = 0.35f;
                while (elapsed < dur)
                {
                    elapsed += Time.deltaTime;
                    _ghostFill.fillAmount = Mathf.Lerp(start, targetFill, elapsed / dur);
                    yield return null;
                }
                _ghostFill.fillAmount = targetFill;
            }
        }

        private void OnBossDefeated(BossDefeatedEvent evt)
        {
            UpdateBars(0f, 0f);
            UpdateHpText(0, _maxHp);

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(DefeatedFadeOutRoutine());
        }

        private IEnumerator DefeatedFadeOutRoutine()
        {
            yield return new WaitForSeconds(1.8f);
            if (_container != null)
            {
                CanvasGroup cg = _container.GetComponent<CanvasGroup>();
                if (cg == null) cg = _container.AddComponent<CanvasGroup>();

                float elapsed = 0f;
                while (elapsed < 0.6f)
                {
                    elapsed += Time.deltaTime;
                    cg.alpha = 1f - (elapsed / 0.6f);
                    yield return null;
                }
                _container.SetActive(false);
                cg.alpha = 1f;
            }
        }

        private void UpdateBars(float mainFill, float ghostFill)
        {
            if (_healthFill != null) _healthFill.fillAmount = mainFill;
            if (_ghostFill != null) _ghostFill.fillAmount = ghostFill;
        }

        private void UpdateHpText(int current, int max)
        {
            if (_healthValueText != null)
            {
                _healthValueText.text = $"{Mathf.Max(0, current)} / {max}";
            }
        }

        private void EnsureHierarchy()
        {
            if (_container != null && _healthFill != null && _bossNameText != null) return;

            Transform existing = transform.Find("BossHealthBar_Container");
            if (existing != null)
            {
                _container = existing.gameObject;
                _bossNameText = existing.Find("BossNameText")?.GetComponent<Text>();
                _healthFill = existing.Find("Bar_Bg/Fill_Main")?.GetComponent<Image>();
                _ghostFill = existing.Find("Bar_Bg/Fill_Ghost")?.GetComponent<Image>();
                _healthValueText = existing.Find("Bar_Bg/HP_Text")?.GetComponent<Text>();
                return;
            }

            // Tự động tạo cây UI Boss Health Bar động
            GameObject containerObj = new GameObject("BossHealthBar_Container");
            containerObj.transform.SetParent(transform, false);
            RectTransform contRt = containerObj.AddComponent<RectTransform>();
            contRt.anchorMin = new Vector2(0.5f, 1f);
            contRt.anchorMax = new Vector2(0.5f, 1f);
            contRt.pivot = new Vector2(0.5f, 1f);
            contRt.anchoredPosition = new Vector2(0f, -22f);
            contRt.sizeDelta = new Vector2(520f, 50f);
            _container = containerObj;

            // 1. Tên Boss
            GameObject nameObj = new GameObject("BossNameText");
            nameObj.transform.SetParent(containerObj.transform, false);
            RectTransform nameRt = nameObj.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 1f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.pivot = new Vector2(0.5f, 1f);
            nameRt.anchoredPosition = new Vector2(0f, 0f);
            nameRt.sizeDelta = new Vector2(0f, 22f);

            Text nameTxt = nameObj.AddComponent<Text>();
            nameTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameTxt.fontSize = 14;
            nameTxt.fontStyle = FontStyle.Bold;
            nameTxt.alignment = TextAnchor.MiddleCenter;
            nameTxt.color = new Color(1f, 0.85f, 0.4f, 1f);
            Outline nameOutline = nameObj.AddComponent<Outline>();
            nameOutline.effectColor = Color.black;
            nameOutline.effectDistance = new Vector2(1.5f, -1.5f);
            _bossNameText = nameTxt;

            // 2. Khung viền thanh máu
            GameObject barBgObj = new GameObject("Bar_Bg");
            barBgObj.transform.SetParent(containerObj.transform, false);
            RectTransform barRt = barBgObj.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 0f);
            barRt.anchorMax = new Vector2(1f, 0f);
            barRt.pivot = new Vector2(0.5f, 0f);
            barRt.anchoredPosition = new Vector2(0f, 0f);
            barRt.sizeDelta = new Vector2(0f, 24f);

            Image bgImg = barBgObj.AddComponent<Image>();
            bgImg.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);
            Outline barOutline = barBgObj.AddComponent<Outline>();
            barOutline.effectColor = new Color(0.85f, 0.65f, 0.25f, 1f);
            barOutline.effectDistance = new Vector2(2f, 2f);

            // 3. Ghost Fill (Vàng cam)
            GameObject ghostObj = new GameObject("Fill_Ghost");
            ghostObj.transform.SetParent(barBgObj.transform, false);
            RectTransform ghostRt = ghostObj.AddComponent<RectTransform>();
            ghostRt.anchorMin = Vector2.zero;
            ghostRt.anchorMax = Vector2.one;
            ghostRt.sizeDelta = Vector2.zero;
            Image ghostImg = ghostObj.AddComponent<Image>();
            ghostImg.type = Image.Type.Filled;
            ghostImg.fillMethod = Image.FillMethod.Horizontal;
            ghostImg.fillOrigin = 0;
            ghostImg.color = new Color(1f, 0.75f, 0.2f, 0.85f);
            _ghostFill = ghostImg;

            // 4. Main Fill (Đỏ thẫm uy lực)
            GameObject fillObj = new GameObject("Fill_Main");
            fillObj.transform.SetParent(barBgObj.transform, false);
            RectTransform fillRt = fillObj.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0;
            fillImg.color = new Color(0.92f, 0.18f, 0.22f, 1f);
            _healthFill = fillImg;

            // 5. HP Value Text
            GameObject hpObj = new GameObject("HP_Text");
            hpObj.transform.SetParent(barBgObj.transform, false);
            RectTransform hpRt = hpObj.AddComponent<RectTransform>();
            hpRt.anchorMin = Vector2.zero;
            hpRt.anchorMax = Vector2.one;
            hpRt.sizeDelta = Vector2.zero;
            Text hpTxt = hpObj.AddComponent<Text>();
            hpTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hpTxt.fontSize = 12;
            hpTxt.fontStyle = FontStyle.Bold;
            hpTxt.alignment = TextAnchor.MiddleCenter;
            hpTxt.color = Color.white;
            Outline hpOutline = hpObj.AddComponent<Outline>();
            hpOutline.effectColor = Color.black;
            _healthValueText = hpTxt;
        }
    }
}
