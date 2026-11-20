using UnityEngine;

namespace Platformer2D.VFX
{
    /// <summary>
    /// Hào quang ánh sáng mềm dịu (Ambient Glow Halo) gắn sau vật phẩm, rương hoặc cột mốc:
    /// - Tự sinh sprite tỏa tròn mềm 1 lần và cache dùng chung toàn game (không tốn Draw Call texture).
    /// - Nhấp nhô kích thước và độ sáng theo nhịp sóng Sin êm mắt.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class AmbientGlowPulse : MonoBehaviour
    {
        private static Sprite _cachedRadialSprite;
        private static Material _cachedGlowMaterial;

        [SerializeField] private float _pulseSpeed = 2.6f;
        [SerializeField] private float _scaleAmplitude = 0.10f;
        [SerializeField] private float _alphaAmplitude = 0.30f;

        private SpriteRenderer _spriteRenderer;
        private Color _baseColor;
        private Vector3 _baseScale;
        private float _phaseOffset;

        public static Sprite GetSoftRadialSprite()
        {
            if (_cachedRadialSprite != null) return _cachedRadialSprite;

            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float half = (size - 1) * 0.5f;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float falloff = Mathf.Clamp01(1f - dist);
                    float alpha = Mathf.Pow(falloff, 2.2f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);

            _cachedRadialSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);

            return _cachedRadialSprite;
        }

        public static Material GetSoftGlowMaterial()
        {
            if (_cachedGlowMaterial != null) return _cachedGlowMaterial;

            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                _cachedGlowMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
            }

            return _cachedGlowMaterial;
        }

        public static AmbientGlowPulse Attach(
            Transform parent,
            Color glowColor,
            float worldRadius,
            int sortingOrderOffset = -1,
            Vector3 localOffset = default)
        {
            if (parent == null) return null;

            GameObject glowObj = new GameObject("AmbientGlowHalo");
            glowObj.transform.SetParent(parent, false);
            glowObj.transform.localPosition = localOffset;

            float parentScaleX = Mathf.Max(0.1f, Mathf.Abs(parent.lossyScale.x));
            float parentScaleY = Mathf.Max(0.1f, Mathf.Abs(parent.lossyScale.y));
            float diameter = Mathf.Max(0.2f, worldRadius * 2f);
            Vector3 targetScale = new Vector3(diameter / parentScaleX, diameter / parentScaleY, 1f);
            glowObj.transform.localScale = targetScale;

            SpriteRenderer sr = glowObj.AddComponent<SpriteRenderer>();
            sr.sprite = GetSoftRadialSprite();
            sr.color = glowColor;

            Material mat = GetSoftGlowMaterial();
            if (mat != null) sr.sharedMaterial = mat;

            SpriteRenderer parentSr = parent.GetComponent<SpriteRenderer>();
            if (parentSr != null)
            {
                sr.sortingLayerID = parentSr.sortingLayerID;
                sr.sortingOrder = parentSr.sortingOrder + sortingOrderOffset;
            }
            else
            {
                sr.sortingOrder = sortingOrderOffset;
            }

            AmbientGlowPulse pulse = glowObj.AddComponent<AmbientGlowPulse>();
            pulse.Initialize(glowColor, targetScale, Mathf.Abs(parent.position.x * 0.7f + parent.position.y * 0.4f));
            return pulse;
        }

        public void Initialize(Color baseColor, Vector3 baseScale, float phaseOffset)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _baseColor = baseColor;
            _baseScale = baseScale;
            _phaseOffset = phaseOffset;
        }

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null && _baseColor.a <= 0f)
            {
                _baseColor = _spriteRenderer.color;
            }
            if (_baseScale == Vector3.zero)
            {
                _baseScale = transform.localScale;
            }
        }

        private void Update()
        {
            if (_spriteRenderer == null) return;

            float wave = Mathf.Sin((Time.time * _pulseSpeed) + _phaseOffset);
            transform.localScale = _baseScale * (1f + wave * _scaleAmplitude);

            Color c = _baseColor;
            c.a = Mathf.Clamp01(_baseColor.a * (1f + wave * _alphaAmplitude));
            _spriteRenderer.color = c;
        }
    }
}
