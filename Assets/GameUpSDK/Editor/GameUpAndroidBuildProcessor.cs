#if UNITY_EDITOR
using System.IO;
using UnityEditor.Android;
using UnityEngine;

namespace GameUpSDK.Editor
{
    // Script này tự động chạy sau khi Unity xuất ra project Gradle (Ngay trước khi build ra APK/AAB)
    public class GameUpAndroidBuildProcessor : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 99; // Chạy sau cùng

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            // 'path' là đường dẫn tới thư mục gradle của project (Temp/gradleOut)
            string proguardFilePath = Path.Combine(path, "proguard-unity.txt");
            
            // Nếu dùng Custom Proguard, Unity có thể đẩy ra tên khác, ta kiểm tra thêm
            if (!File.Exists(proguardFilePath))
            {
                proguardFilePath = Path.Combine(path, "proguard-user.txt");
            }

            if (File.Exists(proguardFilePath))
            {
                // Luật ProGuard bảo vệ class Java của GameUp SDK
                string proGuardRules = @"
# ==========================================
# GAMEUP SDK PROGUARD RULES (AUTO GENERATED)
# ==========================================
-keep class com.plugins.nativebridge.UnityNativeFullScreen { *; }
-keep interface com.plugins.nativebridge.UnityNativeFullScreen$INativeAdCallback { *; }
";
                // Chèn thêm luật vào cuối file
                File.AppendAllText(proguardFilePath, "\n" + proGuardRules);
                Debug.Log("[GameUpSDK] Đã tự động chèn ProGuard Rules cho Native Ads thành công.");
            }
        }
    }
}
#endif