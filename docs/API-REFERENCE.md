# Auga API signature reference

Generated from `Auga/API.cs`. See [the integration guide](MODDING.md) for readiness, ownership, and capability limits.

## Core and assets

```csharp
bool IsLoaded();
```

```csharp
System.Reflection.Assembly LoadAssembly();
```

```csharp
bool IsReady();
```

```csharp
bool IsInventoryReady();
```

```csharp
string GetApiVersion();
```

```csharp
bool SupportsFeature(string feature);
```

## Inventory

```csharp
Transform Inventory_GetRoot();
```

## Core and assets

```csharp
Font GetNorseFont();
```

```csharp
TMPro.TMP_FontAsset GetBoldTMPFont();
```

```csharp
TMPro.TMP_FontAsset GetNorseTMPFont();
```

```csharp
Font GetBoldFont();
```

```csharp
Font GetSemiBoldFont();
```

```csharp
Font GetRegularFont();
```

```csharp
Sprite GetItemBackgroundSprite();
```

## Panel

```csharp
GameObject Panel_Create(Transform parent, Vector2 size, string name, bool withCornerDecoration);
```

## SmallButton

```csharp
Button SmallButton_Create(Transform parent, string name, string labelText);
```

## MediumButton

```csharp
Button MediumButton_Create(Transform parent, string name, string labelText);
```

## FancyButton

```csharp
Button FancyButton_Create(Transform parent, string name, string labelText);
```

## SettingsButton

```csharp
Button SettingsButton_Create(Transform parent, string name, string labelText);
```

## DiamondButton

```csharp
Button DiamondButton_Create(Transform parent, string name, Sprite icon);
```

## Divider

```csharp
GameObject Divider_CreateSmall(Transform parent, string name, float width = -1);
```

```csharp
Tuple<GameObject, GameObject> Divider_CreateMedium(Transform parent, string name, float width = -1);
```

```csharp
Tuple<GameObject, GameObject> Divider_CreateLarge(Transform parent, string name, float width = -1);
```

## Button

```csharp
void Button_SetTextColors(Button button, Color normal, Color highlighted, Color pressed, Color selected, Color disabled, Color baseTextColor);
```

```csharp
void Button_OverrideTextColor(Button button, Color color);
```

## Tooltip

```csharp
void Tooltip_MakeSimpleTooltip(GameObject obj);
```

```csharp
void Tooltip_MakeItemTooltip(GameObject obj, ItemDrop.ItemData item);
```

```csharp
void Tooltip_MakeFoodTooltip(GameObject obj, Player.Food food);
```

```csharp
void Tooltip_MakeStatusEffectTooltip(GameObject obj, StatusEffect statusEffect);
```

```csharp
void Tooltip_MakeSkillTooltip(GameObject obj, Skills.Skill skill);
```

## PlayerPanel

```csharp
bool PlayerPanel_HasTab(string tabID);
```

```csharp
PlayerPanelTabData PlayerPanel_AddTab(string tabID, Sprite tabIcon, string tabTitleText, Action<int> onTabSelected);
```

```csharp
bool PlayerPanel_IsTabActive(GameObject tabButton);
```

```csharp
Button PlayerPanel_GetTabButton(int index);
```

## Workbench

```csharp
bool Workbench_HasWorkbenchTab(string tabID);
```

```csharp
WorkbenchTabData Workbench_AddWorkbenchTab(string tabID, Sprite tabIcon, string tabTitleText, Action<int> onTabSelected);
```

```csharp
WorkbenchTabData Workbench_AddVanillaWorkbenchTab(string tabID, Sprite tabIcon, string tabTitleText, Action<int> onTabSelected);
```

```csharp
bool Workbench_IsTabActive(GameObject tabButton);
```

```csharp
Button Workbench_GetCraftingTabButton();
```

```csharp
Button Workbench_GetUpgradeTabButton();
```

```csharp
GameObject Workbench_CreateNewResultsPanel();
```

## TooltipTextBox

```csharp
void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, Text t, object s, bool localize = true);
```

```csharp
void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, Text t, object s, bool localize, bool overwrite);
```

```csharp
void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, object a, bool localize = true);
```

```csharp
void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, object a, bool localize, bool overwrite);
```

```csharp
void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, object a, object b, bool localize = true);
```

```csharp
void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, object a, object b, bool localize, bool overwrite);
```

```csharp
void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, object a, object b, object parenthetical, bool localize = true);
```

```csharp
void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, object a, object b, object parenthetical, bool localize, bool overwrite);
```

```csharp
void TooltipTextBox_AddUpgradeLine(GameObject tooltipTextBoxGO, object label, object value1, object value2, string color2, bool localize = true);
```

```csharp
void TooltipTextBox_AddUpgradeLine(GameObject tooltipTextBoxGO, object label, object value1, object value2, string color2, bool localize, bool overwrite);
```

## ComplexTooltip

```csharp
IDisposable ComplexTooltip_SubscribeItem(Action<GameObject, ItemDrop.ItemData> listener);
```

```csharp
IDisposable ComplexTooltip_SubscribeFood(Action<GameObject, Player.Food> listener);
```

```csharp
IDisposable ComplexTooltip_SubscribeStatusEffect(Action<GameObject, StatusEffect> listener);
```

```csharp
IDisposable ComplexTooltip_SubscribeSkill(Action<GameObject, Skills.Skill> listener);
```

```csharp
void ComplexTooltip_AddItemTooltipCreatedListener(Action<GameObject, ItemDrop.ItemData> listener);
```

```csharp
void ComplexTooltip_AddFoodTooltipCreatedListener(Action<GameObject, Player.Food> listener);
```

```csharp
void ComplexTooltip_AddStatusEffectTooltipCreatedListener(Action<GameObject, StatusEffect> listener);
```

```csharp
void ComplexTooltip_AddSkillTooltipCreatedListener(Action<GameObject, Skills.Skill> listener);
```

```csharp
void ComplexTooltip_AddItemStatPreprocessor(Func<ItemDrop.ItemData, string, string, Tuple<string, string>> itemStatPreprocessor);
```

```csharp
void ComplexTooltip_ClearTextBoxes(GameObject complexTooltipGO);
```

```csharp
GameObject ComplexTooltip_AddTwoColumnTextBox(GameObject complexTooltipGO);
```

```csharp
GameObject ComplexTooltip_AddCenteredTextBox(GameObject complexTooltipGO);
```

```csharp
GameObject ComplexTooltip_AddUpgradeLabels(GameObject complexTooltipGO);
```

```csharp
GameObject ComplexTooltip_AddUpgradeTwoColumnTextBox(GameObject complexTooltipGO);
```

```csharp
GameObject ComplexTooltip_AddCheckBoxTextBox(GameObject complexTooltipGO);
```

```csharp
GameObject ComplexTooltip_AddDivider(GameObject complexTooltipGO);
```

```csharp
void ComplexTooltip_SetTopic(GameObject complexTooltipGO, string topic);
```

```csharp
void ComplexTooltip_SetSubtitle(GameObject complexTooltipGO, string topic);
```

```csharp
void ComplexTooltip_SetDescription(GameObject complexTooltipGO, string desc);
```

```csharp
void ComplexTooltip_SetIcon(GameObject complexTooltipGO, Sprite icon);
```

```csharp
void ComplexTooltip_EnableDescription(GameObject complexTooltipGO, bool enabled);
```

```csharp
string ComplexTooltip_GenerateItemSubtext(GameObject complexTooltipGO, ItemDrop.ItemData item);
```

```csharp
void ComplexTooltip_SetItem(GameObject complexTooltipGO, ItemDrop.ItemData item, int quality = -1, int variant = -1);
```

```csharp
void ComplexTooltip_SetItemNoTextBoxes(GameObject complexTooltipGO, ItemDrop.ItemData item, int quality = -1, int variant = -1);
```

```csharp
void ComplexTooltip_SetFood(GameObject complexTooltipGO, Player.Food food);
```

```csharp
void ComplexTooltip_SetStatusEffect(GameObject complexTooltipGO, StatusEffect statusEffect);
```

```csharp
void ComplexTooltip_SetSkill(GameObject complexTooltipGO, Skills.Skill skill);
```

## RequirementsPanel

```csharp
Image RequirementsPanel_GetIcon(GameObject requirementsPanelGO);
```

```csharp
GameObject[] RequirementsPanel_RequirementList(GameObject requirementsPanelGO);
```

```csharp
void RequirementsPanel_SetWires(GameObject requirementsPanelGO, RequirementWireState[] wireStates, bool canCraft);
```

## CustomVariantPanel

```csharp
Text CustomVariantPanel_Enable(string buttonLabel, Action<bool> onShow);
```

```csharp
void CustomVariantPanel_SetButtonLabel(string buttonLabel);
```

```csharp
void CustomVariantPanel_Disable();
```
