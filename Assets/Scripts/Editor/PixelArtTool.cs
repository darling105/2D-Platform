#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace Platformer2D.EditorTools
{
    /// <summary>
    /// Công cụ tự động cấu hình toàn bộ ảnh trong dự án thành chuẩn Pixel Art sắc nét (Point filter, Uncompressed).
    /// </summary>
    public class PixelArtTool : AssetPostprocessor
    {
        // 1. Tự động áp dụng cho mọi file ảnh mới thêm vào Assets/GameAssets trong tương lai
        private void OnPreprocessTexture()
        {
            if (assetPath.StartsWith("Assets/GameAssets") || assetPath.StartsWith("Assets/Animations"))
            {
                TextureImporter textureImporter = (TextureImporter)assetImporter;
                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.filterMode = FilterMode.Point;
                textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            }
        }

        // 2. Nút bấm 1-click chuyển đổi toàn bộ 960+ assets hiện có trong dự án
        [MenuItem("Tools/Pixel Art/Tối ưu toàn bộ Assets thành Pixel Art (Point & None)")]
        public static void ConvertAllTexturesToPixelArt()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/GameAssets" });
            int total = guids.Length;

            if (total == 0)
            {
                EditorUtility.DisplayDialog("Thông báo", "Không tìm thấy texture nào trong Assets/GameAssets!", "OK");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Xác nhận",
                $"Tìm thấy {total} file ảnh trong Assets/GameAssets.\n\nBạn có muốn tự động chuyển toàn bộ sang:\n- Filter Mode: Point (no filter)\n- Compression: None (Sắc nét 100%)\nkhông?",
                "Đồng ý thực hiện",
                "Hủy"
            );

            if (!confirmed) return;

            try
            {
                AssetDatabase.StartAssetEditing();

                for (int i = 0; i < total; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    EditorUtility.DisplayProgressBar("Đang tối ưu Pixel Art...", $"[{i + 1}/{total}] {Path.GetFileName(path)}", (float)i / total);

                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer != null)
                    {
                        bool changed = false;

                        if (importer.filterMode != FilterMode.Point)
                        {
                            importer.filterMode = FilterMode.Point;
                            changed = true;
                        }

                        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                        {
                            importer.textureCompression = TextureImporterCompression.Uncompressed;
                            changed = true;
                        }

                        if (changed)
                        {
                            importer.SaveAndReimport();
                        }
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Thành công!", $"Đã xử lý xong toàn bộ {total} ảnh thành chuẩn Pixel Art sắc nét!", "Tuyệt vời");
        }

        // 3. Đồng bộ Pixels Per Unit (PPU) = 32 cho toàn bộ sprite Player
        [MenuItem("Tools/Pixel Art/Đồng bộ PPU = 32 cho toàn bộ Sprites Player")]
        public static void SyncPlayerPPU()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/GameAssets/Player" });
            int total = guids.Length;

            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < total; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer != null && importer.spritePixelsPerUnit != 32f)
                    {
                        importer.spritePixelsPerUnit = 32f;
                        importer.SaveAndReimport();
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Thành công!", $"Đã đồng bộ PPU = 32 cho {total} ảnh trong thư mục Player! Kích thước nhân vật bây giờ sẽ to chuẩn khít ô lưới.", "OK");
            Debug.Log("<color=green>[PixelArtTool] Đã đồng bộ PPU = 32 cho toàn bộ Player Sprites!</color>");
        }
    }
}
#endif
