**Project docs:** [Modding context](MODDING_CONTEXT.md) · [Requirements and design](MODDING_REQUIREMENTS.md) · [Game data reference](GAME_DATA_REFERENCE.md) · [UI integration notes](UI_INTEGRATION_NOTES.md) · [Reverse-engineering log](REVERSE_ENGINEERING_LOG.md)

---

# Game Data and API Reference

## Reverse-engineering findings and current targets

### Current reverse-engineering targets

The following are known relevant classes.

#### Inventory

```text
Inventory.ItemTemplate
Inventory.ItemType
Inventory.ItemExtension
Inventory.ItemExtensionAttribute
Inventory.ItemExtensionObj
Inventory.KItemTypeSet
```

`ItemTemplate` has been observed to contain:

```text
Description : string
DisplayName : string
Type : ItemType
Extensions : ItemExtensionObj
Prefab : string
Tags : HashSet<string>
UIBigIcon : string
UIIcon : string
UIIconColor : string
```

`ItemType` has been observed to contain:

```text
DisplayName : string
ID : string
ParentType : Ref<KItemTypeSet, ItemType>
TabType : EItemTypeTabType
Tags : List<string>
```

#### Farming

A particularly important discovery is:

```text
XSandbox.Farm.FarmSeedItemExt : ItemExtension
```

with:

```text
Entity : string
Template : Ref<KCropTemplateSet, CropTemplate>
```

This extension type can represent an entity string and a crop-template
reference. Runtime enumeration of the loaded `Extensions` dictionary
confirmed a `FarmSeedItemExt` entry on all six tested seed IDs. The earlier
`Extensions.Get(Il2CppType.Of<FarmSeedItemExt>(), create:false)` call returned
null despite those entries being present; treat that as a retrieval-path
failure, not an absence result. The later runtime probe confirmed the value is
non-null and that
`((Il2CppObjectBase)value).TryCast<FarmSeedItemExt>()` succeeds for all six
records. It read `Entity` and resolved `Template.Get()` for each record; see
the [runtime results](REVERSE_ENGINEERING_LOG.md#farmseeditemext-dictionary-value-representation).

`Inventory.ItemExtensionObj` derives from
`GameFramework.DataObj<ItemExtension>`, which derives from
`Dictionary<Il2CppSystem.Type, ItemExtension>`. The dictionary keys identify
the extension types attached to a record, and `Count` reports the number of
entries. Runtime enumeration confirmed that each of the six tested seed
records contains exactly these six extension types:

```text
Inventory.OriginItemExt
Inventory.UseItemExt
XSandbox.Farm.FarmSeedItemExt
XSandbox.HoldingRepresentItemExt
XSandbox.Talent.TalentItemExt
XSandbox.UsageItemExt
```

The historical F7 catalog diagnostic was removed from the release plugin
after its investigation was completed. Its reported scan covered 5,927
`KItemTemplateSet.AllTemplates.Values` records and found 219
`FarmSeedItemExt` entries; all 219 values passed
`((Il2CppObjectBase)value).TryCast<FarmSeedItemExt>()`, and all 219 crop
templates resolved. These counts were provided in the runtime investigation
results; this document does not claim a fresh scan.

#### CropTemplate

`FarmSeedItemExt.Template` references:

```text
CropTemplate
```

through:

```text
Ref<KCropTemplateSet, CropTemplate>
```

The reference type is documented here as API structure. Runtime enumeration
confirmed a `FarmSeedItemExt` entry in each of the six loaded seed records.
Runtime inspection confirmed the dictionary value can be converted to
`FarmSeedItemExt` with `TryCast`, and that `Template.Get()` resolves for all six
tested records. The resolved crop names and observed seasons are listed in the
[reverse-engineering log](REVERSE_ENGINEERING_LOG.md#farmseeditemext-dictionary-value-representation).

Relevant `CropTemplate` fields and reference APIs are documented below. The
extension-key enumeration confirmed `FarmSeedItemExt` is present on all six
tested seed records. Runtime inspection confirmed each
`FarmSeedItemExt.Entity` value and resolved each `.Template` reference. The
observed relationships are documented in the
[reverse-engineering log](REVERSE_ENGINEERING_LOG.md#farmseeditemext-dictionary-value-representation).

Do not assume the answer before inspecting the actual definition.

### Season systems

The game contains several season-related types.

Known examples include:

```text
GameTime.ESeason
GameTime.ESeasonFlag
Gameworld.ESeasonType
```

Known `ESeason` values:

```text
Spring
Summer
Autumn
Winter
Total
```

Known `ESeasonFlag` values:

```text
Spring = 1
Summer = 2
Autumn = 4
Winter = 8
All = Spring | Summer | Autumn | Winter
```

There is also:

```text
XSandbox.Farm.CCrop.IsGrowableSeason()
```

which takes no arguments.

The seed-season display uses `GameTime.ESeason` for both
`KGameTimeUtil.Now.GetSeason()` and `CropTemplate.SeasonConfigs`. It does not
use `ESeasonFlag`, `Gameworld.ESeasonType`, or `CCrop.IsGrowableSeason()` for
the indicator. The all-season decision is made by checking whether
`SeasonConfigs` contains all four actual `ESeason` values.


---

### FarmSeedItemExt and runtime lookup result

During reverse engineering of the game assemblies, the following class was discovered:

```csharp
// XSandbox.Farm.FarmSeedItemExt

public class FarmSeedItemExt : ItemExtension
{
    // Fields:
    Entity : string
    Template : Ref<KCropTemplateSet, CropTemplate>
}
```

`FarmSeedItemExt` is a candidate association from a seed item to a crop
template. The loaded `KItemTemplateSet` records for Potato, Sweet Potato,
Sunflower, Tomato, Water Spinach, and Water Spinach Eternity seeds were
confirmed by extension-key enumeration to contain this extension. The earlier
typed `Get` call returned null, but that result was contradicted by the
dictionary entries.

The important relationship is:

```text
Seed Item
    ↓
ItemTemplate
    ↓
Extensions
    ↓
FarmSeedItemExt
    ↓
Template
    ↓
CropTemplate
```

`CCrop` represents a planted crop instance, while `FarmSeedItemExt` is an
`ItemExtension` that contains an entity string and crop-template reference.
Runtime inspection confirmed both are populated/resolved on the six tested
seed records; see the
[runtime results](REVERSE_ENGINEERING_LOG.md#farmseeditemext-dictionary-value-representation).

### CropTemplate details

`FarmSeedItemExt.Template` is:

```csharp
Ref<KCropTemplateSet, CropTemplate>
```

This suggests that the game maintains a dedicated `CropTemplate` definition for crops.

The decompiled `GameWorld.dll` definition confirms:

```csharp
public List<GameTime.ESeason> SeasonConfigs { get; set; }
```

The property belongs to `XSandbox.Farm.CropTemplate`, and its fully qualified interop type is `Il2CppSystem.Collections.Generic.List<GameTime.ESeason>`. The crop template also exposes `Name`, `IconPath`, `IsTree`, growth-stage data, and other crop configuration.


---

## Decompiled Item Spawner as a Reference

`reference/decompiled_mod_item_spawner` is relevant because it is a working example of a BepInEx mod interacting with Starsand Island's actual item, inventory, template, and UI systems.

The decompiled Item Spawner mod is a valuable reference for this project because it is a working example of a BepInEx mod interacting with Starsand Island's actual item, inventory, template, and UI systems.

It can help us understand **how a functioning mod uses the game's APIs in practice**, especially when the game's own interop assemblies expose low-level IL2CPP wrappers without making the intended usage obvious.

When investigating an unfamiliar API, check the decompiled Item Spawner code for examples of:

- Finding and accessing the player's inventory.
- Working with `KItem`, `ItemTemplate`, and item IDs.
- Resolving game template references.
- Creating or manipulating item data.
- Interacting with Starsand Island UI systems.
- Using BepInEx/IL2CPP interop types correctly.
- Handling game objects and components.
- Calling existing game APIs rather than recreating their behavior.

The Item Spawner should **not** be treated as authoritative documentation of the game's internals. Its code represents one mod author's approach and may contain workarounds, assumptions, or APIs that are unnecessary for our particular task.

Instead, use it as a **practical example and source of proven patterns**:

> "How did another working mod accomplish something similar with the same game APIs?"

If the Item Spawner demonstrates a useful API or data relationship, verify it against the installed game assemblies and, when appropriate, runtime behavior before incorporating it into this project.

The goal is to reuse **established game-facing patterns**, not to copy the Item Spawner's implementation wholesale.

---


---

# FarmSeedItemExt and Runtime Lookup Result

During reverse engineering of the game assemblies, the following class was discovered:

```csharp
// XSandbox.Farm.FarmSeedItemExt

public class FarmSeedItemExt : ItemExtension
{
    // Fields:
    Entity : string
    Template : Ref<KCropTemplateSet, CropTemplate>
}
```

`FarmSeedItemExt` is a confirmed association from the tested seed items to
their entity strings and crop templates. The reported catalog-wide runtime
scan found 219 entries, all successfully cast and resolved, rather than only
the original six focused examples.

The important relationship is:

```text
Seed Item
    ↓
ItemTemplate
    ↓
Extensions
    ↓
FarmSeedItemExt
    ↓
Template
    ↓
CropTemplate
```

`CCrop` represents a planted crop instance, while `FarmSeedItemExt` is an
`ItemExtension` that contains an entity string and crop-template reference.
Runtime inspection confirmed both fields are populated/resolved on the six
tested seed records; see the
[runtime results](REVERSE_ENGINEERING_LOG.md#farmseeditemext-dictionary-value-representation).

---

# CropTemplate

`FarmSeedItemExt.Template` is:

```csharp
Ref<KCropTemplateSet, CropTemplate>
```

This suggests that the game maintains a dedicated `CropTemplate` definition for crops.

The decompiled `GameWorld.dll` definition confirms:

```csharp
public List<GameTime.ESeason> SeasonConfigs { get; set; }
```

The property belongs to `XSandbox.Farm.CropTemplate`, and its fully qualified
interop type is
`Il2CppSystem.Collections.Generic.List<GameTime.ESeason>`. The crop template
also exposes `Name`, `IconPath`, `IsTree`, growth-stage data, and other crop
configuration. Runtime results include crops with all four actual seasons;
the mod treats the presence of Spring, Summer, Autumn, and Winter in this
list as year-round for indicator purposes.

`XSandbox.Farm.FarmSeedItemExt.Template` (also in `GameWorld.dll`) is confirmed
to have type
`GameFramework.JsonTemplateSet<XSandbox.Farm.KCropTemplateSet, XSandbox.Farm.CropTemplate>.Ref`.
The installed `BepInEx\interop\GameFramework.dll` confirms that
`GameFramework.JsonTemplateSet<TSelf, TTemplate>.Ref` exposes the public
instance member `TTemplate Get()` with no parameters. The expected resolution
expression is:

```csharp
CropTemplate cropTemplate = farmSeedItemExt.Template.Get();
```

The method signature is confirmed from assembly metadata, but no concrete
game-code call site was found, and the interop metadata does not establish the
method body's lookup/loading behavior. The `GameWorld.dll` wrapper confirms the
property type but does not provide its original IL2CPP implementation.

The initial runtime diagnostic tried the existing
`template.Extensions?.Get(Il2CppType.Of<FarmSeedItemExt>(), create: false)`
lookup on known inventory seeds and returned null for each. A later runtime
enumeration of the extension dictionary keys confirmed
`XSandbox.Farm.FarmSeedItemExt` is present on all six records, including
`Item.Crop.WaterSpinachSeed_Eternity`:

```text
Item.Crop.PotatoSeed
Item.Crop.SweetPotatoSeed
Item.Crop.SunFlowerSeed
Item.Crop.TomatoSeed
Item.Crop.WaterSpinachSeed
Item.Crop.WaterSpinachSeed_Eternity
```

`Item.Fish_Arowana` was also found during the earlier inventory scan and is
not a crop seed. The later enumeration corrects the interpretation of the
initial null results: the extension key and value exist, and each value can
be converted with `TryCast<FarmSeedItemExt>()`. The removed F7 probe read the
targeted `Entity` and `Template` data; resolved crop names and seasons are
recorded in the
[reverse-engineering log](REVERSE_ENGINEERING_LOG.md#farmseeditemext-dictionary-value-representation).

The API signatures are present in the interop wrappers:

```text
GameFramework.DataObj<TData>.Get<T>(bool create = true) : T
GameFramework.DataObj<TData>.Get(Il2CppSystem.Type type, bool create = true) : TData
```

`ItemExtensionObj` derives from `DataObj<ItemExtension>`, and the second
overload returns the base `ItemExtension` type. The existing `Get(Type, bool)`
pattern works for other item extensions in ItemBrowser, but the null runtime
result means it does not establish that seed metadata is stored there.

## Historical farming-tool F7 probe

An earlier F7 implementation investigated the farming-tool path. Its first
attempt while holding Potato Seeds, ready to plant, found:

```text
[F7] Farming tool lookup complete: active UIFarmGunView count=0.
[Warning] No active UIFarmGunView was found in KUIViewManager.ActiveViews.
```

This confirms that `KUIViewManager.ActiveViews -> UIFarmGunView` is not
available in that gameplay state. It does not establish that the underlying
farm motion or UI object does not exist.

The decompiled game exposes a more promising direct player-component path:

```text
KWorldUtil.MainEntity
    -> GetComponent<CHunter>()
        -> CurrWeaponBullet : ItemTemplate
        -> HunterMotion : IHuntingMotion
            -> KFarmGunMotion (if this is the runtime implementation)
        -> HunterMotionGameObject : GameObject
            -> GetComponent<KFarmGunMotion>() (typed component lookup)
                -> m_FarmSeedItemExt
```

`CHunter` derives from `KGameComponent<CHunter>`, which derives from
`KEcsComponent`; `KEcsEntity.GetComponent<T>()` is the normal typed ECS
component API. The Item Spawner reference uses `KWorldUtil.MainEntity` as its
player entity for item operations. `CHunter.CurrWeaponBullet` is an existing
game-side `ItemTemplate` accessor and is the strongest candidate for the
currently selected farm seed/bullet. `CHunter.HunterMotion` and
`HunterMotionGameObject` are typed accessors on the same component.
`KFarmGunMotion` is a Unity `MonoBehaviour`, has a public `Init(CHunter)`
entry point, and exposes `m_FarmSeedItemExt`. These signatures make the
component/interface chain a justified runtime test, not yet a confirmed live
instance chain.

That historical F7 implementation obtained `CHunter` from
`KWorldUtil.MainEntity`, reported
`CurrWeaponBullet.ID` and `HunterWeapon.Template.ID`, checked the runtime
implementation of `HunterMotion` for `KFarmGunMotion`, and then checked
`HunterMotionGameObject.GetComponent<KFarmGunMotion>()` as a typed fallback.
If found, it reported `m_FarmSeedItemExt`, `Entity`, `Template.Get()`,
`CropTemplate.Name`, and `SeasonConfigs`. It does not search arbitrary player
fields or use reflection for this path.

The later direct `CHunter` attempt is recorded in the reverse-engineering log.
This approach was not adopted in the released plugin and should not be
resumed for the current planting-identifier investigation.

`KFarmGunMotion.Init(CHunter)` and the `CHunter.HunterMotion` /
`HunterMotionGameObject` fields support the ownership hypothesis. However,
the decompiled wrappers expose no managed creator/assignment site for
`KFarmGunMotion` or `m_FarmSeedItemExt`. The ILSpy analysis of `Init` did not
reveal its native implementation/call chain. Do not claim that `Init` creates
the motion or populates the extension based only on its signature.

The ILSpy method entries for `KFarmGunMotion.OnSwitchBulletAction`,
`DoShooting`, `Throwing_OnEnter`, and `Throwing_OnUpdate` decompile to
IL2CPP invocation wrappers; their native method bodies and actual field
accesses are not present in the wrappers. `KFarmGunMotion`'s exposed method
table contains no `OnActionStart` member. `FarmSeedItemExt.OnActionStart`
does exist, but its wrapper also does not show native implementation logic.
The generated `KFarmGunMotion` wrapper has the `m_FarmSeedItemExt` field
declaration/accessor and native field binding only; no managed assignment or
population site appears in the checked decompiled sources. Consequently, the
available static evidence does not establish which native method populates the
extension or how switching seeds updates it.

No loose crop-template/config data was found in the workspace or the
installed game's `Starsand Island_Data\StreamingAssets\Config` directory.
The installed `StreamingAssets\Bundles` contains thousands of packed `.ab`
assets, so the actual serialized crop data may be in those bundles; it has not
been extracted or verified. The scanned `BepInEx\plugins` directory did not
contain crop-template data.

## Crop-to-Item References

The reverse direction is represented in the crop metadata:

```text
CropTemplate.GrowthStages : List<GrowthStage>
GrowthStage.SeedItem : Ref<KItemTemplateSet, ItemTemplate>
CropTemplate.ReturnSeed : Ref<KItemTemplateSet, ItemTemplate>
```

These public members are declared in `GameWorld.dll` under
`XSandbox.Farm.CropTemplate` and `XSandbox.Farm.GrowthStage`. `SeedItem` is
declared alongside `SeedDropCount` and `DropSeedBaseProbability`;
`ReturnSeed` is declared alongside `ReturnCount`. The member types are
confirmed, but their gameplay semantics are not established by the generated
interop wrappers. In particular, neither field is yet confirmed as the
canonical planting-seed-to-crop mapping.

Other confirmed metadata/API facts:

* `Inventory.KItemTemplateSet` derives from
  `JsonTemplateSet<KItemTemplateSet, ItemTemplate>`. The completed focused
  seed-record probe used `Singleton<KItemTemplateSet>.Instance` and
  `TryGetTemplate(primaryKey, out template)` to inspect existing records.
* `XSandbox.Farm.KCropTemplateSet` derives from
  `JsonTemplateSet<KCropTemplateSet, CropTemplate>`.
* `GameFramework.BaseTemplateSet<TSelf, TTemplate>` exposes
  `AllTemplates`, `GetTemplate`, and `TryGetTemplate`; it derives from
  `Singleton<TSelf>`, whose `Instance` property is public.
* `CFarmlandUnit.FarmSeed(string entityTemplate, Il2CppSystem.Guid entityId)`
  returns a `CCrop`. Its available decompiled method is an IL2CPP invocation
  wrapper, not the native implementation.
* In two runtime observations, `FarmSeed` returned a non-null `CCrop` whose
  `Template` property resolved directly to the matching `CropTemplate`.
  `entityTemplate` was `Object.Crop.WaterSpinach` and
  `Object.Crop.WaterSpinach_Eternity`, respectively; both values failed
  `KCropTemplateSet.TryGetTemplate`. The resulting template names were
  `Crop.WaterSpinach` and `Crop.WaterSpinach_Eternity`.
* Both observed `FarmSeed` `entityId` arguments were
  `00000000-0000-0000-0000-000000000000`. Two observations do not establish
  the meaning of this argument or whether zero is typical.
* In two follow-up F7 runs (ordinary and Eternity Water Spinach), no
  `KFarmUtil.RequestFarmSeed` observation was captured before the corresponding
  `FarmSeed` call. The `FarmSeed` arguments and returned crop templates
  matched the earlier observations; the request's `seedTemplate` remains
  **UNRESOLVED** for both runs. This does not establish whether the request
  method was not called or the Harmony observation failed. This route is not
  being pursued further at this stage.
* The all-zero `FarmSeed` `entityId` values from the two Water Spinach captures
  are not being pursued as the seed/crop resolver.
* `KFarmUtil.SyncFarmSeed(CFarmlandUnit unit, string seedTemplate,
  string cropGuid)` declares a string `cropGuid` parameter. This is distinct
  in type and method signature from `FarmSeed`'s `Guid entityId`; their
  relationship is not established by the wrappers.
* `XSandbox.Farm.FarmSeedItemExt.CheckForHybrid(CFarmlandUnit, ItemTemplate)`
  is a real public static method signature. Its IL2CPP wrapper does not show
  how it uses the item template.
* `XSandbox.Farm.KFarmUtil.RequestFarmSeed(CFarmlandUnit, string)` and
  `SyncFarmSeed(CFarmlandUnit, string, string)` are real method signatures.
  Their parameter names/signatures do not prove a crop-template association.

No concrete decompiled call site was found that starts from an inventory seed
`ItemTemplate` and obtains its `CropTemplate`. Runtime inspection did confirm
that the six tested seed records contain a `FarmSeedItemExt` value whose
`Entity` and `Template.Get()` provide the observed entity ID and crop
template. What remains unestablished is the managed caller/data flow that
passes those values into planting. The related entity-template API
includes `Entity.KEntityTemplateSet :
JsonTemplateSet<KEntityTemplateSet, EntityTemplate>`, `EntityTemplate.Name`,
and `KEntityUtil` entity-creation methods accepting a string `templateID`.
These declarations make `KEntityTemplateSet` a candidate system for the
observed `Object.Crop.*` strings, but do not establish that those strings are
keys in that set or that farming maps seed IDs through it. The strings were
not found in the available `reference/decompiled_game/` source, and no
managed caller chain from a seed ID to `CFarmlandUnit.FarmSeed` was found.
The F7 `RequestFarmSeed` boundary attempt captured no request before either
controlled Water Spinach `FarmSeed` call, and that route is not being pursued
further for now. A later temporary F7 diagnostic scanned the loaded seed
ItemTemplates for `FarmSeedItemExt`, used `TryCast<FarmSeedItemExt>()`, and
resolved their crop-template references; that diagnostic was removed from the
release plugin. See the
[reverse-engineering log](REVERSE_ENGINEERING_LOG.md).

---

# Important Discovery: CCrop

The game also contains:

```text
XSandbox.Farm.CCrop
```

with:

```csharp
public bool IsGrowableSeason()
```

This method takes **no arguments**.

The decompiled IL2CPP wrapper looks approximately like:

```csharp
public unsafe bool IsGrowableSeason()
{
    IL2CPP.Il2CppObjectBaseToPtrNotNull(
        (Il2CppObjectBase)(object)this);

    IntPtr* ptr = null;

    Unsafe.SkipInit(out IntPtr intPtr2);

    IntPtr intPtr =
        IL2CPP.il2cpp_runtime_invoke(
            *NativeMethodInfoPtr_IsGrowableSeason_Public_Boolean_0*,
            IL2CPP.Il2CppObjectBaseToPtrNotNull(
                (Il2CppObjectBase)(object)this),
            (void**)ptr,
            ref intPtr2);

    Il2CppException.RaiseExceptionIfNecessary(intPtr2);

    return *(bool*)IL2CPP.il2cpp_object_unbox(intPtr);
}
```

This is an IL2CPP interop wrapper rather than the original source implementation.

The method is interesting because its name strongly suggests it checks whether the crop can currently grow in the current season.

However, this method is not used for inventory seed season decisions. The
indicator reads the crop definition from `FarmSeedItemExt.Template.Get()` and
compares its `SeasonConfigs` directly with the current `ESeason`.

---

# ItemTemplate

The game contains:

```text
Inventory.ItemTemplate
```

Relevant fields/properties discovered:

```text
Description : string
DisplayName : string
Type : ItemType
Extensions : ItemExtensionObj
Prefab : string
Tags : HashSet<string>
UIBigIcon : string
UIIcon : string
UIIconColor : string
```

The `Extensions` property is particularly important.

---

# ItemExtension

The game contains:

```text
Inventory.ItemExtension
```

and:

```text
Inventory.ItemExtensionAttribute
```

`ItemExtensionObj` is a real game class in:

```text
GameWorld.dll
```

It is not merely an ItemBrowser-specific class.

`ItemExtensionObj` is associated with:

```text
ItemExtension
```

and uses an extension registration mechanism.

The existence of:

```text
FarmSeedItemExt : ItemExtension
```

confirms that a seed-related item extension type exists, but does not establish
that it is attached to the inventory `ItemTemplate` for the known seed items.

Investigate other `ItemExtension` derived classes when useful.

The available ItemBrowser mod confirms this retrieval pattern for existing
extensions:

```text
template.Extensions?.Get(Il2CppType.Of<T>(), create: false)
```

`ItemTemplate.Extensions` has type `ItemExtensionObj`, which derives from
`DataObj<ItemExtension>`. `GameFramework.DataObj<TData>.Get(Il2CppSystem.Type,
bool)` returns `TData`; ItemBrowser uses this API with `ImportantItemExt` and
`LivestockItemExt`. This confirms the lookup API pattern. The original `Get(Type, create:false)` probe returned null for all six
records, but later extension-key enumeration confirmed the
`FarmSeedItemExt` entry is present in each. See the runtime findings in the
reverse-engineering log.


---

# ItemType

The game contains:

```text
Inventory.ItemType
```

with relevant properties:

```text
DisplayName : string
ID : string
ParentType : Ref<KItemTypeSet, ItemType>
TabType : EItemTypeTabType
Tags : List<string>
```

`ItemType` has:

```text
Base Type:
Object
```

and currently appears to have no derived types.

`ParentType` references:

```text
KItemTypeSet
```

---

# KItemTypeSet

The game contains:

```text
Inventory.KItemTypeSet
```

which derives from:

```text
JsonTemplateSet<KItemTypeSet, ItemType>
JsonAssetTemplateSet
Base TemplateSet
Singleton
Object
```

This may be the game's item-type database/template collection.

Investigate it if needed to determine how seed item types are defined.

---

# Item Type Hypothesis

The ItemBrowser mod displays Pumpkin Seeds with information resembling:

```text
Pumpkin Seeds
Autumn Crop
Item.Crop.PumpkinSeed
```

The current hypothesis is that these may correspond approximately to:

```text
ItemTemplate.DisplayName
        ↓
Pumpkin Seeds

ItemTemplate.Type.DisplayName
        ↓
Autumn Crop

ItemTemplate.Type.ID
        ↓
Item.Crop.PumpkinSeed
```

This is only a hypothesis.

**Verify this by tracing the actual ItemBrowser code.**

Do not assume that `"Autumn Crop"` is generated directly from the crop's actual growing-season metadata.

It could instead be an item type/category.

---


---

# IL2CPP / Decompiled Code Guidance

Starsand Island uses Unity IL2CPP.

Many decompiled classes contain code like:

```csharp
IL2CPP.il2cpp_runtime_invoke(...)
```

and fields like:

```csharp
NativeFieldInfoPtr_...
NativeMethodInfoPtr_...
```

These are generally generated interop wrappers and should NOT be mistaken for the game's original source code.

When reverse engineering:

1. Identify the class and member.
2. Determine its actual type and relationships.
3. Search references/usages.
4. Follow the data flow.
5. Look for meaningful fields/properties/methods.
6. Avoid treating IL2CPP wrapper boilerplate as the implementation logic.

When possible, inspect callers and usages rather than relying solely on the decompiled wrapper.

---

# Current Working Theory

The desired data flow may ultimately look something like:

```text
Inventory seed item
        │
        ▼
ItemTemplate
        │
        ├── DisplayName
        │
        ├── Type
        │
        └── Extensions
                │
                ▼
          FarmSeedItemExt
           ┌────┴────┐
           ▼         ▼
        Entity    Template.Get()
           │         │
           ▼         ▼
      entity ID  CropTemplate
                     │
                     ▼
            SeasonConfigs
                │
                ▼
          Current GameTime.ESeason
                │
                ├── Spring
                ├── Summer
                ├── Autumn
                ├── Winter
```

The crop-side `SeasonConfigs` property and `Ref.Get()` accessor are confirmed.
The F7 runtime run resolved `FarmSeedItemExt.Entity` and `Template.Get()` for
all six tested seed records. In the two observed Water Spinach plantings, the
`FarmSeed` argument `entityTemplate` did not resolve through
`KCropTemplateSet`, but the returned `CCrop.Template` resolved directly. Do
not represent crop growth seasons as `ESeasonFlag` unless a separate confirmed
crop API requires that conversion.

The currently confirmed reverse-direction metadata is:

---


---

# Current Known Relevant Classes

```text
GameTime.ESeason
GameTime.ESeasonFlag
GameTime.KGameTimeUtil
GameTime.KGameTimeEvent
Gameworld.ESeasonType

Inventory.ItemTemplate
Inventory.ItemType
Inventory.ItemExtension
Inventory.ItemExtensionAttribute
Inventory.ItemExtensionObj
Inventory.KItemTypeSet

XSandbox.Farm.FarmSeedItemExt
XSandbox.Farm.CCrop
CropTemplate
KCropTemplateSet

ItemBrowser.ItemBrowserView
ItemBrowser.CatalogEntry
```

---

# Current Known Season Enums

```csharp
ESeason
{
    Spring,
    Summer,
    Autumn,
    Winter,
    Total
}
```

```csharp
ESeasonFlag
{
    Spring = 1,
    Summer = 2,
    Autumn = 4,
    Winter = 8,
    All = Spring | Summer | Autumn | Winter
}
```

```csharp
ESeasonType
{
    Spring,
    Summer,
    Autumn,
    Winter
}
```

`CropTemplate.SeasonConfigs` uses `GameTime.ESeason`. The current in-game
season is available from `KGameTimeUtil.Now.GetSeason()`. `ESeasonFlag` and
`Gameworld.ESeasonType` exist but are not the crop template's season-list type.

---
