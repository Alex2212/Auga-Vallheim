using System;
using System.Collections;
using BepInEx;
using UnityEngine;
using UnityEngine.UI;

namespace AugaApiExample
{
    [BepInPlugin(PluginID, "Auga API Example", "2.0.0")]
    [BepInDependency("randyknapp.mods.auga", BepInDependency.DependencyFlags.SoftDependency)]
    public class AugaApiExample : BaseUnityPlugin
    {
        public const string PluginID = "mod.randyknapp.augaapiexample";
        private IDisposable _subscription;
        private Transform _root;
        private GameObject _panel;
        private Auga.WorkbenchTabData _workbenchTab;
        private Button _craft, _upgrade;

        private IEnumerator Start()
        {
            if (!Auga.API.IsLoaded()) yield break;
            if (Auga.API.GetApiVersion() == null) yield break;
            while (!Auga.API.IsReady()) yield return null;
            _subscription = Auga.API.ComplexTooltip_SubscribeItem((tooltip, item) =>
            {
                var box = Auga.API.ComplexTooltip_AddCenteredTextBox(tooltip);
                Auga.API.TooltipTextBox_AddLine(box, "Added by Auga API Example", localize: false);
            });
            while (true)
            {
                // Wait for Auga's Start, then recreate mod-owned UI for each inventory scene.
                if (Auga.API.IsInventoryReady())
                {
                    var root = Auga.API.Inventory_GetRoot();
                    if (root != _root || _panel == null)
                    {
                        if (_panel != null) Destroy(_panel);
                        RemoveButtonListeners();
                        _root = root;
                        var playerTab = Auga.API.PlayerPanel_AddTab(PluginID + ".Tools", null, "API Example",
                            index => Logger.LogInfo("Player tab selected: " + index));
                        var parent = playerTab != null ? playerTab.ContentGO.transform : root;
                        _panel = Auga.API.Panel_Create(parent, new Vector2(300, 140), PluginID + ".Panel", true);
                        _panel.SetActive(true);
                        var button = Auga.API.FancyButton_Create(_panel.transform, "ExampleButton", "EXAMPLE");
                        button.gameObject.SetActive(true);
                        ((RectTransform)button.transform).anchoredPosition = Vector2.zero;
                        button.onClick.AddListener(() => Logger.LogInfo("Auga example button clicked."));
                        Auga.API.Tooltip_MakeSimpleTooltip(button.gameObject);
                        var tooltip = button.GetComponent<UITooltip>();
                        tooltip.m_topic = "Auga API Example";
                        tooltip.m_text = "A mod-owned button using the original Auga visuals.";
                        if (Auga.API.SupportsFeature("workbench-tabs"))
                        {
                            _workbenchTab = Auga.API.Workbench_AddWorkbenchTab(PluginID + ".Workbench", null,
                                "API Example", OnWorkbenchSelected);
                            _craft = Auga.API.Workbench_GetCraftingTabButton();
                            _upgrade = Auga.API.Workbench_GetUpgradeTabButton();
                            _craft?.onClick.AddListener(DisableVariants);
                            _upgrade?.onClick.AddListener(DisableVariants);
                        }
                    }
                }
                yield return null;
            }
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
            RemoveButtonListeners();
            DisableVariants();
            if (_panel != null) Destroy(_panel);
        }

        private void OnWorkbenchSelected(int index)
        {
            if (_workbenchTab == null) return;
            Logger.LogInfo("Workbench tab selected: " + index);
            var info = _workbenchTab.ItemInfoGO;
            Auga.API.ComplexTooltip_ClearTextBoxes(info);
            Auga.API.ComplexTooltip_SetTopic(info, "API EXAMPLE");
            Auga.API.ComplexTooltip_SetSubtitle(info, "CUSTOM WORKBENCH TAB");
            Auga.API.ComplexTooltip_SetDescription(info, "This tab owns its display; it does not craft items.");
            var box = Auga.API.ComplexTooltip_AddCenteredTextBox(info);
            Auga.API.TooltipTextBox_AddLine(box, "Select EXAMPLE OPTIONS to open the variant dialog.", localize: false);
            Auga.API.RequirementsPanel_GetIcon(_workbenchTab.RequirementsPanelGO).enabled = false;
            foreach (var requirement in Auga.API.RequirementsPanel_RequirementList(_workbenchTab.RequirementsPanelGO))
                requirement.SetActive(false);
            if (Auga.API.SupportsFeature("custom-variants"))
            {
                var text = Auga.API.CustomVariantPanel_Enable("EXAMPLE OPTIONS", shown => Logger.LogInfo("Variant dialog shown: " + shown));
                if (text != null) text.text = "Custom variant content supplied through the Auga API.";
            }
        }

        private static void DisableVariants() => Auga.API.CustomVariantPanel_Disable();
        private void RemoveButtonListeners()
        {
            if (_craft != null) _craft.onClick.RemoveListener(DisableVariants);
            if (_upgrade != null) _upgrade.onClick.RemoveListener(DisableVariants);
        }
    }
}
