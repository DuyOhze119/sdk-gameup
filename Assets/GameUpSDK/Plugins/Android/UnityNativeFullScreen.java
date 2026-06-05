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
    public interface INativeAdCallback {
        void onAdLoaded();
        void onAdFailedToLoad(String error);
        void onAdClosed();
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
                // Đưa logo AdChoices qua góc trái
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

        // Nút Đóng (Tắt Quảng Cáo)
        View btnClose = mainContainer.findViewById(activity.getResources().getIdentifier("btn_close_ad", "id", activity.getPackageName()));
        btnClose.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                hideAd(activity);
            }
        });

        // Đẩy lên màn hình Unity
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