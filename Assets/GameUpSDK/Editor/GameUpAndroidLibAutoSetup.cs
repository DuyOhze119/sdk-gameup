#if UNITY_EDITOR && ADMOB_DEPENDENCIES_INSTALLED
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameUpSDK.Editor
{
    /// <summary>
    /// Script tự động dò tìm vị trí thư mục 'res' của Native Ads ở bất kỳ đâu trong project
    /// (kể cả trong thư mục Packages) và tự động đóng gói nó thành chuẩn .androidlib
    /// </summary>
    [InitializeOnLoad]
    public class GameUpAndroidLibAutoSetup
    {
        static GameUpAndroidLibAutoSetup()
        {
            // Chạy ngầm sau khi Editor compile xong
            EditorApplication.delayCall += AutoSetupAndroidLib;
        }

        private static void AutoSetupAndroidLib()
        {
            // 1. TỰ ĐỘNG DÒ TÌM ĐƯỜNG DẪN ĐỘNG (DYNAMIC PATH FINDING)
            // Tìm GUID của file layout XML để xác định vị trí thực tế của thư mục res
            string[] guids = AssetDatabase.FindAssets("gameup_native_collapsible");
            if (guids.Length == 0)
            {
                // Chưa import UI hoặc đã bị đổi tên -> Bỏ qua
                return;
            }

            // Lấy đường dẫn tuyệt đối của file XML (vd: .../Plugins/Android/res/layout/gameup_native_collapsible.xml)
            string layoutFilePath = Path.GetFullPath(AssetDatabase.GUIDToAssetPath(guids[0]));

            // Dùng File System để truy ngược lên lấy các thư mục cha
            DirectoryInfo layoutDir = new DirectoryInfo(Path.GetDirectoryName(layoutFilePath)); // Thư mục 'layout'
            DirectoryInfo resDir = layoutDir.Parent; // Thư mục 'res'
            DirectoryInfo parentDir = resDir.Parent; // Thư mục chứa 'res' (Android hoặc .androidlib)

            bool isModified = false;
            string androidLibPath = "";

            // 2. KIỂM TRA VÀ TỰ ĐỘNG DI CHUYỂN
            // Nếu cha của 'res' không phải là thư mục .androidlib -> Cần di chuyển!
            if (!parentDir.Name.EndsWith(".androidlib"))
            {
                androidLibPath = Path.Combine(parentDir.FullName, "GameUpNativeAds.androidlib");

                if (!Directory.Exists(androidLibPath))
                {
                    Directory.CreateDirectory(androidLibPath);
                }

                string targetResPath = Path.Combine(androidLibPath, "res");

                // Bốc toàn bộ thư mục 'res' thả vào bên trong '.androidlib'
                MoveDirectory(resDir.FullName, targetResPath);

                // Dọn dẹp xác thư mục res cũ
                if (Directory.Exists(resDir.FullName))
                    Directory.Delete(resDir.FullName, true);

                // Xóa luôn file .meta cũ của thư mục res để Unity khỏi báo lỗi rác
                string resMeta = resDir.FullName + ".meta";
                if (File.Exists(resMeta))
                    File.Delete(resMeta);

                isModified = true;
                Debug.Log("[GameUpSDK] Đã di chuyển thành công thư mục 'res' vào chuẩn .androidlib");
            }
            else
            {
                // Đã nằm đúng vị trí trong .androidlib, chỉ cần lấy đường dẫn
                androidLibPath = parentDir.FullName;
            }

            // 3. BẢO ĐẢM CÁC FILE CẤU HÌNH BẮT BUỘC LUÔN TỒN TẠI
            isModified |= CreateFileIfMissing(
                Path.Combine(androidLibPath, "project.properties"),
                "target=android-33\nandroid.library=true"
            );

            isModified |= CreateFileIfMissing(
                Path.Combine(androidLibPath, "AndroidManifest.xml"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\"\n    package=\"com.gameup.ads.nativeui\">\n    <application />\n</manifest>"
            );

            // Sinh file chặn R8/ProGuard xóa UI của chúng ta
            string resRawPath = Path.Combine(androidLibPath, "res", "raw");
            if (!Directory.Exists(resRawPath)) Directory.CreateDirectory(resRawPath);

            isModified |= CreateFileIfMissing(
                Path.Combine(resRawPath, "keep.xml"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<resources xmlns:tools=\"http://schemas.android.com/tools\"\n    tools:keep=\"@layout/gameup_native_collapsible, @drawable/gameup_bg_rounded, @color/*, @dimen/*\" />"
            );

            // 4. BÁO CHO UNITY BIẾT ĐỂ NẠP LẠI ASSET
            if (isModified)
            {
                AssetDatabase.Refresh();
                Debug.Log("[GameUpSDK] Hoàn tất cấu hình tự động Android Library cho Native Ads!");
            }
        }

        private static bool CreateFileIfMissing(string path, string content)
        {
            if (!File.Exists(path))
            {
                File.WriteAllText(path, content);
                return true;
            }

            return false;
        }

        private static void MoveDirectory(string sourceDir, string destinationDir)
        {
            if (!Directory.Exists(destinationDir)) Directory.CreateDirectory(destinationDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                if (!File.Exists(destFile)) File.Move(file, destFile);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                string destDir = Path.Combine(destinationDir, Path.GetFileName(dir));
                MoveDirectory(dir, destDir);
            }
        }
    }
}
#endif