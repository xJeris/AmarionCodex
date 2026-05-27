using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmarionCodex.UI
{
    /// <summary>
    /// Programmatically builds the entire Codex UI hierarchy using Unity UI.
    /// Returns the root GameObject with a CodexWindow component attached.
    /// </summary>
    internal static class CodexWindowBuilder
    {
        public static GameObject Build()
        {
            // ── Root: Canvas (Screen Space - Overlay) ──
            var root = new GameObject("AmarionCodex_Root");
            Object.DontDestroyOnLoad(root);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            // ── Window Panel (the visible bordered window) ──
            var windowPanel = CreatePanel(root.transform, "WindowPanel",
                CodexStyles.ParchmentEdge,
                CodexStyles.WindowWidth, CodexStyles.WindowHeight);
            windowPanel.AddComponent<CanvasGroup>();

            // Center the window
            var windowRT = windowPanel.GetComponent<RectTransform>();
            windowRT.anchorMin = new Vector2(0.5f, 0.5f);
            windowRT.anchorMax = new Vector2(0.5f, 0.5f);
            windowRT.pivot = new Vector2(0.5f, 0.5f);
            windowRT.anchoredPosition = Vector2.zero;

            // ── Inner Panel (parchment fill inside the border) ──
            var innerPanel = CreatePanel(windowPanel.transform, "InnerPanel",
                CodexStyles.Parchment, 0, 0);
            StretchFill(innerPanel, 3, 3, 3, 3); // 3px border

            // Vertical layout: title bar, content area, status bar
            var innerLayout = innerPanel.AddComponent<VerticalLayoutGroup>();
            innerLayout.childForceExpandWidth = true;
            innerLayout.childForceExpandHeight = false;
            innerLayout.childControlWidth = true;
            innerLayout.childControlHeight = true; // let flexibleHeight drive sizing
            innerLayout.spacing = 0;

            // ── Title Bar ──
            var titleBar = CreatePanel(innerPanel.transform, "TitleBar",
                CodexStyles.TitleBarTop, 0, CodexStyles.TitleBarHeight);
            var titleBarLE = titleBar.AddComponent<LayoutElement>();
            titleBarLE.preferredHeight = CodexStyles.TitleBarHeight;
            titleBarLE.minHeight = CodexStyles.TitleBarHeight;
            titleBarLE.flexibleHeight = 0; // fixed height, don't grow
            titleBarLE.flexibleWidth = 1;

            var titleBarHL = titleBar.AddComponent<HorizontalLayoutGroup>();
            titleBarHL.padding = new RectOffset(14, 8, 4, 4);
            titleBarHL.childAlignment = TextAnchor.MiddleLeft;
            titleBarHL.childForceExpandWidth = false;
            titleBarHL.childForceExpandHeight = true;
            titleBarHL.childControlWidth = true;
            titleBarHL.childControlHeight = true;

            var titleText = CreateTMPText(titleBar.transform, "TitleText",
                "Amarion Codex", CodexStyles.TitleFontSize, CodexStyles.TitleText);
            titleText.fontStyle = FontStyles.Normal;
            var titleTextLE = titleText.gameObject.AddComponent<LayoutElement>();
            titleTextLE.flexibleWidth = 1;

            // Close button
            var closeBtn = CreateButton(titleBar.transform, "CloseButton", "\u00D7",
                CodexStyles.TitleFontSize + 2, CodexStyles.TitleText, Color.clear);
            var closeBtnLE = closeBtn.AddComponent<LayoutElement>();
            closeBtnLE.preferredWidth = 28;

            // ── Content Area (left + right panels) ──
            // Use childControlWidth=true so LayoutElement.flexibleWidth drives sizing
            var contentArea = new GameObject("ContentArea", typeof(RectTransform));
            contentArea.transform.SetParent(innerPanel.transform, false);
            var contentLE = contentArea.AddComponent<LayoutElement>();
            contentLE.flexibleHeight = 1;
            contentLE.flexibleWidth = 1;
            var contentHL = contentArea.AddComponent<HorizontalLayoutGroup>();
            contentHL.childForceExpandWidth = false;
            contentHL.childForceExpandHeight = true;
            contentHL.childControlWidth = true;
            contentHL.childControlHeight = true;
            contentHL.spacing = 0;

            // ── Left Panel (zone list) ──
            var leftPanel = CreatePanel(contentArea.transform, "LeftPanel",
                CodexStyles.ParchmentDark, CodexStyles.LeftPanelWidth, 0);
            var leftPanelLE = leftPanel.AddComponent<LayoutElement>();
            leftPanelLE.preferredWidth = CodexStyles.LeftPanelWidth;
            leftPanelLE.minWidth = CodexStyles.LeftPanelWidth;
            leftPanelLE.flexibleWidth = 0; // fixed width, don't grow
            leftPanelLE.flexibleHeight = 1;

            var leftVL = leftPanel.AddComponent<VerticalLayoutGroup>();
            leftVL.childForceExpandWidth = true;
            leftVL.childForceExpandHeight = false;
            leftVL.childControlWidth = true;
            leftVL.childControlHeight = true; // let layout manage child heights
            leftVL.spacing = 0;

            // Left panel divider (right border) — not managed by layout
            var leftBorder = CreatePanel(leftPanel.transform, "LeftBorder",
                CodexStyles.ParchmentEdge, 0, 0);
            leftBorder.AddComponent<LayoutElement>().ignoreLayout = true;
            var leftBorderRT = leftBorder.GetComponent<RectTransform>();
            leftBorderRT.anchorMin = new Vector2(1, 0);
            leftBorderRT.anchorMax = new Vector2(1, 1);
            leftBorderRT.pivot = new Vector2(1, 0.5f);
            leftBorderRT.sizeDelta = new Vector2(2, 0);

            // Search bar area
            var searchArea = new GameObject("SearchArea", typeof(RectTransform), typeof(Image));
            searchArea.transform.SetParent(leftPanel.transform, false);
            searchArea.GetComponent<Image>().color = Color.clear;
            var searchAreaLE = searchArea.AddComponent<LayoutElement>();
            searchAreaLE.preferredHeight = CodexStyles.SearchBarHeight;
            searchAreaLE.flexibleWidth = 1;
            searchAreaLE.flexibleHeight = 0; // don't grow
            var searchPad = searchArea.AddComponent<VerticalLayoutGroup>();
            searchPad.padding = new RectOffset(8, 8, 6, 6);
            searchPad.childForceExpandWidth = true;
            searchPad.childForceExpandHeight = true;

            // Search input field
            var searchFieldGO = CreateInputField(searchArea.transform, "SearchInput", "Search creatures...");

            // Zone scroll view — takes all remaining height
            var zoneScrollView = CreateScrollView(leftPanel.transform, "ZoneScrollView");
            var zoneScrollLE = zoneScrollView.AddComponent<LayoutElement>();
            zoneScrollLE.flexibleHeight = 1;
            zoneScrollLE.flexibleWidth = 1;

            // ── Right Panel — takes all remaining width ──
            var rightPanel = CreatePanel(contentArea.transform, "RightPanel",
                CodexStyles.Parchment, 0, 0);
            var rightPanelLE = rightPanel.AddComponent<LayoutElement>();
            rightPanelLE.flexibleWidth = 1; // fill remaining space
            rightPanelLE.flexibleHeight = 1;

            var rightVL = rightPanel.AddComponent<VerticalLayoutGroup>();
            rightVL.childForceExpandWidth = true;
            rightVL.childForceExpandHeight = false;
            rightVL.childControlWidth = true;
            rightVL.childControlHeight = true; // let layout manage child heights
            rightVL.spacing = 0;

            // Zone header — fixed height, children positioned with anchors
            var zoneHeader = new GameObject("ZoneHeader", typeof(RectTransform), typeof(Image));
            zoneHeader.transform.SetParent(rightPanel.transform, false);
            zoneHeader.GetComponent<Image>().color = Color.clear;
            var zoneHeaderLE = zoneHeader.AddComponent<LayoutElement>();
            zoneHeaderLE.preferredHeight = 54;
            zoneHeaderLE.minHeight = 54;
            zoneHeaderLE.flexibleHeight = 0; // don't grow
            zoneHeaderLE.flexibleWidth = 1;

            // Zone header texts — anchor-positioned, not layout-managed
            var zoneNameText = CreateTMPText(zoneHeader.transform, "ZoneNameText",
                "Select a Zone", CodexStyles.ZoneHeaderFontSize, CodexStyles.InkDark);
            zoneNameText.enableWordWrapping = false;
            zoneNameText.overflowMode = TextOverflowModes.Ellipsis;
            var zoneNameRT = zoneNameText.GetComponent<RectTransform>();
            zoneNameRT.anchorMin = new Vector2(0, 0.4f);
            zoneNameRT.anchorMax = new Vector2(0.7f, 1);
            zoneNameRT.offsetMin = new Vector2(16, 0);
            zoneNameRT.offsetMax = new Vector2(0, -6);
            zoneNameText.alignment = TextAlignmentOptions.BottomLeft;

            var zoneSubText = CreateTMPText(zoneHeader.transform, "ZoneSubText",
                "", CodexStyles.ZoneNameFontSize, CodexStyles.InkFaded);
            zoneSubText.enableWordWrapping = false;
            var zoneSubRT = zoneSubText.GetComponent<RectTransform>();
            zoneSubRT.anchorMin = new Vector2(0, 0);
            zoneSubRT.anchorMax = new Vector2(0.7f, 0.4f);
            zoneSubRT.offsetMin = new Vector2(16, 6);
            zoneSubRT.offsetMax = new Vector2(0, 0);
            zoneSubText.fontStyle = FontStyles.Italic;
            zoneSubText.alignment = TextAlignmentOptions.TopLeft;

            var completionText = CreateTMPText(zoneHeader.transform, "CompletionText",
                "", CodexStyles.ZoneNameFontSize, CodexStyles.InkFaded);
            completionText.enableWordWrapping = false;
            var completionRT = completionText.GetComponent<RectTransform>();
            completionRT.anchorMin = new Vector2(0.65f, 0);
            completionRT.anchorMax = new Vector2(1, 1);
            completionRT.offsetMin = new Vector2(0, 4);
            completionRT.offsetMax = new Vector2(-16, -6);
            completionText.alignment = TextAlignmentOptions.MidlineRight;

            // Zone header bottom divider
            var headerDivider = CreatePanel(zoneHeader.transform, "HeaderDivider",
                CodexStyles.DetailDivider, 0, 2);
            var dividerRT = headerDivider.GetComponent<RectTransform>();
            dividerRT.anchorMin = new Vector2(0, 0);
            dividerRT.anchorMax = new Vector2(1, 0);
            dividerRT.pivot = new Vector2(0.5f, 0);
            dividerRT.sizeDelta = new Vector2(0, 2);

            // Entry scroll view (right panel body) — fills remaining vertical space
            var entryScrollView = CreateScrollView(rightPanel.transform, "EntryScrollView");
            var entryScrollLE = entryScrollView.AddComponent<LayoutElement>();
            entryScrollLE.flexibleHeight = 1;

            // Detail view container (hidden by default, replaces entry scroll when shown)
            var detailView = new GameObject("DetailView", typeof(RectTransform), typeof(Image));
            detailView.transform.SetParent(rightPanel.transform, false);
            detailView.GetComponent<Image>().color = CodexStyles.Parchment;
            var detailViewLE = detailView.AddComponent<LayoutElement>();
            detailViewLE.flexibleHeight = 1;
            detailView.SetActive(false);

            // Detail view has its own scroll view for long content
            var detailScrollView = CreateScrollView(detailView.transform, "DetailScrollView");
            StretchFill(detailScrollView, 0, 0, 0, 0);

            // ── Status Bar ──
            var statusBar = CreatePanel(innerPanel.transform, "StatusBar",
                CodexStyles.StatusBar, 0, CodexStyles.StatusBarHeight);
            var statusBarLE = statusBar.AddComponent<LayoutElement>();
            statusBarLE.preferredHeight = CodexStyles.StatusBarHeight;
            statusBarLE.minHeight = CodexStyles.StatusBarHeight;
            statusBarLE.flexibleHeight = 0; // fixed height, don't grow
            statusBarLE.flexibleWidth = 1;

            var statusHL = statusBar.AddComponent<HorizontalLayoutGroup>();
            statusHL.padding = new RectOffset(14, 14, 2, 2);
            statusHL.childAlignment = TextAnchor.MiddleLeft;
            statusHL.childForceExpandWidth = false;
            statusHL.childForceExpandHeight = true;

            var statusLeft = CreateTMPText(statusBar.transform, "StatusLeft",
                $"{Plugin.Cfg.OpenCodexKey.Value} to close \u00B7 /codex in chat",
                CodexStyles.StatusFontSize, CodexStyles.StatusText);
            var statusLeftLE = statusLeft.gameObject.AddComponent<LayoutElement>();
            statusLeftLE.flexibleWidth = 1;

            var statusRight = CreateTMPText(statusBar.transform, "StatusRight",
                $"Amarion Codex v{Plugin.PluginVersion}",
                CodexStyles.StatusFontSize, CodexStyles.StatusText);
            statusRight.alignment = TextAlignmentOptions.MidlineRight;

            // ── Title bar drag handler ──
            var dragHandler = titleBar.AddComponent<TitleBarDragHandler>();
            dragHandler.Target = windowRT;

            // ── Attach the CodexWindow controller ──
            var codexWindow = root.AddComponent<CodexWindow>();
            codexWindow.WindowPanel = windowPanel;
            codexWindow.ZoneNameText = zoneNameText;
            codexWindow.ZoneSubText = zoneSubText;
            codexWindow.CompletionText = completionText;
            codexWindow.ZoneScrollContent = GetScrollContent(zoneScrollView);
            codexWindow.EntryScrollContent = GetScrollContent(entryScrollView);
            codexWindow.DetailView = detailView;
            codexWindow.DetailScrollContent = GetScrollContent(detailScrollView);
            codexWindow.SearchField = searchFieldGO.GetComponent<TMP_InputField>();
            codexWindow.CloseButton = closeBtn.GetComponent<Button>();
            codexWindow.Init();

            // Start hidden
            windowPanel.SetActive(false);

            return root;
        }

        // ── Helper Methods ──

        private static GameObject CreatePanel(Transform parent, string name, Color color, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;

            if (width > 0 || height > 0)
            {
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(width, height);
            }

            return go;
        }

        private static TextMeshProUGUI CreateTMPText(Transform parent, string name,
            string text, float fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;

            return tmp;
        }

        private static GameObject CreateButton(Transform parent, string name,
            string label, float fontSize, Color textColor, Color bgColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = bgColor;

            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = CodexStyles.ButtonHover;
            colors.pressedColor = CodexStyles.ButtonHover;
            btn.colors = colors;

            var tmp = CreateTMPText(go.transform, "Label", label, fontSize, textColor);
            tmp.alignment = TextAlignmentOptions.Center;
            StretchFill(tmp.gameObject, 0, 0, 0, 0);

            return go;
        }

        private static GameObject CreateInputField(Transform parent, string name, string placeholder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = CodexStyles.SearchBg;

            // Outline effect via a child border image is complex; use Image color for now
            var inputField = go.AddComponent<TMP_InputField>();

            // Text area
            var textArea = new GameObject("TextArea", typeof(RectTransform));
            textArea.transform.SetParent(go.transform, false);
            StretchFill(textArea, 8, 8, 4, 4);
            textArea.AddComponent<RectMask2D>();

            // Placeholder
            var phGo = new GameObject("Placeholder", typeof(RectTransform));
            phGo.transform.SetParent(textArea.transform, false);
            var phTMP = phGo.AddComponent<TextMeshProUGUI>();
            phTMP.text = placeholder;
            phTMP.fontSize = CodexStyles.SearchFontSize;
            phTMP.color = CodexStyles.InkLight;
            phTMP.fontStyle = FontStyles.Italic;
            phTMP.enableWordWrapping = false;
            StretchFill(phGo, 0, 0, 0, 0);

            // Input text
            var txtGo = new GameObject("Text", typeof(RectTransform));
            txtGo.transform.SetParent(textArea.transform, false);
            var txtTMP = txtGo.AddComponent<TextMeshProUGUI>();
            txtTMP.fontSize = CodexStyles.SearchFontSize;
            txtTMP.color = CodexStyles.InkDark;
            txtTMP.enableWordWrapping = false;
            StretchFill(txtGo, 0, 0, 0, 0);

            inputField.textViewport = textArea.GetComponent<RectTransform>();
            inputField.textComponent = txtTMP;
            inputField.placeholder = phTMP;
            inputField.fontAsset = txtTMP.font;

            return go;
        }

        private static GameObject CreateScrollView(Transform parent, string name)
        {
            // ScrollView root
            var scrollGO = new GameObject(name, typeof(RectTransform), typeof(ScrollRect));
            scrollGO.transform.SetParent(parent, false);

            var scrollRect = scrollGO.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 20f;

            // Viewport (masked area)
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGO.transform, false);
            viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f); // nearly invisible, needed for mask
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            StretchFill(viewport, 0, 0, 0, 0);

            // Content container
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);

            var contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.sizeDelta = new Vector2(0, 0);

            var contentVL = content.AddComponent<VerticalLayoutGroup>();
            contentVL.childForceExpandWidth = true;
            contentVL.childForceExpandHeight = false;
            contentVL.childControlWidth = true;
            contentVL.childControlHeight = true;
            contentVL.spacing = 0;

            var contentSF = content.AddComponent<ContentSizeFitter>();
            contentSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSF.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = contentRT;

            return scrollGO;
        }

        /// <summary>
        /// Stretch-fill a RectTransform to its parent with optional padding.
        /// </summary>
        internal static void StretchFill(GameObject go, float left, float right, float top, float bottom)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        private static GameObject GetScrollContent(GameObject scrollView)
        {
            return scrollView.transform.Find("Viewport/Content").gameObject;
        }
    }
}
