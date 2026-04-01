# GameUp SDK

SDK tích hợp Quảng cáo (Ads) và Analytics cho game Unity, hỗ trợ:

- **Ads**: IronSource/LevelPlay Mediation (Banner, Interstitial, Rewarded Video) + AdMob (App Open)
- **Analytics**: Firebase Analytics + AppsFlyer MMP; tùy chọn **GameAnalytics** (progression level/wave, design events) qua installer
- **Remote Config**: Firebase Remote Config với auto-sync

**Unity**: 2022.3+  |  **Package**: `com.ohze.gameup.sdk`

---

## Mục lục

1. [Cài đặt](#1-cài-đặt)
2. [Cài đặt Dependencies](#2-cài-đặt-dependencies)
3. [Cấu hình qua Setup Window](#3-cấu-hình-qua-setup-window)
4. [Thêm SDK vào Scene](#4-thêm-sdk-vào-scene)
5. [AdsManager – Quảng cáo](#5-adsmanager--quảng-cáo)
6. [GameUpAnalytics – Analytics](#6-gameupanalytics--analytics)
7. [FirebaseRemoteConfigUtils – Remote Config](#7-firebaseremoteconfigutils--remote-config)
8. [Remote Config Keys Reference](#8-remote-config-keys-reference)

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
| GameAnalytics SDK | 7.10.6 (unitypackage) | Tùy chọn — progression / funnels; cài qua installer hoặc [tài liệu GA Unity](https://docs.gameanalytics.com/event-tracking-and-integrations/sdks-and-collection-api/game-engine-sdks/unity/) |

### GameAnalytics — Installer (tùy chọn)

Logic cài đặt và đồng bộ define nằm trong `Assets/GameUpSDK/Editor/Installer` (`GameUpDependenciesWindow`, `GameUpDefineSymbolsAutoSync`).

- **Cài package**: trong **GameUp SDK → Setup Dependencies**, GameAnalytics nằm nhóm **TÙY CHỌN**. Nút **⬇ Cài dependency theo Primary Mediation** sẽ cố gắng cài cả GameAnalytics cùng Firebase, AppsFlyer và bộ mediation đã chọn (nếu chưa có). Mỗi package có file `.unitypackage` trong `Packages~/` của SDK hoặc **Hosted URL** tải về (GameAnalytics: `GA_SDK_UNITY.unitypackage` từ CDN GameAnalytics, bản tương ứng **7.10.6**).
- **Phát hiện đã cài**: coi là đã có SDK khi load được assembly **`GameAnalyticsSDK`** *hoặc* type **`GameAnalyticsSDK.GameAnalytics`** trong bất kỳ assembly nào (hỗ trợ bản import `.unitypackage` cổ điển đặt script trong `Assembly-CSharp`).
- **Define `GAMEANALYTICS_DEPENDENCIES_INSTALLED`**: được **bật/tắt tự động** theo trạng thái trên (Android / iOS / Standalone). Khi **bật**, `GameUpAnalytics` mới gửi dữ liệu lên GameAnalytics (progression + design event tiền tố `gameup:`); khi tắt, lớp wrapper GA là no-op.
- **Assembly definition**: `GameUpSDK.Runtime` tham chiếu assembly tên `GameAnalyticsSDK`. Nếu bạn import GA bằng `.unitypackage` cũ **không** có `GameAnalyticsSDK.asmdef`, dùng menu **GameUp SDK → Ensure GameAnalytics runtime asmdef** để tạo `Assets/GameAnalytics/Plugins/GameAnalyticsSDK.asmdef` (auto-sync cũng thử bước này khi compile).
- **Cấu hình key & scene**: tạo **GameObject GameAnalytics** (prefab/SDK) và điền Game Key / Secret Key — có thể dùng **Window → GameAnalytics** hoặc tab **Game Analytics** trong **GameUp SDK → Setup** (asset `Assets/Resources/GameAnalytics/Settings.asset`).

Đồng bộ define thủ công bất cứ lúc nào: **GameUp SDK → Sync Define Symbols**.

### Cách cài trong cửa sổ Setup Dependencies

- Chọn **Primary Mediation** (LevelPlay hoặc AdMob), rồi dùng **⬇ Cài dependency theo Primary Mediation** để tải/import gói còn thiếu (Firebase, AppsFlyer, GameAnalytics, Google Mobile Ads hoặc LevelPlay, v.v. — từng mục đã có thì bỏ qua). Gói có file trong `Packages~/` của SDK sẽ import ngay; gói chỉ có URL sẽ được tải rồi import.
- Nhấn **↻ Làm mới trạng thái** sau khi import tay từ trang nhà cung cấp.
- **Scripting Define Symbols** (gồm `GAMEANALYTICS_DEPENDENCIES_INSTALLED`, `GAMEUP_SDK_DEPS_READY`, …) được cập nhật khi mở cửa sổ / sau compile — hoặc chạy **GameUp SDK → Sync Define Symbols**.
- Khi điều kiện bật SDK thỏa (ít nhất một analytics + một mediation), nút **→ Mở cấu hình SDK** xuất hiện → nhấn để sang **GameUp SDK → Setup**.

> **Quan trọng**: `GAMEUP_SDK_DEPS_READY` được installer đặt khi có **ít nhất một** analytics (Firebase, AppsFlyer hoặc GameAnalytics) **và** có mediation (AdMob hoặc LevelPlay). Đây là cờ backward-compat “SDK enabled”; logic no-op cụ thể của từng tính năng có thể dùng thêm define từng nhà cung cấp (ví dụ `GAMEANALYTICS_DEPENDENCIES_INSTALLED` cho GameAnalytics).

### Menu items liên quan

| Menu | Chức năng |
|---|---|
| **GameUp SDK → Setup Dependencies** | Mở cửa sổ cài đặt package phụ thuộc |
| **GameUp SDK → Setup** | Mở cửa sổ cấu hình keys |
| **GameUp SDK → Reset Setup Status** | Reset trạng thái để mở lại installer vào lần load tiếp theo |
| **GameUp SDK → Ensure GameAnalytics runtime asmdef** | Tạo `GameAnalyticsSDK.asmdef` khi GA import thiếu asmdef (bản `.unitypackage` cũ) |
| **GameUp SDK → Sync Define Symbols** | Đồng bộ mọi define dependency (Firebase, AppsFlyer, GameAnalytics, mediation, `GAMEUP_SDK_DEPS_READY`) theo package thực tế |

---

## 3. Cấu hình qua Setup Window

Mở **GameUp SDK → Setup** để cấu hình toàn bộ SDK trong một cửa sổ duy nhất. Window gồm các tab: AppsFlyer, IronSource Mediation, AdMob (App Open), Game Analytics, Firebase Remote Config.

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

### Tab: Game Analytics

Cấu hình Game Key / Secret Key / Build trên asset `Assets/Resources/GameAnalytics/Settings.asset`. Chỉ hiệu lực khi package GameAnalytics đã được import và class `GameAnalyticsSDK.Setup.Settings` tồn tại.

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

## 5. AdsManager – Quảng cáo

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

## 6. GameUpAnalytics – Analytics

`GameUpAnalytics` là static class, log event đến Firebase và/hoặc AppsFlyer. Khi **`GAMEANALYTICS_DEPENDENCIES_INSTALLED`** được bật (sau khi cài GameAnalytics — xem [GameAnalytics — Installer](#gameanalytics--installer-tùy-chọn)), cùng API còn mirror **progression** và **design events** lên GameAnalytics (hierarchy cố định world `main` → số level → segment wave `w{n}`, theo [GA Unity — Progression](https://docs.gameanalytics.com/event-tracking-and-integrations/sdks-and-collection-api/game-engine-sdks/unity/event-tracking)).

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

## 7. FirebaseRemoteConfigUtils – Remote Config

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

## 8. Remote Config Keys Reference

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

- **Namespace**: Tất cả class đều trong namespace `GameUpSDK`.
- **Singleton**: `AdsManager`, `FirebaseRemoteConfigUtils` là `MonoSingleton` – truy cập qua `.Instance`.
- **Define symbols**: `GAMEANALYTICS_DEPENDENCIES_INSTALLED` và các define từng nhà cung cấp được đồng bộ bởi **Setup Dependencies** / **Sync Define Symbols** / auto-sync sau compile. `GAMEUP_SDK_DEPS_READY` bật khi có ít nhất một analytics (Firebase, AppsFlyer hoặc GameAnalytics) **và** có mediation (AdMob hoặc LevelPlay) — tránh chỉnh tay trừ khi bạn biết rõ hệ quả.
- **Test capping**: Dùng `AdsManager.Instance.ResetInterstitialCappingForTest()` để reset timer capping khi test – **không dùng trong production**.
- **GDPR**: Sau khi hoàn tất consent flow, gọi `AdsManager.Instance.SetAfterCheckGDPR()` để forward thông tin đến ad network.
