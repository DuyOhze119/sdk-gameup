package com.gameup.ads;

import android.app.Activity;
import android.util.Log;
import android.view.Gravity;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.FrameLayout;
import android.widget.TextView;

import com.google.android.gms.ads.AdListener;
import com.google.android.gms.ads.AdLoader;
import com.google.android.gms.ads.AdRequest;
import com.google.android.gms.ads.LoadAdError;
import com.google.android.gms.ads.nativead.NativeAd;
import com.google.android.gms.ads.nativead.NativeAdView;
import com.google.android.gms.ads.nativead.MediaView;

public class NativeBannerManager {

    public interface AdCallback {
        void onLoaded();
        void onFailed(String error);
        void onDisplayed();
        void onClosed();
        void onClicked();
        void onPaid(double value);
    }

    // [QUẢN LÝ TRẠNG THÁI CHẶT CHẼ]
    public enum AdState { IDLE, LOADING, LOADED, SHOWING }
    
    private static NativeBannerManager instance;
    private View currentAdLayout;
    private NativeAd currentNativeAd;
    private AdState currentState = AdState.IDLE;

    private final String TAG = "GameUp_NativeJava";

    public static NativeBannerManager getInstance() {
        if (instance == null) {
            instance = new NativeBannerManager();
        }
        return instance;
    }

    public void loadAd(final Activity activity, final String adUnitId, final AdCallback callback) {
        if (currentState == AdState.LOADING) {
            Log.d(TAG, "Ad is already loading. Ignored.");
            return;
        }

        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                currentState = AdState.LOADING;

                AdLoader adLoader = new AdLoader.Builder(activity, adUnitId)
                        .forNativeAd(new NativeAd.OnNativeAdLoadedListener() {
                            @Override
                            public void onNativeAdLoaded(NativeAd nativeAd) {
                                // Nếu đã gọi Destroy trong lúc đang Load ngầm, thì vứt luôn Ad mới
                                if (currentState == AdState.IDLE) {
                                    nativeAd.destroy();
                                    return;
                                }

                                if (currentNativeAd != null) currentNativeAd.destroy();
                                currentNativeAd = nativeAd;
                                currentState = AdState.LOADED;
                                
                                Log.d(TAG, "Native Ad Loaded Successfully.");
                                if (callback != null) callback.onLoaded();
                            }
                        })
                        .withAdListener(new AdListener() {
                            @Override
                            public void onAdFailedToLoad(LoadAdError adError) {
                                currentState = AdState.IDLE;
                                Log.e(TAG, "Native Ad Failed: " + adError.getMessage());
                                if (callback != null) callback.onFailed(adError.getMessage());
                            }
                            @Override
                            public void onAdClicked() {
                                if (callback != null) callback.onClicked();
                            }
                        })
                        .build();

                adLoader.loadAd(new AdRequest.Builder().build());
            }
        });
    }

    public void showAd(final Activity activity, final boolean isTop, final AdCallback callback) {
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                if (currentState != AdState.LOADED || currentNativeAd == null) {
                    Log.e(TAG, "Cannot show: Ad is not ready. Current state: " + currentState);
                    return;
                }

                // Xóa view cũ (nếu có kẹt lại)
                removeCurrentView(activity);

                int layoutId = activity.getResources().getIdentifier("gameup_native_collapsible", "layout", activity.getPackageName());
                currentAdLayout = LayoutInflater.from(activity).inflate(layoutId, null);

                NativeAdView adView = currentAdLayout.findViewById(activity.getResources().getIdentifier("native_ad_view", "id", activity.getPackageName()));
                MediaView mediaView = currentAdLayout.findViewById(activity.getResources().getIdentifier("ad_media", "id", activity.getPackageName()));
                TextView headlineView = currentAdLayout.findViewById(activity.getResources().getIdentifier("ad_headline", "id", activity.getPackageName()));
                
                adView.setMediaView(mediaView);
                adView.setHeadlineView(headlineView);
                headlineView.setText(currentNativeAd.getHeadline());
                adView.setNativeAd(currentNativeAd);

                View btnClose = currentAdLayout.findViewById(activity.getResources().getIdentifier("btn_close_ad", "id", activity.getPackageName()));
                btnClose.setOnClickListener(new View.OnClickListener() {
                    @Override
                    public void onClick(View v) {
                        // Người dùng bấm tắt
                        hideAd(activity);
                        if (callback != null) callback.onClosed();
                    }
                });

                FrameLayout.LayoutParams params = new FrameLayout.LayoutParams(
                        FrameLayout.LayoutParams.MATCH_PARENT, 
                        FrameLayout.LayoutParams.WRAP_CONTENT
                );
                params.gravity = isTop ? Gravity.TOP : Gravity.BOTTOM;

                ViewGroup rootView = activity.findViewById(android.R.id.content);
                rootView.addView(currentAdLayout, params);

                currentState = AdState.SHOWING;
                Log.d(TAG, "Native Ad Displayed.");
                if (callback != null) callback.onDisplayed();
            }
        });
    }

    public void hideAd(final Activity activity) {
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                removeCurrentView(activity);
                
                if (currentNativeAd != null) {
                    currentNativeAd.destroy();
                    currentNativeAd = null;
                }
                
                currentState = AdState.IDLE;
                Log.d(TAG, "Native Ad Destroyed and Cleared.");
            }
        });
    }

    private void removeCurrentView(Activity activity) {
        if (currentAdLayout != null) {
            ViewGroup rootView = activity.findViewById(android.R.id.content);
            rootView.removeView(currentAdLayout);
            currentAdLayout = null;
        }
    }
}