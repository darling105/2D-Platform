using UnityEngine;

namespace Platformer2D.Core.Interfaces
{
    public enum DamageType
    {
        Physical,
        Magical,
        Trap,
        FallDamage
    }

    /// <summary>
    /// Struct chứa dữ liệu chi tiết về một đòn tấn công gây sát thương.
    /// Giúp truyền tải đầy đủ knockback, điểm va chạm, loại sát thương mà không sinh rác bộ nhớ (GC).
    /// </summary>
    public readonly struct DamageData
    {
        public readonly int Amount;
        public readonly GameObject Source;
        public readonly Vector2 HitPoint;
        public readonly Vector2 KnockbackForce;
        public readonly DamageType Type;

        public DamageData(int amount, GameObject source = null, Vector2 hitPoint = default, Vector2 knockbackForce = default, DamageType type = DamageType.Physical)
        {
            Amount = amount;
            Source = source;
            HitPoint = hitPoint;
            KnockbackForce = knockbackForce;
            Type = type;
        }
    }

    /// <summary>
    /// Interface cho toàn bộ các thực thể có thể nhận sát thương (Player, Quái, Boss, Thùng gỗ, Vật cản,...).
    /// </summary>
    public interface IDamageable
    {
        bool IsDead { get; }
        void TakeDamage(DamageData damageData);
    }
}
