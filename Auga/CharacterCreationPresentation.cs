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
    // Presentation only; the native customization component still owns preview and creation.
    public sealed class CharacterCreationPresentation : MonoBehaviour
    {
        private readonly List<Image> _hidden = new List<Image>();
        private readonly List<(Button button, Image border)> _actions = new List<(Button, Image)>();
        private static readonly Color Cream = new Color(.918f, .882f, .851f);
        private static readonly Color Gold = new Color(.72f, .56f, .19f);
        private static readonly Color Muted = new Color(.59f, .56f, .49f);
        private TMP_FontAsset _body, _norse;

        private static Image Visual(Transform parent, string name, Color color, float size)
        {
            var rect = Box(parent, name, color);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(size, size); rect.anchoredPosition = Vector2.zero;
            return rect.GetComponent<Image>();
        }

        private void Text(TMP_Text text, bool heading = false, float size = 20, bool upper = true)
        {
            text.font = heading ? _norse : _body; text.fontSharedMaterial = text.font.material;
            text.fontSize = size; text.fontStyle = upper ? FontStyles.UpperCase : FontStyles.Normal;
            text.color = Cream;
        }

        private void Start()
        {
            try
            {
                var startup = GetComponent<FejdStartup>();
                var root = startup.m_newCharacterPanel.transform;
                Place((RectTransform)root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var custom = root.GetComponentInChildren<PlayerCustomizaton>(true);
                if (custom == null) throw new InvalidOperationException("Native character customization panel was not found.");
                _body = LegacyText.GetFont(Auga.Assets.SourceSansProBold);
                _norse = LegacyText.GetFont(Resources.FindObjectsOfTypeAll<Font>().First(f => f.name.StartsWith("Norsebold", StringComparison.OrdinalIgnoreCase)));
                foreach (var image in root.GetComponentsInChildren<Image>(true))
                    if (image.sprite != null && (image.sprite.name.Contains("woodpanel") || image.sprite.name == "panel_separator")) _hidden.Add(image);
                var labels = root.GetComponentsInChildren<TMP_Text>(true);
                foreach (var label in labels) Text(label, false, 20, label != custom.m_selectedHair && label != custom.m_selectedBeard);
                var panel = Box(root, "Auga Create Character Panel", new Color(.22f, .20f, .165f, .98f));
                panel.SetAsFirstSibling();
                panel.anchorMin = panel.anchorMax = new Vector2(.04f, .92f);
                panel.pivot = new Vector2(0, 1);
                panel.anchoredPosition = Vector2.zero;
                panel.sizeDelta = new Vector2(500, 850);
                SettingsPresentation.AddCorners(panel);
                var title = labels.FirstOrDefault(t => t.text.IndexOf("newcharacter", StringComparison.OrdinalIgnoreCase) >= 0 || t.text.IndexOf("new character", StringComparison.OrdinalIgnoreCase) >= 0);
                if (title != null)
                {
                    title.transform.SetParent(panel, false); Text(title, true, 40, false);
                    Place(title.rectTransform, new Vector2(.06f, 1), new Vector2(.94f, 1), new Vector2(0, -82), new Vector2(0, -20));
                    title.alignment = TextAlignmentOptions.Center;
                }
                // PlayerCustomizaton is attached to the whole creation screen, not its appearance box.
                var appearanceRoot = custom.m_selectedHair.transform.parent;
                while (appearanceRoot != null && (!custom.m_skinHue.transform.IsChildOf(appearanceRoot) || !custom.m_maleToggle.transform.IsChildOf(appearanceRoot)))
                    appearanceRoot = appearanceRoot.parent;
                if (appearanceRoot == null || appearanceRoot == root) throw new InvalidOperationException("Appearance controls have no separate layout group.");
                var controls = (RectTransform)appearanceRoot;
                controls.SetParent(panel, false);
                controls.anchorMin = controls.anchorMax = new Vector2(.5f, 1);
                controls.pivot = new Vector2(.5f, 1);
                controls.anchoredPosition = new Vector2(0, -110);
                controls.localScale = Vector3.one;
                // Center the actual native controls, whose children retain their own legacy offsets.
                Canvas.ForceUpdateCanvases();
                float top = float.MinValue;
                var corners = new Vector3[4];
                foreach (var label in controls.GetComponentsInChildren<TMP_Text>(true))
                {
                    label.rectTransform.GetWorldCorners(corners);
                    top = Mathf.Max(top, corners[1].y);
                }
                var targetTop = panel.TransformPoint(new Vector3(panel.rect.center.x, panel.rect.yMax - 145, 0));
                var currentCenter = custom.m_selectedHair.rectTransform.TransformPoint(custom.m_selectedHair.rectTransform.rect.center);
                controls.position += new Vector3(targetTop.x - currentCenter.x, targetTop.y - top, 0);

                foreach (var toggle in custom.GetComponentsInChildren<Toggle>(true))
                {
                    var bg = toggle.targetGraphic as Image; var check = toggle.graphic as Image;
                    if (bg == null || check == null) continue;
                    _hidden.Add(bg); _hidden.Add(check);
                    var diamond = Visual(bg.transform, "Auga Sex Toggle", new Color(.10f, .105f, .075f), 14);
                    diamond.transform.localRotation = Quaternion.Euler(0, 0, 45);
                    var mark = Visual(diamond.transform, "Auga Sex Check", Gold, 7);
                    toggle.targetGraphic = diamond; toggle.graphic = mark;
                    mark.canvasRenderer.SetAlpha(toggle.isOn ? 1 : 0);
                }
                foreach (var slider in custom.GetComponentsInChildren<Slider>(true))
                {
                    var images = slider.GetComponentsInChildren<Image>(true);
                    _hidden.AddRange(images);
                    foreach (var image in images)
                    {
                        if (image.transform == slider.handleRect) continue;
                        var line = Box(image.transform, "Auga Appearance Track", Cream);
                        Place(line, new Vector2(0, .5f), new Vector2(1, .5f), new Vector2(0, -.5f), new Vector2(0, .5f));
                    }
                    var handle = Visual(slider.handleRect, "Auga Appearance Handle", Gold, 8);
                    handle.transform.localRotation = Quaternion.Euler(0, 0, 45);
                    slider.targetGraphic = handle;
                    var colors = slider.colors; colors.normalColor = Color.white; colors.highlightedColor = colors.selectedColor = new Color(1, .9f, .6f); slider.colors = colors;
                }
                foreach (var button in custom.GetComponentsInChildren<Button>(true))
                {
                    var label = button.GetComponentInChildren<TMP_Text>(true);
                    if (label == null || !(label.text.Contains("<") || label.text.Contains(">"))) continue;
                    bool previous = label.text.Contains("<");
                    _hidden.AddRange(button.GetComponentsInChildren<Image>(true));
                    var border = Visual(button.transform, "Auga Appearance Arrow", Gold, 28);
                    border.transform.localRotation = Quaternion.Euler(0, 0, 45);
                    Visual(border.transform, "Auga Arrow Fill", new Color(.16f, .15f, .12f), 26).raycastTarget = false;
                    label.transform.SetAsLastSibling(); Text(label, false, 22, false);
                    label.text = previous ? "<" : ">"; label.alignment = TextAlignmentOptions.Center;
                    label.rectTransform.localScale = new Vector3(1, 1.5f, 1);
                    var tint = button.GetComponent<ButtonTextColor>(); if (tint != null) tint.m_defaultColor = tint.m_defaultMeshColor = Cream;
                    _actions.Add((button, border));
                }
                var input = startup.m_csNewCharacterName;
                var nameLabel = labels.FirstOrDefault(t => !t.transform.IsChildOf(input.transform) && (t.text == "$menu_name" || t.text.Equals("Name", StringComparison.OrdinalIgnoreCase)));
                if (nameLabel != null)
                {
                    nameLabel.transform.SetParent(panel, false);
                    Place(nameLabel.rectTransform, new Vector2(.12f, 0), new Vector2(.88f, 0), new Vector2(0, 154), new Vector2(0, 180));
                    nameLabel.alignment = TextAlignmentOptions.Center;
                }
                input.transform.SetParent(panel, false);
                Place((RectTransform)input.transform, new Vector2(.15f, 0), new Vector2(.85f, 0), new Vector2(0, 100), new Vector2(0, 144));
                _hidden.AddRange(input.GetComponentsInChildren<Image>(true));
                var field = Box(input.transform, "Auga Character Name", Color.white).GetComponent<Image>(); field.transform.SetAsFirstSibling();
                var source = Auga.Assets.MainMenuPrefab.transform.Find("StartGame/Panel/WorldPanel/ServerPassword").GetComponent<Image>();
                field.sprite = source.sprite; field.type = source.type; field.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
                var area = input.transform.Find("Text Area") as RectTransform;
                if (area != null) Place(area, Vector2.zero, Vector2.one, new Vector2(28, 0), new Vector2(-28, 0));
                foreach (var label in input.GetComponentsInChildren<TMP_Text>(true)) Text(label, false, 22, false);
                if (input.placeholder is TMP_Text placeholder) { placeholder.fontStyle = FontStyles.Italic; placeholder.color = Muted; }
                foreach (var button in new[] { startup.m_csNewCharacterCancel, startup.m_csNewCharacterDone })
                {
                    _hidden.AddRange(button.GetComponentsInChildren<Image>(true));
                    StyleButton(button, panel, button == startup.m_csNewCharacterCancel ? -110 : 110, 42, 200, true, _body, _norse);
                    var border = (Image)button.targetGraphic;
                    // Shared styling clears native sprite states; keep hover on the visible border.
                    _actions.Add((button, border));
                    var tint = button.GetComponent<ButtonTextColor>(); if (tint != null) { tint.m_defaultColor = tint.m_defaultMeshColor = Cream; tint.m_disabledColor = Muted; }
                }
                foreach (var image in _hidden) image.enabled = false;
                Debug.Log("[Auga] New character styled as one panel; native creation and appearance controls retained.");
            }
            catch (Exception e) { Auga.LogError($"Character creation presentation failed: {e}"); enabled = false; }
        }

        private void LateUpdate()
        {
            foreach (var image in _hidden) if (image != null && image.enabled) image.enabled = false;
            foreach (var action in _actions)
                action.border.color = action.button.interactable
                    ? (action.border.name == "Auga Appearance Arrow" ? Gold : Color.white)
                    : new Color(.55f, .55f, .55f);
        }
    }
}
