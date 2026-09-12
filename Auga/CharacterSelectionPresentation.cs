using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    // Presentation only: profiles, selection, confirmations and save management remain native.
    public sealed class CharacterSelectionPresentation : MonoBehaviour
    {
        private FejdStartup _startup;
        private TMP_FontAsset _body, _norse;
        private RectTransform _content;
        private readonly List<Image> _rows = new List<Image>();
        private string _profileKey;
        private readonly Dictionary<string, Texture2D> _portraits = new Dictionary<string, Texture2D>();
        private readonly List<RawImage> _portraitImages = new List<RawImage>();
        private bool _takingPortraits;
        private static readonly Color Cream = new Color(.918f, .882f, .851f);

        private void Start()
        {
            try
            {
                _startup = GetComponent<FejdStartup>();
                var root = _startup.m_selectCharacterPanel.transform;
                _body = TMP_FontAsset.CreateFontAsset(Auga.Assets.SourceSansProBold);
                _norse = TMP_FontAsset.CreateFontAsset(Resources.FindObjectsOfTypeAll<Font>()
                    .First(font => font.name.StartsWith("Norsebold", StringComparison.OrdinalIgnoreCase)));
                var buttons = root.GetComponentsInChildren<Button>(true);
                var back = buttons.First(button => HasListener(button, "OnSelelectCharacterBack"));
                var manage = buttons.First(button => HasListener(button, "OnManageSaves"));
                // Hide only the original selection chrome, preserving the native confirmation dialog.
                foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
                {
                    if (graphic.transform.IsChildOf(_startup.m_removeCharacterDialog.transform)) continue;
                    if (graphic.GetComponentInParent<Button>(true) != null) continue;
                    if (graphic == _startup.m_csSourceInfo) continue;
                    graphic.enabled = false;
                }
                var panel = Box(root, "Auga Character Panel", new Color(.22f, .20f, .165f, .98f));
                panel.SetAsFirstSibling(); // Native confirmation dialogs must remain above the owned panel.
                panel.anchorMin = new Vector2(.04f, .08f);
                panel.anchorMax = new Vector2(.38f, .92f);
                panel.offsetMin = panel.offsetMax = Vector2.zero;
                SettingsPresentation.AddCorners(panel);
                var title = Label(panel, "Select Character", _norse, 40f, TextAlignmentOptions.Center);
                Place(title.rectTransform, new Vector2(.06f, 1f), new Vector2(.94f, 1f), new Vector2(0f, -82f), new Vector2(0f, -20f));
                var viewport = Box(panel, "Character List", new Color(.12f, .11f, .09f, .65f));
                Place(viewport, new Vector2(.07f, 0f), new Vector2(.93f, 1f), new Vector2(0f, 164f), new Vector2(-12f, -100f));
                viewport.gameObject.AddComponent<RectMask2D>();
                var insetBase = Box(panel, "Character List Lower Background", new Color(.12f, .11f, .09f, .65f));
                Place(insetBase, new Vector2(.07f, 0f), new Vector2(.93f, 0f), new Vector2(0f, 140f), new Vector2(-12f, 164f));
                var insetObject = new GameObject("Character Actions Inset", typeof(RectTransform), typeof(CharacterActionsInset));
                insetObject.layer = panel.gameObject.layer;
                insetObject.transform.SetParent(panel, false);
                var inset = (RectTransform)insetObject.transform;
                inset.anchorMin = inset.anchorMax = new Vector2(.5f, 0f);
                inset.pivot = new Vector2(.5f, 0f);
                // End the list background at the action buttons' centerline; retain the top width and 45-degree cuts.
                inset.anchoredPosition = new Vector2(0f, 140f);
                inset.sizeDelta = new Vector2(332f, 24f);
                insetObject.GetComponent<CharacterActionsInset>().color = new Color(.22f, .20f, .165f, 1f);
                _content = Box(viewport, "Content", Color.clear);
                _content.anchorMin = new Vector2(0f, 1f);
                _content.anchorMax = Vector2.one;
                _content.pivot = new Vector2(.5f, 1f);
                _content.sizeDelta = Vector2.zero;
                var scroll = viewport.gameObject.AddComponent<ScrollRect>();
                scroll.viewport = viewport;
                scroll.content = _content;
                scroll.horizontal = false;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                var track = Box(panel, "Character Scrollbar", Color.clear);
                Place(track, new Vector2(.93f, 0f), new Vector2(.93f, 1f), new Vector2(-8f, 164f), new Vector2(4f, -100f));
                var scrollbar = track.gameObject.AddComponent<Scrollbar>();
                var handle = Box(track, "Handle", Color.white);
                scrollbar.handleRect = handle;
                scrollbar.targetGraphic = handle.GetComponent<Image>();
                scrollbar.direction = Scrollbar.Direction.BottomToTop;
                scroll.verticalScrollbar = scrollbar;
                SettingsPresentation.StyleScrollbar(scrollbar);
                StyleButton(_startup.m_csRemoveButton, panel, -80f, 140f, 148f, false);
                StyleButton(_startup.m_csNewButton, panel, 80f, 140f, 148f, false);
                StyleButton(manage, panel, 0f, 96f, 220f, false);
                StyleButton(back, panel, -110f, 42f, 200f, true);
                StyleButton(_startup.m_csStartButton, panel, 110f, 42f, 200f, true);
                StyleRemoveConfirmation();
                Refresh();
                Debug.Log("[Auga] Character selection presentation applied; native profile actions retained.");
            }
            catch (Exception exception)
            {
                Auga.LogError($"Character selection presentation failed: {exception}");
                enabled = false;
            }
        }

        private void StyleRemoveConfirmation()
        {
            var dialog = _startup.m_removeCharacterDialog.transform;
            // This dialog predates UnifiedPopup, so its presentation needs its own pass.
            foreach (var image in dialog.GetComponentsInChildren<Image>(true))
            {
                if (image.GetComponentInParent<Button>(true) != null || image.sprite == null ||
                    image.sprite.name.IndexOf("woodpanel", StringComparison.OrdinalIgnoreCase) < 0) continue;
                var background = Box(image.transform, "Auga Remove Character Panel", new Color(.22f, .20f, .165f, .98f));
                background.SetAsFirstSibling();
                SettingsPresentation.AddCorners(background);
                image.enabled = false;
            }
            foreach (var label in dialog.GetComponentsInChildren<TMP_Text>(true))
            {
                label.font = _norse;
                label.fontSharedMaterial = _norse.material;
                label.color = Cream;
            }
            foreach (var button in dialog.GetComponentsInChildren<Button>(true))
            {
                // Keep native positioning, navigation and persistent confirmation callbacks.
                foreach (var image in button.GetComponentsInChildren<Image>(true)) image.enabled = false;
                var background = Box(button.transform, "Auga Confirmation Button", Color.white);
                background.SetAsFirstSibling();
                var target = background.GetComponent<Image>();
                var source = Auga.Assets.ButtonFancy.GetComponent<Button>().targetGraphic as Image;
                target.sprite = source.sprite;
                target.type = source.type;
                target.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
                StyleButtonStates(button, target);
                foreach (var label in button.GetComponentsInChildren<TMP_Text>(true)) label.fontSize = 26;
                var tint = button.GetComponent<ButtonTextColor>();
                if (tint != null) tint.m_defaultColor = tint.m_defaultMeshColor = Cream;
            }
        }

        private static bool HasListener(Button button, string method)
        {
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                if (button.onClick.GetPersistentMethodName(i) == method) return true;
            return false;
        }

        private void Update()
        {
            if (_content == null || !_startup.m_selectCharacterPanel.activeInHierarchy) return;
            _startup.m_csLeftButton.gameObject.SetActive(false);
            _startup.m_csRightButton.gameObject.SetActive(false);
            _startup.m_csNewBigButton.gameObject.SetActive(false);
            _startup.m_csNewButton.gameObject.SetActive(true);
            Refresh();
            if (!_takingPortraits && _startup.m_profiles.Any(profile => !_portraits.ContainsKey(profile.m_filename)))
                StartCoroutine(CapturePortraits());
            for (int i = 0; i < _rows.Count; i++)
                _rows[i].color = i == _startup.m_profileIndex ? new Color(.18f, .34f, .43f) : new Color(.12f, .11f, .09f);
        }

        private void Refresh()
        {
            if (_startup.m_profiles == null) return;
            string key = string.Join("|", _startup.m_profiles.Select(profile => profile.m_filename + ":" + profile.m_playerName + ":" + profile.m_fileSource));
            if (_profileKey == key) return;
            _profileKey = key;
            foreach (var row in _rows) Destroy(row.gameObject);
            _rows.Clear();
            _portraitImages.Clear();
            _content.sizeDelta = new Vector2(0f, _startup.m_profiles.Count * 144f);
            for (int i = 0; i < _startup.m_profiles.Count; i++)
            {
                var profile = _startup.m_profiles[i];
                var row = Box(_content, "Character Row", new Color(.12f, .11f, .09f));
                Place(row, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -(i + 1) * 144f + 8f), new Vector2(0f, -i * 144f));
                _rows.Add(row.GetComponent<Image>());
                var button = row.gameObject.AddComponent<Button>();
                button.targetGraphic = row.GetComponent<Image>();
                button.onClick.AddListener(() => _startup.SetSelectedProfile(profile.m_filename));
                var portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(RawImage));
                portraitObject.layer = row.gameObject.layer;
                portraitObject.transform.SetParent(row, false);
                var portrait = portraitObject.GetComponent<RawImage>();
                portrait.raycastTarget = false;
                Place(portrait.rectTransform, Vector2.zero, new Vector2(0f, 1f), new Vector2(3f, 3f), new Vector2(106f, -3f));
                portrait.texture = _portraits.TryGetValue(profile.m_filename, out var texture) ? texture : null;
                portrait.color = portrait.texture == null ? Color.clear : Color.white;
                _portraitImages.Add(portrait);
                var name = Label(row, profile.m_playerName, _norse, 30f, TextAlignmentOptions.MidlineLeft);
                Place(name.rectTransform, Vector2.zero, new Vector2(.60f, 1f), new Vector2(120f, 18f), new Vector2(-6f, -8f));
                var source = Label(row, profile.m_fileSource.ToString(), _body, 12f, TextAlignmentOptions.BottomLeft);
                Place(source.rectTransform, Vector2.zero, new Vector2(.60f, 1f), new Vector2(120f, 10f), new Vector2(0f, -80f));
                var stats = Label(row, $"Deaths: {profile.GetStat(PlayerStatType.Deaths)}\nStructures built: {profile.GetStat(PlayerStatType.Builds)}\nItems crafted: {profile.GetStat(PlayerStatType.Crafts)}", _body, 17f, TextAlignmentOptions.MidlineRight);
                Place(stats.rectTransform, new Vector2(.60f, 0f), Vector2.one, new Vector2(0f, 6f), new Vector2(-10f, -6f));
            }
        }

        private IEnumerator CapturePortraits()
        {
            _takingPortraits = true;
            var cameraObject = new GameObject("Auga Character Portrait Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.fieldOfView = 30f;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 20f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.12f, .11f, .09f);
            var target = new RenderTexture(128, 160, 24);
            camera.targetTexture = target;
            try
            {
                foreach (var profile in _startup.m_profiles.ToArray())
                {
                    if (_portraits.ContainsKey(profile.m_filename)) continue;
                    if (!_startup.m_selectCharacterPanel.activeInHierarchy) break;
                    _startup.SetupCharacterPreview(profile);
                    for (int frame = 0; frame < 5; frame++) yield return null;
                    if (_startup.m_playerInstance == null) break;
                    try
                    {
                        var player = _startup.m_playerInstance.transform;
                        var head = Utils.FindChild(player, "Head");
                        if (head == null) throw new InvalidOperationException("Character preview has no head transform.");
                        var look = head.position - Vector3.up * .02f;
                        camera.transform.position = look + player.forward * 1.25f;
                        camera.transform.LookAt(look);
                        camera.Render();
                        var previous = RenderTexture.active;
                        var texture = new Texture2D(128, 160, TextureFormat.RGB24, false);
                        try
                        {
                            RenderTexture.active = target;
                            texture.ReadPixels(new Rect(0, 0, 128, 160), 0, 0);
                            texture.Apply();
                        }
                        finally { RenderTexture.active = previous; }
                        _portraits[profile.m_filename] = texture;
                        int index = _startup.m_profiles.IndexOf(profile);
                        if (index >= 0 && index < _portraitImages.Count)
                        {
                            _portraitImages[index].texture = texture;
                            _portraitImages[index].color = Color.white;
                        }
                    }
                    catch (Exception exception)
                    {
                        Auga.LogError($"Character portrait capture failed: {exception}");
                        _portraits[profile.m_filename] = null;
                    }
                }
            }
            finally
            {
                if (_startup.m_profileIndex >= 0 && _startup.m_profileIndex < _startup.m_profiles.Count)
                    _startup.SetupCharacterPreview(_startup.m_profiles[_startup.m_profileIndex]);
                camera.targetTexture = null;
                target.Release();
                Destroy(target);
                Destroy(cameraObject);
                _takingPortraits = false;
            }
        }

        private void OnDestroy()
        {
            foreach (var texture in _portraits.Values) if (texture != null) Destroy(texture);
            if (_body != null) Destroy(_body);
            if (_norse != null) Destroy(_norse);
        }

        private void StyleButton(Button button, Transform panel, float x, float y, float width, bool fancy)
        {
            StyleButton(button, panel, x, y, width, fancy, _body, _norse);
        }

        internal static void StyleButton(Button button, Transform panel, float x, float y, float width, bool fancy, TMP_FontAsset body, TMP_FontAsset norse)
        {
            button.transform.SetParent(panel, false);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, fancy ? 48f : 34f);
            foreach (var image in button.GetComponentsInChildren<Image>(true)) image.enabled = false;
            var background = Box(rect, "Auga Character Button", Color.white);
            background.SetAsFirstSibling();
            var target = background.GetComponent<Image>();
            var source = (fancy ? Auga.Assets.ButtonFancy : Auga.Assets.ButtonMedium).GetComponent<Button>().targetGraphic as Image;
            target.sprite = source.sprite;
            target.type = source.type;
            target.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            StyleButtonStates(button, target);
            foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
            {
                label.font = fancy ? norse : body;
                label.fontSharedMaterial = label.font.material;
                label.color = Cream;
                label.fontSize = fancy ? 28f : 16f;
                label.fontStyle = fancy ? FontStyles.Normal : FontStyles.UpperCase;
                label.enableAutoSizing = false;
                label.alignment = TextAlignmentOptions.Center;
                label.rectTransform.localScale = Vector3.one;
                Place(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 0f), new Vector2(-16f, 0f));
            }
        }

        internal static void StyleButtonStates(Button button, Image artwork)
        {
            // SpriteSwap retains native hover/focus images even after artwork is replaced.
            button.transition = Selectable.Transition.ColorTint;
            button.spriteState = default;
            artwork.overrideSprite = null;
            button.targetGraphic = artwork;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = colors.selectedColor = new Color(1f, .83f, .5f);
            colors.pressedColor = new Color(.75f, .6f, .3f);
            colors.disabledColor = new Color(.55f, .55f, .55f);
            button.colors = colors;
        }

        private TMP_Text Label(Transform parent, string text, TMP_FontAsset font, float size, TextAlignmentOptions alignment)
        {
            var obj = new GameObject("Auga Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.layer = parent.gameObject.layer;
            obj.transform.SetParent(parent, false);
            var label = obj.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.font = font;
            label.fontSharedMaterial = font.material;
            label.fontSize = size;
            label.color = Cream;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        internal static RectTransform Box(Transform parent, string name, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.layer = parent.gameObject.layer;
            obj.transform.SetParent(parent, false);
            obj.AddComponent<LayoutElement>().ignoreLayout = true;
            var rect = (RectTransform)obj.transform;
            Place(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            obj.GetComponent<Image>().color = color;
            return rect;
        }

        internal static void Place(RectTransform rect, Vector2 min, Vector2 max, Vector2 low, Vector2 high)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = low;
            rect.offsetMax = high;
        }
    }

    public sealed class CharacterActionsInset : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = rectTransform.rect;
            // Equal horizontal run and vertical rise give both cuts a 45-degree angle.
            float cut = rect.height;
            mesh.AddVert(new Vector3(rect.xMin + cut, rect.yMax), color, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMax - cut, rect.yMax), color, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMax, rect.yMin), color, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMin, rect.yMin), color, Vector2.zero);
            mesh.AddTriangle(0, 1, 2);
            mesh.AddTriangle(0, 2, 3);
        }
    }
}
