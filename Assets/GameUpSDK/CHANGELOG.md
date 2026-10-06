# Changelog

Tất cả thay đổi đáng chú ý của **GameUp SDK** (`com.ohze.gameup.sdk`) được ghi ở đây.

Định dạng theo [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [2.1.0] — 2026-10-06

### Summary

Đồng bộ toàn bộ logic SDK từ `gameup-unity-template` (bản 1.3.0 → 2.1.0 của template) sang repo này. **Giữ nguyên** namespace (`GameUpSDK`, `GameUpSDK.Ads`, `GameUpSDK.Editor(.Setup)`, `GameUpSDK.Installer`), tên class / hàm public, tên asmdef (`GameUpSDK.Runtime` / `GameUpSDK.Editor` / `GameUpSDK.Installer`), menu `GameUp SDK/…` và GUID `.meta`. SDK **không** phụ thuộc GameUp Core: `GULogger` → `Debug.Log*`, `MonoSingleton` → `MonoSingletonSdk`.

### Added

- **Cấu hình bằng ScriptableObject** thay cho prefab: `GameUpAdsConfig` (AdMob / MAX / LevelPlay, `mediationPriority`, `nativeCtaClickRate`, `appOpenOnColdStart`) và `GameUpSdkConfig` (AppsFlyer, Adjust, AppMetrica, Remote Config defaults) tại `Assets/SDK/Resources/GameUpSDK/`. `configOverride` trên các network / `AppsFlyerUtils` / `AppMetricaActivator` / `FirebaseRemoteConfigUtils`. Menu migrate dữ liệu cũ: **GameUp SDK → Migrate Ads Config / Migrate SDK Config (Prefab → ScriptableObject)**.
- **Adjust làm MMP thay thế AppsFlyer** (`AdjustAnalyticsUtils`, `MmpProvider`, define `GAMEUP_MMP_APPSFLYER` / `GAMEUP_MMP_ADJUST`, `ADJUST_DEPENDENCIES_INSTALLED`), tab Adjust trong Setup, `GameUpMmpBuildCheck` cảnh báo cấu hình MMP lúc build.
- **Native Overlay** (`AdmobNativeOverlayAd`, `INativeOverlayAd`, `AdsManager.NativeOverlay`).
- `MediationPriority` — waterfall tự khớp với SDK đang cài; tab **Cài đặt chung & Thứ tự ưu tiên**.
- `PrivacyResult` (`CanRequestAds` / `TrackingAllowed`), `PrivacyManager.ShowPrivacyOptionsForm`, `AdsManager.RetryInitializeAfterConsent`.
- `AdsManager.SetRemoveAds(removeInterstitial, removeAllAds)` — thay cho `RemoveAdsSetting` của template (vốn phụ thuộc GameUp Core). `RemoveAdCondition` vẫn dùng như cũ.
- `AdImpressionData.Mediation` / `Currency`; `GameUpConfigValidator`, `AdPlacementGenerator`, `GameUpGitignoreSync`, custom Inspector cho 2 asset config.

### Changed

- Setup Dependencies: chọn tổ hợp AdMob / MAX / LevelPlay + MMP thay cho một "Primary Mediation" (define `GAMEUP_PRIMARY_MEDIATION_*` giữ lại dạng `[Obsolete]`).
- `PrivacyManager.BeginPrivacyFlow` nhận `Action<PrivacyResult>`; overload cũ `Action<bool>` vẫn giữ (nhận `TrackingAllowed`). `ConsentGranted` đánh dấu `[Obsolete]`.
- `MainThreadDispatcher` có runner riêng (không còn phụ thuộc `AdsManager.Update`); `TimerHelper` dùng thời gian thực.
- `SDK.prefab` không còn nhúng `AppsFlyerObject` — `AppsFlyerUtils` tự tạo khi cần.
- Yêu cầu Unity 6 (`"unity": "6000.0"`), `com.unity.services.core` 1.18.0.

### Fixed

- Banner tự hiện lại sau `HideBanner` khi auto-refresh; banner đè fullscreen ad.
- Ad revenue: MAX Banner/App Open, collapsible native banner Android (`setOnPaidEventListener`), đúng mediation & currency cho Firebase / AppsFlyer / Adjust / AppMetrica.
- Callback AdMob / UMP marshal về main thread (bỏ `RaiseAdEventsOnUnityMainThread`).
- **Gỡ package chạy 2 pha: gỡ define → đợi compile → mới xóa file.** Trước đây gỡ lẻ / gỡ toàn bộ clear define và xóa asset trong cùng một lần chạy, nên Unity có thể compile `GameUpSDK.Runtime` với define cũ trong khi SDK đang bị xóa dở. Nay pha 2 (xóa file, gộp trong `StartAssetEditing`) chỉ chạy sau `compilationFinished` / domain reload; danh sách path lưu trong `SessionState` nên vẫn chạy khi cửa sổ installer đã đóng; guard chặn auto-sync define được giữ suốt hai pha.
- Installer: gỡ dependencies không sót define, không xóa `Assets/Plugins/Android`, "Cài tất cả" chạy tiếp sau domain reload, lỗi cài không bị nuốt.

### Migration

Mở **GameUp SDK → Setup** → **Migrate dữ liệu từ Prefab → ScriptableObject** → **Save Configuration**. Field cũ trong prefab được giữ (ẩn) để migrate.

## [1.1.3] — 2026-04-02

### Summary

- **GameAnalytics**: luồng cài đặt, asmdef runtime (`Ensure GameAnalytics runtime asmdef`), define `GAMEANALYTICS_DEPENDENCIES_INSTALLED`, Setup / scene SDK và analytics level–wave được coi là **hoàn thiện** cho consumer.
- **Facebook SDK**: tích hợp trong installer & Setup, define `FACEBOOK_DEPENDENCIES_INSTALLED`, bootstrap/analytics phía GameUp — **hoàn thiện** cùng bản này.

### Changed

- `package.json`: phiên bản **1.1.3**; mô tả & keywords cập nhật (Facebook).

## [1.1.1] — 2026-04-01

### Changed

- **GameAnalytics**: `GameUpAnalytics` gửi **progression events** (Start / Complete / Fail) theo [GA Unity — Progression](https://docs.gameanalytics.com/event-tracking-and-integrations/sdks-and-collection-api/game-engine-sdks/unity/event-tracking); hierarchy cố định `main` → số level → wave (`w{n}`).
- **`GameAnalyticsUtils`**: gọi trực tiếp API `GameAnalyticsSDK`; thêm assembly definition `GameAnalyticsSDK` (`Assets/GameAnalytics/Plugins`) và reference từ `GameUpSDK.Runtime` (không dùng reflection).
- `package.json`: phiên bản **1.1.1** (consumer cập nhật qua Package Manager / Git).

## [1.1.0] — 2026-04-01

### Added

- Tích hợp **GameAnalytics** (tùy chọn): cài qua **GameUp SDK → Setup Dependencies**, define `GAMEANALYTICS_DEPENDENCIES_INSTALLED`, mirror tiến trình **level / wave** qua design events (`gameup:`) trong `GameUpAnalytics`.
- `GameAnalyticsUtils` — gọi GameAnalytics (assembly `GameAnalyticsSDK`); khi chưa bật define GA thì no-op.
- Phát hiện GameAnalytics khi dùng **.unitypackage** cổ điển (type trong `Assembly-CSharp`), không chỉ assembly `GameAnalyticsSDK` (UPM).

### Changed

- `GameUpDependenciesWindow`: thêm package GameAnalytics (hosted `GA_SDK_UNITY.unitypackage`), đưa vào batch cài theo Primary Mediation; `IsGameAnalyticsSdkPresent()` cho define & UI.
- `GameUpDefineSymbolsAutoSync`: đồng bộ `GAMEANALYTICS_DEPENDENCIES_INSTALLED`.
- `GUDefinetion`: `GameAnalyticsDepsInstalled`.

### Fixed

- Trạng thái “chưa cài” GameAnalytics dù đã import `Assets/GameAnalytics` (sai tên assembly so với UPM).

## [1.0.1] — trước đó

- Bản ổn định trước GameAnalytics / cập nhật installer trên.

## [1.0.0]

- Phát hành ban đầu GameUp SDK (Ads + Firebase/AppsFlyer, Setup Dependencies).

[1.1.3]: https://github.com/DuyOhze119/sdk-gameup/compare/v1.1.2...v1.1.3
[1.1.1]: https://github.com/DuyOhze119/sdk-gameup/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/DuyOhze119/sdk-gameup/compare/v1.0.1...v1.1.0
[1.0.1]: https://github.com/DuyOhze119/sdk-gameup/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/DuyOhze119/sdk-gameup/releases/tag/v1.0.0
