using System;
using System.Collections.Generic;
using System.Linq;
using AugaUnity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Auga.CharacterSelectionPresentation;

namespace Auga
{
    // Valheim 1.0's BuildUi owns search, tagged lists, favorites and selection.
    public sealed class BuildPresentation : MonoBehaviour
    {
        private readonly List<Image> _hidden = new List<Image>();
        private BuildUi _build;
        private RectTransform _buildPanel, _filterPrompt;
        private float _panelHeight;
        private RectTransform _tagScroll;
        private Vector2 _tagScrollTop;
        private readonly Vector3[] _controlCorners = new Vector3[4];
        private readonly List<Button> _tabs = new List<Button>();
        private readonly HashSet<Transform> _insetPrompts = new HashSet<Transform>();
        private void Start()
        {
            try
            {
                var hud = GetComponent<Hud>();
                var body = LegacyText.GetFont(Auga.Assets.SourceSansProBold);
                _build = hud.m_buildUi;
                _tagScroll = (RectTransform)_build.m_tagListScrollRect.transform;
                _tagScrollTop = _tagScroll.offsetMax;
                var norse = LegacyText.GetFont(Resources.FindObjectsOfTypeAll<Font>().First(f => f.name.StartsWith("Norsebold", StringComparison.OrdinalIgnoreCase)));
                foreach (var root in new[] { hud.m_buildHud.transform, hud.m_buildUi.transform }.Distinct())
                {
                    foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
                    {
                        bool heading = label.fontSize >= 28 && label.GetComponentInParent<Selectable>(true) == null;
                        label.font = heading ? norse : body; label.fontSharedMaterial = label.font.material;
                        label.color = new Color(.918f, .882f, .851f);
                    }
                    foreach (var image in root.GetComponentsInChildren<Image>(true))
                    {
                        if (image.sprite == null || !image.sprite.name.Contains("woodpanel") || image.transform.Find("Auga Build Panel") != null) continue;
                        if (_buildPanel == null && image.transform.IsChildOf(_build.transform))
                        {
                            var container = image.transform;
                            while (container.parent != null && !_build.m_pieceButtonsContainer.IsChildOf(container)) container = container.parent;
                            _buildPanel = container as RectTransform;
                            if (_buildPanel != null) _panelHeight = _buildPanel.rect.height * 1.15f;
                        }
                        _hidden.Add(image);
                        var panel = Box(image.transform, "Auga Build Panel", new Color(.22f, .20f, .165f, .98f));
                        panel.SetAsFirstSibling(); SettingsPresentation.AddCorners(panel);
                    }
                    foreach (var scrollbar in root.GetComponentsInChildren<Scrollbar>(true)) SettingsPresentation.StyleScrollbar(scrollbar);
                }
                foreach (var prefab in new[] { _build.m_tagButtonPrefab, _build.m_pieceButtonPrefab })
                    foreach (var label in prefab.GetComponentsInChildren<TMP_Text>(true))
                    { label.font = body; label.fontSharedMaterial = body.material; label.color = new Color(.918f, .882f, .851f); }
                foreach (var image in _build.GetComponentsInChildren<Image>(true))
                    if (image.sprite != null && image.sprite.name == "panel_separator") _hidden.Add(image);
                foreach (var button in _build.m_tabContainer.GetComponentsInChildren<Button>(true))
                {
                    var label = button.GetComponentInChildren<TMP_Text>(true); if (label == null) continue;
                    _tabs.Add(button);
                    _hidden.AddRange(button.GetComponentsInChildren<Image>(true).Where(i => !i.transform.parent.name.StartsWith("gamepad_hint")));
                    var hit = Box(button.transform, "Auga Build Tab Hit", Color.clear); hit.SetAsFirstSibling();
                    label.font = body; label.fontSharedMaterial = body.material; label.fontStyle = FontStyles.UpperCase;
                    var tint = button.GetComponent<ButtonTextColor>(); if (tint != null) tint.enabled = false;
                    button.targetGraphic = label; button.transition = Selectable.Transition.ColorTint;
                    var colors = button.colors; colors.normalColor = colors.disabledColor = Color.white;
                    colors.highlightedColor = colors.selectedColor = new Color(1, .83f, .5f); button.colors = colors;
                }
                StyleBuildSlot(_build.m_pieceButtonPrefab.transform);
                foreach (var slot in _build.GetComponentsInChildren<BuildUiPieceButton>(true)) StyleBuildSlot(slot.transform);
                var input = _build.m_searchField;
                if (input.textViewport != null) Place(input.textViewport, Vector2.zero, Vector2.one, new Vector2(28, 3), new Vector2(-24, -3));
                _hidden.AddRange(input.GetComponentsInChildren<Image>(true));
                var field = Box(input.transform, "Auga Build Filter", Color.white).GetComponent<Image>(); field.transform.SetAsFirstSibling();
                var source = Auga.Assets.MainMenuPrefab.transform.Find("StartGame/Panel/WorldPanel/ServerPassword").GetComponent<Image>();
                field.sprite = source.sprite; field.type = source.type; field.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
                foreach (var image in _hidden) image.enabled = false;
                Debug.Log("[Auga] Current BuildUi retained and styled; native piece lists, search and favorites preserved.");
            }
            catch (Exception e) { Auga.LogError($"Build presentation failed: {e}"); enabled = false; }
        }
        private static void StyleBuildSlot(Transform root)
        {
            InventoryPresentation.StyleSlotBackground(root);
            var button = root.GetComponent<Button>();
            var slot = root.Find("Auga Slot")?.GetComponent<Graphic>();
            if (button == null || slot == null) return;
            slot.color = Color.white;
            button.targetGraphic = slot; button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = colors.disabledColor = new Color(.12f, .11f, .09f, .85f);
            colors.highlightedColor = colors.selectedColor = new Color(.29f, .265f, .22f);
            colors.pressedColor = new Color(.36f, .32f, .25f); button.colors = colors;
        }

        private void LateUpdate()
        {
            if (_buildPanel != null) _buildPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _panelHeight);
            LayoutTagList();
            if (_filterPrompt != null)
            {
                var field = (RectTransform)_build.m_searchField.transform;
                _filterPrompt.position = field.TransformPoint(new Vector3(field.rect.xMin - 18, field.rect.center.y, 0));
            }
            if (_build != null && _build.gameObject.activeInHierarchy && _insetPrompts.Count < 3)
            {
                foreach (var label in _build.GetComponentsInChildren<TMP_Text>(true))
                {
                    var key = label.text.Trim();
                    if (key != "Q" && key != "E" && key != "F") continue;
                    RectTransform prompt = label.rectTransform;
                    for (var parent = label.transform.parent; parent != null && parent != _build.transform; parent = parent.parent)
                    {
                        var rect = parent as RectTransform;
                        if (rect != null && rect.rect.width <= 64 && rect.rect.height <= 64 && parent.GetComponent<Image>() != null)
                            prompt = rect;
                    }
                    if (_insetPrompts.Add(prompt))
                    {
                        if (key == "F")
                        {
                            _filterPrompt = prompt;
                            prompt.pivot = new Vector2(.5f, .5f); prompt.sizeDelta = new Vector2(24, 24);
                            label.alignment = TextAlignmentOptions.Center;
                            var field = (RectTransform)_build.m_searchField.transform;
                            prompt.position = field.TransformPoint(new Vector3(field.rect.xMin - 18, field.rect.center.y, 0));
                        }
                        else prompt.anchoredPosition += new Vector2(key == "Q" ? 18 : -18, -12);
                    }
                }
            }
            foreach (var image in _hidden) if (image != null && image.enabled) image.enabled = false;
            foreach (var tab in _tabs) if (tab.targetGraphic is TMP_Text label) label.color = tab.interactable ? new Color(.59f, .56f, .49f) : new Color(.918f, .882f, .851f);
        }

        private void LayoutTagList()
        {
            if (_tagScroll == null || !_tagScroll.gameObject.activeInHierarchy) return;
            // Keep the list and its scrollbar below the native search/repair toolbar.
            // Start from the original inset each frame so adjustments cannot accumulate.
            _tagScroll.offsetMax = _tagScrollTop;
            var parent = _tagScroll.parent;
            _tagScroll.GetWorldCorners(_controlCorners);
            float top = parent.InverseTransformPoint(_controlCorners[1]).y;
            float limit = top;
            ReserveAboveList((RectTransform)_build.m_searchField.transform, parent, ref limit);
            if (_build.m_specialPieceButton != null)
                ReserveAboveList((RectTransform)_build.m_specialPieceButton.transform, parent, ref limit);
            _tagScroll.offsetMax = new Vector2(_tagScrollTop.x, _tagScrollTop.y + Mathf.Min(0, limit - top));
        }

        private void ReserveAboveList(RectTransform control, Transform parent, ref float limit)
        {
            if (!control.gameObject.activeInHierarchy) return;
            control.GetWorldCorners(_controlCorners);
            float bottom = parent.InverseTransformPoint(_controlCorners[0]).y;
            limit = Mathf.Min(limit, bottom - 12);
        }
    }
}
