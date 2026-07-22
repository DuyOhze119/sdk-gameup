#if UNITY_EDITOR
using System.IO;
using UnityEditor.Android;
using UnityEngine;

namespace GameUpSDK.Editor
{
    // Script này tự động chạy sau khi Unity xuất ra project Gradle (Ngay trước khi build ra APK/AAB) [source: 5]
    public class GameUpAndroidBuildProcessor : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 99; // Chạy sau cùng để đảm bảo file proguard đã được Unity sinh ra [source: 5]

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            // 'path' là đường dẫn tới thư mục gradle của project (Temp/gradleOut) [source: 5]
            string proguardFilePath = Path.Combine(path, "proguard-unity.txt");

            // Nếu dùng Custom Proguard, Unity có thể đẩy ra tên file khác [source: 5]
            if (!File.Exists(proguardFilePath))
            {
                proguardFilePath = Path.Combine(path, "proguard-user.txt");
            }

            if (File.Exists(proguardFilePath))
            {
                string currentContent = File.ReadAllText(proguardFilePath);

                // Kiểm tra chống trùng lặp [source: 5]. 
                // Tránh việc mỗi lần Build lại bị append thêm 1 cục text giống hệt nhau làm rác file [source: 5].
                if (!currentContent.Contains("GAMEUP SDK PROGUARD RULES"))
                {
                    // Luật ProGuard bảo vệ class Java của GameUp SDK khỏi bị obfuscate (làm rối mã) [source: 5]
                    string proGuardRules = @"
# ==========================================
# GAMEUP SDK PROGUARD RULES (AUTO GENERATED)
# ==========================================

# 1. Rules cho Native FullScreen (Hỗ trợ Multi-ID Waterfall & Bidding)
-keep class com.plugins.nativebridge.UnityNativeFullScreen { *; }
-keep interface com.plugins.nativebridge.UnityNativeFullScreen$* { *; }
-keep class com.plugins.nativebridge.UnityNativeFullScreen$* { *; }

# 2. Rules cho Native Collapsible Banner & LogBridge
-keep class com.gameup.ads.NativeBannerManager { *; }
-keep interface com.gameup.ads.NativeBannerManager$* { *; }
-keep class com.gameup.ads.NativeBannerManager$* { *; }
";
                    // Chèn thêm luật vào cuối file [source: 5]
                    File.AppendAllText(proguardFilePath, "\n" + proGuardRules);
                    Debug.Log("[GameUpSDK] Đã tự động chèn ProGuard Rules cho Native Ads & LogBridge thành công.");
                }
                else
                {
                    // Log nhẹ để báo hiệu rules đã tồn tại và hệ thống đã chủ động bỏ qua [source: 5]
                    Debug.Log("[GameUpSDK] ProGuard Rules đã tồn tại trong cấu hình Gradle, bỏ qua bước chèn thêm.");
                }
            }
            else
            {
                Debug.LogWarning(
                    "[GameUpSDK] Không tìm thấy file proguard-unity.txt hoặc proguard-user.txt để cấu hình Native Ads!");
            }
        }
    }
}
#endif