# GameUp SDK

SDK tích hợp Quảng cáo (Ads) và Analytics cho game Unity, hỗ trợ:

- **Ads**: IronSource/LevelPlay Mediation (Banner, Interstitial, Rewarded Video) + AdMob (App Open)
- **Analytics**: Firebase Analytics + AppsFlyer MMP
- **Remote Config**: Firebase Remote Config với auto-sync

**Unity**: 2022.3+  |  **Package**: `com.ohze.gameup.sdk`

### Yêu cầu Android (quan trọng)

| Cài đặt | Giá trị | Ghi chú |
|---|---|---|
| **Minimum API Level** | **24** (Android 7.0) | Dưới 24 có thể không tương thích với mediation / thư viện quảng cáo. |
| **Target API Level** | **tối đa 36** | Giữ target trong khoảng hỗ trợ của SDK và network; chỉnh tại **Edit → Project Settings → Player → Android → Other Settings** (`Minimum API Level`, `Target API Level`). |

---

## Mục lục

1. [Cài đặt](#1-cài-đặt)
2. [Cài đặt Dependencies](#2-cài-đặt-dependencies)
3. [Cấu hình qua Setup Window](#3-cấu-hình-qua-setup-window)
4. [Thêm SDK vào Scene](#4-thêm-sdk-vào-scene)
5. [Khi nào coi setup là xong](#5-khi-nào-coi-setup-là-xong)
6. [Cấu trúc `Assets/GameUpSDK/Scripts` và API](#6-cấu-trúc-assetsgameupsdkscripts-và-api)
7. [AdsManager – Quảng cáo](#7-adsmanager--quảng-cáo)
8. [GameUpAnalytics – Analytics](#8-gameupanalytics--analytics)
9. [FirebaseUtils & FirebaseRemoteConfigUtils](#9-firebaseutils--firebaseremoteconfigutils)
10. [AppsFlyerUtils](#10-appsflyerutils)
11. [Remote Config Keys Reference](#11-remote-config-keys-reference)

---

## 1. Cài đặt

### Cài qua UPM (khuyến nghị)

Mở **Package Manager** → **Add package from git URL**:

```
https://github.com/DuyOhze119/sdk-gameup.git?path=Assets/GameUpSDK#main
```

Sau khi import xong, Unity sẽ tự động mở cửa sổ **"GameUp SDK — Setup Dependencies"** để hướng dẫn cài các package phụ thuộc → xem [Bước 2](#2-cài-đặt-dependencies).

### Cài thủ công

Copy thư mục `Assets/GameUpSDK` vào project, sau đó mở thủ công: **GameUp SDK → Setup Dependencies**.

---

## 2. Cài đặt Dependencies

Mở **GameUp SDK → Setup Dependencies** trên thanh menu (hoặc để cửa sổ tự động mở khi cài lần đầu).

### Packages bắt buộc

| Package | Version | Ghi chú |
|---|---|---|
| IronSource LevelPlay SDK | 9.2.0 | Mediation chính: Banner, Interstitial, Rewarded |
| Firebase SDK (Analytics + Crashlytics + Remote Config) | — | Bao gồm EDM4U |

### Packages tùy chọn

| Package | Version | Ghi chú |
|---|---|---|
| Google Mobile Ads (AdMob) | 10.7.0 | Chỉ cần nếu dùng App Open Ads |
| AppsFlyer Attribution SDK | 6.17.81 | Attribution & MMP |

### Cách cài trong cửa sổ Setup Dependencies

- Nhấn **"⬇ Download & Import"** bên cạnh mỗi package để tự động tải và import.
- Hoặc nhấn **"⬇ Cài tất cả (tự động)"** ở footer để cài tất cả cùng lúc.
- Sau khi tất cả package **bắt buộc** đã cài xong, Unity tự động thêm define symbol `GAMEUP_SDK_DEPS_READY` vào Player Settings.
- Khi đó nút **"→ Mở cấu hình SDK"** xuất hiện → nhấn để chuyển sang bước cấu hình.

> **Quan trọng**: `GAMEUP_SDK_DEPS_READY` là define symbol bảo vệ toàn bộ code của SDK. Nếu chưa có define này (chưa cài đủ deps bắt buộc), tất cả các tính năng Ads sẽ là no-op (không hoạt động).

### Menu items liên quan

| Menu | Chức năng |
|---|---|
| **GameUp SDK → Setup Dependencies** | Mở cửa sổ cài đặt package phụ thuộc |
| **GameUp SDK → Setup** | Mở cửa sổ cấu hình keys |
| **GameUp SDK → Reset Setup Status** | Reset trạng thái để mở lại installer vào lần load tiếp theo |

---

## 3. Cấu hình qua Setup Window

Mở **GameUp SDK → Setup** để cấu hình toàn bộ SDK trong một cửa sổ duy nhất. Window có 4 tab:

### Tab: AppsFlyer

| Trường | Mô tả |
|---|---|
| Dev Key | AppsFlyer Dev Key (lấy từ AppsFlyer dashboard) |
| App ID (iOS) | App ID trên App Store |
| SDK Key | Key dùng trong `AppsFlyerUtils` (thường giống Dev Key) |
| Dev Mode | Bật log chi tiết khi test – **tắt khi release** |

### Tab: IronSource Mediation

| Trường | Mô tả |
|---|---|
| App Key (bắt buộc) | App Key từ LevelPlay/IronSource dashboard |
| Banner / Interstitial / Rewarded ID | Ad Unit ID (để trống = dùng `DefaultBanner`, `DefaultInterstitial`, `DefaultRewardedVideo`) |
| Android/iOS App Key | App Key điền vào `LevelPlayMediationSettings` |

### Tab: AdMob (App Open)

AdMob chỉ dùng cho **App Open Ads**. Banner/Interstitial/Rewarded đi qua IronSource Mediation.

| Trường | Mô tả |
|---|---|
| App Open ID | Ad Unit ID cho App Open |
| Android/iOS App ID | Google Mobile Ads App ID (điền vào `GoogleMobileAdsSettings`) |

### Tab: Firebase Remote Config

Giá trị mặc định (fallback khi chưa fetch hoặc Firebase lỗi):

| Key | Mặc định | Mô tả |
|---|---|---|
| `inter_capping_time` | `120` | Thời gian tối thiểu (giây) giữa 2 lần hiện Interstitial |
| `inter_start_level` | `3` | Level bắt đầu cho phép hiện Interstitial |
| `enable_rate_app` | `false` | Bật/tắt popup Rate App |
| `level_start_show_rate_app` | `5` | Level bắt đầu hiện Rate App |
| `no_internet_popup_enable` | `true` | Bật/tắt popup yêu cầu Internet |
| `enable_banner` | `true` | Bật/tắt Banner (ưu tiên cao hơn `showBannerAfterInit`) |

Sau khi chỉnh sửa, bấm **Save Configuration** để lưu vào prefab.

---

## 4. Thêm SDK vào Scene

Sau khi Save Configuration, bấm **"Tạo SDK trong Scene hiện tại"** trong Setup Window.

SDK sẽ được khởi tạo tự động từ prefab `SDK.prefab` (Singleton, `DontDestroyOnLoad`).

> **Lưu ý**: Chỉ cần thêm vào scene **đầu tiên** (Splash/Loading). Không thêm lại ở các scene khác.

---

## 5. Khi nào coi setup là xong

Làm lần lượt các bước dưới đây; **khi tất cả mục đều đạt**, phần tích hợp SDK trong editor coi là hoàn tất — sau đó chỉ cần gọi API trong game code.

| # | Điều kiện | Cách kiểm tra |
|---|-----------|---------------|
| 1 | Package GameUp SDK đã vào project | Thư mục `Assets/GameUpSDK` (hoặc package UPM) có trong project. |
| 2 | Dependencies **bắt buộc** đã cài | **GameUp SDK → Setup Dependencies**: IronSource LevelPlay + Firebase đã import; Unity thêm define **`GAMEUP_SDK_DEPS_READY`** (xem [Bước 2](#2-cài-đặt-dependencies)). |
| 3 | Android API trong phạm vi hỗ trợ | **Player → Android**: Min **24**, Target **≤ 36** (bảng [Yêu cầu Android](#yêu-cầu-android-quan-trọng) ở đầu README). |
| 4 | Keys đã lưu trong Setup | **GameUp SDK → Setup** → điền tab cần dùng → **Save Configuration** ([Bước 3](#3-cấu-hình-qua-setup-window)). |
| 5 | SDK đã có trong scene khởi động | Trong Setup đã bấm **Tạo SDK trong Scene hiện tại**; scene đầu có prefab/SDK singleton, không trùng instance ([Bước 4](#4-thêm-sdk-vào-scene)). |
| 6 | (Tùy chọn) AdMob / AppsFlyer | Chỉ khi dùng App Open hoặc MMP: package tương ứng đã cài và tab đã cấu hình. |

Sau bước này, chuyển sang [API theo từng script](#6-cấu-trúc-assetsgameupsdkscripts-và-api).

---

## 6. Cấu trúc `Assets/GameUpSDK/Scripts` và API

Code runtime nằm dưới `Assets/GameUpSDK/Scripts`. Dùng bảng sau để biết file nào cung cấp API công khai (namespace `GameUpSDK`).

| Thư mục / file | Vai trò | API chính (dùng từ game) |
|----------------|---------|---------------------------|
| `Ads/AdsManager.cs` | Điều phối quảng cáo (singleton) | `ShowBanner`, `HideBanner`, `ShowInterstitial`, `ShowRewardedVideo`, `ShowAppOpenAds`, `SetAfterCheckGDPR` |
| `Ads/AdsRules.cs` | Rule capping / level (dùng nội bộ + Remote Config) | Thường không gọi trực tiếp; logic gắn với `AdsManager`. |
| `Ads/AdsEvent.cs` | Sự kiện impression / callback nội bộ | Subscribe nâng cao nếu cần; ad revenue thường đã nối sẵn. |
| `Analytics/GameUpAnalytics.cs` | Analytics thống nhất (Firebase, AppsFlyer, GameAnalytics progression) | `LogLevelStart`, `LogLevelComplete`, `LogStartLoading`, `LogPurchase`, … — xem [mục 8](#8-gameupanalytics--analytics). |
| `Analytics/AnalyticsEvent.cs` | Hằng tên sự kiện / param | Tham chiếu tên event chuẩn khi custom. |
| `Firebase/FirebaseUtils.cs` | Firebase App, Analytics, Crashlytics | `FirebaseUtils.Instance`, `LogEventsAPI`, `LogEvent`, `LogError`, `LogException` |
| `Firebase/FirebaseRemoteConfigUtils.cs` | Remote Config + field sync | `Instance`, đọc field (`inter_capping_time`, `enable_banner`, …), `OnFetchCompleted` — xem [mục 9](#9-firebaseutils--firebaseremoteconfigutils). |
| `AppsFlyerCheck/AppsFlyerUtils.cs` | Init AppsFlyer + event / ad revenue | `AppsFlyerUtils.LogEvents`, `LogAdRevenue` — xem [mục 10](#10-appsflyerutils). |
| `GameUtils.cs` | Tiện ích nền tảng | `IsAndroid()`, `IsIOS()`, `IsEditor()` |
| `MonoSingleton.cs` | Base singleton cho component | Dùng nội bộ; các manager kế thừa pattern này. |
| `MainThreadDispatcher.cs` | Đưa callback về main thread | Dùng nội bộ SDK. |
| `GUDefinetion.cs` | Tên define symbol (`GAMEUP_SDK_DEPS_READY`, v.v.) | Tham chiếu khi viết `#if`; không xóa / đổi tay các symbol do installer quản lý. |

`GameAnalyticsMirror.cs` là **internal** (reflection tới GameAnalytics SDK); không gọi từ game — dùng `GameUpAnalytics` khi cần progression lên GameAnalytics.

---

## 7. AdsManager – Quảng cáo

`AdsManager` là điểm trung tâm để hiển thị tất cả loại quảng cáo. Dùng **waterfall**: network có độ ưu tiên cao nhất (`OrderExecute` nhỏ nhất) và đang available sẽ được dùng.

### Banner

```csharp
// Hiện Banner
AdsManager.Instance.ShowBanner("main");

// Ẩn Banner
AdsManager.Instance.HideBanner("main");
```

**Kích thước Banner** được chọn qua field `Banner Size` trên component `AdsManager` trong Inspector:

| Giá trị | Kích thước | Ghi chú |
|---|---|---|
| `Banner` | 320 × 50 | Nhỏ, phổ biến nhất |
| `Large` | 320 × 90 | **Mặc định** – fill rate tốt |
| `Adaptive` | Full width × auto | Fill rate cao nhất – **IronSource khuyến nghị** |
| `Medium Rectangle` | 300 × 250 | MREC, thường dùng trong content |
| `Leaderboard` | 728 × 90 | Chỉ phù hợp iPad / tablet |

> Kích thước được áp dụng khi `Initialize()` – **không thay đổi được sau khi init**.  
> Banner luôn hiện ở vị trí **BottomCenter**.  
> Banner chỉ hiện khi `enable_banner = true` trên Firebase Remote Config.  
> `enable_banner` có ưu tiên cao hơn `showBannerAfterInit`: nếu Remote Config = `false` thì Banner không hiện dù Inspector = `true`.

### Interstitial

```csharp
// Hiện Interstitial (chỉ kiểm tra capping time, không kiểm tra level)
AdsManager.Instance.ShowInterstitial(
    where: "level_complete",
    onSuccess: () => { /* tiếp tục flow game */ },
    onFail: () => { /* tiếp tục flow game */ }
);

// Hiện Interstitial với kiểm tra level (khuyến nghị)
int currentLevel = 5;
AdsManager.Instance.ShowInterstitial(
    where: "level_complete",
    currentLevel: currentLevel,
    onSuccess: () => { /* tiếp tục flow game */ },
    onFail: () => { /* tiếp tục flow game */ }
);
```

> SDK tự động kiểm tra `inter_start_level` và `inter_capping_time` qua `AdsRules`.  
> `onSuccess` được gọi khi quảng cáo **đóng lại** bình thường.  
> `onFail` được gọi khi bị block bởi rule, không có quảng cáo, hoặc hiển thị lỗi.  
> **Luôn xử lý cả `onSuccess` và `onFail`** để tiếp tục flow game.

### Rewarded Video

```csharp
// Hiện Rewarded Video (không cần level)
AdsManager.Instance.ShowRewardedVideo(
    where: "revive",
    onSuccess: () => { /* trao thưởng cho người chơi */ },
    onFail: () => { /* không trao thưởng, người chơi bỏ qua */ }
);

// Truyền thêm currentLevel để log analytics đầy đủ hơn
AdsManager.Instance.ShowRewardedVideo(
    where: "revive",
    currentLevel: currentLevel,
    onSuccess: () => { /* trao thưởng */ },
    onFail: () => { /* không trao thưởng */ }
);
```

> `onSuccess` chỉ được gọi khi người chơi **xem đủ** để nhận reward (sự kiện `OnAdRewarded`).  
> `onFail` được gọi khi không có quảng cáo, lỗi hiển thị, hoặc người chơi **đóng sớm** trước khi nhận reward.

### App Open Ads

```csharp
AdsManager.Instance.ShowAppOpenAds(
    where: "app_foreground",
    onSuccess: () => { /* tiếp tục */ },
    onFail: () => { /* tiếp tục */ }
);
```

> App Open Ads chỉ khả dụng khi đã cài và cấu hình **AdMob**. IronSource/LevelPlay không hỗ trợ loại quảng cáo này.

### Tham số `where`

Chuỗi mô tả vị trí/ngữ cảnh hiển thị quảng cáo, dùng để tracking analytics (Firebase + AppsFlyer). Ví dụ: `"main_menu"`, `"level_complete"`, `"revive"`, `"settings"`.

### GDPR / Consent

```csharp
// Gọi sau khi người dùng hoàn tất consent flow
AdsManager.Instance.SetAfterCheckGDPR();
```

---

## 8. GameUpAnalytics – Analytics

`GameUpAnalytics` là static class, log event đến Firebase và/hoặc AppsFlyer.

### Lifecycle Loading

```csharp
// Gọi khi bắt đầu loading (màn hình splash/loading)
GameUpAnalytics.LogStartLoading();

// Gọi khi loading xong, vào màn hình home
GameUpAnalytics.LogCompleteLoading();
```

### Level Events

```csharp
// Bắt đầu level (level tính từ 1, index = số lần bắt đầu level này)
GameUpAnalytics.LogLevelStart(level: 1, index: 1);

// Thua level (index = số lần thua ở level này, timeSeconds = giây từ lúc bắt đầu đến khi thua)
GameUpAnalytics.LogLevelFail(level: 1, index: 1, timeSeconds: 45.5f);

// Thắng level → log Firebase (level_complete) + AppsFlyer (af_level_achieved)
GameUpAnalytics.LogLevelComplete(level: 1, index: 1, timeSeconds: 120f);

// Thắng level kèm điểm (score dùng cho AppsFlyer af_score)
GameUpAnalytics.LogLevelComplete(level: 1, index: 1, timeSeconds: 120f, score: 5000);
```

### Wave Events (game có chia wave)

```csharp
GameUpAnalytics.LogWaveStart(level: 3, wave: 1);
GameUpAnalytics.LogWaveFail(level: 3, wave: 1);
GameUpAnalytics.LogWaveComplete(level: 3, wave: 1);
```

### Tutorial (Level 1)

```csharp
// Dùng khi game coi level 1 là tutorial
GameUpAnalytics.LogStartLevel1();
GameUpAnalytics.LogCompleteLevel1();
```

### Virtual Currency

```csharp
// Nhận tiền ảo (earn_virtual_currency)
GameUpAnalytics.LogEarnVirtualCurrency(
    virtualCurrencyName: "coin",
    value: "100",
    source: "level_complete"
);

// Tiêu tiền ảo (spend_virtual_currency)
GameUpAnalytics.LogSpendVirtualCurrency(
    virtualCurrencyName: "coin",
    value: "50",
    source: "buy_revive"
);
```

### Button Click

```csharp
// source = tên button kèm vị trí để phân biệt
GameUpAnalytics.LogButtonClick("btn_play_home");
GameUpAnalytics.LogButtonClick("btn_revive_popup");
```

### AppsFlyer – Registration, Purchase, Achievement

```csharp
// Đăng ký tài khoản (af_complete_registration)
GameUpAnalytics.LogCompleteRegistration("Facebook");

// Hoàn thành tutorial (af_tutorial_completion)
GameUpAnalytics.LogTutorialCompletion(success: true, tutorialId: "intro");

// Mua hàng trong app (af_purchase)
GameUpAnalytics.LogPurchase(
    currencyCode: "USD",
    quantity: 1,
    contentId: "no_ads_pack",
    purchasePrice: "1.99",   // giá USD quy đổi (localized price * 0.63)
    orderId: "order_123"
);

// Mở khóa thành tích (af_achievement_unlocked)
GameUpAnalytics.LogAchievementUnlocked("first_win", level: 1);
```

### Ad Revenue (tự động)

Ad Revenue được log tự động đến cả Firebase và AppsFlyer thông qua `AdsEvent.OnImpressionDataReady`. **Không cần gọi thủ công** — `AdsManager` đã subscribe event này khi khởi tạo.

---

## 9. FirebaseUtils & FirebaseRemoteConfigUtils

### FirebaseUtils (`Firebase/FirebaseUtils.cs`)

Singleton `FirebaseUtils.Instance` khởi tạo Firebase (Analytics + Crashlytics) trên thiết bị; trong Editor chỉ log ra Console.

```csharp
// Sau khi Firebase sẵn sàng (tùy chọn — thường Remote Config đã chờ sẵn)
FirebaseUtils.Instance.onInitialized += ok => { /* ... */ };
bool ready = FirebaseUtils.Instance.IsInitialized;

// Gửi event Firebase Analytics (param tùy chọn)
FirebaseUtils.LogEventsAPI("custom_event", new Dictionary<object, object> { ["key"] = "value" });
FirebaseUtils.LogEvent("custom_event", "param_name", "param_value");

// Crashlytics
FirebaseUtils.Instance.LogError("message");
FirebaseUtils.Instance.LogException(exception);
```

### FirebaseRemoteConfigUtils (`Firebase/FirebaseRemoteConfigUtils.cs`)

`FirebaseRemoteConfigUtils` tự động fetch và sync giá trị từ Firebase Remote Config vào các public field cùng tên (via reflection). Không cần gọi thủ công trong luồng bình thường.

### Đọc giá trị

```csharp
var rc = FirebaseRemoteConfigUtils.Instance;

int cappingTime    = rc.inter_capping_time;           // capping Interstitial (giây)
int startLevel     = rc.inter_start_level;            // level bắt đầu hiện Interstitial
bool bannerOn      = rc.enable_banner;                // Banner bật/tắt
bool rateAppOn     = rc.enable_rate_app;              // Rate App bật/tắt
int rateLevel      = rc.level_start_show_rate_app;    // Level hiện Rate App
bool internetPopup = rc.no_internet_popup_enable;     // Popup no-internet bật/tắt
```

### Chờ Remote Config sẵn sàng

```csharp
void Start()
{
    var rc = FirebaseRemoteConfigUtils.Instance;

    if (rc.IsRemoteConfigReady)
    {
        OnConfigReady(true);
    }
    else
    {
        rc.OnFetchCompleted += OnConfigReady;
    }
}

void OnConfigReady(bool activated)
{
    // activated = true: fetch thành công, dữ liệu đã được cập nhật từ server
    // activated = false: dùng giá trị mặc định (Firebase lỗi hoặc không có gì mới)
    Debug.Log("Remote Config ready. Banner: " + FirebaseRemoteConfigUtils.Instance.enable_banner);
}
```

### Fetch lại thủ công (tùy chọn)

```csharp
FirebaseRemoteConfigUtils.Instance.FetchAndActivate(ok =>
{
    Debug.Log("Fetch result: " + ok);
});
```

> **Trong Unity Editor**: Remote Config luôn dùng giá trị mặc định (không kết nối Firebase) để thuận tiện khi test.

---

## 10. AppsFlyerUtils

File: `AppsFlyerCheck/AppsFlyerUtils.cs`. Keys (`sdkKey`, `appId`, `isDevMode`) được gán từ **GameUp SDK → Setup** vào prefab; runtime tự `initSDK` / `startSDK` khi có package AppsFlyer.

```csharp
using System.Collections.Generic;
#if APPSFLYER_DEPENDENCIES_INSTALLED
using AppsFlyerSDK;
#endif

// Event tùy chỉnh (MMP)
AppsFlyerUtils.LogEvents("my_event", new Dictionary<string, string> { ["key"] = "value" });

// Ad revenue (khi tự tích hợp ngoài luồng mặc định của AdsManager)
#if APPSFLYER_DEPENDENCIES_INSTALLED
AppsFlyerUtils.LogAdRevenue(
    monetizationNetwork: "ironsource",
    mediationNetwork: MediationNetwork.IronSource,
    eventRevenue: 0.01,
    revenueCurrency: "USD"
);
#endif
```

`GameUpAnalytics` đã gọi `AppsFlyerUtils.LogEvents` cho nhiều event chuẩn; chỉ cần dùng trực tiếp `AppsFlyerUtils` khi gửi event / revenue tùy biến.

---

## 11. Remote Config Keys Reference

Các key phải đặt **đúng tên** trên Firebase Remote Config console để SDK tự động sync (tên biến = tên key):

| Key | Type | Default | Mô tả |
|---|---|---|---|
| `inter_capping_time` | Number (int) | `120` | Thời gian tối thiểu (giây) giữa 2 lần hiện Interstitial |
| `inter_start_level` | Number (int) | `3` | Level tối thiểu để hiện Interstitial (tính từ 1) |
| `enable_banner` | Boolean | `true` | `false` = tắt Banner trên toàn bộ game |
| `enable_rate_app` | Boolean | `false` | `true` = hiện popup Rate App |
| `level_start_show_rate_app` | Number (int) | `5` | Level bắt đầu hiện Rate App |
| `no_internet_popup_enable` | Boolean | `true` | `true` = hiện popup yêu cầu kết nối Internet |

> **Ưu tiên `enable_banner`**: Remote Config `enable_banner = false` sẽ tắt hoàn toàn Banner, kể cả khi `showBannerAfterInit = true` trong Inspector của `AdsManager`.

---

## Ghi chú

- **Android**: Luôn giữ **Min API 24** và **Target API ≤ 36** trước khi build store (xem bảng yêu cầu ở đầu file).
- **Namespace**: Tất cả class đều trong namespace `GameUpSDK`.
- **Singleton**: `AdsManager`, `FirebaseRemoteConfigUtils` là `MonoSingleton` – truy cập qua `.Instance`.
- **Define symbol**: `GAMEUP_SDK_DEPS_READY` được tự động quản lý bởi installer. Không thêm/xóa thủ công.
- **Test capping**: Dùng `AdsManager.Instance.ResetInterstitialCappingForTest()` để reset timer capping khi test – **không dùng trong production**.
- **GDPR**: Sau khi hoàn tất consent flow, gọi `AdsManager.Instance.SetAfterCheckGDPR()` để forward thông tin đến ad network.
