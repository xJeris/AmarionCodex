using System.Collections.Generic;
using AmarionCodex.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmarionCodex.UI
{
    /// <summary>
    /// Main controller for the Amarion Codex window.
    /// Manages zone list, entry list, detail view, and search.
    /// </summary>
    internal class CodexWindow : MonoBehaviour
    {
        // ── References set by CodexWindowBuilder ──
        public GameObject WindowPanel;
        public TextMeshProUGUI ZoneNameText;
        public TextMeshProUGUI ZoneSubText;
        public TextMeshProUGUI CompletionText;
        public GameObject ZoneScrollContent;
        public GameObject EntryScrollContent;
        public GameObject DetailView;
        public GameObject DetailScrollContent;
        public TMP_InputField SearchField;
        public Button CloseButton;

        // ── State ──
        private string _selectedZone;
        private List<string> _zones = new List<string>();
        private List<GameObject> _zoneButtons = new List<GameObject>();
        private List<GameObject> _entryRows = new List<GameObject>();
        private List<GameObject> _detailElements = new List<GameObject>();
        private bool _isSearchMode;

        /// <summary>
        /// True when the search input field has focus (user is typing in it).
        /// Used by PluginCore.OnUpdate() to suppress the toggle keybind.
        /// </summary>
        public bool IsSearchFocused =>
            SearchField != null && SearchField.isFocused;

        /// <summary>
        /// Call after the builder has assigned all references.
        /// Wires up button and input listeners.
        /// </summary>
        public void Init()
        {
            if (CloseButton != null)
                CloseButton.onClick.AddListener(Hide);

            if (SearchField != null)
                SearchField.onValueChanged.AddListener(OnSearchChanged);

            EncounterTracker.OnNewDiscovery += OnNpcDiscovered;
        }

        private void OnDestroy()
        {
            EncounterTracker.OnNewDiscovery -= OnNpcDiscovered;

            if (_itemOverlayRoot != null)
                Destroy(_itemOverlayRoot);
        }

        /// <summary>
        /// Called when a new NPC is discovered. Refreshes the current view
        /// so the player sees the update without navigating away.
        /// Receives compound key "npcName|zone" from EncounterTracker.OnNewDiscovery.
        /// </summary>
        private void OnNpcDiscovered(string discoveryKey)
        {
            if (!WindowPanel.activeSelf)
                return;

            // Update zone progress counts in the left panel
            RebuildZoneList();

            // Refresh the currently displayed view
            if (_isSearchMode)
                OnSearchChanged(SearchField.text);
            else if (_selectedZone != null)
                SelectZone(_selectedZone);
        }

        public void Toggle()
        {
            if (WindowPanel.activeSelf)
                Hide();
            else
                Show();
        }

        public void Show()
        {
            WindowPanel.SetActive(true);
            RefreshData();
        }

        public void Hide()
        {
            if (GameData.ItemInfoWindow != null)
                GameData.ItemInfoWindow.CloseItemWindow();
            RestoreItemWindowParent();
            WindowPanel.SetActive(false);
        }

        private void RefreshData()
        {
            _zones = BestiaryDataProvider.GetAllZones();
            RebuildZoneList();

            if (_selectedZone != null && _zones.Contains(_selectedZone))
                SelectZone(_selectedZone);
            else if (_zones.Count > 0)
                SelectZone(_zones[0]);
            else
                ClearRightPanel();
        }

        // ── Zone List ──

        private void RebuildZoneList()
        {
            ClearChildren(ZoneScrollContent, _zoneButtons);

            foreach (var zoneName in _zones)
            {
                var btn = CreateZoneButton(zoneName);
                _zoneButtons.Add(btn);
            }
        }

        private GameObject CreateZoneButton(string zoneName)
        {
            var go = new GameObject("Zone_" + zoneName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(ZoneScrollContent.transform, false);
            go.GetComponent<Image>().color = Color.clear;

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = CodexStyles.ZoneRowHeight;
            le.flexibleWidth = 1;

            // Layout: horizontal with left (name+details) and right (count)
            var hl = go.AddComponent<HorizontalLayoutGroup>();
            hl.padding = new RectOffset(8, 6, 3, 3);
            hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childForceExpandWidth = false;
            hl.childForceExpandHeight = false;
            hl.childControlWidth = true;
            hl.childControlHeight = true;

            // Left side: zone name (row 1) + level/dungeon (row 2)
            var leftContainer = new GameObject("LeftContainer", typeof(RectTransform));
            leftContainer.transform.SetParent(go.transform, false);
            var leftContainerLE = leftContainer.AddComponent<LayoutElement>();
            leftContainerLE.flexibleWidth = 1;
            var leftVL = leftContainer.AddComponent<VerticalLayoutGroup>();
            leftVL.childForceExpandWidth = true;
            leftVL.childForceExpandHeight = false;
            leftVL.childControlHeight = true;
            leftVL.childControlWidth = true;
            leftVL.spacing = 0;

            // Row 1: Zone name only
            var nameText = CreateLabel(leftContainer.transform, "ZoneName", zoneName,
                CodexStyles.ZoneNameFontSize, CodexStyles.InkDark, FontStyles.Bold);
            nameText.enableWordWrapping = true;
            nameText.overflowMode = TextOverflowModes.Overflow;

            // Row 2: Level range + dungeon badge
            var zoneInfo = BestiaryDataProvider.GetZoneInfo(zoneName);
            string detailText = "";
            if (zoneInfo != null && !string.IsNullOrEmpty(zoneInfo.levelRange))
                detailText = $"Lv {zoneInfo.levelRange}";
            if (zoneInfo != null && zoneInfo.isDungeon)
                detailText += (detailText.Length > 0 ? "  " : "") + "<color=#8B2323>DUNGEON</color>";
            CreateLabel(leftContainer.transform, "ZoneDetails", detailText,
                CodexStyles.ZoneLevelFontSize, CodexStyles.InkLight, FontStyles.Normal);

            // Right side: count badge
            BestiaryDataProvider.GetZoneProgress(zoneName, out int total, out int discovered);
            var countText = CreateLabel(go.transform, "Count", $"{discovered}/{total}",
                CodexStyles.StatusFontSize, CodexStyles.InkFaded, FontStyles.Normal);
            countText.alignment = TextAlignmentOptions.MidlineRight;
            var countLE = countText.gameObject.AddComponent<LayoutElement>();
            countLE.preferredWidth = 40;

            // Click handler
            string zone = zoneName; // capture for closure
            go.GetComponent<Button>().onClick.AddListener(() => SelectZone(zone));

            // Bottom border
            var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(go.transform, false);
            border.GetComponent<Image>().color = CodexStyles.RowBorder;
            var borderRT = border.GetComponent<RectTransform>();
            borderRT.anchorMin = new Vector2(0, 0);
            borderRT.anchorMax = new Vector2(1, 0);
            borderRT.pivot = new Vector2(0.5f, 0);
            borderRT.sizeDelta = new Vector2(0, 1);

            return go;
        }

        public void SelectZone(string zoneName)
        {
            _selectedZone = zoneName;
            _isSearchMode = false;
            DetailView.SetActive(false);

            // Highlight active zone
            for (int i = 0; i < _zones.Count && i < _zoneButtons.Count; i++)
            {
                var img = _zoneButtons[i].GetComponent<Image>();
                if (_zones[i] == zoneName)
                    img.color = CodexStyles.ZoneActive;
                else
                    img.color = Color.clear;
            }

            // Update zone header
            var zoneInfo = BestiaryDataProvider.GetZoneInfo(zoneName);
            ZoneNameText.text = zoneName;
            if (zoneInfo != null && !string.IsNullOrEmpty(zoneInfo.levelRange))
                ZoneSubText.text = $"Levels {zoneInfo.levelRange}";
            else
                ZoneSubText.text = "";

            BestiaryDataProvider.GetZoneProgress(zoneName, out int total, out int discovered);
            CompletionText.text = $"Discovered: <b>{discovered}</b> / {total}";

            // Populate entry list
            var entries = BestiaryDataProvider.GetEntriesForZone(zoneName);
            RebuildEntryList(entries, zoneName);
        }

        // ── Entry List ──

        private void RebuildEntryList(List<BestiaryEntry> entries, string zoneName = null)
        {
            ClearChildren(EntryScrollContent, _entryRows);

            foreach (var bestiary in entries)
            {
                bool discovered = zoneName != null
                    ? EncounterTracker.IsDiscovered(bestiary.NormalizedName, zoneName)
                    : EncounterTracker.IsDiscoveredAnywhere(bestiary.NormalizedName);
                var row = CreateEntryRow(bestiary, discovered);
                _entryRows.Add(row);
            }
        }

        private GameObject CreateEntryRow(BestiaryEntry bestiary, bool discovered)
        {
            var go = new GameObject("Entry_" + bestiary.NormalizedName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(EntryScrollContent.transform, false);
            go.GetComponent<Image>().color = Color.clear;

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = CodexStyles.EntryRowHeight;
            le.flexibleWidth = 1;

            var hl = go.AddComponent<HorizontalLayoutGroup>();
            hl.padding = new RectOffset(16, 16, 4, 4);
            hl.childAlignment = TextAnchor.MiddleLeft;
            hl.childForceExpandWidth = false;
            hl.childForceExpandHeight = true;
            hl.childControlWidth = true;

            // NPC name (with inline boss badge via rich text)
            string displayName;
            if (!discovered)
                displayName = "???";
            else if (bestiary.IsBoss)
                displayName = $"{bestiary.Name}  <color=#8B2323><size={CodexStyles.BadgeFontSize}><b>BOSS</b></size></color>";
            else
                displayName = bestiary.Name;
            Color nameColor = discovered ? CodexStyles.InkDark : CodexStyles.Undiscovered;
            FontStyles nameStyle = discovered ? FontStyles.Normal : FontStyles.Italic;
            var nameText = CreateLabel(go.transform, "Name", displayName,
                CodexStyles.EntryNameFontSize, nameColor, nameStyle);
            nameText.richText = true;
            var nameLE = nameText.gameObject.AddComponent<LayoutElement>();
            nameLE.flexibleWidth = 1;

            // Level (range if multiple variants exist)
            string levelStr = discovered ? bestiary.LevelString : "Level ??";
            Color levelColor = discovered ? CodexStyles.InkFaded : CodexStyles.Undiscovered;
            var levelText = CreateLabel(go.transform, "Level", levelStr,
                CodexStyles.EntryLevelFontSize, levelColor, FontStyles.Normal);
            levelText.alignment = TextAlignmentOptions.MidlineRight;
            var levelLE = levelText.gameObject.AddComponent<LayoutElement>();
            levelLE.preferredWidth = 60;
            levelLE.minWidth = 60;

            // Click handler
            BestiaryEntry be = bestiary; // capture
            go.GetComponent<Button>().onClick.AddListener(() => OnEntryClicked(be, discovered));

            // Hover color
            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.clear;
            colors.highlightedColor = CodexStyles.ButtonHover;
            colors.pressedColor = CodexStyles.ButtonHover;
            colors.selectedColor = Color.clear;
            btn.colors = colors;

            return go;
        }

        private void OnEntryClicked(BestiaryEntry bestiary, bool discovered)
        {
            ShowDetailView(bestiary, discovered);
        }

        // ── Detail View ──

        private void ShowDetailView(BestiaryEntry bestiary, bool discovered)
        {
            DetailView.SetActive(true);
            ClearChildren(DetailScrollContent, _detailElements);

            // Padding container
            var padding = new GameObject("Padding", typeof(RectTransform));
            padding.transform.SetParent(DetailScrollContent.transform, false);
            var paddingVL = padding.AddComponent<VerticalLayoutGroup>();
            paddingVL.padding = new RectOffset(20, 20, 12, 16);
            paddingVL.spacing = 4;
            paddingVL.childForceExpandWidth = true;
            paddingVL.childForceExpandHeight = false;
            paddingVL.childControlWidth = true;
            paddingVL.childControlHeight = true;

            var paddingCSF = padding.AddComponent<ContentSizeFitter>();
            paddingCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            paddingCSF.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            _detailElements.Add(padding);

            // Back button
            string backText = _isSearchMode ? "\u2190 Back to search results" : $"\u2190 Back to {_selectedZone}";
            var backLabel = CreateLabel(padding.transform, "BackButton", backText,
                CodexStyles.DetailBodyFontSize, CodexStyles.InkFaded, FontStyles.Normal);
            var backBtn = backLabel.gameObject.AddComponent<Button>();
            backBtn.onClick.AddListener(BackToList);
            backLabel.raycastTarget = true;

            AddSpacer(padding.transform, 4);

            if (!discovered)
            {
                // Undiscovered detail
                AddSpacer(padding.transform, 40);

                var questionMarks = CreateLabel(padding.transform, "QuestionMarks", "? ? ?",
                    48, CodexStyles.Undiscovered, FontStyles.Normal);
                questionMarks.alignment = TextAlignmentOptions.Center;

                AddSpacer(padding.transform, 8);

                var notFoundText = CreateLabel(padding.transform, "NotFound",
                    "This creature has not yet been encountered.",
                    16, CodexStyles.Undiscovered, FontStyles.Italic);
                notFoundText.alignment = TextAlignmentOptions.Center;

                var seekText = CreateLabel(padding.transform, "Seek",
                    $"Seek it out in <b><color=#6B4F2E>{bestiary.ZoneName}</color></b>.",
                    14, CodexStyles.Undiscovered, FontStyles.Italic);
                seekText.alignment = TextAlignmentOptions.Center;
                seekText.enableWordWrapping = true;
                seekText.richText = true;

                return;
            }

            // ── Discovered NPC Detail ──

            // Name + Level header line
            string headerStr = bestiary.IsBoss
                ? $"{bestiary.Name}  <color=#8B2323><size={CodexStyles.BadgeFontSize}>BOSS</size></color>"
                : bestiary.Name;
            var headerText = CreateLabel(padding.transform, "Header", headerStr,
                CodexStyles.DetailTitleFontSize, CodexStyles.InkDark, FontStyles.Normal);
            headerText.richText = true;

            // Level + Kill count on one line
            int kills = EncounterTracker.GetKillCount(bestiary.NormalizedName);
            string levelStr = kills > 0
                ? $"{bestiary.LevelString}  <color=#78593A><size={CodexStyles.DetailBodyFontSize - 1}>({kills:N0} slain)</size></color>"
                : bestiary.LevelString;
            var levelLine = CreateLabel(padding.transform, "Level", levelStr,
                CodexStyles.DetailBodyFontSize + 2, CodexStyles.InkFaded, FontStyles.Normal);
            levelLine.richText = true;

            CreateLabel(padding.transform, "Zone",
                bestiary.ZoneName,
                CodexStyles.DetailBodyFontSize + 1, CodexStyles.InkFaded, FontStyles.Italic);

            // ── Loot Section ──
            if (bestiary.Loot != null && bestiary.Loot.Count > 0)
            {
                AddSpacer(padding.transform, 6);
                AddDivider(padding.transform);
                AddSpacer(padding.transform, 4);

                CreateLabel(padding.transform, "LootHeader", "Drops",
                    CodexStyles.DetailSectionFontSize, CodexStyles.TitleBarTop, FontStyles.Normal);

                // Two-column layout for loot items
                var lootColumns = new GameObject("LootColumns", typeof(RectTransform));
                lootColumns.transform.SetParent(padding.transform, false);
                var lootHL = lootColumns.AddComponent<HorizontalLayoutGroup>();
                lootHL.spacing = 4;
                lootHL.childForceExpandWidth = true;
                lootHL.childForceExpandHeight = false;
                lootHL.childControlWidth = true;
                lootHL.childControlHeight = true;

                var leftCol = new GameObject("LeftCol", typeof(RectTransform));
                leftCol.transform.SetParent(lootColumns.transform, false);
                var leftVL = leftCol.AddComponent<VerticalLayoutGroup>();
                leftVL.childForceExpandWidth = true;
                leftVL.childForceExpandHeight = false;
                leftVL.childControlWidth = true;
                leftVL.childControlHeight = true;
                leftVL.spacing = 0;
                var leftLE = leftCol.AddComponent<LayoutElement>();
                leftLE.flexibleWidth = 1;

                var rightCol = new GameObject("RightCol", typeof(RectTransform));
                rightCol.transform.SetParent(lootColumns.transform, false);
                var rightVL = rightCol.AddComponent<VerticalLayoutGroup>();
                rightVL.childForceExpandWidth = true;
                rightVL.childForceExpandHeight = false;
                rightVL.childControlWidth = true;
                rightVL.childControlHeight = true;
                rightVL.spacing = 0;
                var rightLE = rightCol.AddComponent<LayoutElement>();
                rightLE.flexibleWidth = 1;

                int lootIdx = 0;
                foreach (var itemName in bestiary.Loot)
                {
                    if (string.IsNullOrEmpty(itemName))
                        continue;

                    var col = (lootIdx % 2 == 0) ? leftCol : rightCol;
                    var resolvedItem = ItemLookup.FindByName(itemName);

                    if (resolvedItem != null)
                    {
                        // Clickable loot item — left-click opens the game's item info window
                        var lootLabel = CreateLabel(col.transform, "Loot_" + itemName,
                            $"\u2022 {itemName}",
                            CodexStyles.DetailBodyFontSize, CodexStyles.LootLink, FontStyles.Normal);
                        lootLabel.raycastTarget = true;

                        var btn = lootLabel.gameObject.AddComponent<Button>();
                        var btnColors = btn.colors;
                        btnColors.normalColor = Color.white;
                        btnColors.highlightedColor = new Color(1f, 1f, 0.7f, 1f);
                        btnColors.pressedColor = new Color(1f, 1f, 0.5f, 1f);
                        btnColors.selectedColor = Color.white;
                        btn.colors = btnColors;

                        Item capturedItem = resolvedItem; // capture for closure
                        btn.onClick.AddListener(() => ShowItemInfo(capturedItem));
                    }
                    else
                    {
                        // Non-clickable loot item — name couldn't be resolved to a game Item
                        CreateLabel(col.transform, "Loot_" + itemName,
                            $"\u2022 {itemName}",
                            CodexStyles.DetailBodyFontSize, CodexStyles.InkDark, FontStyles.Normal);
                    }

                    lootIdx++;
                }
            }

            // ── Quest Section ──
            if (bestiary.Quests != null && bestiary.Quests.Count > 0)
            {
                AddSpacer(padding.transform, 6);
                AddDivider(padding.transform);
                AddSpacer(padding.transform, 4);

                CreateLabel(padding.transform, "QuestHeader", "Quests",
                    CodexStyles.DetailSectionFontSize, CodexStyles.TitleBarTop, FontStyles.Normal);

                foreach (var questName in bestiary.Quests)
                {
                    if (string.IsNullOrEmpty(questName)) continue;
                    CreateLabel(padding.transform, "Quest_" + questName,
                        $"\u2022 {questName}",
                        CodexStyles.DetailBodyFontSize, CodexStyles.InkDark, FontStyles.Normal);
                }
            }
        }

        private void BackToList()
        {
            DetailView.SetActive(false);

            if (_isSearchMode)
                OnSearchChanged(SearchField.text);
            else if (_selectedZone != null)
                SelectZone(_selectedZone);
        }

        private void Update()
        {
            if (!WindowPanel.activeSelf)
                return;

            // If the item info window is open, intercept Escape to close just the item window
            if (_itemWindowReparented && GameData.ItemInfoWindow != null)
            {
                if (!GameData.ItemInfoWindow.isWindowActive())
                {
                    // Item window was closed by the game (e.g. clicking elsewhere)
                    RestoreItemWindowParent();
                }
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    // Close item window but keep the codex open
                    GameData.ItemInfoWindow.CloseItemWindow();
                    RestoreItemWindowParent();
                }
            }
        }

        /// <summary>
        /// Opens the game's native item info window for the given item.
        /// Reparents the item window to a high-sortOrder overlay canvas
        /// so it renders above the codex.
        /// </summary>
        private void ShowItemInfo(Item item)
        {
            if (GameData.ItemInfoWindow == null)
                return;

            GameData.ItemInfoWindow.CloseItemWindow();
            ReparentItemWindowToOverlay();
            GameData.ItemInfoWindow.DisplayItem(item, Input.mousePosition, 1);
        }

        private GameObject _itemOverlayRoot;
        private Transform _itemWindowOriginalParent;
        private bool _itemWindowReparented;

        /// <summary>
        /// Reparents just the ItemInfoWindow.ParentWindow onto a small overlay canvas
        /// at sortingOrder 200 so it renders above the codex (100) without affecting
        /// any other game UI elements.
        /// </summary>
        private void ReparentItemWindowToOverlay()
        {
            if (_itemWindowReparented)
                return;

            var parentWindow = GameData.ItemInfoWindow.ParentWindow;
            if (parentWindow == null)
                return;

            // Create the overlay canvas on first use
            if (_itemOverlayRoot == null)
            {
                _itemOverlayRoot = new GameObject("CodexItemOverlay");
                Object.DontDestroyOnLoad(_itemOverlayRoot);

                var overlayCanvas = _itemOverlayRoot.AddComponent<Canvas>();
                overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                overlayCanvas.sortingOrder = 200;

                var scaler = _itemOverlayRoot.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                _itemOverlayRoot.AddComponent<GraphicRaycaster>();
            }

            _itemWindowOriginalParent = parentWindow.transform.parent;
            parentWindow.transform.SetParent(_itemOverlayRoot.transform, false);
            _itemWindowReparented = true;

            // Remove codex from UIWindows so game's Escape handler won't close it
            // while the item info window is displayed
            if (GameData.Misc != null && GameData.Misc.UIWindows != null)
                GameData.Misc.UIWindows.Remove(WindowPanel);
        }

        /// <summary>
        /// Returns the ItemInfoWindow.ParentWindow to its original parent canvas
        /// and re-adds the codex to the game's UIWindows list.
        /// </summary>
        private void RestoreItemWindowParent()
        {
            if (!_itemWindowReparented)
                return;

            var parentWindow = GameData.ItemInfoWindow?.ParentWindow;
            if (parentWindow != null && _itemWindowOriginalParent != null)
                parentWindow.transform.SetParent(_itemWindowOriginalParent, false);

            _itemWindowReparented = false;

            // Re-add codex to UIWindows so Escape closes it normally again
            if (GameData.Misc != null && GameData.Misc.UIWindows != null
                && !GameData.Misc.UIWindows.Contains(WindowPanel))
                GameData.Misc.UIWindows.Add(WindowPanel);
        }

        // ── Search ──

        private void OnSearchChanged(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                // Return to zone view
                _isSearchMode = false;
                if (_selectedZone != null)
                    SelectZone(_selectedZone);
                return;
            }

            _isSearchMode = true;
            DetailView.SetActive(false);

            ZoneNameText.text = "Search Results";
            ZoneSubText.text = $"\"{query}\"";

            var results = BestiaryDataProvider.Search(query);
            CompletionText.text = $"{results.Count} found";

            // Deselect zones
            foreach (var btn in _zoneButtons)
                btn.GetComponent<Image>().color = Color.clear;

            RebuildEntryList(results);
        }

        // ── Helpers ──

        private TextMeshProUGUI CreateLabel(Transform parent, string name, string text,
            float fontSize, Color color, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            tmp.richText = true;

            return tmp;
        }

        private void AddDivider(Transform parent)
        {
            var go = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = CodexStyles.DetailDivider;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 1;
            le.flexibleWidth = 1;
        }

        private void AddSpacer(Transform parent, float height)
        {
            var go = new GameObject("Spacer", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
        }

        private void ClearChildren(GameObject parent, List<GameObject> tracked)
        {
            foreach (var obj in tracked)
            {
                if (obj != null)
                    Destroy(obj);
            }
            tracked.Clear();
        }

        private void ClearRightPanel()
        {
            ZoneNameText.text = "No zones available";
            ZoneSubText.text = "";
            CompletionText.text = "";
            ClearChildren(EntryScrollContent, _entryRows);
        }
    }
}
