using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    [HarmonyPatch(typeof(Menu), nameof(Menu.Start))]
    public static class PauseMenuPresentationPatch
    {
        private static void Postfix(Menu __instance)
        {
            if (__instance.GetComponent<PauseMenuPresentation>() == null)
                __instance.gameObject.AddComponent<PauseMenuPresentation>();
        }
    }

    public sealed class PauseMenuPresentation : MonoBehaviour
    {
        private Menu _menu;
        private Button _compendium;
        private readonly List<Button> _rows = new List<Button>();
        private RectTransform _topDivider, _bottomDivider;
        private AugaUnity.AugaCompendiumController _compendiumPanel;

        private void Start()
        {
            try
            {
                _menu = GetComponent<Menu>();
                var source = Auga.Assets.MenuPrefab.GetComponentsInChildren<Transform>(true)
                    .First(t => t.name == "MenuEntries");
                var entries = Instantiate(source, _menu.m_menuDialog, false);
                entries.name = "Auga Pause Entries";
                // Clone only the original presentation, never its outdated Menu controller.
                foreach (Transform child in _menu.m_menuDialog)
                    if (child != entries) child.gameObject.SetActive(false);
                entries.gameObject.SetActive(true);
                _menu.menuEntriesParent = (RectTransform)entries;
                Localization.instance.Localize(entries);

                Button Bind(string name, Button native)
                {
                    var button = entries.GetComponentsInChildren<Button>(true).First(b => b.name == name);
                    button.onClick = native.onClick;
                    button.gameObject.SetActive(native.gameObject.activeSelf);
                    button.interactable = native.interactable;
                    return button;
                }
                var oldContinue = _menu.m_continueButton;
                _menu.m_continueButton = Bind("CloseButton", oldContinue);
                foreach (var group in _menu.GetComponentsInChildren<UIGroupHandler>(true))
                    if (group.m_defaultElement == oldContinue.gameObject)
                        group.m_defaultElement = _menu.m_continueButton.gameObject;
                _menu.m_saveButton = Bind("Save", _menu.m_saveButton);
                _menu.m_skipButton = Bind("SkipIntro", _menu.m_skipButton);
                _menu.m_playerListButton = Bind("CurrentPlayerList", _menu.m_playerListButton);
                _menu.m_settingsButton = Bind("Settings", _menu.m_settingsButton);
                _menu.m_logoutButton = Bind("Logout", _menu.m_logoutButton);
                _menu.m_quitButton = Bind("Exit", _menu.m_quitButton);
                // Invite is a newer game action: give it the same original Auga row design.
                var invite = Instantiate(_menu.m_playerListButton, entries, false);
                invite.name = "Invite"; invite.onClick = _menu.m_inviteButton.onClick;
                var nativeInviteLabel = _menu.m_inviteButton.GetComponentInChildren<TMPro.TMP_Text>(true);
                foreach (var label in invite.GetComponentsInChildren<Text>(true)) label.text = nativeInviteLabel.text;
                invite.gameObject.SetActive(_menu.m_inviteButton.gameObject.activeSelf);
                _menu.m_inviteButton = invite;
                _compendium = entries.GetComponentsInChildren<Button>(true).First(b => b.name == "Compendium");
                _compendium.onClick = new Button.ButtonClickedEvent();
                _compendium.onClick.AddListener(OpenCompendium);
                _compendium.gameObject.SetActive(true);
                _rows.AddRange(new[] { _menu.m_skipButton, _menu.m_saveButton, _menu.m_playerListButton,
                    _menu.m_inviteButton, _menu.m_settingsButton, _compendium, _menu.m_logoutButton, _menu.m_quitButton });
                _topDivider = (RectTransform)entries.Find("DividerSmall");
                _bottomDivider = (RectTransform)entries.Find("DividerMedium");
                // Keep the native saved-time binding; it updates independently of button labels.
                var saved = entries.Find("LastTimeSaved");
                if (saved != null) saved.gameObject.SetActive(false);
                _menu.m_quitDialog = CreateConfirmation("ExitConfirm", _menu.m_quitDialog, _menu.OnQuitYes, _menu.OnQuitNo);
                _menu.m_logoutDialog = CreateConfirmation("LogoutConfirm", _menu.m_logoutDialog, _menu.OnLogoutYes, _menu.OnLogoutNo);
                _menu.UpdateNavigation();
                Debug.Log("[Auga] Original pause-menu entries restored with current native menu actions.");
            }
            catch (Exception e) { Auga.LogError($"Pause menu presentation failed: {e}"); enabled = false; }
        }

        private Transform CreateConfirmation(string sourceName, Transform native,
            UnityEngine.Events.UnityAction accept, UnityEngine.Events.UnityAction cancel)
        {
            var source = Auga.Assets.MenuPrefab.GetComponentsInChildren<Transform>(true).First(t => t.name == sourceName);
            var dialog = Instantiate(source, native.parent, false);
            dialog.gameObject.SetActive(false);
            Button no = null;
            foreach (var button in dialog.GetComponentsInChildren<Button>(true))
            {
                bool yes = Enumerable.Range(0, button.onClick.GetPersistentEventCount())
                    .Any(i => button.onClick.GetPersistentMethodName(i).EndsWith("Yes", StringComparison.Ordinal));
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(yes ? accept : cancel);
                if (!yes) no = button;
            }
            int priority = _menu.GetComponentsInChildren<UIGroupHandler>(true)
                .Select(g => g.m_groupPriority).DefaultIfEmpty(10).Max() + 1;
            foreach (var group in dialog.GetComponentsInChildren<UIGroupHandler>(true))
            {
                group.m_groupPriority = priority;
                group.m_defaultElement = no != null ? no.gameObject : null;
                group.SetActive(true);
                var canvas = group.GetComponent<CanvasGroup>();
                if (canvas != null) canvas.ignoreParentGroups = true;
            }
            Localization.instance.Localize(dialog);
            native.gameObject.SetActive(false);
            return dialog;
        }

        private void OpenCompendium()
        {
            try
            {
                if (_compendiumPanel == null) _compendiumPanel = CompendiumPresentation.Create(_menu.m_root);
                _compendiumPanel.ShowCompendium();
            }
            catch (Exception e) { Auga.LogError($"Could not open Auga Compendium: {e}"); }
        }

        private void LateUpdate()
        {
            // Native UpdateCrosshair restores gameplay visibility each frame. Hide it
            // after that update while the pause root (including its dialogs) is open.
            if (_menu.m_root.gameObject.activeInHierarchy && Hud.instance != null)
            {
                Hud.instance.m_crosshair.gameObject.SetActive(false);
                Hud.instance.m_crosshairBow.gameObject.SetActive(false);
            }
            if (!_menu.m_menuDialog.gameObject.activeInHierarchy) return;
            var rows = _rows.Where(b => b.gameObject.activeSelf).ToList();
            float top = (rows.Count - 1) * 18f;
            for (int i = 0; i < rows.Count; i++)
            {
                var rect = (RectTransform)rows[i].transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = new Vector2(0, top - i * 36);
            }
            _topDivider.anchoredPosition = new Vector2(0, top + 18);
            _bottomDivider.anchoredPosition = new Vector2(0, -top - 65);
            // Include Compendium in controller navigation alongside the native actions.
            rows.Insert(0, _menu.m_continueButton);
            rows = rows.Where(b => b.interactable).ToList();
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].navigation = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnUp = rows[(i + rows.Count - 1) % rows.Count], selectOnDown = rows[(i + 1) % rows.Count] };
            }
        }
    }
}
