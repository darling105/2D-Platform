using UnityEngine;

namespace Platformer2D.Core.Interfaces
{
    /// <summary>
    /// Interface cho toàn bộ các vật thể tương tác được trong game 2D (rương kho báu, đòn bẩy, NPC, cửa dịch chuyển, biển báo).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Chuỗi văn bản hiển thị gợi ý tương tác trên UI (ví dụ: "Nhấn E để mở rương").
        /// </summary>
        string InteractionPrompt { get; }

        /// <summary>
        /// Kiểm tra xem đối tượng initiator (thường là Player) có đủ điều kiện tương tác lúc này hay không.
        /// </summary>
        bool CanInteract(GameObject initiator);

        /// <summary>
        /// Thực thi hành động tương tác.
        /// </summary>
        void Interact(GameObject initiator);
    }
}
