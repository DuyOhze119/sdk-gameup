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
    
    CGFloat mediaHeight = 180.0;
    CGFloat footerHeight = 68.0;
    CGFloat totalAdHeight = mediaHeight + footerHeight;
    CGFloat yPos = isTop ? safeArea.top : (rootView.bounds.size.height - safeArea.bottom - totalAdHeight);

    // 1. Root Layout (Theme Trắng)
    self.currentAdLayout = [[UIView alloc] initWithFrame:CGRectMake(0, yPos, screenWidth, totalAdHeight)];
    self.currentAdLayout.backgroundColor = [UIColor whiteColor];
    
    self.nativeAdView = [[GADNativeAdView alloc] initWithFrame:CGRectMake(0, 0, screenWidth, totalAdHeight)];
    [self.currentAdLayout addSubview:self.nativeAdView];
    
    // ==========================================
    // KHU VỰC MEDIA (BLUR VÀ SHADOW)
    // ==========================================
    UIView *mediaContainer = [[UIView alloc] initWithFrame:CGRectMake(0, 0, screenWidth, mediaHeight)];
    mediaContainer.clipsToBounds = YES;
    [self.nativeAdView addSubview:mediaContainer];

    // Blur Background
    UIImageView *blurBg = [[UIImageView alloc] initWithFrame:mediaContainer.bounds];
    blurBg.contentMode = UIViewContentModeScaleAspectFill;
    blurBg.clipsToBounds = YES;
    if (self.currentNativeAd.images.count > 0) {
        blurBg.image = self.currentNativeAd.images.firstObject.image;
    }
    [mediaContainer addSubview:blurBg];
    
    // Áp dụng Blur chuẩn iOS (Effect) và lớp phủ trắng mờ 70%
    UIVisualEffectView *blurEffect = [[UIVisualEffectView alloc] initWithEffect:[UIBlurEffect effectWithStyle:UIBlurEffectStyleLight]];
    blurEffect.frame = blurBg.bounds;
    [blurBg addSubview:blurEffect];
    UIView *whiteOverlay = [[UIView alloc] initWithFrame:blurBg.bounds];
    whiteOverlay.backgroundColor = [UIColor colorWithWhite:1.0 alpha:0.7];
    [blurBg addSubview:whiteOverlay];

    // Media View Bóng Đổ (Drop Shadow)
    UIView *shadowContainer = [[UIView alloc] initWithFrame:CGRectMake(12, 12, screenWidth - 24, mediaHeight - 24)];
    shadowContainer.layer.shadowColor = [UIColor blackColor].CGColor;
    shadowContainer.layer.shadowOffset = CGSizeMake(0, 4);
    shadowContainer.layer.shadowOpacity = 0.25;
    shadowContainer.layer.shadowRadius = 8.0;
    shadowContainer.backgroundColor = [UIColor clearColor];
    [mediaContainer addSubview:shadowContainer];

    GADMediaView *mediaView = [[GADMediaView alloc] initWithFrame:shadowContainer.bounds];
    mediaView.layer.cornerRadius = 8.0;
    mediaView.clipsToBounds = YES;
    [shadowContainer addSubview:mediaView];
    self.nativeAdView.mediaView = mediaView;

    // Nút Tắt (X) Tròn
    UIButton *closeBtn = [UIButton buttonWithType:UIButtonTypeCustom];
    closeBtn.frame = CGRectMake(screenWidth - 32 - 10, 10, 32, 32);
    [closeBtn setTitle:@"X" forState:UIControlStateNormal];
    [closeBtn setTitleColor:[UIColor whiteColor] forState:UIControlStateNormal];
    closeBtn.titleLabel.font = [UIFont boldSystemFontOfSize:14];
    [closeBtn addTarget:self action:@selector(closeTapped) forControlEvents:UIControlEventTouchUpInside];
    closeBtn.backgroundColor = [UIColor colorWithWhite:0.1 alpha:0.5];
    closeBtn.layer.cornerRadius = 16.0;
    closeBtn.layer.borderWidth = 1.5;
    closeBtn.layer.borderColor = [UIColor colorWithWhite:0.7 alpha:1.0].CGColor;
    [mediaContainer addSubview:closeBtn];

    // ==========================================
    // KHU VỰC THÔNG TIN (INFO)
    // ==========================================
    UIView *footerContainer = [[UIView alloc] initWithFrame:CGRectMake(0, mediaHeight, screenWidth, footerHeight)];
    footerContainer.backgroundColor = [UIColor whiteColor];
    [self.nativeAdView addSubview:footerContainer];
    
    self.iconView = [[UIImageView alloc] initWithFrame:CGRectMake(10, 10, 48, 48)];
    self.iconView.contentMode = UIViewContentModeScaleAspectFill;
    self.iconView.clipsToBounds = YES;
    self.iconView.layer.cornerRadius = 8.0;
    [footerContainer addSubview:self.iconView];
    
    // Nút CTA (Có viền và Bo góc)
    self.ctaBtn = [UIButton buttonWithType:UIButtonTypeSystem];
    self.ctaBtn.frame = CGRectMake(screenWidth - 10 - 80, 14, 80, 40);
    self.ctaBtn.backgroundColor = [UIColor colorWithRed:244.0/255.0 green:139.0/255.0 blue:68.0/255.0 alpha:1.0]; // Cam
    [self.ctaBtn setTitleColor:[UIColor whiteColor] forState:UIControlStateNormal];
    self.ctaBtn.titleLabel.font = [UIFont boldSystemFontOfSize:13];
    self.ctaBtn.layer.cornerRadius = 8.0;
    self.ctaBtn.layer.borderWidth = 1.5;
    self.ctaBtn.layer.borderColor = [UIColor colorWithRed:211.0/255.0 green:84.0/255.0 blue:0.0/255.0 alpha:1.0].CGColor; // Viền cam đậm
    [footerContainer addSubview:self.ctaBtn];
    
    CGFloat textWidth = screenWidth - 10 - 48 - 10 - 80 - 10; 
    self.headlineLabel = [[UILabel alloc] initWithFrame:CGRectMake(68, 10, textWidth, 20)];
    self.headlineLabel.textColor = [UIColor colorWithWhite:0.13 alpha:1.0]; // Chữ đen
    self.headlineLabel.font = [UIFont boldSystemFontOfSize:15];
    [footerContainer addSubview:self.headlineLabel];
    
    self.bodyLabel = [[UILabel alloc] initWithFrame:CGRectMake(68, 32, textWidth, 18)];
    self.bodyLabel.textColor = [UIColor colorWithWhite:0.4 alpha:1.0]; // Chữ xám
    self.bodyLabel.font = [UIFont systemFontOfSize:12];
    [footerContainer addSubview:self.bodyLabel];
    
    [self populateUI];
    [rootView addSubview:self.currentAdLayout];
    self.currentState = AdStateShowing;
    if (self.onDisplayed) self.onDisplayed();
}

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
    void NativeBanner_LoadAd(const char* adUnitId) { [[NativeBannerManager sharedInstance] loadAd:[NSString stringWithUTF8String:adUnitId]]; }
    void NativeBanner_ShowAd(bool isTop) { [[NativeBannerManager sharedInstance] showAd:isTop]; }
    void NativeBanner_HideAd() { [[NativeBannerManager sharedInstance] hideAd]; }
}