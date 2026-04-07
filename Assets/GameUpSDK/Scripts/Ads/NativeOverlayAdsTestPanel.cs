using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace GameUpSDK
{
    /// <summary>
    /// Quick in-game panel to test AdMob Native Overlay Ads through AdsManager.
    /// Drop this into a scene to get a small UI with buttons.
    /// </summary>
    public sealed class NativeOverlayAdsTestPanel : MonoBehaviour
    {
        [Header("Placement")]
        [SerializeField] private string where = "native_overlay_test";
        [SerializeField] private NativeOverlayAnchor anchor = NativeOverlayAnchor.Bottom;

        [Tooltip("Optional. If set, Render will attempt to place the ad at this RectTransform's screen position.")]
        [SerializeField] private RectTransform placementRectTransform;

        [Tooltip("Optional. Needed for Screen Space - Camera / World Space canvas coordinate conversion.")]
        [SerializeField] private Canvas canvas;

        [Header("Buttons (optional - assign in prefab/scene)")]
        [SerializeField] private Button btnRequest;
        [SerializeField] private Button btnShow;
        [SerializeField] private Button btnHide;
        [SerializeField] private Button btnDestroy;

        [Header("Render by anchor")]
        [SerializeField] private Button btnTop;
        [SerializeField] private Button btnBottom;
        [SerializeField] private Button btnTopLeft;
        [SerializeField] private Button btnTopRight;
        [SerializeField] private Button btnBottomLeft;
        [SerializeField] private Button btnBottomRight;
        [SerializeField] private Button btnCenter;

        [Header("Render by RectTransform")]
        [SerializeField] private Button btnRenderRectTransform;

        [Header("Fullscreen test (simulation)")]
        [Tooltip("Native overlay is not a full-screen format. This simulates a fullscreen test by using a fullscreen marker and rendering at its center.")]
        [SerializeField] private Button btnFullScreenTest;

        [Header("Template Style (optional)")]
        [SerializeField] private NativeOverlayTemplateId templateId = NativeOverlayTemplateId.Medium;
        [SerializeField] private Color mainBackgroundColor = new Color(0f, 0f, 0f, 0f);
        [SerializeField] private bool useMainBackgroundColor;

        [Header("UI (optional - auto created if null)")]
        [SerializeField] private Canvas uiCanvas;
        [SerializeField] private RectTransform panelRoot;

        private RectTransform _fullScreenMarker;

        private void Awake()
        {
            if (canvas == null)
                canvas = FindFirstObjectByType<Canvas>();

            EnsureUi();
            WireAssignedButtons();
        }

        private void EnsureUi()
        {
            EnsureEventSystem();

            if (uiCanvas == null)
            {
                var go = new GameObject("[GameUp] NativeOverlay Test Canvas");
                uiCanvas = go.AddComponent<Canvas>();
                uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                go.AddComponent<GraphicRaycaster>();
                DontDestroyOnLoad(go);
            }

            if (panelRoot == null)
            {
                var panelGo = new GameObject("[GameUp] NativeOverlay Test Panel");
                panelGo.transform.SetParent(uiCanvas.transform, false);
                panelRoot = panelGo.AddComponent<RectTransform>();
                panelRoot.anchorMin = new Vector2(0f, 1f);
                panelRoot.anchorMax = new Vector2(0f, 1f);
                panelRoot.pivot = new Vector2(0f, 1f);
                panelRoot.anchoredPosition = new Vector2(12f, -12f);
                panelRoot.sizeDelta = new Vector2(380f, 420f);

                var img = panelGo.AddComponent<Image>();
                img.color = new Color(0f, 0f, 0f, 0.6f);

                var layout = panelGo.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(10, 10, 10, 10);
                layout.spacing = 8;
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;

                var fitter = panelGo.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            }

            // Auto-create only when buttons aren't assigned in inspector.
            if (HasAnyAssignedButtons())
                return;

            CreateButtonIfMissing("RequestNativeOverlay", RequestNativeOverlay);
            CreateButtonIfMissing("Show", Show);
            CreateButtonIfMissing("Hide", Hide);
            CreateButtonIfMissing("Destroy", DestroyAd);
            CreateButtonIfMissing("Render Top", () => RenderAtAnchor(NativeOverlayAnchor.Top));
            CreateButtonIfMissing("Render Bottom", () => RenderAtAnchor(NativeOverlayAnchor.Bottom));
            CreateButtonIfMissing("Render TopLeft", () => RenderAtAnchor(NativeOverlayAnchor.TopLeft));
            CreateButtonIfMissing("Render TopRight", () => RenderAtAnchor(NativeOverlayAnchor.TopRight));
            CreateButtonIfMissing("Render BottomLeft", () => RenderAtAnchor(NativeOverlayAnchor.BottomLeft));
            CreateButtonIfMissing("Render BottomRight", () => RenderAtAnchor(NativeOverlayAnchor.BottomRight));
            CreateButtonIfMissing("Render Center", () => RenderAtAnchor(NativeOverlayAnchor.Center));
            CreateButtonIfMissing("Render (RectTransform)", RenderRectTransform);
            CreateButtonIfMissing("FullScreen test (sim)", FullScreenTest);
        }

        private bool HasAnyAssignedButtons()
        {
            return btnRequest != null ||
                   btnShow != null ||
                   btnHide != null ||
                   btnDestroy != null ||
                   btnTop != null ||
                   btnBottom != null ||
                   btnTopLeft != null ||
                   btnTopRight != null ||
                   btnBottomLeft != null ||
                   btnBottomRight != null ||
                   btnCenter != null ||
                   btnRenderRectTransform != null ||
                   btnFullScreenTest != null;
        }

        private void WireAssignedButtons()
        {
            Wire(btnRequest, RequestNativeOverlay);
            Wire(btnShow, Show);
            Wire(btnHide, Hide);
            Wire(btnDestroy, DestroyAd);

            Wire(btnTop, () => RenderAtAnchor(NativeOverlayAnchor.Top));
            Wire(btnBottom, () => RenderAtAnchor(NativeOverlayAnchor.Bottom));
            Wire(btnTopLeft, () => RenderAtAnchor(NativeOverlayAnchor.TopLeft));
            Wire(btnTopRight, () => RenderAtAnchor(NativeOverlayAnchor.TopRight));
            Wire(btnBottomLeft, () => RenderAtAnchor(NativeOverlayAnchor.BottomLeft));
            Wire(btnBottomRight, () => RenderAtAnchor(NativeOverlayAnchor.BottomRight));
            Wire(btnCenter, () => RenderAtAnchor(NativeOverlayAnchor.Center));

            Wire(btnRenderRectTransform, RenderRectTransform);
            Wire(btnFullScreenTest, FullScreenTest);
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null)
                return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void CreateButtonIfMissing(string label, UnityEngine.Events.UnityAction onClick)
        {
            // Avoid duplicates if EnsureUi called multiple times.
            for (int i = 0; i < panelRoot.childCount; i++)
            {
                var t = panelRoot.GetChild(i);
                if (t.name == label)
                    return;
            }

            var btnGo = new GameObject(label);
            btnGo.transform.SetParent(panelRoot, false);

            var rect = btnGo.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 46f);

            var le = btnGo.AddComponent<LayoutElement>();
            le.minHeight = 46f;
            le.preferredHeight = 46f;

            var img = btnGo.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.9f);

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(btnGo.transform, false);
            var text = textGo.AddComponent<Text>();
            text.text = label;
            text.color = Color.black;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            var esGo = new GameObject("[GameUp] EventSystem");
            esGo.AddComponent<EventSystem>();
            // StandaloneInputModule works with legacy Input Manager.
            // If your project uses the new Input System, Unity will still often include a compatible module,
            // but this ensures there's at least one module present in typical setups.
            esGo.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(esGo);
        }

        private NativeOverlayTemplateStyle BuildStyle()
        {
            var style = new NativeOverlayTemplateStyle
            {
                TemplateId = templateId,
            };
            if (useMainBackgroundColor)
                style.MainBackgroundColor = mainBackgroundColor;
            return style;
        }

        // ---- Button handlers ----

        public void RequestNativeOverlay()
        {
            if (AdsManager.Instance == null)
            {
                Debug.LogWarning("[GameUp] NativeOverlayTestPanel: AdsManager.Instance is null.");
                return;
            }
            AdsManager.Instance.RequestNativeOverlay(where);
            Debug.Log("[GameUp] NativeOverlayTestPanel: RequestNativeOverlay " + where);
        }

        public void RenderAnchor()
        {
            RenderAtAnchor(anchor);
        }

        private void RenderAtAnchor(NativeOverlayAnchor targetAnchor)
        {
            if (AdsManager.Instance == null)
                return;

            var placement = new NativeOverlayPlacement
            {
                Anchor = targetAnchor,
                PixelX = null,
                PixelY = null
            };
            AdsManager.Instance.RenderNativeOverlay(where, placement, BuildStyle());
            AdsManager.Instance.ShowNativeOverlay(where);
            Debug.Log("[GameUp] NativeOverlayTestPanel: RenderAtAnchor " + targetAnchor);
        }

        public void RenderRectTransform()
        {
            if (AdsManager.Instance == null)
                return;

            if (placementRectTransform == null)
            {
                Debug.LogWarning("[GameUp] NativeOverlayTestPanel: placementRectTransform is null. Assign one in Inspector.");
                return;
            }

            AdsManager.Instance.RenderNativeOverlay(where, placementRectTransform, canvas, BuildStyle());
            AdsManager.Instance.ShowNativeOverlay(where);
            Debug.Log("[GameUp] NativeOverlayTestPanel: RenderRectTransform " + placementRectTransform.name);
        }

        public void FullScreenTest()
        {
            if (AdsManager.Instance == null)
                return;

            EnsureFullScreenMarker();
            AdsManager.Instance.RenderNativeOverlay(where, _fullScreenMarker, uiCanvas, BuildStyle());
            AdsManager.Instance.ShowNativeOverlay(where);
            Debug.Log("[GameUp] NativeOverlayTestPanel: FullScreenTest (sim)");
        }

        private void EnsureFullScreenMarker()
        {
            if (_fullScreenMarker != null)
                return;

            var go = new GameObject("[GameUp] NativeOverlay FullScreen Marker");
            go.transform.SetParent(uiCanvas.transform, false);
            _fullScreenMarker = go.AddComponent<RectTransform>();
            _fullScreenMarker.anchorMin = Vector2.zero;
            _fullScreenMarker.anchorMax = Vector2.one;
            _fullScreenMarker.pivot = new Vector2(0.5f, 0.5f);
            _fullScreenMarker.offsetMin = Vector2.zero;
            _fullScreenMarker.offsetMax = Vector2.zero;
        }

        public void Show()
        {
            if (AdsManager.Instance == null)
                return;
            AdsManager.Instance.ShowNativeOverlay(where);
            Debug.Log("[GameUp] NativeOverlayTestPanel: Show");
        }

        public void Hide()
        {
            if (AdsManager.Instance == null)
                return;
            AdsManager.Instance.HideNativeOverlay(where);
            Debug.Log("[GameUp] NativeOverlayTestPanel: Hide");
        }

        public void DestroyAd()
        {
            if (AdsManager.Instance == null)
                return;
            AdsManager.Instance.DestroyNativeOverlay(where);
            Debug.Log("[GameUp] NativeOverlayTestPanel: Destroy");
        }
    }
}

