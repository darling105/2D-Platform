using System.Collections;
using System.Linq;
using UnityEngine;
using Platformer2D.Core.Architecture;
using Platformer2D.Managers;
using Platformer2D.Player.Controllers;
using Platformer2D.VFX;

namespace Platformer2D.Environment.Interactive
{
    /// <summary>
    /// Rương kho báu mở bằng chìa khóa (ví dụ Rương Bạc):
    /// - Yêu cầu nhặt đúng loại chìa khóa mới mở được.
    /// - Khi mở: phát hiệu ứng sáng, chạy hoạt ảnh bật nắp rương 8 frame, thưởng Coins và rơi vật phẩm.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TreasureChest : MonoBehaviour
    {
        private const string SilverChestSheetPath = "Assets/GameAssets/Field-Tileset/Objects_Animated/Silver_Chest_Open.png";
        private const string GoldenChestSheetPath = "Assets/GameAssets/Field-Tileset/Objects_Animated/Golden_Chest_Open.png";
        private const string PickupVfxSheetPath = "Assets/GameAssets/Field-Tileset/Objects_Animated/Pickup.png";

        [Header("Chest Settings")]
        [SerializeField] private bool _requireKey = true;
        [SerializeField] private KeyType _requiredKeyType = KeyType.Silver;
        [SerializeField] private int _bonusCoins = 40;
        [SerializeField] private GameObject _dropPrefab;
        [SerializeField] private GameObject[] _dropPrefabs;
        [SerializeField] private string _openSoundId = "ChestOpen";

        [Header("Animation & Loot VFX")]
        [SerializeField] private Sprite[] _openFrames;
        [SerializeField] private Sprite[] _pickupVfxFrames;
        [SerializeField] private float _openFrameRate = 14f;
        [SerializeField] private float _lootFlightDuration = 0.52f;
        [SerializeField] private float _lootSpawnInterval = 0.16f;

        private SpriteRenderer _spriteRenderer;
        private float _groundBottomY;
        private float _lastLockedToastTime = -10f;
        private bool _isOpened;

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            col.isTrigger = true;

            _spriteRenderer = GetComponent<SpriteRenderer>();
            EnsureFrames();

            if (_spriteRenderer != null && _openFrames != null && _openFrames.Length > 0 && _openFrames[0] != null)
            {
                _spriteRenderer.sprite = _openFrames[0];
            }

            float initialMinY = _spriteRenderer != null && _spriteRenderer.sprite != null
                ? _spriteRenderer.sprite.bounds.min.y
                : -0.28125f;
            _groundBottomY = transform.position.y + initialMinY * transform.lossyScale.y;

            Color glowColor = _requiredKeyType == KeyType.Golden
                ? new Color(1f, 0.84f, 0.22f, 0.30f)
                : new Color(0.68f, 0.88f, 1f, 0.28f);
            AmbientGlowPulse.Attach(transform, glowColor, 1.45f, -1, new Vector3(0f, 0.05f, 0f));
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isOpened) return;

            bool isPlayer = other.CompareTag("Player")
                || other.name.Contains("Pink_Monster")
                || other.name.Contains("Player");

            if (!isPlayer) return;

            PlayerController player = other.GetComponentInParent<PlayerController>()
                ?? other.GetComponent<PlayerController>()
                ?? FindAnyObjectByType<PlayerController>();

            if (player != null && player.IsDead) return;

            if (_requireKey && (!GameManager.HasInstance || !GameManager.Instance.TryUseKey(_requiredKeyType)))
            {
                if (Time.time - _lastLockedToastTime >= 1.2f)
                {
                    _lastLockedToastTime = Time.time;
                    string keyLabel = _requiredKeyType == KeyType.Golden ? "CHÌA KHÓA VÀNG" : "CHÌA KHÓA BẠC";
                    EventBus.Publish(new PlaySoundEvent("Locked", SoundType.SFX, transform.position));
                    EventBus.Publish(new HudToastMessageEvent($"CẦN {keyLabel} ĐỂ MỞ RƯƠNG!", new Color(1f, 0.55f, 0.35f, 1f)));
                }
                return;
            }

            _isOpened = true;
            StartCoroutine(OpenChestRoutine(player));
        }

        private IEnumerator OpenChestRoutine(PlayerController player)
        {
            if (_bonusCoins > 0)
            {
                if (GameManager.HasInstance)
                {
                    GameManager.Instance.AddScore(_bonusCoins);
                }
                else
                {
                    EventBus.Publish(new ScoreChangedEvent(_bonusCoins, _bonusCoins));
                }
            }

            SpriteAnimationEffect.SpawnOneShot(transform.position + new Vector3(0f, 0.25f, 0f), _pickupVfxFrames, 16f, 5, 1.25f);
            EventBus.Publish(new PlaySoundEvent(_openSoundId, SoundType.SFX, transform.position));
            EventBus.Publish(new HudToastMessageEvent($"ĐÃ MỞ RƯƠNG! +{_bonusCoins} COINS", new Color(0.8f, 0.92f, 1f, 1f)));

            if (_spriteRenderer != null && _openFrames != null && _openFrames.Length > 0)
            {
                float delay = 1f / Mathf.Max(1f, _openFrameRate);
                for (int i = 0; i < _openFrames.Length; i++)
                {
                    Sprite frame = _openFrames[i];
                    if (frame != null)
                    {
                        _spriteRenderer.sprite = frame;
                        VictoryGoal.PinSpriteBottomToGround(transform, frame, _groundBottomY);
                    }
                    yield return new WaitForSeconds(delay);
                }
            }

            // Xử lý các vật phẩm phóng ra từ rương
            GameObject[] dropList = GetDropsToSpawn();
            if (dropList != null && dropList.Length > 0)
            {
                for (int i = 0; i < dropList.Length; i++)
                {
                    GameObject dropPrefab = dropList[i];
                    if (dropPrefab == null) continue;

                    StartCoroutine(ProcessLootDropRoutine(dropPrefab, player, i, dropList.Length));
                    if (dropList.Length > 1)
                    {
                        yield return new WaitForSeconds(_lootSpawnInterval);
                    }
                }
            }
        }

        private GameObject[] GetDropsToSpawn()
        {
            if (_dropPrefabs != null && _dropPrefabs.Length > 0)
            {
                return _dropPrefabs.Where(p => p != null).ToArray();
            }

            if (_dropPrefab != null)
            {
                return new[] { _dropPrefab };
            }

            return System.Array.Empty<GameObject>();
        }

        /// <summary>
        /// Xử lý phóng 1 vật phẩm:
        /// - Nếu người chơi chưa đầy (đá hoặc tim): vọt lên rồi ném lượn thẳng vào người chơi kèm hoạt ảnh.
        /// - Nếu người chơi đã đầy túi đá hoặc đầy tim: nảy vọt lên khỏi rương rồi rơi vòng cung xuống đất cạnh rương.
        /// </summary>
        private IEnumerator ProcessLootDropRoutine(GameObject dropPrefab, PlayerController player, int itemIndex, int totalItems)
        {
            Vector3 spawnPos = transform.position + new Vector3(0f, 0.25f, 0f);
            GameObject lootObj = Instantiate(dropPrefab, spawnPos, Quaternion.identity);

            Collider2D lootCol = lootObj.GetComponent<Collider2D>();
            if (lootCol != null) lootCol.enabled = false;

            // Kiểm tra trạng thái đầy của người chơi
            bool isFull = false;
            string fullReason = string.Empty;

            var combat = player != null ? (player.GetComponent<Player.Components.PlayerCombat>() ?? player.GetComponentInParent<Player.Components.PlayerCombat>()) : null;
            var health = player != null ? (player.GetComponent<Combat.Health>() ?? player.GetComponentInParent<Combat.Health>()) : null;

            var rockComp = lootObj.GetComponent<Items.RockPickup>();
            var healthComp = lootObj.GetComponent<Items.HealthPickup>();
            var coinComp = lootObj.GetComponent<Items.CoinItem>();

            if (rockComp != null)
            {
                if (combat != null && combat.CurrentRocks >= combat.MaxRocks)
                {
                    isFull = true;
                    fullReason = "TÚI ĐÁ ĐÃ ĐẦY!";
                }
            }
            else if (healthComp != null)
            {
                if (health != null && health.IsAtMaxHealth)
                {
                    isFull = true;
                    fullReason = "MÁU ĐÃ ĐẦY!";
                }
            }

            if (!isFull && player != null)
            {
                yield return StartCoroutine(FlyIntoPlayerRoutine(lootObj, lootCol, player, combat, health, rockComp, healthComp, coinComp, itemIndex, totalItems));
            }
            else
            {
                yield return StartCoroutine(DropToGroundRoutine(lootObj, lootCol, rockComp, healthComp, itemIndex, totalItems, fullReason));
            }
        }

        /// <summary>
        /// Hoạt ảnh vật phẩm phóng vọt lên khỏi rương rồi uốn lượn lao thẳng vào ngực người chơi.
        /// </summary>
        private IEnumerator FlyIntoPlayerRoutine(
            GameObject lootObj,
            Collider2D lootCol,
            PlayerController player,
            Player.Components.PlayerCombat combat,
            Combat.Health health,
            Items.RockPickup rockComp,
            Items.HealthPickup healthComp,
            Items.CoinItem coinComp,
            int itemIndex,
            int totalItems)
        {
            Vector3 startPos = lootObj.transform.position;
            Vector3 originalScale = lootObj.transform.localScale;

            float horizontalSpread = (itemIndex - (totalItems - 1) * 0.5f) * 0.35f;
            float duration = Mathf.Max(0.35f, _lootFlightDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                Vector3 targetPos = player != null
                    ? player.transform.position + new Vector3(0f, 0.42f, 0f)
                    : startPos;

                float midY = Mathf.Max(startPos.y, targetPos.y) + 1.35f + (itemIndex % 2) * 0.25f;
                Vector3 midPoint = new Vector3(Mathf.Lerp(startPos.x, targetPos.x, 0.32f) + horizontalSpread, midY, 0f);

                // Quadratic Bezier với easing gia tốc lao vào người chơi
                float easeT = Mathf.SmoothStep(0f, 1f, t);
                Vector3 currentPos = (1f - easeT) * (1f - easeT) * startPos
                                   + 2f * (1f - easeT) * easeT * midPoint
                                   + easeT * easeT * targetPos;

                lootObj.transform.position = currentPos;

                // Hoạt ảnh scale phập phồng: vọt lên to ra -> lao nhanh vào người
                float scaleMul = t < 0.35f
                    ? Mathf.Lerp(0.5f, 1.35f, t / 0.35f)
                    : Mathf.Lerp(1.35f, 1.0f, (t - 0.35f) / 0.65f);
                lootObj.transform.localScale = originalScale * scaleMul;

                // Xoay nhẹ theo quán tính ném
                float rotAngle = Mathf.Sin(t * Mathf.PI * 3f) * 16f;
                lootObj.transform.rotation = Quaternion.Euler(0f, 0f, rotAngle);

                yield return null;
            }

            Vector3 finalChestPos = player != null ? player.transform.position + new Vector3(0f, 0.42f, 0f) : lootObj.transform.position;

            // Bung hiệu ứng sáng chói lóa khi vật phẩm chạm vào người chơi
            SpriteAnimationEffect.SpawnOneShot(finalChestPos, _pickupVfxFrames, 18f, 5, 1.35f);

            // Thu nhận trực tiếp vào chỉ số của người chơi
            if (rockComp != null && combat != null)
            {
                rockComp.CollectDirectly(combat);
            }
            else if (healthComp != null && health != null)
            {
                healthComp.CollectDirectly(health);
            }
            else if (coinComp != null)
            {
                coinComp.CollectDirectly();
            }
            else
            {
                EventBus.Publish(new PlaySoundEvent("Coin", SoundType.SFX, finalChestPos));
                Destroy(lootObj);
            }
        }

        /// <summary>
        /// Hoạt ảnh vật phẩm phóng vọt lên khỏi rương nhưng bị văng rơi vòng cung xuống đất cạnh rương do người chơi đã đầy.
        /// </summary>
        private IEnumerator DropToGroundRoutine(
            GameObject lootObj,
            Collider2D lootCol,
            Items.RockPickup rockComp,
            Items.HealthPickup healthComp,
            int itemIndex,
            int totalItems,
            string fullReason)
        {
            Vector3 startPos = lootObj.transform.position;
            Vector3 originalScale = lootObj.transform.localScale;

            float sideOffset = (itemIndex % 2 == 0 ? 1f : -1f) * (0.85f + (itemIndex / 2) * 0.42f);
            Vector3 targetGroundPos = new Vector3(transform.position.x + sideOffset, _groundBottomY + 0.22f, 0f);
            Vector3 apexPos = new Vector3(Mathf.Lerp(startPos.x, targetGroundPos.x, 0.45f), Mathf.Max(startPos.y, targetGroundPos.y) + 1.25f, 0f);

            float dropDuration = 0.48f;
            float elapsed = 0f;

            while (elapsed < dropDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dropDuration);

                Vector3 currentPos = (1f - t) * (1f - t) * startPos
                                   + 2f * (1f - t) * t * apexPos
                                   + t * t * targetGroundPos;

                lootObj.transform.position = currentPos;
                lootObj.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 2f) * 22f);
                yield return null;
            }

            // Nảy nhẹ (Bounce) khi chạm đất
            float bounceDuration = 0.16f;
            float bElapsed = 0f;
            while (bElapsed < bounceDuration)
            {
                bElapsed += Time.deltaTime;
                float bt = Mathf.Clamp01(bElapsed / bounceDuration);
                float bounceY = Mathf.Sin(bt * Mathf.PI) * 0.16f;
                lootObj.transform.position = targetGroundPos + new Vector3(0f, bounceY, 0f);
                yield return null;
            }

            lootObj.transform.position = targetGroundPos;
            lootObj.transform.rotation = Quaternion.identity;
            lootObj.transform.localScale = originalScale;

            if (rockComp != null) rockComp.ResetStartPosition(targetGroundPos);
            if (healthComp != null) healthComp.ResetBasePosition(targetGroundPos);

            if (lootCol != null) lootCol.enabled = true;

            EventBus.Publish(new PlaySoundEvent("Drop", SoundType.SFX, targetGroundPos));
            if (!string.IsNullOrEmpty(fullReason))
            {
                EventBus.Publish(new HudToastMessageEvent($"{fullReason} VẬT PHẨM RƠI RA ĐẤT", new Color(1f, 0.78f, 0.32f, 1f)));
            }
        }

        private void EnsureFrames()
        {
#if UNITY_EDITOR
            if (_openFrames == null || _openFrames.Length <= 1)
            {
                string sheetPath = _requiredKeyType == KeyType.Golden ? GoldenChestSheetPath : SilverChestSheetPath;
                Sprite[] loaded = LoadSortedSprites(sheetPath);
                if (loaded.Length > 0) _openFrames = loaded;
            }

            if (_pickupVfxFrames == null || _pickupVfxFrames.Length == 0)
            {
                Sprite[] loadedVfx = LoadSortedSprites(PickupVfxSheetPath);
                if (loadedVfx.Length > 0) _pickupVfxFrames = loadedVfx;
            }
#endif
        }

#if UNITY_EDITOR
        private static Sprite[] LoadSortedSprites(string assetPath)
        {
            Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets == null) return System.Array.Empty<Sprite>();

            return assets.OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();
        }
#endif
    }
}
