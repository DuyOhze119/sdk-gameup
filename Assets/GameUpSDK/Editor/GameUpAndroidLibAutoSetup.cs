#if UNITY_EDITOR && ADMOB_DEPENDENCIES_INSTALLED    
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameUpSDK.Editor
{
    /// <summary>
    /// Script tự động trích xuất giao diện Native UI từ trong UPM Package (Immutable) 
    /// ra ngoài thư mục Assets/Plugins/Android của Project để Unity có thể build được.
    /// </summary>
    [InitializeOnLoad]
    public class GameUpAndroidLibAutoSetup
    {
        static GameUpAndroidLibAutoSetup()
        {
            EditorApplication.delayCall += AutoSetupAndroidLib;
        }

        private static void AutoSetupAndroidLib()
        {
            // 1. ĐỊNH NGHĨA ĐƯỜNG DẪN ĐÍCH NẰM NGOÀI ASSETS (WRITABLE FOLDER)
            string targetAndroidPluginPath = Path.Combine(Application.dataPath, "Plugins", "Android");
            string targetLibPath = Path.Combine(targetAndroidPluginPath, "GameUpNativeAds.androidlib");

            // Nếu đích đến đã có đầy đủ rồi thì dừng script để tiết kiệm tài nguyên
            if (Directory.Exists(targetLibPath) && File.Exists(Path.Combine(targetLibPath, "AndroidManifest.xml")))
            {
                return;
            }

            // 2. TÌM KIẾM TÀI NGUYÊN GỐC BÊN TRONG UPM PACKAGE
            string[] guids = AssetDatabase.FindAssets("gameup_native_collapsible");
            if (guids.Length == 0) return; // Không tìm thấy file gốc

            // Lấy đường dẫn của file XML đầu tiên tìm được
            string sourceLayoutPath = Path.GetFullPath(AssetDatabase.GUIDToAssetPath(guids[0]));
            
            // Dò ngược lên thư mục 'res'
            DirectoryInfo layoutDir = new DirectoryInfo(Path.GetDirectoryName(sourceLayoutPath));
            DirectoryInfo sourceResDir = layoutDir.Parent;

            if (sourceResDir == null || sourceResDir.Name != "res") return;

            // 3. TIẾN HÀNH SAO CHÉP VÀ KHỞI TẠO TẠI THƯ MỤC ASSETS
            bool isModified = false;

            if (!Directory.Exists(targetLibPath))
            {
                Directory.CreateDirectory(targetLibPath);
                isModified = true;
            }

            string targetResPath = Path.Combine(targetLibPath, "res");
            if (!Directory.Exists(targetResPath))
            {
                // Copy toàn bộ thư mục res từ Package ra Assets
                CopyDirectory(sourceResDir.FullName, targetResPath);
                isModified = true;
            }

            // 4. SINH CÁC FILE CẤU HÌNH CHO ANDROID LIBRARY
            isModified |= CreateFileIfMissing(
                Path.Combine(targetLibPath, "project.properties"), 
                "android.library=true"
            );

            isModified |= CreateFileIfMissing(
                Path.Combine(targetLibPath, "AndroidManifest.xml"), 
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\"\n    package=\"com.gameup.ads.nativeui\">\n    <application />\n</manifest>"
            );

            string resRawPath = Path.Combine(targetLibPath, "res", "raw");
            if (!Directory.Exists(resRawPath)) Directory.CreateDirectory(resRawPath);

            isModified |= CreateFileIfMissing(
                Path.Combine(resRawPath, "keep.xml"), 
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<resources xmlns:tools=\"http://schemas.android.com/tools\"\n    tools:keep=\"@layout/gameup_native_collapsible, @drawable/gameup_bg_rounded, @color/*, @dimen/*\" />"
            );

            if (isModified)
            {
                AssetDatabase.Refresh();
                Debug.Log("<b>[GameUpSDK]</b> Đã trích xuất và khởi tạo cấu hình Native Ads UI ra thư mục <b>Assets/Plugins/Android/GameUpNativeAds.androidlib</b> thành công! Bạn có thể tùy biến giao diện tại đây.");
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

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            if (!Directory.Exists(destinationDir)) Directory.CreateDirectory(destinationDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                // Không copy các file .meta của thư mục Package vì Unity sẽ tự sinh file .meta mới cho thư mục Assets
                if (file.EndsWith(".meta")) continue; 

                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, false);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                string destDir = Path.Combine(destinationDir, Path.GetFileName(dir));
                CopyDirectory(dir, destDir);
            }
        }
    }
}
#endif