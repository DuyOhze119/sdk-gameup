package com.plugins.nativebridge;

import android.app.Activity;
import android.graphics.Color;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.FrameLayout;
import android.widget.TextView;
import com.google.android.gms.ads.AdLoader;
import com.google.android.gms.ads.AdRequest;
import com.google.android.gms.ads.AdListener;
import com.google.android.gms.ads.LoadAdError;
import com.google.android.gms.ads.nativead.MediaView;
import com.google.android.gms.ads.nativead.NativeAd;
import com.google.android.gms.ads.nativead.NativeAdView;

public class UnityNativeFullScreen {
    // 1. ĐỊNH NGHĨA INTERFACE CALLBACK
    public interface INativeAdCallback {
        void onAdLoaded();
        void onAdFailedToLoad(String error);
        void onAdClosed();
    }

    private static FrameLayout mainContainer;
    private static NativeAd loadedAd = null;
    private static boolean isAdLoading = false; 
    
    // Lưu trữ callback do Unity truyền sang
    private static INativeAdCallback mCallback;

    // 2. NHẬN CALLBACK QUA HÀM LOAD
    public static void loadAd(final Activity activity, final String adUnitId, final INativeAdCallback callback) {
        mCallback = callback; // Lưu lại để dùng

        if (loadedAd != null || isAdLoading) {
            return;
        }

        isAdLoading = true;
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                AdLoader adLoader = new AdLoader.Builder(activity, adUnitId)
                    .forNativeAd(new NativeAd.OnNativeAdLoadedListener() {
                        @Override
                        public void onNativeAdLoaded(NativeAd nativeAd) {
                            loadedAd = nativeAd;
                            isAdLoading = false;
                            
                            // GỌI CALLBACK THAY VÌ SEND MESSAGE
                            if (mCallback != null) mCallback.onAdLoaded();
                        }
                    })
                    .withAdListener(new AdListener() {
                        @Override
                        public void onAdFailedToLoad(LoadAdError adError) {
                            super.onAdFailedToLoad(adError);
                            isAdLoading = false;
                            loadedAd = null;
                            
                            // GỌI CALLBACK
                            if (mCallback != null) mCallback.onAdFailedToLoad(adError.getMessage());
                        }
                    })
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
        mainContainer = new FrameLayout(activity);
        mainContainer.setBackgroundColor(Color.BLACK);
        FrameLayout.LayoutParams rootParams = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT);
        activity.addContentView(mainContainer, rootParams);

        NativeAdView adView = new NativeAdView(activity);
        adView.setLayoutParams(new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT));

        MediaView mediaView = new MediaView(activity);
        mediaView.setLayoutParams(new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT));
        adView.addView(mediaView);
        adView.setMediaView(mediaView);

        TextView txtHeadline = new TextView(activity);
        txtHeadline.setText(nativeAd.getHeadline());
        txtHeadline.setTextColor(Color.WHITE);
        txtHeadline.setTextSize(20);
        FrameLayout.LayoutParams headlineParams = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.WRAP_CONTENT, ViewGroup.LayoutParams.WRAP_CONTENT);
        headlineParams.gravity = Gravity.BOTTOM | Gravity.START;
        headlineParams.setMargins(50, 0, 50, 280);
        txtHeadline.setLayoutParams(headlineParams);
        adView.addView(txtHeadline);
        adView.setHeadlineView(txtHeadline);

        Button btnCta = new Button(activity);
        btnCta.setText(nativeAd.getCallToAction());
        btnCta.setBackgroundColor(Color.parseColor("#FF4081"));
        btnCta.setTextColor(Color.WHITE);
        FrameLayout.LayoutParams ctaParams = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT);
        ctaParams.gravity = Gravity.BOTTOM;
        ctaParams.setMargins(50, 0, 50, 100);
        btnCta.setLayoutParams(ctaParams);
        adView.addView(btnCta);
        adView.setCallToActionView(btnCta);

        mainContainer.addView(adView);
        adView.setNativeAd(nativeAd);

        android.graphics.drawable.GradientDrawable circleBackground = new android.graphics.drawable.GradientDrawable();
        circleBackground.setShape(android.graphics.drawable.GradientDrawable.OVAL);
        circleBackground.setColor(Color.parseColor("#88000000")); 
        circleBackground.setStroke(3, Color.WHITE); 

        final Button btnClose = new Button(activity);
        btnClose.setBackground(circleBackground);
        btnClose.setTextColor(Color.WHITE);
        btnClose.setTextSize(14); 
        btnClose.setGravity(Gravity.CENTER);
        btnClose.setPadding(0, 0, 0, 0); 
        
        FrameLayout.LayoutParams closeParams = new FrameLayout.LayoutParams(90, 90);
        closeParams.gravity = Gravity.TOP | Gravity.END;
        closeParams.setMargins(0, 60, 40, 0); 
        btnClose.setLayoutParams(closeParams);

        final int[] secondsLeft = {3};
        btnClose.setText(String.valueOf(secondsLeft[0]));
        btnClose.setEnabled(false);

        final android.os.Handler handler = new android.os.Handler();
        final Runnable countdownRunnable = new Runnable() {
            @Override
            public void run() {
                secondsLeft[0]--;
                if (secondsLeft[0] > 0) {
                    btnClose.setText(String.valueOf(secondsLeft[0]));
                    handler.postDelayed(this, 1000);
                } else {
                    btnClose.setText("X");
                    btnClose.setTextSize(16); 
                    btnClose.setEnabled(true);
                    btnClose.setOnClickListener(new View.OnClickListener() {
                        @Override
                        public void onClick(View v) {
                            hideAd(activity);
                        }
                    });
                }
            }
        };
        handler.postDelayed(countdownRunnable, 1000);
        mainContainer.addView(btnClose);
    }

    public static void hideAd(Activity activity) {
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
                
                // GỌI CALLBACK
                if (mCallback != null) mCallback.onAdClosed();
            }
        });
    }
}