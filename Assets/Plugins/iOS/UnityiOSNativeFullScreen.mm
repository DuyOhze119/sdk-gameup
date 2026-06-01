#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <GoogleMobileAds/GoogleMobileAds.h>

// Hàm lấy ViewController hiện tại của Unity
extern UIViewController* UnityGetGLViewController();

// 1. Định nghĩa các kiểu Callback (Con trỏ hàm) từ C#
typedef void (*NativeAdLoadedCallback)();
typedef void (*NativeAdFailedCallback)(const char* error);
typedef void (*NativeAdClosedCallback)();

@interface UnityiOSNativeFullScreen : NSObject <GADNativeAdLoaderDelegate>
@property(nonatomic, strong) GADAdLoader *adLoader;
@property(nonatomic, strong) GADNativeAd *loadedAd;
@property(nonatomic, strong) UIView *mainContainer;
@property(nonatomic, assign) BOOL isAdLoading;

// 2. Lưu trữ các Callback
@property(nonatomic, assign) NativeAdLoadedCallback onLoadedCallback;
@property(nonatomic, assign) NativeAdFailedCallback onFailedCallback;
@property(nonatomic, assign) NativeAdClosedCallback onClosedCallback;

+ (instancetype)sharedInstance;
- (void)loadAd:(NSString *)adUnitId;
- (BOOL)isAdReady;
- (void)showAd;
- (void)hideAd;
@end

@implementation UnityiOSNativeFullScreen

+ (instancetype)sharedInstance {
    static UnityiOSNativeFullScreen *sharedInstance = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        sharedInstance = [[UnityiOSNativeFullScreen alloc] init];
    });
    return sharedInstance;
}

- (void)loadAd:(NSString *)adUnitId {
    if (self.loadedAd != nil || self.isAdLoading) return;

    self.isAdLoading = YES;
    UIViewController *rootVC = UnityGetGLViewController();

    self.adLoader = [[GADAdLoader alloc] initWithAdUnitID:adUnitId
                                       rootViewController:rootVC
                                                  adTypes:@[ GADAdFormatNative ]
                                                  options:nil];
    self.adLoader.delegate = self;
    [self.adLoader loadRequest:[GADRequest request]];
}

- (BOOL)isAdReady {
    return self.loadedAd != nil;
}

- (void)showAd {
    if (!self.loadedAd) return;

    UIViewController *rootVC = UnityGetGLViewController();
    CGRect screenBounds = [UIScreen mainScreen].bounds;

    self.mainContainer = [[UIView alloc] initWithFrame:screenBounds];
    self.mainContainer.backgroundColor = [UIColor blackColor];
    [rootVC.view addSubview:self.mainContainer];

    GADNativeAdView *adView = [[GADNativeAdView alloc] initWithFrame:screenBounds];
    [self.mainContainer addSubview:adView];

    GADMediaView *mediaView = [[GADMediaView alloc] initWithFrame:screenBounds];
    [adView addSubview:mediaView];
    adView.mediaView = mediaView;

    UILabel *headlineLabel = [[UILabel alloc] initWithFrame:CGRectMake(20, screenBounds.size.height - 180, screenBounds.size.width - 40, 40)];
    headlineLabel.text = self.loadedAd.headline;
    headlineLabel.textColor = [UIColor whiteColor];
    headlineLabel.font = [UIFont boldSystemFontOfSize:20];
    [adView addSubview:headlineLabel];
    adView.headlineView = headlineLabel;

    UIButton *ctaButton = [UIButton buttonWithType:UIButtonTypeCustom];
    ctaButton.frame = CGRectMake(20, screenBounds.size.height - 100, screenBounds.size.width - 40, 50);
    [ctaButton setTitle:self.loadedAd.callToAction forState:UIControlStateNormal];
    [ctaButton setBackgroundColor:[UIColor colorWithRed:1.0 green:0.25 blue:0.51 alpha:1.0]]; 
    [ctaButton setTitleColor:[UIColor whiteColor] forState:UIControlStateNormal];
    ctaButton.layer.cornerRadius = 8;
    [adView addSubview:ctaButton];
    adView.callToActionView = ctaButton;

    adView.nativeAd = self.loadedAd;

    // --- NÚT ĐÓNG ĐẾM NGƯỢC ---
    UIButton *closeButton = [UIButton buttonWithType:UIButtonTypeCustom];
    closeButton.frame = CGRectMake(screenBounds.size.width - 65, 50, 45, 45); 
    closeButton.layer.cornerRadius = 22.5; 
    closeButton.layer.masksToBounds = YES;
    closeButton.backgroundColor = [UIColor colorWithWhite:0.0 alpha:0.5];
    closeButton.layer.borderWidth = 1.5; 
    closeButton.layer.borderColor = [UIColor whiteColor].CGColor;
    
    [closeButton setTitle:@"3" forState:UIControlStateNormal];
    [closeButton setTitleColor:[UIColor whiteColor] forState:UIControlStateNormal];
    closeButton.titleLabel.font = [UIFont systemFontOfSize:14]; 
    closeButton.userInteractionEnabled = NO; 
    closeButton.tag = 999; 
    [self.mainContainer addSubview:closeButton];

    __block int secondsLeft = 3; 
    dispatch_queue_t queue = dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0);
    dispatch_source_t timer = dispatch_source_create(DISPATCH_SOURCE_TYPE_TIMER, 0, 0, queue);
    dispatch_source_set_timer(timer, dispatch_time(DISPATCH_TIME_NOW, 1.0 * NSEC_PER_SEC), 1.0 * NSEC_PER_SEC, 0.1 * NSEC_PER_SEC);
    
    __block dispatch_source_t blockTimer = timer; 
    
    dispatch_source_set_event_handler(timer, ^{
        secondsLeft--;
        dispatch_async(dispatch_get_main_queue(), ^{
            UIButton *btn = (UIButton *)[self.mainContainer viewWithTag:999];
            if (btn) {
                if (secondsLeft > 0) {
                    [btn setTitle:[NSString stringWithFormat:@"%d", secondsLeft] forState:UIControlStateNormal];
                } else {
                    dispatch_source_cancel(blockTimer);
                    [btn setTitle:@"X" forState:UIControlStateNormal];
                    btn.titleLabel.font = [UIFont systemFontOfSize:16 weight:UIFontWeightMedium];
                    btn.userInteractionEnabled = YES;
                    [btn addTarget:self action:@selector(hideAd) forControlEvents:UIControlEventTouchUpInside];
                }
            } else {
                dispatch_source_cancel(blockTimer);
            }
        });
    });
    
    dispatch_resume(timer);
}

// Ẩn quảng cáo và dọn dẹp
- (void)hideAd {
    if (self.mainContainer) {
        [self.mainContainer removeFromSuperview];
        self.mainContainer = nil;
    }
    if (self.loadedAd) {
        self.loadedAd = nil; 
    }
    
    // GỌI CALLBACK ĐÓNG VỀ C#
    if (self.onClosedCallback) {
        self.onClosedCallback();
    }
}

#pragma mark - GADNativeAdLoaderDelegate Implementation
- (void)adLoader:(GADAdLoader *)adLoader didReceiveNativeAd:(GADNativeAd *)nativeAd {
    self.loadedAd = nativeAd;
    self.isAdLoading = NO;
    
    // GỌI CALLBACK THÀNH CÔNG VỀ C#
    if (self.onLoadedCallback) {
        self.onLoadedCallback();
    }
}

- (void)adLoader:(GADAdLoader *)adLoader didFailToReceiveAdWithError:(NSError *)error {
    self.isAdLoading = NO;
    self.loadedAd = nil;
    
    // GỌI CALLBACK LỖI VỀ C# (Gửi chuỗi lỗi qua)
    if (self.onFailedCallback) {
        self.onFailedCallback(error.localizedDescription.UTF8String);
    }
}
@end

// 3. C-Style Linker cập nhật (Thêm tham số Callback vào Load)
extern "C" {
    void _iosLoadNativeAd(const char* adUnitId, NativeAdLoadedCallback onLoaded, NativeAdFailedCallback onFailed, NativeAdClosedCallback onClosed) {
        NSString *unitIdStr = [NSString stringWithUTF8String:adUnitId];
        
        UnityiOSNativeFullScreen *instance = [UnityiOSNativeFullScreen sharedInstance];
        instance.onLoadedCallback = onLoaded;
        instance.onFailedCallback = onFailed;
        instance.onClosedCallback = onClosed;
        
        [instance loadAd:unitIdStr];
    }

    bool _iosIsNativeAdReady() {
        return [[UnityiOSNativeFullScreen sharedInstance] isAdReady];
    }

    void _iosShowNativeAd() {
        [[UnityiOSNativeFullScreen sharedInstance] showAd];
    }

    void _iosHideNativeAd() {
        [[UnityiOSNativeFullScreen sharedInstance] hideAd];
    }
}