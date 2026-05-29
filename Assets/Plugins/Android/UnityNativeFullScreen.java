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
    private static FrameLayout mainContainer;
    private static NativeAd loadedAd = null;
    private static boolean isAdLoading = false; // Trạng thái đang tải quảng cáo

    // 1. Tải trước quảng cáo (Pre-load)
    public static void loadAd(final Activity activity, final String adUnitId) {
        // Nếu đã có quảng cáo sẵn hoặc đang tải thì không tải lại
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
                        }
                    })
                    .withAdListener(new AdListener() {
                        @Override
                        public void onAdFailedToLoad(LoadAdError adError) {
                            super.onAdFailedToLoad(adError);
                            isAdLoading = false;
                            loadedAd = null;
                        }
                    })
                    .build();
                adLoader.loadAd(new AdRequest.Builder().build());
            }
        });
    }

    // 2. HÀM MỚI: Kiểm tra xem quảng cáo đã sẵn sàng chưa
    public static boolean isAdLoaded() {
        return loadedAd != null;
    }

    // 3. HÀM CẬP NHẬT: Chỉ hiển thị quảng cáo nếu đã được load trước đó
    public static void showAd(final Activity activity) {
        if (loadedAd == null) {
            return; // Chưa load xong thì không làm gì cả
        }
        
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                renderFullScreenAd(activity, loadedAd);
            }
        });
    }

private static void renderFullScreenAd(final Activity activity, final NativeAd nativeAd) {
        // 1. Tạo Root Container phủ toàn màn hình nền đen
        mainContainer = new FrameLayout(activity);
        mainContainer.setBackgroundColor(Color.BLACK);
        FrameLayout.LayoutParams rootParams = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT);
        activity.addContentView(mainContainer, rootParams);

        // 2. Tạo NativeAdView
        NativeAdView adView = new NativeAdView(activity);
        adView.setLayoutParams(new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT));

        // 3. Tạo MediaView hiển thị Video hoặc Ảnh chính
        MediaView mediaView = new MediaView(activity);
        mediaView.setLayoutParams(new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT));
        adView.addView(mediaView);
        adView.setMediaView(mediaView);

        // 4. Tạo Tiêu đề (Headline)
        TextView txtHeadline = new TextView(activity);
        txtHeadline.setText(nativeAd.getHeadline());
        txtHeadline.setTextColor(Color.WHITE);
        txtHeadline.setTextSize(20);
        FrameLayout.LayoutParams headlineParams = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.WRAP_CONTENT, ViewGroup.LayoutParams.WRAP_CONTENT);
        headlineParams.gravity = Gravity.BOTTOM | Gravity.START;
        headlineParams.setMargins(50, 0, 50, 280);
        txtHeadline.setLayoutParams(headlineParams);
        adView.addView(txtHeadline);
        adView.setHeadlineView(txtHeadline);

        // 5. Tạo nút Kêu gọi hành động (Call To Action)
        Button btnCta = new Button(activity);
        btnCta.setText(nativeAd.getCallToAction());
        btnCta.setBackgroundColor(Color.parseColor("#FF4081"));
        btnCta.setTextColor(Color.WHITE);
        FrameLayout.LayoutParams ctaParams = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT);
        ctaParams.gravity = Gravity.BOTTOM;
        ctaParams.setMargins(50, 0, 50, 100);
        btnCta.setLayoutParams(ctaParams);
        adView.addView(btnCta);
        adView.setCallToActionView(btnCta);

        mainContainer.addView(adView);
        adView.setNativeAd(nativeAd);

        // 6. Nút Đóng đếm ngược dạng vòng tròn
        android.graphics.drawable.GradientDrawable circleBackground = new android.graphics.drawable.GradientDrawable();
        circleBackground.setShape(android.graphics.drawable.GradientDrawable.OVAL);
        circleBackground.setColor(Color.parseColor("#88000000")); 
        circleBackground.setStroke(3, Color.WHITE); 

        // 2. Khởi tạo Button
        final Button btnClose = new Button(activity);
        btnClose.setBackground(circleBackground);
        btnClose.setTextColor(Color.WHITE);
        btnClose.setTextSize(14); // Giảm nhẹ size chữ số xuống 14
        btnClose.setGravity(Gravity.CENTER);
        btnClose.setPadding(0, 0, 0, 0); // Xóa padding mặc định để chữ căn đúng tâm
        
        // GIẢM SIZE: Thay đổi kích thước từ 120x120 xuống 90x90
        FrameLayout.LayoutParams closeParams = new FrameLayout.LayoutParams(90, 90);
        closeParams.gravity = Gravity.TOP | Gravity.END;
        closeParams.setMargins(0, 60, 40, 0); // Giữ nguyên vị trí góc phải
        btnClose.setLayoutParams(closeParams);

        // Cấu hình đếm ngược
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
                    // Khi hiện chữ X, để size 16 là vừa khít với vòng tròn 90x90
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
                    loadedAd = null; // Reset để có thể load lượt tiếp theo
                }
            }
        });
    }
}