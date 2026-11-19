namespace Platformer2D.Core.Architecture
{
    /// <summary>
    /// Tập trung toàn bộ tên và đường dẫn Scene chuẩn của dự án (tránh dùng chuỗi cứng rải rác).
    /// - Main/: Các Scene chính thức đưa vào bản build game hoàn chỉnh.
    /// - Testing/: Các Scene Sandbox dành riêng cho kiểm thử tính năng & cơ chế.
    /// </summary>
    public static class SceneNames
    {
        public const string MainMenu = "MainMenu";
        public const string Level01 = "Level_01";
        public const string MechanicsTest = "MechanicsTestScene";

        public const string MainMenuPath = "Assets/Scenes/Main/MainMenu.unity";
        public const string Level01Path = "Assets/Scenes/Main/Level_01.unity";
        public const string MechanicsTestPath = "Assets/Scenes/Testing/MechanicsTestScene.unity";
    }
}
