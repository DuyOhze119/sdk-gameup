# Ads Cheat Sheet (AdMulti + Single mode)

Dùng nhanh cho dev mới khi gọi ads với `AdsManager`, cho cả:

- **AdMulti** (`useMultiAdUnitIds = true`)
- **Mode bình thường / Single IDs** (`useMultiAdUnitIds = false`)

---

## 0) Khi nào dùng API nào (nhìn nhanh)

| Trường hợp | API nên dùng | Ghi chú |
|---|---|---|
| Gọi theo placement rõ ràng (`main`, `revive`, `level_end`) | `ShowBanner(string where)` / `ShowInterstitial(string where, ...)` / `ShowRewardedVideo(string where, ...)` / `ShowAppOpenAds(string where, ...)` | Dùng cho cả AdMulti và Single mode |
| Dùng mapping id số từ config AdMulti | `ShowById(intId, ...)` | Resolve từ `adUnitIds` -> `adType + nameId` |
| Chỉ có số nhưng muốn dùng như placement text | `ShowXxx(int whereId, ...)` | Chỉ convert `whereId.ToString()`, không resolve `adUnitIds.intId` |
| Single mode (`useMultiAdUnitIds = false`) | Ưu tiên `ShowXxx(string where, ...)` | Không cần `ShowById` |
| Test preload lại ads | `RequestAll()` | Dùng khi cần load lại sau init/test |
| Test bỏ qua capping inter | `ResetInterstitialCappingForTest()` | Chỉ nên dùng trong test |

---

## 1) Điều kiện trước khi gọi

- Scene đã có `AdsManager` (qua SDK prefab/object).
- Dependencies đã cài xong, project compile sạch.
- Nếu dùng AdMulti: `useMultiAdUnitIds = true` và đã có `adUnitIds`.
- Nếu dùng mode bình thường: `useMultiAdUnitIds = false` và đã điền single IDs.

---

## 2) Helper dùng chung (copy vào class của bạn)

```csharp
using UnityEngine;
using GameUpSDK;

public static class AdsQuick
{
    public static bool Ready()
    {
        if (AdsManager.Instance == null)
        {
            Debug.LogWarning("[AdsQuick] AdsManager.Instance is null");
            return false;
        }

        if (!AdsManager.Instance.CheckInitialized())
        {
            Debug.LogWarning("[AdsQuick] AdsManager not initialized yet");
            return false;
        }

        return true;
    }
}
```

---

## 3) Setup cực nhanh theo mode

### AdMulti

- Bật `Use Multi IDs`
- Điền list `adUnitIds`: `IntId` + `AdType` + `NameId(where)` + `Id`

### Mode bình thường (Single IDs)

- Tắt `Use Multi IDs`
- Điền ID đơn theo format:
  - AdMob: Banner / Interstitial / Rewarded / AppOpen
  - LevelPlay: Banner / Interstitial / Rewarded

---

## 4) Gọi theo `string` (`where`) - khuyến nghị cho mọi mode

```csharp
if (AdsQuick.Ready())
{
    AdsManager.Instance.ShowBanner("main");
}
```

```csharp
if (AdsQuick.Ready())
{
    AdsManager.Instance.ShowInterstitial(
        where: "level_end",
        currentLevel: 10,
        onSuccess: () => Debug.Log("Interstitial success"),
        onFail: () => Debug.LogWarning("Interstitial fail")
    );
}
```

```csharp
if (AdsQuick.Ready())
{
    AdsManager.Instance.ShowRewardedVideo(
        where: "revive",
        currentLevel: 10,
        onSuccess: () =>
        {
            // Grant reward ở đây
            Debug.Log("Rewarded success -> grant reward");
        },
        onFail: () =>
        {
            // User skip / ad fail / no fill
            Debug.LogWarning("Rewarded fail");
        }
    );
}
```

```csharp
if (AdsQuick.Ready())
{
    AdsManager.Instance.ShowAppOpenAds(
        where: "main",
        onSuccess: () => Debug.Log("AppOpen success"),
        onFail: () => Debug.LogWarning("AppOpen fail")
    );
}
```

---

## 5) Gọi theo `id` (resolve từ `adUnitIds`)

`ShowById(intId, ...)` sẽ map `intId -> (adType + nameId)` rồi tự gọi API tương ứng.

```csharp
if (AdsQuick.Ready())
{
    AdsManager.Instance.ShowById(
        intId: 101,          // phải tồn tại trong adUnitIds
        currentLevel: 12,    // dùng cho Inter/Reward
        onSuccess: () => Debug.Log("ShowById success"),
        onFail: () => Debug.LogWarning("ShowById fail")
    );
}
```

`ShowById(...)` dùng tốt nhất cho **AdMulti**. Ở mode bình thường (không có map `adUnitIds`) thường sẽ không resolve được `intId`.

---

## 6) Overload `int whereId` (khác với ShowById)

Các hàm dưới đây chỉ đổi `whereId.ToString()`:

```csharp
AdsManager.Instance.ShowBanner(1);
AdsManager.Instance.ShowInterstitial(2, onSuccess: null, onFail: null);
AdsManager.Instance.ShowRewardedVideo(3, onSuccess: null, onFail: null);
AdsManager.Instance.ShowAppOpenAds(4, onSuccess: null, onFail: null);
```

Không resolve theo `adUnitIds.intId`. Muốn resolve thật theo config AdMulti -> dùng `ShowById(...)`.

---

## 7) Snippet cho mode bình thường (Single IDs)

Trong mode này, bạn chỉ cần gọi theo `string where` như bình thường:

```csharp
if (AdsQuick.Ready())
{
    AdsManager.Instance.ShowBanner("main");
    AdsManager.Instance.ShowInterstitial("main", currentLevel: 5);
    AdsManager.Instance.ShowRewardedVideo("main", currentLevel: 5);
}
```

Không cần `ShowById(...)` nếu bạn không cấu hình `adUnitIds`.

---

## 8) Utility thường dùng khi test

```csharp
if (AdsQuick.Ready())
{
    AdsManager.Instance.RequestAll(); // preload lại
    AdsManager.Instance.ResetInterstitialCappingForTest(); // test inter liên tục
}
```

---

## 9) Debug nhanh khi không hiện ads

- `AdsManager.Instance == null` -> chưa có SDK object trong scene.
- `CheckInitialized() == false` -> gọi quá sớm.
- `ShowById` fail -> `intId` không tồn tại trong `adUnitIds`.
- Banner không hiện -> có thể bị Remote Config `enable_banner = false`.
- Inter fail dù có ad -> có thể bị capping/level rule (`AdsRules`).
- Mode bình thường vẫn gọi fail -> kiểm tra single IDs đã điền đúng chưa.

