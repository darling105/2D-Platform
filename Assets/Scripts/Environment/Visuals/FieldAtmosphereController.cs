using UnityEngine;
using UnityEngine.SceneManagement;
using Platformer2D.Core.Architecture;
using Platformer2D.VFX;

namespace Platformer2D.Environment.Visuals
{
    /// <summary>
    /// Bộ điều khiển không khí & ánh sáng nghệ thuật cho khu vực Field:
    /// - Đồng bộ màu nền Camera chuẩn xanh trời Field (#2490CD).
    /// - Tạo các dải tia nắng xiên (Sunbeams) ấm áp lung linh phía sau tán cây.
    /// - Tạo đàn hạt nắng / phấn hoa vàng bay lơ lửng theo làn gió quanh Camera.
    /// </summary>
    public class FieldAtmosphereController : MonoBehaviour
    {
        private static readonly Color FieldSkyColor = new Color(0.141f, 0.565f, 0.804f, 1f);

        [Header("Atmospheric Settings")]
        [SerializeField] private int _moteCount = 24;
        [SerializeField] private float _moteAreaHalfWidth = 11.5f;
        [SerializeField] private float _moteAreaHalfHeight = 6.5f;
        [SerializeField] private int _sunbeamCount = 5;

        private struct SunMote
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public float BaseAlpha;
            public float BaseScale;
            public float SpeedX;
            public float SpeedY;
            public float BobFreq;
            public float Phase;
        }

        private struct Sunbeam
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public float BaseAlpha;
            public float LocalOffsetX;
            public float Phase;
        }

        private static Sprite _cachedRaySprite;
        private Transform _cameraTransform;
        private SunMote[] _motes;
        private Sunbeam[] _sunbeams;
        private bool _built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureAtmosphereInFieldScenes()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName != SceneNames.MechanicsTest && sceneName != SceneNames.Level01) return;

            if (FindAnyObjectByType<FieldAtmosphereController>() == null)
            {
                GameObject obj = new GameObject("FieldAtmosphere");
                obj.AddComponent<FieldAtmosphereController>();
            }
        }

        private void Start()
        {
            ApplyCameraSkyColor();
            BuildAtmosphere();
        }

        private void ApplyCameraSkyColor()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            _cameraTransform = cam.transform;
            cam.backgroundColor = FieldSkyColor;
        }

        private void BuildAtmosphere()
        {
            if (_built) return;
            _built = true;

            Material glowMat = AmbientGlowPulse.GetSoftGlowMaterial();
            Sprite softCircle = AmbientGlowPulse.GetSoftRadialSprite();
            Sprite raySprite = GetSoftRaySprite();
            Vector3 center = _cameraTransform != null ? _cameraTransform.position : Vector3.zero;

            // 1. Dải tia nắng xiên ấm áp lớp hậu cảnh
            int beamCount = Mathf.Max(0, _sunbeamCount);
            _sunbeams = new Sunbeam[beamCount];
            for (int i = 0; i < beamCount; i++)
            {
                GameObject beamObj = new GameObject($"Sunbeam_{i:00}");
                beamObj.transform.SetParent(transform, false);

                float t = beamCount > 1 ? (float)i / (beamCount - 1) - 0.5f : 0f;
                float offsetX = t * 18f + (i % 2 == 0 ? 0.6f : -0.6f);
                beamObj.transform.position = new Vector3(center.x + offsetX, center.y + 1.8f, 0f);
                beamObj.transform.rotation = Quaternion.Euler(0f, 0f, -24f);

                float width = 2.6f + (i % 3) * 0.7f;
                beamObj.transform.localScale = new Vector3(width, 16f, 1f);

                SpriteRenderer sr = beamObj.AddComponent<SpriteRenderer>();
                sr.sprite = raySprite;
                sr.sortingOrder = -12;
                float baseAlpha = 0.085f + (i % 2) * 0.03f;
                sr.color = new Color(1f, 0.96f, 0.78f, baseAlpha);
                if (glowMat != null) sr.sharedMaterial = glowMat;

                _sunbeams[i] = new Sunbeam
                {
                    Transform = beamObj.transform,
                    Renderer = sr,
                    BaseAlpha = baseAlpha,
                    LocalOffsetX = offsetX,
                    Phase = i * 1.15f
                };
            }

            // 2. Đàn hạt nắng vàng / phấn hoa bay nhẹ theo làn gió thảo nguyên
            int count = Mathf.Max(0, _moteCount);
            _motes = new SunMote[count];
            for (int i = 0; i < count; i++)
            {
                GameObject moteObj = new GameObject($"SunMote_{i:00}");
                moteObj.transform.SetParent(transform, false);

                float rx = Random.Range(-_moteAreaHalfWidth, _moteAreaHalfWidth);
                float ry = Random.Range(-_moteAreaHalfHeight, _moteAreaHalfHeight);
                moteObj.transform.position = new Vector3(center.x + rx, center.y + ry, 0f);

                float scale = Random.Range(0.08f, 0.22f);
                moteObj.transform.localScale = new Vector3(scale, scale, 1f);

                SpriteRenderer sr = moteObj.AddComponent<SpriteRenderer>();
                sr.sprite = softCircle;
                sr.sortingOrder = (i % 3 == 0) ? 2 : -4;
                float baseAlpha = Random.Range(0.35f, 0.72f);
                Color moteColor = (i % 4 == 0)
                    ? new Color(0.85f, 1f, 0.70f, baseAlpha)
                    : new Color(1f, 0.94f, 0.58f, baseAlpha);
                sr.color = moteColor;
                if (glowMat != null) sr.sharedMaterial = glowMat;

                _motes[i] = new SunMote
                {
                    Transform = moteObj.transform,
                    Renderer = sr,
                    BaseAlpha = baseAlpha,
                    BaseScale = scale,
                    SpeedX = Random.Range(0.28f, 0.75f),
                    SpeedY = Random.Range(-0.10f, 0.22f),
                    BobFreq = Random.Range(1.2f, 2.4f),
                    Phase = Random.Range(0f, Mathf.PI * 2f)
                };
            }
        }

        private void LateUpdate()
        {
            if (_cameraTransform == null)
            {
                Camera cam = Camera.main;
                if (cam != null) _cameraTransform = cam.transform;
                if (_cameraTransform == null) return;
            }

            float dt = Time.deltaTime;
            float time = Time.time;
            Vector3 camPos = _cameraTransform.position;

            if (_sunbeams != null)
            {
                for (int i = 0; i < _sunbeams.Length; i++)
                {
                    Sunbeam b = _sunbeams[i];
                    if (b.Transform == null) continue;

                    float swayX = Mathf.Sin(time * 0.35f + b.Phase) * 0.45f;
                    b.Transform.position = new Vector3(camPos.x * 0.96f + b.LocalOffsetX + swayX, camPos.y * 0.92f + 1.8f, 0f);

                    Color c = b.Renderer.color;
                    c.a = b.BaseAlpha * (0.78f + 0.22f * Mathf.Sin(time * 0.75f + b.Phase));
                    b.Renderer.color = c;
                }
            }

            if (_motes != null)
            {
                float fullW = _moteAreaHalfWidth * 2f;
                float fullH = _moteAreaHalfHeight * 2f;

                for (int i = 0; i < _motes.Length; i++)
                {
                    SunMote m = _motes[i];
                    if (m.Transform == null) continue;

                    Vector3 pos = m.Transform.position;
                    pos.x += m.SpeedX * dt;
                    pos.y += (m.SpeedY + Mathf.Sin(time * m.BobFreq + m.Phase) * 0.18f) * dt;

                    float dx = pos.x - camPos.x;
                    float dy = pos.y - camPos.y;
                    if (dx > _moteAreaHalfWidth) pos.x -= fullW;
                    else if (dx < -_moteAreaHalfWidth) pos.x += fullW;

                    if (dy > _moteAreaHalfHeight) pos.y -= fullH;
                    else if (dy < -_moteAreaHalfHeight) pos.y += fullH;

                    m.Transform.position = pos;

                    float twinkle = 0.55f + 0.45f * Mathf.Sin(time * 2.1f + m.Phase);
                    Color c = m.Renderer.color;
                    c.a = m.BaseAlpha * twinkle;
                    m.Renderer.color = c;
                    m.Transform.localScale = Vector3.one * (m.BaseScale * (0.85f + 0.25f * twinkle));
                }
            }
        }

        private static Sprite GetSoftRaySprite()
        {
            if (_cachedRaySprite != null) return _cachedRaySprite;

            const int width = 32;
            const int height = 64;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float vFade = Mathf.Sin((float)y / (height - 1) * Mathf.PI);
                vFade = Mathf.Pow(vFade, 0.75f);

                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / (width - 1) * 2f - 1f;
                    float hFade = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(u)), 1.7f);
                    pixels[y * width + x] = new Color(1f, 1f, 1f, hFade * vFade);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);

            _cachedRaySprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                width);

            return _cachedRaySprite;
        }
    }
}
