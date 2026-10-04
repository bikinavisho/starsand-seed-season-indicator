**Project docs:** [Modding context](MODDING_CONTEXT.md) · [Requirements and design](MODDING_REQUIREMENTS.md) · [Game data reference](GAME_DATA_REFERENCE.md) · [UI integration notes](UI_INTEGRATION_NOTES.md) · [Reverse-engineering log](REVERSE_ENGINEERING_LOG.md)

---

# UI Integration Notes

## Current seasonal-indicator integration

The implemented plugin is `SeedSeasonDisplayPlugin` in
`src/SeedSeasonDisplayPlugin.cs`. It adds
`SeasonalSeedIndicatorManager`, which applies the same image-overlay logic to
the following UI elements:

| Surface | Cell/card component | Status |
| ------- | ------------------- | ------ |
| Player inventory | `UI.KUICell_Item` | Implemented; developer-confirmed working |
| Storage | `UI.KUICell_Item` | Implemented through inventory item cells; developer-confirmed working |
| Seed shop | `UI.KUIShopItemCard` | Implemented; developer-confirmed working |

The inventory cell's `Template` (or its current `Item.Template` fallback) and
the shop card's `ShopItem.Template.ItemTemplate.Get()` provide the item
template. The shared resolver checks `ItemTemplate.Extensions` for the
`XSandbox.Farm.FarmSeedItemExt` entry, converts its value using
`((Il2CppObjectBase)value).TryCast<FarmSeedItemExt>()`, and resolves
`FarmSeedItemExt.Template.Get()` to the crop template.

Each indicator is a separate `UnityEngine.UI.Image` child of the cell's
`Preview` transform (falling back to the cell transform if needed). It does
not replace or modify the preview. The image is anchored at the top right,
offset by 8 UI units, sized 28×28, preserves aspect, and does not receive
raycasts. The named child is reused; if an old TextMeshPro overlay child is
found, its text component is removed and an `Image` is added.

The current season comes from `KGameTimeUtil.Now.GetSeason()`. The plugin
compares it to the last observed `ESeason`; on change, and every 0.75 seconds,
it updates active `KUICell_Item` and `KUIShopItemCard` instances. The recurring
refresh updates recycled cells whose item binding changes. Empty, non-seed,
out-of-season, and all-four-season items clear/hide the image. Only the
current season's sprite is assigned.

Four 64×64 PNGs are included as explicit embedded resources in
`src/SeedSeasonDisplay.csproj` with logical names under
`StarsandIsland.SeedSeasonDisplay.Assets.*`. The plugin reads the embedded
bytes through its assembly, loads them into `Texture2D` instances with
`ImageConversion.LoadImage`, and creates a sprite from the full 64×64 rect.
Point filtering and clamp wrapping are set; the four sprites are cached in
static fields and are decoded on first use. There is no TextMeshPro assembly
reference in the project.

The current source contains no F7 hotkey or diagnostic scan code. F7 references
in the reverse-engineering documents describe historical investigation tools,
not the released plugin.

---

### ItemBrowser investigation

The notes below record earlier ItemBrowser research and are not required for
the current seed-indicator implementation.

A community ItemBrowser/Item Spawner mod is installed and has helped expose item information.

Its assembly is:

```text
ItemBrowser.dll
```

Important discovered types:

```text
ItemBrowser.ItemBrowserView
ItemBrowser.CatalogEntry
```

`CatalogEntry` contains:

```text
Id
DisplayName
Description
TypeId
TypeName
InventoryCategory
IconPath
BigIconPath
IconColorPath
MaxStack
MaxQuality
Template : ItemTemplate
```

The ItemBrowser displays Pumpkin Seeds approximately as:

```text
Pumpkin Seeds
Autumn Crop
Item.Crop.PumpkinSeed
```

It is currently suspected that these may correspond to:

```text
ItemTemplate.DisplayName
ItemTemplate.Type.DisplayName
ItemTemplate.Type.ID
```

but this has NOT been proven.


---

# ItemBrowser Mod

A community mod called **Item Spawner / ItemBrowser** is installed and has been useful for reverse engineering.

It provides a searchable catalog of game items.

For Pumpkin Seeds, its UI displays information including:

```text
Pumpkin Seeds
Autumn Crop
Item.Crop.PumpkinSeed
```

The relevant assembly is:

```text
ItemBrowser.dll
```

The important class discovered is:

```text
ItemBrowser.ItemBrowserView
```

It contains:

```text
_qtyMax
_slots : List<SlotView>
_categoryTabs : List<CategoryTabView>
_filtered : List<CatalogEntry>
_status
```

---

# ItemBrowser.CatalogEntry

The ItemBrowser mod contains:

```csharp
internal sealed class CatalogEntry
{
    public string Id = "";
    public string DisplayName = "";
    public string Description = "";
    public string TypeId = "";
    public string TypeName = "";
    public ItemInventoryCategory InventoryCategory;
    public string IconPath = "";
    public string BigIconPath = "";
    public string IconColorPath = "";
    public int MaxStack = 1;
    public int MaxQuality;
    public ItemTemplate? Template;
}
```

The important fields are:

```text
DisplayName
TypeId
TypeName
Template
```

`Template` is:

```text
Inventory.ItemTemplate
```

This is another strong indication that ItemBrowser obtains its information from the game's item-template system.

---

# ItemBrowser SlotView

The ItemBrowser also contains:

```csharp
private sealed class SlotView
{
    public GameObject Go;
    public Image Background;
    public Image Icon;
    public CatalogEntry? Bound;
    public string BoundIconKey = "";
}
```

This may be useful as an example of how an inventory/catalog item icon is represented in Unity UI.

---

# ItemBrowser CategoryTabView

```csharp
private sealed class CategoryTabView
{
    public ItemInventoryCategory Category;
    public Image Background;
}
```

---

# ItemBrowser Tooltip

`ItemBrowser.ItemBrowserView.BuildTooltip()` was inspected.

It creates the tooltip UI:

```csharp
private void BuildTooltip(Transform parent)
{
    ...
    _tooltipTitle = ...
    ...
    _tooltipBody = ...
    ...
}
```

This method does NOT appear to be where the actual item data is determined.

It primarily constructs the Unity UI objects.

At the time of this ItemBrowser inspection, the proposed next investigation
was to find where:

```text
_tooltipTitle.text
_tooltipBody.text
```

are assigned.

Similarly, find where `CatalogEntry` instances are constructed and where:

```text
TypeId
TypeName
DisplayName
Template
```

are populated.

This could reveal exactly where ItemBrowser gets:

```text
Autumn Crop
Item.Crop.PumpkinSeed
```

from.

---
