# GameUp SDK

SDK tích hợp Quảng cáo (Ads) và Analytics cho game Unity, hỗ trợ:

- **Ads**: IronSource/LevelPlay Mediation (Banner, Interstitial, Rewarded Video) + AdMob (App Open)
- **Analytics**: Firebase Analytics + AppsFlyer MMP
- **Remote Config**: Firebase Remote Config với auto-sync

**Unity**: 2022.3+  |  **Package**: `com.ohze.gameup.sdk`

---

## Mục lục

1. [Cài đặt](#1-cài-đặt)
2. [Cấu hình qua Setup Window](#2-cấu-hình-qua-setup-window)
3. [Thêm SDK vào Scene](#3-thêm-sdk-vào-scene)
4. [AdsManager – Quảng cáo](#4-adsmanager--quảng-cáo)
5. [GameUpAnalytics – Analytics](#5-gameupanalytics--analytics)
6. [FirebaseRemoteConfigUtils – Remote Config](#6-firebaseremoteconfigutils--remote-config)
7. [Remote Config Keys Reference](#7-remote-config-keys-reference)

---

## 1. Cài đặt

### Cài qua UPM (khuyến nghị)

Mở **Package Manager** → **Add package from git URL**:

```
https://github.com/DuyOhze119/sdk-gameup.git?path=Assets/GameUpSDK#main
```

### Cài thủ công

Copy thư mục `Assets/GameUpSDK` vào project.

### Dependencies cần cài thêm

SDK yêu cầu các package sau (cài thủ công hoặc theo hướng dẫn từ nhà cung cấp):

| Package | Ghi chú |
|---|---|
| Firebase SDK (Analytics, Remote Config) | Tải từ [Firebase Unity SDK](https://firebase.google.com/docs/unity/setup) |
| IronSource / LevelPlay | Tải từ Unity Asset Store hoặc dashboard LevelPlay |
| Google Mobile Ads (AdMob) | Cài qua UPM hoặc `.unitypackage` |
| AppsFlyer SDK | Tải từ [AppsFlyer Unity SDK](https://dev.appsflyer.com/hc/docs/unity-sdk-overview) |

---

## 2. Cấu hình qua Setup Window

Mở **GameUp SDK → Setup** trên thanh menu để cấu hình toàn bộ SDK trong một cửa sổ duy nhất.

Window có 4 tab:

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
| Banner / Interstitial / Rewarded ID | Ad Unit ID (để trống = dùng Default Placement) |
| Android/iOS App Key | App Key điền vào `LevelPlayMediationSettings` |

### Tab: AdMob (App Open)

AdMob chỉ dùng cho **App Open Ads**. Banner/Interstitial/Rewarded đi qua IronSource Mediation.

| Trường | Mô tả |
|---|---|
| App Open ID | Ad Unit ID cho App Open |
| Android/iOS App ID | Google Mobile Ads App ID |

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

## 3. Thêm SDK vào Scene

Sau khi Save Configuration, bấm **"Tạo SDK trong Scene hiện tại"** trong Setup Window.

SDK sẽ được khởi tạo tự động từ prefab `SDK.prefab` (Singleton, không bị destroy khi chuyển scene).

> **Lưu ý**: Chỉ cần thêm `SDK.prefab` vào scene đầu tiên (Splash/Loading). Không thêm lại ở các scene khác.

---

## 4. AdsManager – Quảng cáo

`AdsManager` là điểm trung tâm để hiển thị tất cả loại quảng cáo. Dùng waterfall: network đầu tiên available sẽ được dùng.

### Banner

```csharp
// Hiện Banner tại vị trí "main" (tự động hiện sau init nếu showBannerAfterInit = true)
AdsManager.Instance.ShowBanner("main");

// Ẩn Banner
AdsManager.Instance.HideBanner("main");
```

> Banner chỉ hiện khi `enable_banner = true` trên Firebase Remote Config.

### Interstitial

```csharp
// Hiện Interstitial (không kiểm tra level, chỉ kiểm tra capping time)
AdsManager.Instance.ShowInterstitial(
    where: "level_complete",
    onSuccess: () => Debug.Log("Interstitial hiện thành công"),
    onFail: () => Debug.Log("Interstitial không hiện được")
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

> SDK tự động kiểm tra `inter_start_level` và `inter_capping_time` từ Remote Config thông qua `AdsRules`.  
> `onFail` được gọi cả khi bị block bởi rule và khi không có quảng cáo.

### Rewarded Video

```csharp
// Hiện Rewarded Video (không cần level)
AdsManager.Instance.ShowRewardedVideo(
    where: "revive",
    onSuccess: () => { /* trao thưởng cho người chơi */ },
    onFail: () => { /* người chơi bỏ qua hoặc không có ads */ }
);

// Truyền thêm currentLevel để log analytics
AdsManager.Instance.ShowRewardedVideo(
    where: "revive",
    currentLevel: currentLevel,
    onSuccess: () => { /* trao thưởng */ },
    onFail: () => { /* không trao thưởng */ }
);
```

> `onSuccess` chỉ được gọi khi người chơi **xem hết** video.  
> `onFail` được gọi khi không có quảng cáo hoặc người chơi **bỏ qua** giữa chừng.

### App Open Ads

```csharp
AdsManager.Instance.ShowAppOpenAds(
    where: "app_foreground",
    onSuccess: () => { /* tiếp tục */ },
    onFail: () => { /* tiếp tục */ }
);
```

### Tham số `where`

`where` là chuỗi mô tả vị trí/ngữ cảnh hiển thị quảng cáo, dùng để tracking analytics. Ví dụ: `"main_menu"`, `"level_complete"`, `"revive"`, `"settings"`.

---

## 5. GameUpAnalytics – Analytics

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

// Thua level (index = số lần thua ở level này, time = giây từ lúc bắt đầu)
GameUpAnalytics.LogLevelFail(level: 1, index: 1, timeSeconds: 45.5f);

// Thắng level (log Firebase + AppsFlyer af_level_achieved)
GameUpAnalytics.LogLevelComplete(level: 1, index: 1, timeSeconds: 120f);

// Thắng level kèm điểm
GameUpAnalytics.LogLevelComplete(level: 1, index: 1, timeSeconds: 120f, score: 5000);
```

### Wave Events (game có chia wave)

```csharp
GameUpAnalytics.LogWaveStart(level: 3, wave: 1);
GameUpAnalytics.LogWaveFail(level: 3, wave: 1);
GameUpAnalytics.LogWaveComplete(level: 3, wave: 1);
```

### Tutorial

```csharp
// Level 1 – dùng khi game coi level 1 là tutorial
GameUpAnalytics.LogStartLevel1();
GameUpAnalytics.LogCompleteLevel1();
```

### Virtual Currency

```csharp
// Nhận tiền ảo
GameUpAnalytics.LogEarnVirtualCurrency(
    virtualCurrencyName: "coin",
    value: "100",
    source: "level_complete"
);

// Tiêu tiền ảo
GameUpAnalytics.LogSpendVirtualCurrency(
    virtualCurrencyName: "coin",
    value: "50",
    source: "buy_revive"
);
```

### Button Click

```csharp
// Tracking button click (source = tên button kèm vị trí)
GameUpAnalytics.LogButtonClick("btn_play_home");
GameUpAnalytics.LogButtonClick("btn_revive_popup");
```

### AppsFlyer – Registration & Purchase

```csharp
// Đăng ký tài khoản (AppsFlyer af_complete_registration)
GameUpAnalytics.LogCompleteRegistration("Facebook");

// Tutorial (AppsFlyer af_tutorial_completion)
GameUpAnalytics.LogTutorialCompletion(success: true, tutorialId: "intro");

// Mua hàng trong app (AppsFlyer af_purchase)
GameUpAnalytics.LogPurchase(
    currencyCode: "USD",
    quantity: 1,
    contentId: "no_ads_pack",
    purchasePrice: "1.99",   // giá đã quy đổi (localized * 0.63)
    orderId: "order_123"
);

// Mở khóa thành tích (AppsFlyer af_achievement_unlocked)
GameUpAnalytics.LogAchievementUnlocked("first_win", level: 1);
```

### Ad Revenue (tự động)

Ad Revenue được log tự động thông qua `AdsEvent.OnImpressionDataReady`. **Không cần gọi thủ công** `LogAdImpression` – `AdsManager` đã subscribe event này khi khởi tạo.

---

## 6. FirebaseRemoteConfigUtils – Remote Config

`FirebaseRemoteConfigUtils` tự động fetch và sync giá trị từ Firebase Remote Config vào các public field cùng tên. Không cần gọi thủ công trong luồng bình thường.

### Đọc giá trị Remote Config

```csharp
// Truy cập trực tiếp qua Instance
var rc = FirebaseRemoteConfigUtils.Instance;

int cappingTime = rc.inter_capping_time;      // thời gian capping Interstitial (giây)
int startLevel  = rc.inter_start_level;       // level bắt đầu hiện Interstitial
bool bannerOn   = rc.enable_banner;           // Banner có được bật không
bool rateAppOn  = rc.enable_rate_app;         // Rate App có được bật không
int rateLevel   = rc.level_start_show_rate_app; // Level hiện Rate App
bool internetPopup = rc.no_internet_popup_enable; // Popup no-internet
```

### Chờ Remote Config sẵn sàng

```csharp
void Start()
{
    var rc = FirebaseRemoteConfigUtils.Instance;

    if (rc.IsRemoteConfigReady)
    {
        OnRemoteConfigReady(true);
    }
    else
    {
        rc.OnFetchCompleted += OnRemoteConfigReady;
    }
}

void OnRemoteConfigReady(bool activated)
{
    // activated = true: fetch thành công và có dữ liệu mới
    // activated = false: dùng giá trị default (Firebase lỗi hoặc không đổi)
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

> **Editor**: Remote Config luôn dùng giá trị default (không kết nối Firebase) để thuận tiện khi test trong Editor.

---

## 7. Remote Config Keys Reference

Các key này phải đặt **đúng tên** trên Firebase Remote Config console để SDK tự động sync:

| Key | Type | Default | Mô tả |
|---|---|---|---|
| `inter_capping_time` | Number (int) | `120` | Thời gian tối thiểu (giây) giữa 2 lần hiện Interstitial |
| `inter_start_level` | Number (int) | `3` | Level tối thiểu để hiện Interstitial (tính từ 1) |
| `enable_banner` | Boolean | `true` | `false` = tắt Banner trên toàn bộ game |
| `enable_rate_app` | Boolean | `false` | `true` = hiện popup Rate App |
| `level_start_show_rate_app` | Number (int) | `5` | Level bắt đầu hiện Rate App |
| `no_internet_popup_enable` | Boolean | `true` | `true` = hiện popup yêu cầu kết nối Internet |

> **Lưu ý về ưu tiên**: `enable_banner` (Remote Config) ưu tiên cao hơn `showBannerAfterInit` trong Inspector. Nếu `enable_banner = false` thì Banner không hiện dù `showBannerAfterInit = true`.

---

## Ghi chú

- **Namespace**: Tất cả class đều nằm trong namespace `GameUpSDK`.
- **Singleton**: `AdsManager`, `FirebaseRemoteConfigUtils` là `MonoSingleton` – truy cập qua `.Instance`.
- **Test capping**: Dùng `AdsManager.Instance.ResetInterstitialCappingForTest()` để reset timer capping khi test – **không dùng trong production**.
- **GDPR**: Sau khi hoàn tất consent flow, gọi `AdsManager.Instance.SetAfterCheckGDPR()` để forward thông tin đến các ad network.
