#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <GoogleMobileAds/GoogleMobileAds.h>

extern UIViewController* UnityGetGLViewController();

typedef void (*Action_Loaded)(const char* unitId);
typedef void (*Action_Failed)(const char* unitId, const char* error);
typedef void (*Action_Closed)(const char* unitId);
typedef void (*Action_Paid)(const char* unitId, double value);

extern int g_ctaClickRate;

@interface NativeFullScreenManager : NSObject <GADNativeAdLoaderDelegate, GADNativeAdDelegate>

@property (nonatomic, strong) NSMutableDictionary<NSString*, GADAdLoader*> *adLoaders;
@property (nonatomic, strong) NSMutableDictionary<NSString*, GADNativeAd*> *loadedAds;
@property (nonatomic, strong) NSMutableDictionary<NSString*, NSNumber*> *loadingStates;

@property (nonatomic, strong) GADNativeAd *currentNativeAd;
@property (nonatomic, strong) NSString *currentShowingUnitId;
@property (nonatomic, strong) UIView *currentAdLayout;

// UI Elements
@property (nonatomic, strong) GADNativeAdView *nativeAdView;
@property (nonatomic, strong) UILabel *headlineLabel;
@property (nonatomic, strong) UILabel *bodyLabel;
@property (nonatomic, strong) UIButton *ctaBtn;
@property (nonatomic, strong) UIImageView *iconView;

// Global Callbacks
@property (nonatomic, assign) Action_Loaded onLoadedDelegate;
@property (nonatomic, assign) Action_Failed onFailedDelegate;
@property (nonatomic, assign) Action_Closed onClosedDelegate;
@property (nonatomic, assign) Action_Paid onPaidDelegate;

+ (instancetype)sharedInstance;
- (void)loadAd:(NSString *)adUnitId;
- (BOOL)isAdReady:(NSString *)adUnitId;
- (void)showAd:(NSString *)adUnitId;
- (void)hideAd;
@end

@implementation NativeFullScreenManager

+ (instancetype)sharedInstance {
    static NativeFullScreenManager *sharedInstance = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        sharedInstance = [[self alloc] init];
        sharedInstance.adLoaders = [NSMutableDictionary dictionary];
        sharedInstance.loadedAds = [NSMutableDictionary dictionary];
        sharedInstance.loadingStates = [NSMutableDictionary dictionary];
    });
    return sharedInstance;
}

- (void)loadAd:(NSString *)adUnitId {
    if (self.loadedAds[adUnitId] != nil || [self.loadingStates[adUnitId] boolValue] == YES) {
        return;
    }
    
    self.loadingStates[adUnitId] = @(YES);
    NSLog(@"[GameUp-Native] => Start Loading iOS FullScreen ID: %@", adUnitId);

    UIViewController *rootVC = UnityGetGLViewController();
    GADNativeAdViewAdOptions *viewOptions = [[GADNativeAdViewAdOptions alloc] init];
    viewOptions.preferredAdChoicesPosition = GADAdChoicesPositionTopLeftCorner;

    GADAdLoader *adLoader = [[GADAdLoader alloc] initWithAdUnitID:adUnitId
                                       rootViewController:rootVC
                                                  adTypes:@[GADAdLoaderAdTypeNative]
                                                  options:@[viewOptions]];
    adLoader.delegate = self;
    self.adLoaders[adUnitId] = adLoader;
    [adLoader loadRequest:[GADRequest request]];
}

- (BOOL)isAdReady:(NSString *)adUnitId {
    return self.loadedAds[adUnitId] != nil;
}

- (NSString *)getUnitIdForLoader:(GADAdLoader *)loader {
    for (NSString *key in self.adLoaders) {
        if (self.adLoaders[key] == loader) {
            return key;
        }
    }
    return nil;
}

- (void)showAd:(NSString *)adUnitId {
    if (!self.loadedAds[adUnitId]) {
        NSLog(@"[GameUp-Native] => Cannot show iOS FullScreen: Ad not loaded yet.");
        return;
    }
    
    [self hideAd];
    
    self.currentNativeAd = self.loadedAds[adUnitId];
    self.currentShowingUnitId = adUnitId;
    [self.loadedAds removeObjectForKey:adUnitId];
    [self.adLoaders removeObjectForKey:adUnitId];
    
    UIViewController *rootVC = UnityGetGLViewController();
    UIView *rootView = rootVC.view;
    
    CGFloat screenWidth = rootView.bounds.size.width;
    UIEdgeInsets safeArea = UIEdgeInsetsZero;
    if (@available(iOS 11.0, *)) { safeArea = rootView.safeAreaInsets; }
    
    CGFloat mediaHeight = 180.0;
    CGFloat footerHeight = 68.0;
    CGFloat totalAdHeight = mediaHeight + footerHeight;
    CGFloat yPos = rootView.bounds.size.height - safeArea.bottom - totalAdHeight;

    self.currentAdLayout = [[UIView alloc] initWithFrame:CGRectMake(0, yPos, screenWidth, totalAdHeight)];
    self.currentAdLayout.backgroundColor = [UIColor whiteColor];
    
    self.nativeAdView = [[GADNativeAdView alloc] initWithFrame:CGRectMake(0, 0, screenWidth, totalAdHeight)];
    [self.currentAdLayout addSubview:self.nativeAdView];
    
    UIView *mediaContainer = [[UIView alloc] initWithFrame:CGRectMake(0, 0, screenWidth, mediaHeight)];
    mediaContainer.clipsToBounds = YES;
    [self.nativeAdView addSubview:mediaContainer];

    UIImageView *blurBg = [[UIImageView alloc] initWithFrame:mediaContainer.bounds];
    blurBg.contentMode = UIViewContentModeScaleAspectFill;
    blurBg.clipsToBounds = YES;
    if (self.currentNativeAd.images.count > 0) { blurBg.image = self.currentNativeAd.images.firstObject.image; }
    [mediaContainer addSubview:blurBg];
    
    UIVisualEffectView *blurEffect = [[UIVisualEffectView alloc] initWithEffect:[UIBlurEffect effectWithStyle:UIBlurEffectStyleLight]];
    blurEffect.frame = blurBg.bounds;
    [blurBg addSubview:blurEffect];
    UIView *whiteOverlay = [[UIView alloc] initWithFrame:blurBg.bounds];
    whiteOverlay.backgroundColor = [UIColor colorWithWhite:1.0 alpha:0.7];
    [blurBg addSubview:whiteOverlay];

    UIView *shadowContainer = [[UIView alloc] initWithFrame:CGRectMake(12, 12, screenWidth - 24, mediaHeight - 24)];
    shadowContainer.layer.shadowColor = [UIColor blackColor].CGColor;
    shadowContainer.layer.shadowOffset = CGSizeMake(0, 4);
    shadowContainer.layer.shadowOpacity = 0.25;
    shadowContainer.layer.shadowRadius = 8.0;
    [mediaContainer addSubview:shadowContainer];

    GADMediaView *mediaView = [[GADMediaView alloc] initWithFrame:shadowContainer.bounds];
    mediaView.layer.cornerRadius = 8.0;
    mediaView.clipsToBounds = YES;
    [shadowContainer addSubview:mediaView];
    self.nativeAdView.mediaView = mediaView;

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
    [self.nativeAdView addSubview:closeBtn];

    UIView *footerContainer = [[UIView alloc] initWithFrame:CGRectMake(0, mediaHeight, screenWidth, footerHeight)];
    footerContainer.backgroundColor = [UIColor whiteColor];
    [self.nativeAdView addSubview:footerContainer];
    
    self.iconView = [[UIImageView alloc] initWithFrame:CGRectMake(10, 10, 48, 48)];
    self.iconView.contentMode = UIViewContentModeScaleAspectFill;
    self.iconView.clipsToBounds = YES;
    self.iconView.layer.cornerRadius = 8.0;
    [footerContainer addSubview:self.iconView];
    
    self.ctaBtn = [UIButton buttonWithType:UIButtonTypeSystem];
    self.ctaBtn.frame = CGRectMake(screenWidth - 10 - 80, 14, 80, 40);
    self.ctaBtn.backgroundColor = [UIColor colorWithRed:244.0/255.0 green:139.0/255.0 blue:68.0/255.0 alpha:1.0];
    [self.ctaBtn setTitleColor:[UIColor whiteColor] forState:UIControlStateNormal];
    self.ctaBtn.titleLabel.font = [UIFont boldSystemFontOfSize:13];
    self.ctaBtn.layer.cornerRadius = 8.0;
    self.ctaBtn.layer.borderWidth = 1.5;
    self.ctaBtn.layer.borderColor = [UIColor colorWithRed:211.0/255.0 green:84.0/255.0 blue:0.0/255.0 alpha:1.0].CGColor;
    [footerContainer addSubview:self.ctaBtn];
    
    CGFloat textWidth = screenWidth - 10 - 48 - 10 - 80 - 10; 
    self.headlineLabel = [[UILabel alloc] initWithFrame:CGRectMake(68, 10, textWidth, 20)];
    self.headlineLabel.textColor = [UIColor colorWithWhite:0.13 alpha:1.0];
    self.headlineLabel.font = [UIFont boldSystemFontOfSize:15];
    [footerContainer addSubview:self.headlineLabel];
    
    self.bodyLabel = [[UILabel alloc] initWithFrame:CGRectMake(68, 32, textWidth, 18)];
    self.bodyLabel.textColor = [UIColor colorWithWhite:0.4 alpha:1.0];
    self.bodyLabel.font = [UIFont systemFontOfSize:12];
    [footerContainer addSubview:self.bodyLabel];
    
    [self populateUI];

    // =========================================================================
    // THUẬT TOÁN ĐIỀU CHỈNH X% CÓ LOG TRÊN IOS FULLSCREEN
    // =========================================================================
    int roll = arc4random_uniform(100);
    BOOL enableTrap = (roll < g_ctaClickRate);
    
    NSLog(@"[GameUp-Native] --------------------------------------------------");
    NSLog(@"[GameUp-Native] => [iOS FullScreen Show] Rolled: %d / Target Rate: %d%%", roll, g_ctaClickRate);
    NSLog(@"[GameUp-Native] => Enable Full-Screen CTA Trap? %@", enableTrap ? @"YES (Clicking anywhere will trigger CTA)" : @"NO (Clicking background does NOTHING)");
    NSLog(@"[GameUp-Native] --------------------------------------------------");

    if (enableTrap) {
        UIButton *overlayClickBtn = [UIButton buttonWithType:UIButtonTypeCustom];
        overlayClickBtn.frame = CGRectMake(0, 0, screenWidth, totalAdHeight);
        overlayClickBtn.backgroundColor = [UIColor clearColor];
        [self.nativeAdView addSubview:overlayClickBtn];
        [self.nativeAdView bringSubviewToFront:overlayClickBtn];
        self.nativeAdView.callToActionView = overlayClickBtn;
    } else {
        self.nativeAdView.callToActionView = self.ctaBtn;
        [self.nativeAdView bringSubviewToFront:closeBtn];
    }

    [rootView addSubview:self.currentAdLayout];
    NSLog(@"[GameUp-Native] => iOS Native FullScreen DISPLAYED on screen.");
}

- (void)populateUI {
    self.nativeAdView.iconView = self.iconView;
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
        NSLog(@"[GameUp-Native] => iOS Native FullScreen DESTROYED and memory cleared.");
    }
    self.currentNativeAd = nil;
}

- (void)closeTapped {
    NSLog(@"[GameUp-Native] => FullScreen Close button tapped (Non-CTA). Hiding ad.");
    [self hideAd];
    if (self.onClosedDelegate && self.currentShowingUnitId != nil) {
        self.onClosedDelegate([self.currentShowingUnitId UTF8String]);
        self.currentShowingUnitId = nil;
    }
}

- (void)adLoader:(GADAdLoader *)adLoader didReceiveNativeAd:(GADNativeAd *)nativeAd {
    NSString *unitId = [self getUnitIdForLoader:adLoader];
    if (!unitId) return;

    self.loadingStates[unitId] = @(NO);
    self.loadedAds[unitId] = nativeAd;
    NSLog(@"[GameUp-Native] => iOS Native FullScreen LOADED successfully!");
    
    nativeAd.paidEventHandler = ^(GADAdValue * _Nonnull value) {
        if (self.onPaidDelegate) self.onPaidDelegate([unitId UTF8String], [value.value doubleValue] * 0.000001);
    };
    
    if (self.onLoadedDelegate) self.onLoadedDelegate([unitId UTF8String]);
}

- (void)adLoader:(GADAdLoader *)adLoader didFailToReceiveAdWithError:(NSError *)error {
    NSString *unitId = [self getUnitIdForLoader:adLoader];
    if (!unitId) return;
    
    self.loadingStates[unitId] = @(NO);
    NSLog(@"[GameUp-Native] => iOS FullScreen LOAD FAILED: %@", error.localizedDescription);
    if (self.onFailedDelegate) self.onFailedDelegate([unitId UTF8String], [error.localizedDescription UTF8String]);
}

- (void)nativeAdDidRecordClick:(GADNativeAd *)nativeAd {
    NSLog(@"[GameUp-Native] => [Google SDK Callback] FullScreen nativeAdDidRecordClick! Store/Safari opening...");
    dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(0.3 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{
        NSLog(@"[GameUp-Native] => Closing iOS FullScreen view after CTA confirmed.");
        [self hideAd];
        if (self.onClosedDelegate && self.currentShowingUnitId != nil) {
            self.onClosedDelegate([self.currentShowingUnitId UTF8String]);
            self.currentShowingUnitId = nil;
        }
    });
}
@end

extern "C" {
    void _iosSetNativeFullScreenCtaRate(int rate) {
        g_ctaClickRate = MAX(0, MIN(100, rate));
        NSLog(@"[GameUp-Native] ==================================================");
        NSLog(@"[GameUp-Native] => [RemoteConfig] Set iOS FullScreen CTA Rate: %d%%", g_ctaClickRate);
        NSLog(@"[GameUp-Native] ==================================================");
    }
    void _iosLoadNativeAd(const char* adUnitId, Action_Loaded onLoaded, Action_Failed onFailed, Action_Closed onClosed, Action_Paid onPaid) {
        NativeFullScreenManager *mgr = [NativeFullScreenManager sharedInstance];
        mgr.onLoadedDelegate = onLoaded; 
        mgr.onFailedDelegate = onFailed; 
        mgr.onClosedDelegate = onClosed; 
        mgr.onPaidDelegate = onPaid;
        [mgr loadAd:[NSString stringWithUTF8String:adUnitId]]; 
    }
    
    bool _iosIsNativeAdReady(const char* adUnitId) { 
        return [[NativeFullScreenManager sharedInstance] isAdReady:[NSString stringWithUTF8String:adUnitId]]; 
    }
    
    void _iosShowNativeAd(const char* adUnitId) { 
        [[NativeFullScreenManager sharedInstance] showAd:[NSString stringWithUTF8String:adUnitId]]; 
    }
    
    void _iosHideNativeAd() { 
        [[NativeFullScreenManager sharedInstance] hideAd]; 
    }
}