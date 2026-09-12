using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Auga
{
    // Style the current game's controls in place so new menu entries, callbacks,
    // localization and controller navigation remain owned by FejdStartup.
    public sealed class MainMenuPresentation : MonoBehaviour
    {
        private static TMP_FontAsset _menuFont;
        private static TMP_FontAsset _projectFont;

        private void Start()
        {
            try
            {
                var startup = GetComponent<FejdStartup>();
                if (startup == null || startup.m_menuList == null)
                    throw new InvalidOperationException("FejdStartup has no menu list.");
                if (_menuFont == null)
                {
                    var fontSource = Resources.FindObjectsOfTypeAll<Font>()
                        .FirstOrDefault(font => font.name.StartsWith("Norsebold", StringComparison.OrdinalIgnoreCase))
                        ?? Resources.FindObjectsOfTypeAll<Font>()
                            .FirstOrDefault(font => font.name.Equals("Norse", StringComparison.OrdinalIgnoreCase));
                    if (fontSource == null) throw new InvalidOperationException("The Norse font source is not loaded.");
                    _menuFont = TMP_FontAsset.CreateFontAsset(fontSource);
                    if (_menuFont == null || _menuFont.material == null)
                        throw new InvalidOperationException("Cannot create a valid Norse menu font.");
                    _menuFont.name = "Auga Norse Menu";
                }

                var buttons = startup.m_menuList.GetComponentsInChildren<Button>(true);
                var originalMenu = Auga.Assets.MainMenuPrefab.transform.Find("Menu/MenuList");
                var originalKnots = originalMenu.GetComponentsInChildren<Button>(true)
                    .Select(button => button.transform.Find("Knots")).First(knots => knots != null);
                if (buttons.Length == 0) throw new InvalidOperationException("The current menu list contains no buttons.");
                foreach (var layout in buttons.Select(button => button.transform.parent.GetComponent<VerticalLayoutGroup>())
                             .Where(layout => layout != null).Distinct())
                {
                    layout.spacing += 6f;
                    layout.childControlWidth = true;
                    layout.childForceExpandWidth = false;
                }
                foreach (var button in buttons)
                {
                    foreach (var nativeImage in button.GetComponentsInChildren<Image>(true))
                        nativeImage.enabled = false;
                    // Keep the full padded click target for the text-only menu.
                    var background = new GameObject("Auga Menu Hit Area", typeof(RectTransform), typeof(Image));
                    background.layer = button.gameObject.layer;
                    background.transform.SetParent(button.transform, false);
                    background.transform.SetAsFirstSibling();
                    background.AddComponent<LayoutElement>().ignoreLayout = true;
                    var rect = (RectTransform)background.transform;
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                    var image = background.GetComponent<Image>();
                    image.enabled = true;
                    image.raycastTarget = true;
                    image.canvasRenderer.SetAlpha(1f);
                    image.color = Color.clear;
                    button.targetGraphic = button.GetComponentInChildren<TMP_Text>(true);
                    button.transition = Selectable.Transition.ColorTint;
                    var colors = button.colors;
                    colors.normalColor = new Color(0.918f, 0.882f, 0.851f);
                    colors.highlightedColor = new Color(0.98f, 0.96f, 0.92f);
                    colors.selectedColor = colors.highlightedColor;
                    colors.pressedColor = new Color(0.9f, 0.45f, 0.1f);
                    colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    button.colors = colors;
                    foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
                    {
                        label.font = _menuFont;
                        label.fontSharedMaterial = _menuFont.material;
                        label.color = Color.white;
                        label.fontStyle = FontStyles.Normal;
                        label.fontSize = 32;
                    }
                    var knots = Instantiate(originalKnots.gameObject, button.transform, false);
                    knots.name = "Auga Selection Knots";
                    var knotRect = (RectTransform)knots.transform;
                    knotRect.anchorMin = knotRect.anchorMax = new Vector2(.5f, .5f);
                    knotRect.anchoredPosition = Vector2.zero;
                    knotRect.sizeDelta = new Vector2(280, 32);
                    (knots.GetComponent<LayoutElement>() ?? knots.AddComponent<LayoutElement>()).ignoreLayout = true;
                    foreach (var graphic in knots.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
                    button.gameObject.AddComponent<MainMenuSelectionOrnaments>().Ornaments = knots;
                }
                var startButton = buttons.FirstOrDefault(button => button.name.Replace(" ", "").Equals("StartGame", StringComparison.OrdinalIgnoreCase));
                var startLabel = startButton != null ? startButton.GetComponentInChildren<TMP_Text>(true) : null;
                if (startLabel == null) throw new InvalidOperationException("Cannot measure the Start Game label.");
                // Keep equal padded hit areas based on the Norse Start Game label.
                var commonWidth = Mathf.Ceil(startLabel.GetPreferredValues(startLabel.text, float.PositiveInfinity, float.PositiveInfinity).x) + 64f;
                foreach (var button in buttons)
                {
                    var size = button.GetComponent<LayoutElement>() ?? button.gameObject.AddComponent<LayoutElement>();
                    size.layoutPriority = 10;
                    size.minWidth = commonWidth;
                    size.preferredWidth = commonWidth;
                    size.flexibleWidth = 0f;
                    var contentLayout = button.GetComponent<HorizontalLayoutGroup>();
                    if (contentLayout != null)
                    {
                        contentLayout.childAlignment = TextAnchor.MiddleCenter;
                        contentLayout.padding.left = 32;
                        contentLayout.padding.right = 32;
                        contentLayout.childControlWidth = true;
                        contentLayout.childForceExpandWidth = false;
                    }
                    foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
                        label.alignment = TextAlignmentOptions.Center;
                }
                Debug.Log($"[Auga] Common menu button width: {commonWidth} (Start Game plus padding).");
                Debug.Log($"[Auga] Main-menu styling applied to {buttons.Length} native buttons; callbacks and navigation preserved.");
                ApplyBranding(startup);
                ApplyLoadingPresentation(startup);
                StartCoroutine(LogGeometry(buttons));
            }
            catch (Exception exception)
            {
                Auga.LogError($"Main-menu styling failed: {exception}");
                enabled = false;
            }
        }

        private void ApplyBranding(FejdStartup startup)
        {
            var menu = startup.m_mainMenu.transform as RectTransform;
            var nativeLogo = menu != null ? menu.Find("Logo") as RectTransform : null;
            var logoImage = Auga.Assets.AugaLogo.GetComponentsInChildren<Image>(true)
                .FirstOrDefault(image => image.sprite != null && image.sprite.name == "AugaLogo");
            if (menu == null || nativeLogo == null || logoImage == null)
                throw new InvalidOperationException("Cannot locate the native menu logo or original Auga logo sprite.");

            // Normalized anchors follow the reference composition at different resolutions.
            // Keep decoration inside the native menu so it hides on character selection.
            var logo = Decoration(menu, "Auga Main Logo", new Vector2(0.08f, 0.43f), new Vector2(0.92f, 0.95f));
            var image = logo.gameObject.AddComponent<Image>();
            image.sprite = logoImage.sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            if (_projectFont == null)
                _projectFont = TMP_FontAsset.CreateFontAsset(Auga.Assets.SourceSansProRegular);
            var heading = Decoration(menu, "Auga Project Heading", new Vector2(0.35f, 0.86f), new Vector2(0.65f, 0.92f))
                .gameObject.AddComponent<TextMeshProUGUI>();
            heading.font = _projectFont;
            heading.fontSharedMaterial = _projectFont.material;
            heading.text = Localization.instance.Localize("$project").ToUpperInvariant();
            heading.fontSize = 32f;
            heading.characterSpacing = 30f;
            heading.alignment = TextAlignmentOptions.Center;
            heading.color = new Color(0.918f, 0.882f, 0.851f);
            heading.raycastTarget = false;

            nativeLogo.gameObject.SetActive(false);

            var menuRect = (RectTransform)startup.m_menuList.transform;
            menuRect.anchorMin = menuRect.anchorMax = new Vector2(.5f, .23f);
            menuRect.pivot = new Vector2(.5f, .5f);
            menuRect.anchoredPosition = Vector2.zero;
            menuRect.sizeDelta = new Vector2(420, 220);

            // The current MenuList includes the native gold separator outside its buttons.
            foreach (var decoration in startup.m_menuList.GetComponentsInChildren<Image>(true))
                if (decoration.GetComponentInParent<Button>(true) == null)
                    decoration.enabled = false;

            var divider = Decoration(startup.m_menuList.transform, "Auga Menu Divider", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            divider.sizeDelta = new Vector2(420f, 10f);
            divider.anchoredPosition = new Vector2(0f, -22f);
            AddDividerLine(divider, new Vector2(0f, 0.5f), new Vector2(0.488f, 0.5f));
            AddDividerLine(divider, new Vector2(0.512f, 0.5f), new Vector2(1f, 0.5f));
            var diamond = Decoration(divider, "Diamond", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            diamond.sizeDelta = new Vector2(7f, 7f);
            diamond.localRotation = Quaternion.Euler(0f, 0f, 45f);
            AddDividerLine(diamond, Vector2.zero, Vector2.right);
            AddDividerLine(diamond, Vector2.up, Vector2.one);
            foreach (float x in new[] { 0f, 1f })
            {
                var side = AddDividerLine(diamond, new Vector2(x, 0f), new Vector2(x, 1f));
                side.sizeDelta = new Vector2(1f, 0f);
            }
            Debug.Log("[Auga] Reference menu branding applied: upper Auga logo and lower menu divider.");
        }

        private static void ApplyLoadingPresentation(FejdStartup startup)
        {
            // Preserve the current loading root and its transition logic; reuse original artwork beneath it.
            var native = startup.m_loading.transform;
            foreach (var graphic in native.GetComponentsInChildren<Graphic>(true)) graphic.enabled = false;
            var original = Auga.Assets.MainMenuPrefab.transform.Find("Loading");
            var presentation = Instantiate(original.gameObject, native, false);
            presentation.name = "Auga Loading Presentation";
            var rect = (RectTransform)presentation.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            foreach (var graphic in presentation.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            Localization.instance.Localize(presentation.transform);
            presentation.SetActive(true);
        }

        private static RectTransform Decoration(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.layer = parent.gameObject.layer;
            obj.transform.SetParent(parent, false);
            obj.AddComponent<LayoutElement>().ignoreLayout = true;
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static RectTransform AddDividerLine(Transform parent, Vector2 min, Vector2 max)
        {
            var rect = Decoration(parent, "Divider Line", min, max);
            rect.sizeDelta = new Vector2(0f, 1f);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.918f, 0.882f, 0.851f, 0.7f);
            image.raycastTarget = false;
            return rect;
        }

        private System.Collections.IEnumerator LogGeometry(Button[] buttons)
        {
            yield return new WaitForSeconds(3f);
            foreach (var button in buttons)
            {
                var label = button.targetGraphic as TMP_Text;
                Debug.Log($"[Auga] Menu geometry {button.name}: buttonRect={((RectTransform)button.transform).rect}, active={button.gameObject.activeInHierarchy}, textTint={label.canvasRenderer.GetColor()}");
            }
        }
    }

    public sealed class MainMenuSelectionOrnaments : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public GameObject Ornaments;
        private bool _hovered;
        public void OnPointerEnter(PointerEventData eventData) => _hovered = true;
        public void OnPointerExit(PointerEventData eventData) => _hovered = false;
        private void OnDisable() { _hovered = false; if (Ornaments != null) Ornaments.SetActive(false); }
        private void LateUpdate()
        {
            if (Ornaments != null)
                Ornaments.SetActive(GetComponent<Button>().IsInteractable() &&
                    (_hovered || (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)));
        }
    }
}
