using System;
using UnityEngine;

namespace GameUpSDK
{
    /// <summary>
    /// Optional contract for networks that support AdMob "Native overlay ads".
    /// Kept separate from IAds to avoid breaking existing networks.
    /// </summary>
    public interface INativeOverlayAds
    {
        /// <summary>Begin loading / refreshing a native overlay ad.</summary>
        void RequestNativeOverlay(string where);

        /// <summary>Returns true if a native overlay ad object is ready.</summary>
        bool IsNativeOverlayAvailable();

        /// <summary>
        /// Render the native overlay ad using a template style.
        /// If <paramref name="placement"/> contains pixel coordinates, implementation should attempt a custom placement.
        /// </summary>
        void RenderNativeOverlay(string where, NativeOverlayPlacement placement, NativeOverlayTemplateStyle style = null);

        /// <summary>Show the previously rendered native overlay ad.</summary>
        void ShowNativeOverlay(string where);

        /// <summary>Hide the native overlay ad (keeps it in memory).</summary>
        void HideNativeOverlay(string where);

        /// <summary>Destroy the native overlay ad and free resources.</summary>
        void DestroyNativeOverlay(string where);

        /// <summary>
        /// Utility to convert a UI RectTransform placement to screen pixels.
        /// Caller can pass Canvas (for camera) to get correct results for Screen Space - Camera / World Space.
        /// </summary>
        NativeOverlayPlacement BuildPlacementFromRectTransform(RectTransform rectTransform, Canvas canvas = null);
    }

    public enum NativeOverlayTemplateId
    {
        Small,
        Medium
    }

    /// <summary>
    /// Placement for Native Overlay ads.
    /// - Anchor presets map to SDK's AdPosition when custom pixel placement isn't supported.
    /// - PixelX/Y are in screen pixels (origin: bottom-left) if supported by the plugin version.
    /// </summary>
    [Serializable]
    public struct NativeOverlayPlacement
    {
        public NativeOverlayAnchor Anchor;
        public int? PixelX;
        public int? PixelY;
    }

    public enum NativeOverlayAnchor
    {
        Bottom,
        Top,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Center
    }

    [Serializable]
    public sealed class NativeOverlayTemplateStyle
    {
        public NativeOverlayTemplateId TemplateId = NativeOverlayTemplateId.Medium;
        public Color? MainBackgroundColor;
        public NativeOverlayTextStyle CallToAction;
    }

    [Serializable]
    public sealed class NativeOverlayTextStyle
    {
        public Color? BackgroundColor;
        public Color? FontColor;
        public int? FontSize;
        public NativeOverlayFontStyle? FontStyle;
    }

    public enum NativeOverlayFontStyle
    {
        Normal,
        Bold,
        Italic,
        Monospace
    }
}
