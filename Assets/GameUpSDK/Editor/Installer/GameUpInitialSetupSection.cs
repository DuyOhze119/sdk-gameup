using System;
using UnityEditor;
using UnityEngine;

namespace GameUpSDK.Installer
{
    /// <summary>
    /// Khối UI "Cài đặt ban đầu" — hiển thị ở đầu cửa sổ Dependencies / Setup.
    /// </summary>
    public static class GameUpInitialSetupSection
    {
        public const int RequiredMinAndroidApi = 24;
        public const int RecommendedMaxTargetAndroidApi = 36;

        public static void Draw(string nextStepHint = null)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Cài đặt ban đầu", EditorStyles.boldLabel);

            string tail = string.IsNullOrEmpty(nextStepHint)
                ? ""
                : "\n\n" + nextStepHint;

            EditorGUILayout.HelpBox(
                "Kiểm tra trước khi cài dependency và build Android:\n\n" +
                "• Unity: khuyến nghị 2022.3 LTS trở lên.\n" +
                "• Android — Minimum API Level: " + RequiredMinAndroidApi + " (Android 7.0).\n" +
                "• Android — Target API Level: tối đa " + RecommendedMaxTargetAndroidApi +
                " (phạm vi hỗ trợ GameUp SDK / mediation).\n" +
                "• GameUp SDK đã có trong project (UPM Git URL hoặc thư mục Assets/GameUpSDK)." +
                tail,
                MessageType.Info);

            EditorGUILayout.LabelField("Trạng thái hiện tại (Player → Android)", EditorStyles.miniBoldLabel);
            var minSdk = PlayerSettings.Android.minSdkVersion;
            var targetSdk = PlayerSettings.Android.targetSdkVersion;
            EditorGUILayout.LabelField("Minimum API", minSdk.ToString());
            EditorGUILayout.LabelField("Target API", targetSdk.ToString());

            int minLevel = SdkVersionToApiLevel(minSdk, isTarget: false);
            int targetLevel = SdkVersionToApiLevel(targetSdk, isTarget: true);

            if (minLevel > 0 && minLevel < RequiredMinAndroidApi)
            {
                EditorGUILayout.HelpBox(
                    "Minimum API đang thấp hơn " + RequiredMinAndroidApi +
                    ". Nên tăng để tương thích SDK quảng cáo.",
                    MessageType.Warning);
            }

            if (targetLevel > RecommendedMaxTargetAndroidApi)
            {
                EditorGUILayout.HelpBox(
                    "Target API đang cao hơn " + RecommendedMaxTargetAndroidApi +
                    ". GameUp SDK khuyến nghị giữ Target tối đa " + RecommendedMaxTargetAndroidApi + ".",
                    MessageType.Warning);
            }

            if (GUILayout.Button("Mở Project Settings → Player", GUILayout.Height(24)))
            {
                SettingsService.OpenProjectSettings("Project/Player");
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        /// <summary>
        /// 0 = Automatic / không map được số API — không dùng để cảnh báo target.
        /// </summary>
        private static int SdkVersionToApiLevel(AndroidSdkVersions version, bool isTarget)
        {
            try
            {
                if (isTarget && version == AndroidSdkVersions.AndroidApiLevelAuto)
                    return 0;

                int v = Convert.ToInt32(version);
                if (isTarget && v == 0)
                    return 0;
                return v;
            }
            catch
            {
                return 0;
            }
        }
    }
}
