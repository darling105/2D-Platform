namespace Platformer2D.Core.Interfaces
{
    /// <summary>
    /// Interface cho các thực thể có thể được hồi máu (bình máu, phép hồi máu, trạm nghỉ checkpoint).
    /// </summary>
    public interface IHealable
    {
        bool IsAtMaxHealth { get; }
        void Heal(int amount);
    }
}
