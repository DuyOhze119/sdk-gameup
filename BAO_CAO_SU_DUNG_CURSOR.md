# Báo cáo sử dụng Cursor – Bài toán kỹ thuật đã giải quyết

*Dựa trên **toàn bộ** lịch sử sử dụng AI (agent-transcripts) và **toàn bộ** lịch sử thay đổi (git) trong SDK GameUp — không giới hạn theo thời gian gần đây.*

---

## Nguồn dữ liệu đã rà soát

- **Lịch sử AI**: Toàn bộ 9 phiên chat trong `agent-transcripts` (đã đọc đầy đủ nội dung từng phiên).
- **Lịch sử code**: Toàn bộ 15 commit trong repo (từ Initial commit đến hiện tại):

| Commit | Mô tả |
|--------|--------|
| 2903b7e | Initial commit |
| 0cccbfd | init |
| f656ed8 | Add AppsFlyer, Firebase and AdMob SDKs |
| d471a40 | Refactor SDK to GameUpSDK; add LevelPlay UnityAds |
| 30b4050 | Add ads load events and centralized logging |
| f966e42 | Add GameUp SDK setup, analytics & Android configs |
| 79362de | Add AppsFlyer prefab and update SDK refs |
| feca9ca | Reorganize GameUpSDK into Runtime folder |
| 49e0ad5 | Update GameUpSetupWindow.cs |
| 2a00f06 | Persist SDK config to Resources asset |
| 11758cc | Add AppsFlyer prefab and editor SDK tools |
| 89d4a11 | Add Remote Config and ad rules; update AdsManager |
| 27432c6 | Add Firebase RC tab and prefab fields |
| 07f3b86 | Ads: IronSource mediation, AdMob init & banner fixes |
| 35478f9 | Add ad revenue logging and remote config fixes |

---

## Cấu trúc báo cáo

| **Vấn đề** | **Giải pháp** | **Kết quả tối ưu** |
|------------|---------------|--------------------|
| *Mô tả bài toán kỹ thuật* | *Cách Cursor đã xử lý* | *Hiệu quả / thời gian ước tính* |

---

## 1. Ads – Rewarded Video: user thoát giữa chừng không nhận reward

| **Vấn đề** | **Giải pháp** | **Kết quả tối ưu** |
|------------|---------------|--------------------|
| Trong `ShowRewardedVideo`, khi user đang xem ad rồi thoát (không xem hết, không nhận reward) cần gọi `onFail` và log đúng để game xử lý và analytics đo đúng. | Kiểm tra interface `IAds` và event; xác nhận IronSource/Unity đã gọi callback khi closed without reward. Cập nhật **AdsManager**: khi `wrappedFail` được gọi (bao gồm thoát giữa chừng), log `AdsShowFail` rồi gọi `onFail`. Thêm comment rõ: `onFail` dùng cho không có network, display failed, **hoặc user thoát không nhận reward**. | Luồng thống nhất: user thoát giữa chừng → network báo closed without reward → log `ads_show_fail` + gọi `onFail`; game và analytics nhất quán, tránh bỏ sót sự kiện. |

---

## 2. Đo lường ad impression chính xác (LogAdImpression)

| **Vấn đề** | **Giải pháp** | **Kết quả tối ưu** |
|------------|---------------|--------------------|
| Cần gọi `LogAdImpression` **sau khi** ad thực sự được show để đo lường đúng (Firebase ARM, AppsFlyer ad revenue). | Rà soát LevelPlay SDK: dùng callback **`OnImpressionDataReady`** (gọi sau khi ad show và có revenue). Trong **IronSourceAds**: subscribe callback, map `LevelPlayImpressionData` → `AdImpressionData`, dùng `MainThreadDispatcher` rồi raise `AdsEvent.RaiseImpressionDataReady`. Trong **AdsManager**: subscribe `OnImpressionDataReady` và gọi `GameUpAnalytics.LogAdImpression`. Hủy subscribe trong OnDestroy. | LogAdImpression chỉ chạy khi đã có impression thật từ LevelPlay; đo đúng doanh thu và ARM, tránh log sớm hoặc trùng. |

---

## 3. Thêm tham số level khi xem Interstitial & Rewarded

| **Vấn đề** | **Giải pháp** | **Kết quả tối ưu** |
|------------|---------------|--------------------|
| Cần cập nhật log khi xem xong reward và inter, thêm param **level** theo spec (Firebase + AppsFlyer). | Thêm hằng `ParamLevel` trong **AdsEvent**. Thêm `LogAdsEventWithLevel` gửi Firebase (where + level) và AppsFlyer (`af_level`). Inter: khi show complete gọi log kèm `currentLevel`. Rewarded: thêm overload `ShowRewardedVideo(where, currentLevel, onSuccess, onFail)`; khi xem xong log với level; overload cũ gọi overload mới với level = 0. | Event `ad_inter_show_complete` và `ad_rewarded_show_complete` có đủ `where` và `level`; phân tích theo level chính xác, không phá API cũ. |

---

## 4. Đồng nhất enable_banner (Remote Config) với showBannerAfterInit

| **Vấn đề** | **Giải pháp** | **Kết quả tối ưu** |
|------------|---------------|--------------------|
| Logic **enable_banner** (Firebase Remote Config) và **showBannerAfterInit** (AdsManager) chưa thống nhất; cần ưu tiên **enable_banner** cao hơn. | Trong **AdsManager.Initialize()**: chỉ chạy `ShowBannerAfterInitCoroutine()` khi **cả hai** đúng: `showBannerAfterInit` **và** `AdsRules.IsBannerEnabled()` (enable_banner). Giữ check `IsBannerEnabled()` trong `ShowBanner()`. Thêm Tooltip và cập nhật summary trong **FirebaseRemoteConfigUtils** ghi rõ thứ tự ưu tiên. | Bảng rõ ràng: enable_banner = false → không show banner (kể cả showBannerAfterInit = true); tránh hiển thị banner khi RC tắt. |

---

## 5. Banner vẫn hiện dù bỏ tick showBannerAfterInit

| **Vấn đề** | **Giải pháp** | **Kết quả tối ưu** |
|------------|---------------|--------------------|
| Dù bỏ tick **Show Banner After Init**, banner vẫn hiện; nghi do Request/load. | Xác định: LevelPlay mặc định **tự show banner khi load xong** (chưa set `SetDisplayOnLoad(false)`). Admob/IronSource vẫn gọi `RequestBanner()` trong init. Sửa: **IronSourceAds** thêm `.SetDisplayOnLoad(false)` cho banner; **AdsManager** luôn gọi `RequestAll()` (preload) nhưng **chỉ** chạy `ShowBannerAfterInitCoroutine()` khi `showBannerAfterInit == true`. | Bỏ tick thì banner không tự hiện; banner chỉ hiện khi game gọi `ShowBanner()`; request vẫn chạy để sẵn ad khi cần. |

---

## 6. Firebase Remote Config: init trên Editor và đồng bộ với FirebaseUtils

| **Vấn đề** | **Giải pháp** | **Kết quả tối ưu** |
|------------|---------------|--------------------|
| Cần init Remote Config cả trên Editor để test; đồng thời init cùng FirebaseUtils để tránh lỗi (FirebaseApp null). | **FirebaseUtils**: thêm `_initialized`, `IsInitialized`, và event `onInitialized`. **FirebaseRemoteConfigUtils**: Editor dùng `ApplyDefaultValues()` (giống bộ default build) và set ready; build đợi `FirebaseUtils.IsInitialized` hoặc `onInitialized` rồi mới `InitRemoteConfig()`. Dùng chung `GetDefaultValues()` cho SetDefaultsAsync và ApplyDefaultValues; mọi nhánh lỗi đều apply default rồi mới set ready. | Test Remote Config ngay trong Editor; không còn lỗi init do Firebase chưa sẵn sàng; game luôn có default an toàn khi lỗi. |

---

## 7. Đồng bộ script GameUpSDK với SDK hiện tại (project How-Many-Guys)

| **Vấn đề** | **Giải pháp** | **Kết quả tối ưu** |
|------------|---------------|--------------------|
| Script trong project game (How-Many-Guys) cần cập nhật cho khớp SDK hiện tại, đồng thời **giữ** logic sửa banner và **FirebaseRemoteConfigUtils** đã làm trước đó. | So sánh từng file SDK (FirebaseUtils, FirebaseRemoteConfigUtils, AdsManager, IronSourceAds, AdsEvent) với bản SDK; áp dụng thay đổi SDK (RequestAll, ShowBanner, SetDisplayOnLoad(false), ARM, Init flow) nhưng giữ code riêng game (AdsRules level/placement, UIManager.Waiting, GameUtils.IsEditor). Sau đó sửa lỗi build: thêm `using GameUpSDK.Utils;` cho `MonoSingleton<>` trong FirebaseRemoteConfigUtils. | Game dùng đúng bản SDK mới, giữ logic banner và Remote Config; build thành công, không mất tính năng riêng của game. |

---

## 8. Kiểm tra / cập nhật script cho phù hợp SDK (tổng quát)

| **Vấn đề** | **Giải pháp** | **Kết quả tối ưu** |
|------------|---------------|--------------------|
| Yêu cầu kiểm tra và cập nhật toàn bộ script trong `Assets/GameUpSDK/Scripts` cho phù hợp với bộ SDK hiện tại. | So sánh từng script với SDK, xác định phần cần đồng bộ và phần cần giữ (logic game); áp dụng cập nhật từng file. | Codebase thống nhất với SDK, dễ bảo trì và nâng cấp sau này. |

---

## Tóm tắt theo nhóm

- **Ads (Rewarded/Inter/Banner)**: xử lý thoát giữa chừng, đo impression đúng lúc, thêm level vào log, thống nhất enable_banner và showBannerAfterInit, sửa banner tự hiện khi load (SetDisplayOnLoad).
- **Firebase / Remote Config**: init trên Editor, đồng bộ với FirebaseUtils, default an toàn khi lỗi.
- **Đồng bộ SDK với game**: cập nhật script game theo SDK, giữ logic đặc thù và sửa lỗi build (MonoSingleton).

---

*Báo cáo phản ánh **toàn bộ** bài toán kỹ thuật (từ toàn bộ lịch sử chat và toàn bộ git) đã giải quyết với Cursor trong phạm vi SDK GameUp và project liên quan — không chỉ các thay đổi gần đây.*
