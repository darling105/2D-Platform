namespace Platformer2D.Core.ObjectPool
{
    /// <summary>
    /// Giao diện cho các GameObject được quản lý bởi ObjectPoolManager.
    /// Giúp reset vận tốc, vị trí, timer khi lấy ra khỏi pool hoặc hoàn trả lại pool.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Gọi ngay sau khi đối tượng được lấy ra khỏi pool và kích hoạt (SetActive(true)).
        /// </summary>
        void OnSpawnFromPool();

        /// <summary>
        /// Gọi ngay trước khi đối tượng được trả lại pool và tắt đi (SetActive(false)).
        /// </summary>
        void OnReturnToPool();
    }
}
