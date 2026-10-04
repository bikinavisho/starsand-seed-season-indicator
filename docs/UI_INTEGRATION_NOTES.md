**Project docs:** [Modding context](MODDING_CONTEXT.md) · [Requirements and design](MODDING_REQUIREMENTS.md) · [Game data reference](GAME_DATA_REFERENCE.md) · [UI integration notes](UI_INTEGRATION_NOTES.md) · [Reverse-engineering log](REVERSE_ENGINEERING_LOG.md)

---

# UI Integration Notes

### ItemBrowser investigation

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

The next useful investigation is to find where:

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

This can reveal exactly where ItemBrowser gets:

```text
Autumn Crop
Item.Crop.PumpkinSeed
```

from.

---
