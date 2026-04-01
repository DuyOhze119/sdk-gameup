# Changelog

Tất cả thay đổi đáng chú ý của **GameUp SDK** (`com.ohze.gameup.sdk`) được ghi ở đây.

Định dạng theo [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [1.1.0] — 2026-04-01

### Added

- Tích hợp **GameAnalytics** (tùy chọn): cài qua **GameUp SDK → Setup Dependencies**, define `GAMEANALYTICS_DEPENDENCIES_INSTALLED`, mirror tiến trình **level / wave** qua design events (`gameup:`) trong `GameUpAnalytics`.
- `GameAnalyticsMirror` (reflection) để Runtime không phụ thuộc assembly GameAnalytics khi chưa cài.
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

[1.1.0]: https://github.com/DuyOhze119/sdk-gameup/compare/v1.0.1...v1.1.0
[1.0.1]: https://github.com/DuyOhze119/sdk-gameup/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/DuyOhze119/sdk-gameup/releases/tag/v1.0.0
