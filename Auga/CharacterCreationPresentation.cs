using System;
using System.Linq;
using AugaUnity;
using UnityEngine;
using UnityEngine.UI;
using static Auga.CharacterSelectionPresentation;

namespace Auga
{
    // Original Auga presentation, backed by the current game's customization and save logic.
    public sealed class CharacterCreationPresentation : MonoBehaviour
    {
        private void Start()
        {
            try
            {
                var startup = GetComponent<FejdStartup>();
                var root = startup.m_newCharacterPanel.transform;
                var custom = root.GetComponent<PlayerCustomizaton>();
                var source = Auga.Assets.MainMenuPrefab.transform.Find("CharacterSelection/NewCharacterPanel");
                var sourceCustom = source.GetComponent<PlayerCustomizaton>();
                var sourcePanel = source.Find("Panel");
                var oldChildren = root.Cast<Transform>().ToArray();
                var panel = Instantiate(sourcePanel, root, false);
                panel.name = "Auga New Character Panel";
                foreach (var child in oldChildren) child.gameObject.SetActive(false);
                Place((RectTransform)root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var rect = (RectTransform)panel;
                rect.anchorMin = rect.anchorMax = new Vector2(.04f, .97f);
                rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(640, 980);

                T CopyControl<T>(T original) where T : Component
                {
                    string path = original.name;
                    for (var parent = original.transform.parent; parent != sourcePanel; parent = parent.parent)
                    {
                        if (parent == null) throw new InvalidOperationException("Auga appearance control is outside its panel.");
                        path = parent.name + "/" + path;
                    }
                    return panel.Find(path).GetComponent<T>();
                }
                custom.m_skinHue = CopyControl(sourceCustom.m_skinHue);
                custom.m_hairTone = CopyControl(sourceCustom.m_hairTone);
                custom.m_hairLevel = CopyControl(sourceCustom.m_hairLevel);
                custom.m_maleToggle = CopyControl(sourceCustom.m_maleToggle);
                custom.m_femaleToggle = CopyControl(sourceCustom.m_femaleToggle);
                custom.m_skinHue.onValueChanged = new Slider.SliderEvent();
                custom.m_skinHue.onValueChanged.AddListener(custom.OnSkinHueChange);
                foreach (var slider in new[] { custom.m_hairTone, custom.m_hairLevel })
                {
                    slider.onValueChanged = new Slider.SliderEvent();
                    slider.onValueChanged.AddListener(custom.OnHairHueChange);
                }
                void BindGender(Toggle toggle, int model)
                {
                    toggle.onValueChanged = new Toggle.ToggleEvent();
                    toggle.onValueChanged.AddListener(on => {
                        if (on) custom.SetPlayerModel(model);
                        var selected = toggle.transform.Find("On");
                        if (selected != null) selected.gameObject.SetActive(on);
                    });
                }
                BindGender(custom.m_maleToggle, 0);
                BindGender(custom.m_femaleToggle, 1);

                // Keep the native TMP input, validation and persistent Done/Cancel callbacks.
                var inputSlot = panel.Find("Content/CharacterName");
                var input = startup.m_csNewCharacterName;
                MoveToSlot((RectTransform)input.transform, (RectTransform)inputSlot);
                foreach (var image in input.GetComponentsInChildren<Image>(true)) image.enabled = false;
                var field = Box(input.transform, "Auga Name Field", Color.white).GetComponent<Image>();
                field.transform.SetAsFirstSibling();
                var inputArt = inputSlot.GetComponent<Image>();
                field.sprite = inputArt.sprite; field.type = inputArt.type;
                field.pixelsPerUnitMultiplier = inputArt.pixelsPerUnitMultiplier;
                input.targetGraphic = field;
                input.transition = Selectable.Transition.ColorTint;
                input.spriteState = default;
                // Keep placeholder, typed text and caret inside the decorative end caps.
                var textArea = input.textViewport;
                if (textArea != null)
                    Place(textArea, Vector2.zero, Vector2.one, new Vector2(30, 4), new Vector2(-30, -4));
                foreach (var text in input.GetComponentsInChildren<TMPro.TMP_Text>(true))
                {
                    Place(text.rectTransform, Vector2.zero, Vector2.one,
                        textArea != null && text.transform.IsChildOf(textArea) ? Vector2.zero : new Vector2(30, 4),
                        textArea != null && text.transform.IsChildOf(textArea) ? Vector2.zero : new Vector2(-30, -4));
                    text.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
                    text.margin = Vector4.zero;
                }
                if (input.placeholder is TMPro.TMP_Text placeholder) placeholder.text = "Enter Name";
                inputSlot.gameObject.SetActive(false);
                var errorSlot = panel.Find("Content/NameExistsWarning");
                bool errorVisible = startup.m_newCharacterError.activeSelf;
                MoveToSlot((RectTransform)startup.m_newCharacterError.transform, (RectTransform)errorSlot);
                startup.m_newCharacterError.SetActive(errorVisible);
                errorSlot.gameObject.SetActive(false);
                foreach (var pair in new[] { (startup.m_csNewCharacterCancel, "Cancel"), (startup.m_csNewCharacterDone, "Done") })
                {
                    var slot = panel.Find(pair.Item2);
                    MoveToSlot((RectTransform)pair.Item1.transform, (RectTransform)slot);
                    foreach (var image in pair.Item1.GetComponentsInChildren<Image>(true)) image.enabled = false;
                    var artwork = Box(pair.Item1.transform, "Auga Character Action", Color.white).GetComponent<Image>();
                    artwork.transform.SetAsFirstSibling();
                    var original = slot.GetComponent<Button>().targetGraphic as Image;
                    artwork.sprite = original.sprite; artwork.type = original.type;
                    artwork.pixelsPerUnitMultiplier = original.pixelsPerUnitMultiplier;
                    StyleButtonStates(pair.Item1, artwork);
                    var font = API.GetNorseTMPFont();
                    foreach (var label in pair.Item1.GetComponentsInChildren<TMPro.TMP_Text>(true))
                    {
                        label.font = font; label.fontSharedMaterial = font.material;
                        label.fontSize = 28; label.color = new Color(.918f, .882f, .851f);
                    }
                    var tint = pair.Item1.GetComponent<ButtonTextColor>();
                    if (tint != null) tint.m_defaultColor = tint.m_defaultMeshColor = new Color(.918f, .882f, .851f);
                    slot.gameObject.SetActive(false);
                }
                foreach (var scroll in panel.GetComponentsInChildren<ScrollRect>(true))
                {
                    scroll.horizontal = false;
                }
                foreach (var grid in panel.GetComponentsInChildren<GridLayoutGroup>(true))
                {
                    grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    grid.constraintCount = 5;
                }
                foreach (var label in panel.GetComponentsInChildren<Text>(true))
                    label.text = Localization.instance.Localize(label.text);
                foreach (var label in panel.GetComponentsInChildren<TMPro.TMP_Text>(true))
                    label.text = Localization.instance.Localize(label.text);
                foreach (var label in input.GetComponentsInChildren<TMPro.TMP_Text>(true))
                {
                    label.font = API.GetBoldTMPFont(); label.fontSharedMaterial = label.font.material;
                    label.fontSize = 20; label.fontStyle = label == input.placeholder ? TMPro.FontStyles.Italic : TMPro.FontStyles.Normal;
                }
                var portraits = panel.GetComponentInChildren<CharacterPortraitsController>(true);
                if (portraits == null) throw new InvalidOperationException("Original portrait controller is missing.");
                portraits.enabled = true;
                for (var parent = portraits.transform; parent != panel; parent = parent.parent) parent.gameObject.SetActive(true);
                panel.gameObject.SetActive(true);
                Debug.Log("[Auga] Original new-character layout restored with native creation controls.");
            }
            catch (Exception exception)
            {
                Auga.LogError($"Character creation presentation failed: {exception}");
                enabled = false;
            }
        }

        private static void MoveToSlot(RectTransform target, RectTransform slot)
        {
            target.SetParent(slot.parent, false);
            target.anchorMin = slot.anchorMin; target.anchorMax = slot.anchorMax;
            target.pivot = slot.pivot; target.sizeDelta = slot.sizeDelta;
            target.anchoredPosition = slot.anchoredPosition;
            target.localScale = slot.localScale;
            target.gameObject.SetActive(true);
        }
    }
}
