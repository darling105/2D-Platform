namespace Platformer2D.Core.Architecture
{
    /// <summary>
    /// Interface đánh dấu (marker interface) cho toàn bộ Event trong hệ thống Event Bus.
    /// Khuyến khích dùng struct cho event payload để giảm thiểu Garbage Collection.
    /// </summary>
    public interface IEvent
    {
    }
}
