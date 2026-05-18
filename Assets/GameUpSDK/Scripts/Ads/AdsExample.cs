using UnityEngine;
using System;

namespace GameUpSDK
{
    public class AdsExample : MonoBehaviour
    {
        private int _currentLevel = 1;
        private string _statusLog = "Sẵn sàng. Nhấn các nút bên dưới để test ad.";
        private Vector2 _scrollPosition;

        private void OnGUI()
        {
            // Thiết lập vùng hiển thị UI bằng GUILayout tại góc trên bên trái màn hình
            GUILayout.BeginArea(new Rect(20, 20, 350, Screen.height - 40));
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Width(350), GUILayout.Height(Screen.height - 40));

            GUILayout.Box("--- GAMEUP SDK ADS DEMO ---", GUILayout.ExpandWidth(true));
            
            // Hiển thị trạng thái mô phỏng
            GUILayout.Label($"<b>Trạng thái:</b> {_statusLog}", new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true });
            GUILayout.Label($"Mô phỏng Level hiện tại: {_currentLevel}");
            
            // Tăng giảm level giả lập để test điều kiện inter_start_level
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Level -1")) { _currentLevel = Mathf.Max(1, _currentLevel - 1); }
            if (GUILayout.Button("Level +1")) { _currentLevel++; }
            GUILayout.EndHorizontal();

            GUILayout.Space(15);

            // =================================================================
            // SECTION 1: REQUESTS / PRELOAD ADS
            // =================================================================
            GUILayout.Box("1. Tải Trước Quảng Cáo (Preload)", GUILayout.ExpandWidth(true));
            
            if (GUILayout.Button("Request All Ads (Tải tất cả các định dạng)", GUILayout.Height(35)))
            {
                LogStatus("Đang gọi RequestAll()...");
                AdsManager.Instance.RequestAll();
            }

            if (GUILayout.Button("Preload Collapsible Banner", GUILayout.Height(30)))
            {
                LogStatus("Đang tải trước Collapsible Banner cho vị trí 'main'...");
                AdsManager.Instance.RequestCollapsibleBanner("main", CollapsibleBannerPlacement.Bottom);
            }

            GUILayout.Space(10);

            // =================================================================
            // SECTION 2: BANNER ADS
            // =================================================================
            GUILayout.Box("2. Quảng cáo Banner", GUILayout.ExpandWidth(true));

            if (GUILayout.Button("Show Standard Banner ('main')", GUILayout.Height(30)))
            {
                LogStatus("Yêu cầu hiển thị Banner tiêu chuẩn vị trí 'main'...");
                AdsManager.Instance.ShowBanner("main", onRqFail: () => LogStatus("Show Standard Banner thất bại (onRqFail)."));
            }

            if (GUILayout.Button("Show Collapsible Banner (Bottom)", GUILayout.Height(30)))
            {
                LogStatus("Yêu cầu hiển thị Collapsible Banner dạng trượt ở dưới màn hình...");
                AdsManager.Instance.ShowCollapsibleBanner("main", CollapsibleBannerPlacement.Bottom, onRqFail: () => LogStatus("Show Collapsible Banner thất bại."));
            }

            if (GUILayout.Button("Hide Banner", GUILayout.Height(30)))
            {
                LogStatus("Đang ẩn Banner vị trí 'main'...");
                AdsManager.Instance.HideBanner("main");
            }

            GUILayout.Space(10);

            // =================================================================
            // SECTION 3: INTERSTITIAL ADS
            // =================================================================
            GUILayout.Box("3. Quảng cáo Xen Kẽ (Interstitial)", GUILayout.ExpandWidth(true));

            if (GUILayout.Button("Show Interstitial (Kiểm tra luật)", GUILayout.Height(35)))
            {
                LogStatus("Yêu cầu hiển thị Interstitial (Có check capping time và level)...");
                
                AdsManager.Instance.ShowInterstitial(
                    where: "end_game_revive",
                    currentLevel: _currentLevel,
                    onSuccess: () => LogStatus("Interstitial đã xem xong hoặc được đóng thành công!"),
                    onFail: () => LogStatus("Interstitial bị chặn (Chưa đủ thời gian capping hoặc chưa đạt level yêu cầu)."),
                    onRqFail: () => LogStatus("Không có mạng quảng cáo nào sẵn sàng xử lý Interstitial.")
                );
            }

            if (GUILayout.Button("Force Show Interstitial (Ép hiển thị)", GUILayout.Height(30)))
            {
                LogStatus("Yêu cầu ép hiển thị Interstitial (Bỏ qua điều kiện kiểm tra)...");
                
                AdsManager.Instance.ShowInterWithoutCondition(
                    where: "forced_button",
                    currentLevel: _currentLevel,
                    onSuccess: () => LogStatus("Ép hiển thị Interstitial thành công!"),
                    onFail: () => LogStatus("Hiển thị thất bại (Quảng cáo chưa kịp tải hoặc lỗi network)."),
                    onRqFail: () => LogStatus("Mạng quảng cáo báo lỗi hệ thống.")
                );
            }

            GUILayout.Space(10);

            // =================================================================
            // SECTION 4: REWARDED ADS
            // =================================================================
            GUILayout.Box("4. Quảng cáo Nhận Quà (Rewarded)", GUILayout.ExpandWidth(true));

            if (GUILayout.Button("Show Rewarded Video", GUILayout.Height(35)))
            {
                LogStatus("Đang mở quảng cáo video nhận thưởng...");
                
                // Mẹo: Bạn nên tạm thời tắt âm thanh của game tại đây trước khi gọi ad
                AdsManager.Instance.ShowRewardedVideo(
                    where: "claim_double_gold",
                    currentLevel: _currentLevel,
                    onSuccess: () => {
                        LogStatus("Thành công! Người chơi đã xem hết video. Tặng quà: +100 Gold!");
                        // Thực hiện cộng tiền vàng/vật phẩm thực tế ở đây
                    },
                    onFail: () => {
                        LogStatus("Thất bại! Người chơi tắt quảng cáo giữa chừng hoặc lỗi hiển thị.");
                    },
                    onRqFail: () => {
                        LogStatus("Video nhận quà chưa sẵn sàng hoặc không tìm thấy video khả dụng.");
                    }
                );
            }

            GUILayout.Space(10);

            // =================================================================
            // SECTION 5: APP OPEN ADS
            // =================================================================
            GUILayout.Box("5. Quảng cáo Mở App (App Open)", GUILayout.ExpandWidth(true));

            if (GUILayout.Button("Show App Open Ad", GUILayout.Height(35)))
            {
                LogStatus("Yêu cầu hiển thị App Open Ad...");
                
                AdsManager.Instance.ShowAppOpenAds(
                    where: "resume_app",
                    onSuccess: () => LogStatus("Đã đóng App Open Ad -> Tiếp tục game."),
                    onFail: () => LogStatus("Hiển thị App Open Ad lỗi hoặc hết hạn (4 tiếng)."),
                    onRqFail: () => LogStatus("App Open Ad chưa được tải xong.")
                );
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void LogStatus(string text)
        {
            _statusLog = $"[{DateTime.Now:HH:mm:ss}] {text}";
            Debug.Log($"[AdsExample] {text}");
        }
    }
}