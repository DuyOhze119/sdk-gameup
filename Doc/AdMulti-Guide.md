# Hướng dẫn Ads (AdMulti + mode bình thường)

Tài liệu này hướng dẫn cách dùng Ads trong `Assets/GameUpSDK`, gồm cả:

- Mode **AdMulti** (`useMultiAdUnitIds = true`)
- Mode **bình thường / Single IDs** (`useMultiAdUnitIds = false`)
- Cách gọi quảng cáo theo `id` và theo `string`
- Giới thiệu file test `Assets/GameUpSDK/Scripts/Ads/AdsTester.cs`
- Tổng quan thư mục `Assets/GameUpSDK`

---

## 1) AdMulti là gì?

Trong GameUp SDK, AdMulti là cách map nhiều ad unit/placement theo từng vị trí hiển thị (`where`) bằng `AdUnitIdEntry`.

`AdUnitIdEntry` có các trường chính:

- `adType`: loại ads (`Banner`, `Interstitial`, `RewardedVideo`, `AppOpen`)
- `intId`: id số để gọi nhanh qua `ShowById(...)`
- `nameId`: tên placement (được dùng như tham số `where`)
- `id`: ad unit id / placement id của network

Khi bật `useMultiAdUnitIds`, SDK sẽ ưu tiên map theo danh sách `adUnitIds`.

---

## 2) Setup AdMulti

### Cách 1: Setup qua cửa sổ GameUp

1. Mở `GameUp SDK -> Setup`.
2. Vào tab mediation tương ứng:
   - **IronSource/LevelPlay**
   - **AdMob**
3. Bật `Use Multi IDs`.
4. Thêm các dòng `Ad Unit ID list (Multi)` với format:
   - `IntId`
   - `AdType`
   - `NameId` (chính là `where`)
   - `Id` (ad unit id / placement id thật)
5. `Save Configuration`.

### Cách 2: Setup trực tiếp trong Inspector

- Trên component `AdmobAds` hoặc `IronSourceAds`:
  - bật `useMultiAdUnitIds`
  - điền danh sách `adUnitIds`

> Lưu ý:
> - `nameId` nên đặt theo ngữ cảnh dễ đọc: `main`, `level_end`, `revive`, ...
> - `intId` nên unique trong cùng nguồn cấu hình để tránh resolve nhầm.
> - Với LevelPlay, App Open không được hỗ trợ native trong `IronSourceAds`.

---

## 3) Setup mode bình thường (Single IDs)

Mode này dùng 1 ID cho mỗi format, không cần map `adUnitIds`.

### Setup qua GameUp Setup

1. Mở `GameUp SDK -> Setup`.
2. Ở tab mediation (AdMob hoặc IronSource/LevelPlay), tắt `Use Multi IDs`.
3. Điền các field single ID:
   - AdMob: `Banner`, `Interstitial`, `Rewarded`, `App Open`
   - LevelPlay: `Banner`, `Interstitial`, `Rewarded`
4. `Save Configuration`.

### Setup qua Inspector

- Trên `AdmobAds`/`IronSourceAds`:
  - tắt `useMultiAdUnitIds`
  - điền các field ID đơn tương ứng.

> Lưu ý:
> - Với mode bình thường, placement `where` vẫn được truyền khi gọi API để log/event routing.
> - `ShowById(...)` không phù hợp cho mode này vì không có mapping `intId` trong `adUnitIds`.

---

## 4) Cách gọi quảng cáo theo `string` (where)

Đây là cách gọi phổ biến nhất:

```csharp
AdsManager.Instance.ShowBanner("main");

AdsManager.Instance.ShowInterstitial(
    "level_end",
    currentLevel: 10,
    onSuccess: () => Debug.Log("Inter success"),
    onFail: () => Debug.LogWarning("Inter fail")
);

AdsManager.Instance.ShowRewardedVideo(
    "revive",
    currentLevel: 10,
    onSuccess: () => Debug.Log("Reward success"),
    onFail: () => Debug.LogWarning("Reward fail")
);

AdsManager.Instance.ShowAppOpenAds("main");
```

Khi gọi theo `string`, SDK sẽ chọn network available theo cơ chế waterfall trong `AdsManager`.
Áp dụng được cho cả **AdMulti** và **mode bình thường**.

---

## 5) Cách gọi quảng cáo theo `id`

Có 2 kiểu gọi theo id:

### 5.1 `ShowById(intId, ...)` (khuyến nghị cho AdMulti)

```csharp
AdsManager.Instance.ShowById(
    intId: 101,
    currentLevel: 12,
    onSuccess: () => Debug.Log("ShowById success"),
    onFail: () => Debug.LogWarning("ShowById fail")
);
```

- `ShowById` sẽ resolve `intId -> (adType + nameId)` từ `adUnitIds`.
- Sau đó tự điều hướng đến API phù hợp (`ShowBanner`, `ShowInterstitial`, `ShowRewardedVideo`, `ShowAppOpenAds`).
- Nếu không resolve được `intId`, SDK gọi `onFail`.
- Trong **mode bình thường**, API này thường fail do không có mapping `adUnitIds`.

### 5.2 Overload `int whereId` (chuyển sang string)

Ví dụ:

```csharp
AdsManager.Instance.ShowInterstitial(5, onSuccess: ..., onFail: ...);
AdsManager.Instance.ShowRewardedVideo(7, onSuccess: ..., onFail: ...);
AdsManager.Instance.ShowBanner(1);
```

Các overload này chỉ đổi `whereId.ToString()` thành `where` string, **không** resolve theo `adUnitIds.intId` như `ShowById`.
Áp dụng được cho cả AdMulti và mode bình thường.

---

## 6) Giới thiệu `Assets/GameUpSDK/Scripts/Ads/AdsTester.cs`

`AdsTester` là tool runtime để test nhanh luồng ads bằng UI `OnGUI`.

Các điểm chính:

- Có 2 mode:
  - `Single`: test 1 item
  - `Multi`: test danh sách item
- Mỗi `AdsTestItem` hỗ trợ:
  - gọi theo `intId` (`useIntId = true` -> `ShowById`)
  - hoặc gọi theo `adType + where` (`useIntId = false`)
- Có nút tiện ích:
  - `RequestAll`
  - `Reset Interstitial Capping`
  - `Auto Build Items From Ads Config`
- Tự đọc config từ `AdmobAds`/`IronSourceAds` để build danh sách test, rất phù hợp khi QA flow AdMulti.
- Khi source đang ở mode bình thường, `AdsTester` tự chuyển về `Single` và build item theo các field single ID đang có.

Gợi ý dùng:

1. Add `AdsTester` vào scene test.
2. Bật `autoBuildMultiItemsOnStart`.
3. Chạy game và bấm từng nút để kiểm tra placement / callback.

---

## 7) Giới thiệu nhanh `Assets/GameUpSDK`

`Assets/GameUpSDK` là package chính của SDK:

- `Scripts/Ads`: toàn bộ logic Ads (`AdsManager`, `AdmobAds`, `IronSourceAds`, `AdsRules`, `AdsTester`, interfaces)
- `Scripts/Analytics`: wrapper analytics (Firebase / AppsFlyer / GameAnalytics)
- `Scripts/Firebase`: util Firebase, remote config
- `Editor`: công cụ setup dependency và cấu hình SDK trong Unity Editor
- `Prefab`: prefab sẵn cho SDK và ad behaviours
- `package.json`: metadata package (`com.ohze.gameup.sdk`)

Điểm bắt đầu quan trọng cho Ads:

- `Assets/GameUpSDK/Scripts/Ads/AdsManager.cs` (entrypoint gọi ads)
- `Assets/GameUpSDK/Scripts/Ads/IAds.cs` (contract + `AdUnitIdEntry`)
- `Assets/GameUpSDK/Scripts/Ads/AdsTester.cs` (test nhanh runtime)

---

## 8) Checklist nhanh trước khi gọi API

- Đã cài dependencies và không còn lỗi compile.
- Đã cấu hình mediation trong `GameUp SDK -> Setup`.
- Đã thêm SDK prefab/object vào scene đầu.
- Nếu dùng AdMulti: đã bật `useMultiAdUnitIds` và điền `adUnitIds` hợp lệ.
- Nếu dùng mode bình thường: đã tắt `useMultiAdUnitIds` và điền đủ single IDs.
- Nếu gọi `ShowById`: đảm bảo `intId` tồn tại trong `adUnitIds` (chủ yếu cho AdMulti).

