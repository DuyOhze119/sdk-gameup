package com.plugins.nativebridge;

import android.app.Activity;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.FrameLayout;
import android.widget.ImageView;
import android.widget.TextView;
import com.google.android.gms.ads.AdLoader;
import com.google.android.gms.ads.AdRequest;
import com.google.android.gms.ads.AdListener;
import com.google.android.gms.ads.LoadAdError;
import com.google.android.gms.ads.nativead.MediaView;
import com.google.android.gms.ads.nativead.NativeAd;
import com.google.android.gms.ads.nativead.NativeAdView;

public class UnityNativeFullScreen {
    // THÊM SỰ KIỆN PAID VÀO INTERFACE
    public interface INativeAdCallback {
        void onAdLoaded();
        void onAdFailedToLoad(String error);
        void onAdClosed();
        void onAdPaid(double value); // <--- MỚI
    }

    private static View mainContainer;
    private static NativeAd loadedAd = null;
    private static boolean isAdLoading = false; 
    private static INativeAdCallback mCallback;

    public static void loadAd(final Activity activity, final String adUnitId, final INativeAdCallback callback) {
        mCallback = callback; 
        if (loadedAd != null || isAdLoading) return;

        isAdLoading = true;
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                com.google.android.gms.ads.nativead.NativeAdOptions adOptions = 
                    new com.google.android.gms.ads.nativead.NativeAdOptions.Builder()
                        .setAdChoicesPlacement(com.google.android.gms.ads.nativead.NativeAdOptions.ADCHOICES_TOP_LEFT)
                        .build();

                AdLoader adLoader = new AdLoader.Builder(activity, adUnitId)
                    .forNativeAd(new NativeAd.OnNativeAdLoadedListener() {
                        @Override
                        public void onNativeAdLoaded(NativeAd nativeAd) {
                            loadedAd = nativeAd;
                            isAdLoading = false;
                            
                            // ĐĂNG KÝ HỨNG DOANH THU TỪ GOOGLE TRẢ VỀ
                            loadedAd.setOnPaidEventListener(new com.google.android.gms.ads.OnPaidEventListener() {
                                @Override
                                public void onPaidEvent(com.google.android.gms.ads.AdValue adValue) {
                                    if (mCallback != null) {
                                        // Đổi từ Micros sang USD
                                        mCallback.onAdPaid(adValue.getValueMicros() * 0.000001);
                                    }
                                }
                            });

                            if (mCallback != null) mCallback.onAdLoaded();
                        }
                    })
                    .withAdListener(new AdListener() {
                        @Override
                        public void onAdFailedToLoad(LoadAdError adError) {
                            super.onAdFailedToLoad(adError);
                            isAdLoading = false;
                            loadedAd = null;
                            if (mCallback != null) mCallback.onAdFailedToLoad(adError.getMessage());
                        }
                    })
                    .withNativeAdOptions(adOptions)
                    .build();
                adLoader.loadAd(new AdRequest.Builder().build());
            }
        });
    }

    public static boolean isAdLoaded() {
        return loadedAd != null;
    }

    public static void showAd(final Activity activity) {
        if (loadedAd == null) return; 
        
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                renderFullScreenAd(activity, loadedAd);
            }
        });
    }

    private static void renderFullScreenAd(final Activity activity, final NativeAd nativeAd) {
        int layoutId = activity.getResources().getIdentifier("gameup_native_fullscreen", "layout", activity.getPackageName());
        mainContainer = LayoutInflater.from(activity).inflate(layoutId, null);

        NativeAdView adView = mainContainer.findViewById(activity.getResources().getIdentifier("native_ad_view", "id", activity.getPackageName()));
        MediaView mediaView = mainContainer.findViewById(activity.getResources().getIdentifier("ad_media", "id", activity.getPackageName()));
        TextView headlineView = mainContainer.findViewById(activity.getResources().getIdentifier("ad_headline", "id", activity.getPackageName()));
        TextView bodyView = mainContainer.findViewById(activity.getResources().getIdentifier("ad_body", "id", activity.getPackageName()));
        Button ctaView = mainContainer.findViewById(activity.getResources().getIdentifier("ad_call_to_action", "id", activity.getPackageName()));
        ImageView iconView = mainContainer.findViewById(activity.getResources().getIdentifier("ad_app_icon", "id", activity.getPackageName()));
        com.google.android.gms.ads.nativead.AdChoicesView adChoicesView = mainContainer.findViewById(activity.getResources().getIdentifier("ad_choices", "id", activity.getPackageName()));

        adView.setMediaView(mediaView);
        adView.setHeadlineView(headlineView);
        adView.setBodyView(bodyView);
        adView.setCallToActionView(ctaView);
        adView.setIconView(iconView);
        adView.setAdChoicesView(adChoicesView);

        headlineView.setText(nativeAd.getHeadline());

        if (nativeAd.getBody() == null) bodyView.setVisibility(View.GONE);
        else { bodyView.setVisibility(View.VISIBLE); bodyView.setText(nativeAd.getBody()); }

        if (nativeAd.getCallToAction() == null) ctaView.setVisibility(View.INVISIBLE);
        else { ctaView.setVisibility(View.VISIBLE); ctaView.setText(nativeAd.getCallToAction()); }

        if (nativeAd.getIcon() == null) iconView.setVisibility(View.GONE);
        else { iconView.setVisibility(View.VISIBLE); iconView.setImageDrawable(nativeAd.getIcon().getDrawable()); }

        adView.setNativeAd(nativeAd);

        ImageView blurBg = mainContainer.findViewById(activity.getResources().getIdentifier("ad_blur_bg", "id", activity.getPackageName()));
        if (blurBg != null && nativeAd.getImages() != null && nativeAd.getImages().size() > 0) {
            try {
                android.graphics.drawable.Drawable drawable = nativeAd.getImages().get(0).getDrawable();
                if (drawable instanceof android.graphics.drawable.BitmapDrawable) {
                    android.graphics.Bitmap bitmap = ((android.graphics.drawable.BitmapDrawable) drawable).getBitmap();
                    int w = Math.round(bitmap.getWidth() * 0.1f);
                    int h = Math.round(bitmap.getHeight() * 0.1f);
                    if (w > 0 && h > 0) {
                        android.graphics.Bitmap scaled = android.graphics.Bitmap.createScaledBitmap(bitmap, w, h, true);
                        blurBg.setImageBitmap(scaled);
                        blurBg.setColorFilter(android.graphics.Color.argb(180, 255, 255, 255)); 
                    }
                }
            } catch (Exception ignored) { }
        }

        View btnClose = mainContainer.findViewById(activity.getResources().getIdentifier("btn_close_ad", "id", activity.getPackageName()));
        btnClose.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                hideAd(activity);
            }
        });

        FrameLayout.LayoutParams rootParams = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT);
        activity.addContentView(mainContainer, rootParams);
    }

    public static void hideAd(final Activity activity) {
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                if (mainContainer != null && mainContainer.getParent() != null) {
                    ((ViewGroup) mainContainer.getParent()).removeView(mainContainer);
                    mainContainer = null;
                }
                if (loadedAd != null) {
                    loadedAd.destroy();
                    loadedAd = null; 
                }
                if (mCallback != null) mCallback.onAdClosed();
            }
        });
    }
}