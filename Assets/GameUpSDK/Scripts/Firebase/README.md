# Firebase Remote Config Utils

Tiện ích đọc và đồng bộ **Firebase Remote Config** với các biến public trong code bằng reflection. Tên biến trong class phải **trùng với key** trên Firebase Console để tự động map.

## Yêu cầu

- **Firebase SDK** (Firebase.RemoteConfig) đã thêm vào project.
- **FirebaseUtils** đã khởi tạo (dùng khi Firebase chưa sẵn sàng lúc Start).

## Cách dùng

### 1. Singleton

```csharp
FirebaseRemoteConfigUtils.Instance
```

### 2. Đọc giá trị sau khi fetch xong

Các giá trị chỉ đúng sau khi Remote Config đã fetch và activate. Nên dùng `IsRemoteConfigReady` hoặc `OnFetchCompleted` trước khi đọc.

```csharp
// Kiểm tra đã sẵn sàng
if (FirebaseRemoteConfigUtils.Instance.IsRemoteConfigReady)
{
    int capping = FirebaseRemoteConfigUtils.Instance.inter_capping_time;
    bool showBanner = FirebaseRemoteConfigUtils.Instance.enable_banner;
}

// Hoặc đăng ký callback
FirebaseRemoteConfigUtils.Instance.OnFetchCompleted += (activated) =>
{
    // activated = true nếu fetch và activate thành công
    var utils = FirebaseRemoteConfigUtils.Instance;
    int startLevel = utils.inter_start_level;
    bool rateEnabled = utils.enable_rate_app;
};
```

### 3. Refresh config (fetch lại)

Khi cần cập nhật config (ví dụ: sau vài phút chơi, hoặc từ menu):

```csharp
FirebaseRemoteConfigUtils.Instance.FetchAndActivate(success =>
{
    if (success)
        Debug.Log("Remote Config đã cập nhật.");
});
```

## Các key (biến) mặc định

| Key (Firebase Console)     | Kiểu   | Mặc định | Mô tả |
|----------------------------|--------|----------|--------|
| `inter_capping_time`       | int    | 120      | Khoảng thời gian tối thiểu (giây) giữa 2 lần hiển thị Interstitial. |
| `inter_start_level`        | int    | 3        | Level bắt đầu hiện Interstitial (level tính từ 1). |
| `enable_rate_app`          | bool   | false    | Bật/tắt hiển thị Rate App trong game. |
| `level_start_show_rate_app`| int    | 5        | Level bắt đầu hiện Rate App. |
| `no_internet_popup_enable` | bool   | true     | Bật/tắt popup yêu cầu kết nối Internet. |
| `enable_banner`            | bool   | true     | Bật/tắt hiển thị Banner trong game. |

Trên **Firebase Console → Remote Config**, tạo các key trùng tên và set kiểu phù hợp.

## Cơ chế default & mapping (mới)

- **Default values**: SDK tự build defaults bằng cách quét **tất cả `public` instance field** (kiểu đơn giản) trên các target:
  - `FirebaseRemoteConfigUtils` (chính nó)
  - `remoteConfigExtraData` (nếu được gán)
- **Remote → field mapping**: sau `FetchAndActivate`, SDK duyệt tất cả key từ Remote Config và set vào field có **tên trùng key** (trên các target ở trên).
- **Kiểu dữ liệu hỗ trợ**:
  - `int`, `long` ← Firebase **Number**
  - `float`, `double` ← Firebase **Number**
  - `bool` ← Firebase **Boolean**
  - `string` ← Firebase **String**

## Hành vi đặc biệt

- **Editor (Windows/macOS):** Không gọi Firebase thật; `IsRemoteConfigReady = true` ngay và dùng giá trị mặc định trong code.
- **Firebase chưa init:** Tự đăng ký với `FirebaseUtils.Instance.onInitialized` và fetch sau khi Firebase sẵn sàng.
- **Lỗi khi init:** Vẫn set `_remoteConfigReady = true` và gọi `OnFetchCompleted(false)` để game không bị chặn; giá trị dùng default.

## Thêm key mới

### Cách 1 (khuyến nghị): Dự án khác kế thừa để thêm field mới (không sửa SDK gốc)

Tạo class kế thừa và thêm `public` field. Default sẽ tự lấy từ giá trị bạn set trong Inspector/serialized; Remote sẽ tự map theo key trùng tên.

```csharp
using GameUpSDK;
using UnityEngine;

public class MyFirebaseRemoteConfig : FirebaseRemoteConfigUtils
{
    [Header("My Project Remote Config")]
    public bool enable_special_event = false;
    public int event_start_level = 10;
    public float iap_discount = 0.25f;
    public string live_ops_message = "hello";
}
```

Trên Firebase Console, tạo các key:
- `enable_special_event` (Boolean)
- `event_start_level` (Number)
- `iap_discount` (Number)
- `live_ops_message` (String)

### Cách 2: Override defaults/targets khi muốn “flex” hơn

Ví dụ bạn muốn chỉ map thêm 1 object khác (ngoài `remoteConfigExtraData`) hoặc muốn thêm/ghi đè default theo logic riêng.

```csharp
using System.Collections.Generic;
using GameUpSDK;
using UnityEngine;

public class MyFirebaseRemoteConfig : FirebaseRemoteConfigUtils
{
    [SerializeField] private ScriptableObject myExtraData;

    protected override IEnumerable<object> GetRemoteConfigTargets()
    {
        foreach (var t in base.GetRemoteConfigTargets()) yield return t;
        if (myExtraData != null) yield return myExtraData;
    }

    protected override Dictionary<string, object> GetDefaultValues()
    {
        var defaults = base.GetDefaultValues();

        // Ghi đè 1 key cụ thể (nếu muốn)
        defaults["inter_capping_time"] = 90;

        // Thêm 1 key “ảo” (nếu bạn có cơ chế bind custom ở nơi khác)
        // defaults["some_custom_key"] = "value";

        return defaults;
    }
}
```

### Lưu ý

- Mapping chỉ áp dụng với **`public` instance field** (không phải property) và các kiểu hỗ trợ ở trên.
- Nếu nhiều target có field trùng tên key, **target đầu tiên sẽ thắng** khi build default (mặc định: `this` trước, rồi `remoteConfigExtraData`).

## API nhanh

| Thành phần           | Mô tả |
|----------------------|--------|
| `IsRemoteConfigReady`| `true` khi đã init (và fetch xong trên device). |
| `OnFetchCompleted`   | `Action<bool>`: được gọi khi fetch xong (thành công = true). |
| `FetchAndActivate(onDone)` | Fetch lại và activate; gọi `onDone(bool)` khi xong. |
| Các field (inter_capping_time, enable_banner, ...) | Đọc trực tiếp sau khi config ready. |

---

*Phần Firebase của GameUp SDK.*
