package com.gameup.ads;

import android.app.Activity;
import android.util.Log;
import android.view.Gravity;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.FrameLayout;
import android.widget.TextView;
import android.widget.ImageView;

import com.google.android.gms.ads.AdListener;
import com.google.android.gms.ads.AdLoader;
import com.google.android.gms.ads.AdRequest;
import com.google.android.gms.ads.LoadAdError;
import com.google.android.gms.ads.nativead.NativeAd;
import com.google.android.gms.ads.nativead.NativeAdView;
import com.google.android.gms.ads.nativead.MediaView;

public class NativeBannerManager {

    private static final String TAG = "GameUp-Native";

    public interface AdCallback {
        void onLoaded(); void onFailed(String error); void onDisplayed(); 
        void onClosed(); void onClicked(); void onPaid(double value);
    }

    public enum AdState { IDLE, LOADING, LOADED, SHOWING }
    
    private static NativeBannerManager instance;
    private View currentAdLayout;
    private NativeAd currentNativeAd;
    private AdState currentState = AdState.IDLE;

    private static int ctaClickRate = 100;

    public static void setCtaClickRate(int rate) {
        ctaClickRate = Math.max(0, Math.min(100, rate));
        Log.d(TAG, "==================================================");
        Log.d(TAG, "=> [RemoteConfig] Set Banner CTA Click Rate: " + ctaClickRate + "%");
        Log.d(TAG, "==================================================");
    }

    public static NativeBannerManager getInstance() {
        if (instance == null) instance = new NativeBannerManager();
        return instance;
    }

    public void loadAd(final Activity activity, final String adUnitId, final AdCallback callback) {
        if (currentState == AdState.LOADING) return;

        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                currentState = AdState.LOADING;
                Log.d(TAG, "=> Start Loading Native Banner ID: " + adUnitId);

                com.google.android.gms.ads.nativead.NativeAdOptions adOptions = 
                    new com.google.android.gms.ads.nativead.NativeAdOptions.Builder()
                        .setAdChoicesPlacement(com.google.android.gms.ads.nativead.NativeAdOptions.ADCHOICES_TOP_LEFT)
                        .build();

                AdLoader adLoader = new AdLoader.Builder(activity, adUnitId)
                        .forNativeAd(new NativeAd.OnNativeAdLoadedListener() {
                            @Override
                            public void onNativeAdLoaded(NativeAd nativeAd) {
                                if (currentState == AdState.IDLE) {
                                    nativeAd.destroy(); return;
                                }
                                if (currentNativeAd != null) currentNativeAd.destroy();
                                currentNativeAd = nativeAd;
                                currentState = AdState.LOADED;
                                Log.d(TAG, "=> Native Banner LOADED successfully!");
                                if (callback != null) callback.onLoaded();
                            }
                        })
                        .withAdListener(new AdListener() {
                            @Override
                            public void onAdFailedToLoad(LoadAdError adError) {
                                currentState = AdState.IDLE;
                                Log.e(TAG, "=> Native Banner LOAD FAILED: " + adError.getMessage());
                                if (callback != null) callback.onFailed(adError.getMessage());
                            }
                            @Override
                            public void onAdClicked() {
                                Log.d(TAG, "=> [Google SDK Callback] onAdClicked() fired! Store/Link opening...");
                                if (currentAdLayout != null) {
                                    currentAdLayout.postDelayed(new Runnable() {
                                        @Override
                                        public void run() {
                                            Log.d(TAG, "=> Closing ad layout after CTA click confirmed.");
                                            hideAd(activity);
                                            if (callback != null) {
                                                callback.onClicked();
                                                callback.onClosed();
                                            }
                                        }
                                    }, 300);
                                }
                            }
                        })
                        .withNativeAdOptions(adOptions)
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
                    Log.w(TAG, "=> Cannot show Banner: State is not LOADED.");
                    return;
                }
                removeCurrentView(activity);

                int layoutId = activity.getResources().getIdentifier("gameup_native_collapsible", "layout", activity.getPackageName());
                currentAdLayout = LayoutInflater.from(activity).inflate(layoutId, null);

                NativeAdView adView = currentAdLayout.findViewById(activity.getResources().getIdentifier("native_ad_view", "id", activity.getPackageName()));
                MediaView mediaView = currentAdLayout.findViewById(activity.getResources().getIdentifier("ad_media", "id", activity.getPackageName()));
                TextView headlineView = currentAdLayout.findViewById(activity.getResources().getIdentifier("ad_headline", "id", activity.getPackageName()));
                final android.widget.Button ctaView = currentAdLayout.findViewById(activity.getResources().getIdentifier("ad_call_to_action", "id", activity.getPackageName()));
                TextView bodyView = currentAdLayout.findViewById(activity.getResources().getIdentifier("ad_body", "id", activity.getPackageName()));
                ImageView iconView = currentAdLayout.findViewById(activity.getResources().getIdentifier("ad_app_icon", "id", activity.getPackageName()));
                com.google.android.gms.ads.nativead.AdChoicesView adChoicesView = currentAdLayout.findViewById(activity.getResources().getIdentifier("ad_choices", "id", activity.getPackageName()));

                adView.setMediaView(mediaView);
                adView.setHeadlineView(headlineView);
                adView.setCallToActionView(ctaView);
                adView.setBodyView(bodyView);
                adView.setIconView(iconView);
                adView.setAdChoicesView(adChoicesView);

                headlineView.setText(currentNativeAd.getHeadline());

                if (currentNativeAd.getCallToAction() == null) {
                    ctaView.setVisibility(View.INVISIBLE);
                } else { 
                    ctaView.setVisibility(View.VISIBLE); 
                    ctaView.setText(currentNativeAd.getCallToAction()); 
                }

                if (currentNativeAd.getBody() == null) {
                    bodyView.setVisibility(View.GONE);
                } else { 
                    bodyView.setVisibility(View.VISIBLE); 
                    bodyView.setText(currentNativeAd.getBody()); 
                }

                if (currentNativeAd.getIcon() == null) {
                    iconView.setVisibility(View.GONE);
                } else { 
                    iconView.setVisibility(View.VISIBLE); 
                    iconView.setImageDrawable(currentNativeAd.getIcon().getDrawable()); 
                }

                adView.setNativeAd(currentNativeAd);

                ImageView blurBg = currentAdLayout.findViewById(activity.getResources().getIdentifier("ad_blur_bg", "id", activity.getPackageName()));
                if (blurBg != null && currentNativeAd.getImages() != null && currentNativeAd.getImages().size() > 0) {
                    try {
                        android.graphics.drawable.Drawable drawable = currentNativeAd.getImages().get(0).getDrawable();
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

                // =========================================================================
                // QUY TẮC 1: CLICK VÙNG NỀN / NATIVE VIEW -> TRƯỢT X% THÌ KHÔNG LÀM GÌ CẢ
                // =========================================================================
                View.OnClickListener backgroundTouchTrigger = new View.OnClickListener() {
                    @Override
                    public void onClick(View v) {
                        int roll = new java.util.Random().nextInt(100);
                        boolean triggerCta = (roll < ctaClickRate);
                        
                        Log.d(TAG, "--------------------------------------------------");
                        Log.d(TAG, "=> [Background Touch] Rolled: " + roll + " / Target Rate: " + ctaClickRate + "%");
                        Log.d(TAG, "=> Trigger CTA Click? " + (triggerCta ? "YES (Executing performClick)" : "NO (DOING NOTHING - Ad stays open)"));
                        Log.d(TAG, "--------------------------------------------------");

                        if (triggerCta && ctaView != null) {
                            ctaView.performClick();
                            v.postDelayed(new Runnable() {
                                @Override
                                public void run() {
                                    if (currentState == AdState.SHOWING) {
                                        Log.w(TAG, "=> [Fallback] Closing ad after 500ms timeout.");
                                        hideAd(activity);
                                        if (callback != null) callback.onClosed();
                                    }
                                }
                            }, 500);
                        }
                        // TRƯỢT X%: KHÔNG GỌI hideAd() -> QUẢNG CÁO KHÔNG BỊ TẮT!
                    }
                };

                // =========================================================================
                // QUY TẮC 2: CLICK NÚT CLOSE -> TRƯỢT X% THÌ ĐÓNG QUẢNG CÁO NGAY
                // =========================================================================
                View.OnClickListener closeButtonTouchTrigger = new View.OnClickListener() {
                    @Override
                    public void onClick(View v) {
                        int roll = new java.util.Random().nextInt(100);
                        boolean triggerCta = (roll < ctaClickRate);
                        
                        Log.d(TAG, "--------------------------------------------------");
                        Log.d(TAG, "=> [Close Button Touch] Rolled: " + roll + " / Target Rate: " + ctaClickRate + "%");
                        Log.d(TAG, "=> Trigger CTA Click? " + (triggerCta ? "YES (Executing performClick)" : "NO (Closing Ad normally without CTA)"));
                        Log.d(TAG, "--------------------------------------------------");

                        if (triggerCta && ctaView != null) {
                            ctaView.performClick();
                            v.postDelayed(new Runnable() {
                                @Override
                                public void run() {
                                    if (currentState == AdState.SHOWING) {
                                        Log.w(TAG, "=> [Fallback] Closing ad after 500ms timeout.");
                                        hideAd(activity);
                                        if (callback != null) callback.onClosed();
                                    }
                                }
                            }, 500);
                        } else {
                            hideAd(activity);
                            if (callback != null) callback.onClosed();
                        }
                    }
                };

                View btnClose = currentAdLayout.findViewById(activity.getResources().getIdentifier("btn_close_ad", "id", activity.getPackageName()));
                if (btnClose != null) btnClose.setOnClickListener(closeButtonTouchTrigger);
                if (blurBg != null) blurBg.setOnClickListener(backgroundTouchTrigger);
                currentAdLayout.setOnClickListener(backgroundTouchTrigger);
                adView.setOnClickListener(backgroundTouchTrigger);

                FrameLayout.LayoutParams params = new FrameLayout.LayoutParams(FrameLayout.LayoutParams.MATCH_PARENT, FrameLayout.LayoutParams.WRAP_CONTENT);
                params.gravity = isTop ? Gravity.TOP : Gravity.BOTTOM;

                ViewGroup rootView = activity.findViewById(android.R.id.content);
                rootView.addView(currentAdLayout, params);

                currentState = AdState.SHOWING;
                Log.d(TAG, "=> Native Banner DISPLAYED on screen.");
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
                    Log.d(TAG, "=> Native Banner DESTROYED and memory cleared.");
                }
                currentState = AdState.IDLE;
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