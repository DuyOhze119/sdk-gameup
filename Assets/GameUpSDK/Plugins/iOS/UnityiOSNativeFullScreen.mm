#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <GoogleMobileAds/GoogleMobileAds.h>

extern UIViewController* UnityGetGLViewController();

typedef void (*Action_Void)();
typedef void (*Action_String)(const char* error);
typedef void (*Action_Double)(double value);

typedef NS_ENUM(NSInteger, AdState) {
    AdStateIdle, AdStateLoaded, AdStateShowing
};

@interface NativeBannerManager : NSObject <GADNativeAdLoaderDelegate, GADNativeAdDelegate>
@property (nonatomic, strong) GADAdLoader *adLoader;
@property (nonatomic, strong) GADNativeAd *currentNativeAd;
@property (nonatomic, strong) UIView *currentAdLayout;
@property (nonatomic, assign) AdState currentState;
@property (nonatomic, assign) BOOL isLoadingAd;

// Trỏ tham chiếu tới các View để Update In-Place
@property (nonatomic, strong) GADNativeAdView *nativeAdView;
@property (nonatomic, strong) UILabel *headlineLabel;
@property (nonatomic, strong) UILabel *bodyLabel;
@property (nonatomic, strong) UIButton *ctaBtn;
@property (nonatomic, strong) UIImageView *iconView;

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
        sharedInstance.isLoadingAd = NO;
    });
    return sharedInstance;
}

- (void)loadAd:(NSString *)adUnitId {
    if (self.isLoadingAd) return;
    self.isLoadingAd = YES;

    UIViewController *rootVC = UnityGetGLViewController();
    
    GADNativeAdViewAdOptions *viewOptions = [[GADNativeAdViewAdOptions alloc] init];
    viewOptions.preferredAdChoicesPosition = GADAdChoicesPositionTopLeftCorner;

    self.adLoader = [[GADAdLoader alloc] initWithAdUnitID:adUnitId
                                       rootViewController:rootVC
                                                  adTypes:@[GADAdLoaderAdTypeNative]
                                                  options:@[viewOptions]];
    self.adLoader.delegate = self;
    [self.adLoader loadRequest:[GADRequest request]];
}

- (void)showAd:(BOOL)isTop {
    if (self.currentState != AdStateLoaded || !self.currentNativeAd) return;
    [self hideAd]; 
    
    UIViewController *rootVC = UnityGetGLViewController();
    UIView *rootView = rootVC.view;
    
    CGFloat screenWidth = rootView.bounds.size.width;
    UIEdgeInsets safeArea = UIEdgeInsetsZero;
    if (@available(iOS 11.0, *)) { safeArea = rootView.safeAreaInsets; }
    
    CGFloat headerHeight = 36.0;
    CGFloat mediaHeight = 180.0;
    CGFloat footerHeight = 60.0;
    CGFloat totalAdHeight = headerHeight + mediaHeight + footerHeight;
    CGFloat yPos = isTop ? safeArea.top : (rootView.bounds.size.height - safeArea.bottom - totalAdHeight);

    self.currentAdLayout = [[UIView alloc] initWithFrame:CGRectMake(0, yPos, screenWidth, totalAdHeight)];
    self.currentAdLayout.backgroundColor = [UIColor colorWithRed:26.0/255.0 green:26.0/255.0 blue:26.0/255.0 alpha:1.0];
    
    self.nativeAdView = [[GADNativeAdView alloc] initWithFrame:CGRectMake(0, headerHeight, screenWidth, mediaHeight + footerHeight)];
    [self.currentAdLayout addSubview:self.nativeAdView];
    
    GADMediaView *mediaView = [[GADMediaView alloc] initWithFrame:CGRectMake(0, 0, screenWidth, mediaHeight)];
    [self.nativeAdView addSubview:mediaView];
    self.nativeAdView.mediaView = mediaView;
    
    self.iconView = [[UIImageView alloc] initWithFrame:CGRectMake(8, mediaHeight + 8, 44, 44)];
    self.iconView.contentMode = UIViewContentModeScaleAspectFill;
    self.iconView.clipsToBounds = YES;
    self.iconView.layer.cornerRadius = 8.0;
    [self.nativeAdView addSubview:self.iconView];
    
    self.ctaBtn = [UIButton buttonWithType:UIButtonTypeSystem];
    self.ctaBtn.frame = CGRectMake(screenWidth - 8 - 80, mediaHeight + 12, 80, 36);
    self.ctaBtn.backgroundColor = [UIColor colorWithRed:33.0/255.0 green:150.0/255.0 blue:243.0/255.0 alpha:1.0];
    [self.ctaBtn setTitleColor:[UIColor whiteColor] forState:UIControlStateNormal];
    self.ctaBtn.titleLabel.font = [UIFont boldSystemFontOfSize:13];
    self.ctaBtn.layer.cornerRadius = 4.0;
    [self.nativeAdView addSubview:self.ctaBtn];
    
    CGFloat textWidth = screenWidth - 8 - 44 - 8 - 80 - 8; 
    self.headlineLabel = [[UILabel alloc] initWithFrame:CGRectMake(60, mediaHeight + 10, textWidth, 20)];
    self.headlineLabel.textColor = [UIColor whiteColor];
    self.headlineLabel.font = [UIFont boldSystemFontOfSize:15];
    [self.nativeAdView addSubview:self.headlineLabel];
    
    self.bodyLabel = [[UILabel alloc] initWithFrame:CGRectMake(60, mediaHeight + 30, textWidth, 18)];
    self.bodyLabel.textColor = [UIColor colorWithRed:179.0/255.0 green:179.0/255.0 blue:179.0/255.0 alpha:1.0];
    self.bodyLabel.font = [UIFont systemFontOfSize:12];
    [self.nativeAdView addSubview:self.bodyLabel];
    
    // Đổ dữ liệu vào Frame bằng hàm populateUI
    [self populateUI];
    
    UIButton *closeBtn = [UIButton buttonWithType:UIButtonTypeCustom];
    closeBtn.frame = CGRectMake(screenWidth - 64, 0, 64, headerHeight);
    [closeBtn setTitle:@"▼" forState:UIControlStateNormal];
    [closeBtn setTitleColor:[UIColor colorWithRed:179.0/255.0 green:179.0/255.0 blue:179.0/255.0 alpha:1.0] forState:UIControlStateNormal];
    closeBtn.titleLabel.font = [UIFont boldSystemFontOfSize:16];
    [closeBtn addTarget:self action:@selector(closeTapped) forControlEvents:UIControlEventTouchUpInside];
    closeBtn.backgroundColor = [UIColor colorWithRed:18.0/255.0 green:18.0/255.0 blue:18.0/255.0 alpha:1.0];
    
    UIBezierPath *maskPath = [UIBezierPath bezierPathWithRoundedRect:closeBtn.bounds byRoundingCorners:UIRectCornerBottomLeft cornerRadii:CGSizeMake(8.0, 8.0)];
    CAShapeLayer *maskLayer = [CAShapeLayer layer];
    maskLayer.path = maskPath.CGPath;
    closeBtn.layer.mask = maskLayer;
    [self.currentAdLayout addSubview:closeBtn];
    
    [rootView addSubview:self.currentAdLayout];
    self.currentState = AdStateShowing;
    if (self.onDisplayed) self.onDisplayed();
}

// Hàm tráo nội dung quảng cáo
- (void)populateUI {
    self.nativeAdView.iconView = self.iconView;
    self.nativeAdView.callToActionView = self.ctaBtn;
    self.nativeAdView.headlineView = self.headlineLabel;
    self.nativeAdView.bodyView = self.bodyLabel;

    self.headlineLabel.text = self.currentNativeAd.headline;
    self.bodyLabel.text = self.currentNativeAd.body;
    self.iconView.image = self.currentNativeAd.icon.image;
    [self.ctaBtn setTitle:self.currentNativeAd.callToAction forState:UIControlStateNormal];
    
    self.nativeAdView.nativeAd = self.currentNativeAd;
    self.currentNativeAd.delegate = self;
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

- (void)adLoader:(GADAdLoader *)adLoader didReceiveNativeAd:(GADNativeAd *)nativeAd {
    self.isLoadingAd = NO;
    if (self.currentState == AdStateIdle) return; 
    
    self.currentNativeAd = nativeAd;
    
    // [CHÌA KHÓA AUTO-REFRESH] Nếu đang hiển thị UI, cập nhật tại chỗ!
    if (self.currentState == AdStateShowing && self.currentAdLayout != nil) {
        [self populateUI];
    } else {
        self.currentState = AdStateLoaded;
    }
    
    __weak typeof(self) weakSelf = self;
    nativeAd.paidEventHandler = ^(GADAdValue * _Nonnull value) {
        if (weakSelf.onPaid) weakSelf.onPaid([value.value doubleValue] * 0.000001);
    };
    if (self.onLoaded) self.onLoaded();
}

- (void)adLoader:(GADAdLoader *)adLoader didFailToReceiveAdWithError:(NSError *)error {
    self.isLoadingAd = NO;
    if (self.currentState != AdStateShowing) self.currentState = AdStateIdle;
    if (self.onFailed) self.onFailed([error.localizedDescription UTF8String]);
}

- (void)nativeAdDidRecordClick:(GADNativeAd *)nativeAd {
    if (self.onClicked) self.onClicked();
}
@end

extern "C" {
    void NativeBanner_SetCallbacks(Action_Void onLoaded, Action_String onFailed, Action_Void onDisplayed, Action_Void onClosed, Action_Void onClicked, Action_Double onPaid) {
        NativeBannerManager *mgr = [NativeBannerManager sharedInstance];
        mgr.onLoaded = onLoaded; mgr.onFailed = onFailed; mgr.onDisplayed = onDisplayed;
        mgr.onClosed = onClosed; mgr.onClicked = onClicked; mgr.onPaid = onPaid;
    }
    void NativeBanner_LoadAd(const char* adUnitId) {
        [[NativeBannerManager sharedInstance] loadAd:[NSString stringWithUTF8String:adUnitId]];
    }
    void NativeBanner_ShowAd(bool isTop) {
        [[NativeBannerManager sharedInstance] showAd:isTop];
    }
    void NativeBanner_HideAd() {
        [[NativeBannerManager sharedInstance] hideAd];
    }
}