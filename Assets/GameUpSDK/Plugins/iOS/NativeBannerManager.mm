#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <GoogleMobileAds/GoogleMobileAds.h>

// Lấy UIViewController chính của Unity
extern UIViewController* UnityGetGLViewController();

// Khai báo kiểu Callback gửi về C#
typedef void (*Action_Void)();
typedef void (*Action_String)(const char* error);
typedef void (*Action_Double)(double value);

// Trạng thái Ad
typedef NS_ENUM(NSInteger, AdState) {
    AdStateIdle,
    AdStateLoading,
    AdStateLoaded,
    AdStateShowing
};

@interface NativeBannerManager : NSObject <GADNativeAdLoaderDelegate, GADNativeAdDelegate>

@property (nonatomic, strong) GADAdLoader *adLoader;
@property (nonatomic, strong) GADNativeAd *currentNativeAd;
@property (nonatomic, strong) UIView *currentAdLayout;
@property (nonatomic, assign) AdState currentState;

// Callbacks
@property (nonatomic, assign) Action_Void onLoaded;
@property (nonatomic, assign) Action_String onFailed;
@property (nonatomic, assign) Action_Void onDisplayed;
@property (nonatomic, assign) Action_Void onClosed;
@property (nonatomic, assign) Action_Void onClicked;
@property (nonatomic, assign) Action_Double onPaid;

+ (instancetype)sharedInstance;
- (void)loadAd:(NSString *)adUnitId;
- (void)showAd:(BOOL)isTop;
- (void)hideAd;

@end

@implementation NativeBannerManager

+ (instancetype)sharedInstance {
    static NativeBannerManager *sharedInstance = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        sharedInstance = [[self alloc] init];
        sharedInstance.currentState = AdStateIdle;
    });
    return sharedInstance;
}

- (void)loadAd:(NSString *)adUnitId {
    if (self.currentState == AdStateLoading) return;
    self.currentState = AdStateLoading;

    UIViewController *rootVC = UnityGetGLViewController();
    self.adLoader = [[GADAdLoader alloc] initWithAdUnitID:adUnitId
                                       rootViewController:rootVC
                                                  adTypes:@[GADAdLoaderAdTypeNative]
                                                  options:nil];
    self.adLoader.delegate = self;
    [self.adLoader loadRequest:[GADRequest request]];
}

- (void)showAd:(BOOL)isTop {
    if (self.currentState != AdStateLoaded || !self.currentNativeAd) {
        NSLog(@"[iOS Native] Ad not ready to show.");
        return;
    }

    [self hideAd]; // Xóa layout cũ nếu còn kẹt
    
    UIViewController *rootVC = UnityGetGLViewController();
    UIView *rootView = rootVC.view;
    
    // Tính toán Kích thước và Safe Area
    CGFloat screenWidth = rootView.bounds.size.width;
    UIEdgeInsets safeArea = UIEdgeInsetsZero;
    if (@available(iOS 11.0, *)) { safeArea = rootView.safeAreaInsets; }
    
    CGFloat headerHeight = 36.0;
    CGFloat mediaHeight = 200.0;
    CGFloat textHeight = 40.0;
    CGFloat totalAdHeight = headerHeight + mediaHeight + textHeight;
    
    CGFloat yPos = isTop ? safeArea.top : (rootView.bounds.size.height - safeArea.bottom - totalAdHeight);

    // 1. Tạo Khối Nền Tối
    self.currentAdLayout = [[UIView alloc] initWithFrame:CGRectMake(0, yPos, screenWidth, totalAdHeight)];
    self.currentAdLayout.backgroundColor = [UIColor colorWithRed:26.0/255.0 green:26.0/255.0 blue:26.0/255.0 alpha:1.0];
    
    // 2. Tạo Native Ad View của Google
    GADNativeAdView *adView = [[GADNativeAdView alloc] initWithFrame:CGRectMake(0, headerHeight, screenWidth, mediaHeight + textHeight)];
    [self.currentAdLayout addSubview:adView];
    
    // 3. Tạo Media View (Video/Ảnh)
    GADMediaView *mediaView = [[GADMediaView alloc] initWithFrame:CGRectMake(0, 0, screenWidth, mediaHeight)];
    [adView addSubview:mediaView];
    adView.mediaView = mediaView;
    
    // 4. Tạo Headline Text
    UILabel *headline = [[UILabel alloc] initWithFrame:CGRectMake(8, mediaHeight, screenWidth - 16, textHeight)];
    headline.textColor = [UIColor whiteColor];
    headline.font = [UIFont boldSystemFontOfSize:16];
    headline.text = self.currentNativeAd.headline;
    [adView addSubview:headline];
    adView.headlineView = headline;
    
    // Gắn dữ liệu Native
    adView.nativeAd = self.currentNativeAd;
    self.currentNativeAd.delegate = self;
    
    // 5. Tạo Nút Close (Góc Phải Trên Cùng)
    UIButton *closeBtn = [UIButton buttonWithType:UIButtonTypeCustom];
    closeBtn.frame = CGRectMake(screenWidth - 64, 0, 64, headerHeight);
    [closeBtn setTitle:@"▼" forState:UIControlStateNormal];
    [closeBtn setTitleColor:[UIColor colorWithRed:179.0/255.0 green:179.0/255.0 blue:179.0/255.0 alpha:1.0] forState:UIControlStateNormal];
    closeBtn.titleLabel.font = [UIFont boldSystemFontOfSize:16];
    [closeBtn addTarget:self action:@selector(closeTapped) forControlEvents:UIControlEventTouchUpInside];
    closeBtn.backgroundColor = [UIColor colorWithRed:18.0/255.0 green:18.0/255.0 blue:18.0/255.0 alpha:1.0];
    
    // Bo góc dưới cùng bên trái cho nút
    UIBezierPath *maskPath = [UIBezierPath bezierPathWithRoundedRect:closeBtn.bounds 
                                                   byRoundingCorners:UIRectCornerBottomLeft 
                                                         cornerRadii:CGSizeMake(8.0, 8.0)];
    CAShapeLayer *maskLayer = [CAShapeLayer layer];
    maskLayer.path = maskPath.CGPath;
    closeBtn.layer.mask = maskLayer;
    
    [self.currentAdLayout addSubview:closeBtn];
    
    // Gắn lên màn hình Unity
    [rootView addSubview:self.currentAdLayout];
    
    self.currentState = AdStateShowing;
    if (self.onDisplayed) self.onDisplayed();
}

- (void)hideAd {
    if (self.currentAdLayout) {
        [self.currentAdLayout removeFromSuperview];
        self.currentAdLayout = nil;
    }
    self.currentNativeAd = nil;
    self.currentState = AdStateIdle;
}

- (void)closeTapped {
    [self hideAd];
    if (self.onClosed) self.onClosed();
}

// =====================================
// GADNativeAdLoaderDelegate Callbacks
// =====================================
- (void)adLoader:(GADAdLoader *)adLoader didReceiveNativeAd:(GADNativeAd *)nativeAd {
    if (self.currentState == AdStateIdle) { return; } // Nếu đã bị Hide trong lúc tải thì hủy
    
    self.currentNativeAd = nativeAd;
    self.currentState = AdStateLoaded;
    
    __weak typeof(self) weakSelf = self;
    nativeAd.paidEventHandler = ^(GADAdValue * _Nonnull value) {
        if (weakSelf.onPaid) weakSelf.onPaid([value.value doubleValue] * 0.000001); // Quy đổi ra USD
    };
    
    if (self.onLoaded) self.onLoaded();
}

- (void)adLoader:(GADAdLoader *)adLoader didFailToReceiveAdWithError:(NSError *)error {
    self.currentState = AdStateIdle;
    if (self.onFailed) self.onFailed([error.localizedDescription UTF8String]);
}

// =====================================
// GADNativeAdDelegate Callbacks
// =====================================
- (void)nativeAdDidRecordClick:(GADNativeAd *)nativeAd {
    if (self.onClicked) self.onClicked();
}

@end

// ========================================================
// C CẦU NỐI (C-BINDINGS DÀNH CHO C# DllImport)
// ========================================================
extern "C" {
    void NativeBanner_SetCallbacks(Action_Void onLoaded, Action_String onFailed, Action_Void onDisplayed, Action_Void onClosed, Action_Void onClicked, Action_Double onPaid) {
        NativeBannerManager *mgr = [NativeBannerManager sharedInstance];
        mgr.onLoaded = onLoaded;
        mgr.onFailed = onFailed;
        mgr.onDisplayed = onDisplayed;
        mgr.onClosed = onClosed;
        mgr.onClicked = onClicked;
        mgr.onPaid = onPaid;
    }

    void NativeBanner_LoadAd(const char* adUnitId) {
        NSString *unitIdStr = [NSString stringWithUTF8String:adUnitId];
        [[NativeBannerManager sharedInstance] loadAd:unitIdStr];
    }

    void NativeBanner_ShowAd(bool isTop) {
        [[NativeBannerManager sharedInstance] showAd:isTop];
    }

    void NativeBanner_HideAd() {
        [[NativeBannerManager sharedInstance] hideAd];
    }
}