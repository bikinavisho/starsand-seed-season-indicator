**Project docs:** [Modding context](MODDING_CONTEXT.md) · [Requirements and design](MODDING_REQUIREMENTS.md) · [Game data reference](GAME_DATA_REFERENCE.md) · [UI integration notes](UI_INTEGRATION_NOTES.md) · [Reverse-engineering log](REVERSE_ENGINEERING_LOG.md)

---

# Reverse-Engineering Log

## Historical player-inventory traversal probe

An earlier diagnostic used this API path to scan the main player's bag:

```text
World.KWorldUtil.MainEntity : GameFramework.GameECS.KEcsEntity
    → KEcsEntity.GetComponent<Inventory.CInventory>()
    → CInventory.ItemSet : Inventory.KGridItemSet
    → KGridItemSet.Count and KGridItemSet[int] : Inventory.KItem
    → KItem.Template : Inventory.ItemTemplate
```

The public `KEcsEntity.GetComponent<TComponent>()` method and these inventory
members are present in the decompiled wrappers. ItemBrowser also uses
`KWorldUtil.MainEntity` as the local player entity when giving items. At
runtime, check `KWorldUtil.IsWorldReady` and null-check the entity, component,
item set, item, and template before reading them. The user tested the
diagnostic and confirmed this traversal finds the known inventory items.

That implementation was superseded. The current F7 diagnostic scans the
loaded `KItemTemplateSet` catalog for `FarmSeedItemExt` entries and validates
their entity/crop/season metadata.

An earlier implementation also used F8 for a separate read-only probe over
`KCropTemplateSet.Instance.AllTemplates`. It logged crop-template keys/names
and `SeasonConfigs`, attempted `ReturnSeed.Get()` and each
`GrowthStage.SeedItem.Get()`, compared resolved `ItemTemplate.ID` values with
the five tested seed IDs, and printed resolution and per-seed match totals.
Each crop or reference was handled independently. The probe did not assume
either field was the canonical planting-seed association; its runtime results
are summarized in the crop-reference sections below.

---


---

# Historical crop-reference probe findings (2026-10-03)

This section records results from a prior read-only F7 crop-reference probe.
Those results establish observations from that run, not the behavior of the
current F7 handler. The `FarmSeedItemExt` path has since been tested directly
against the loaded item-template records, as described below.

## What the scan actually proved

The diagnostic reported:

```text
Starting read-only crop-reference scan: 108 crop-template entries; matching
Item.Crop.PotatoSeed, Item.Crop.SweetPotatoSeed, Item.Crop.SunFlowerSeed,
Item.Crop.TomatoSeed, Item.Crop.WaterSpinachSeed.
```

This confirms that:

* The runtime game data includes a crop-template set with at least 108 entry records.
* `CropTemplate.SeasonConfigs` is a real populated runtime field.
* The underlying values are stored as `GameTime.ESeason` entries rather than a custom flag enum.
* The game supports a mix of year-round, seasonal, and multi-season crops.

## Confirmed `SeasonConfigs` distribution

Across the scan, the observed season lists were:

```text
68 templates: [Spring, Summer, Autumn, Winter]
13 templates: [Summer]
9 templates: [Winter]
9 templates: [Autumn]
9 templates: [Spring]
4 templates: [Spring, Summer]
1 template: [Summer, Autumn]
```

This is the first direct runtime evidence that the game uses
`CropTemplate.SeasonConfigs` to represent seed/crop seasonality and that it can
be read without hardcoded crop metadata.

## Confirmed seed-to-crop reference matches from the runtime scan

The probe reported the following matches using the runtime `GrowthStages[3].SeedItem`
association:

```text
MATCH: seed ID='Item.Crop.PotatoSeed'; CropTemplate key/ID='Crop.Potato'; source=GrowthStages[3].SeedItem; SeasonConfigs=[Spring, Summer, Autumn, Winter].
MATCH: seed ID='Item.Crop.SweetPotatoSeed'; CropTemplate key/ID='Crop.SweetPotato'; source=GrowthStages[3].SeedItem; SeasonConfigs=[Spring, Summer].
MATCH: seed ID='Item.Crop.TomatoSeed'; CropTemplate key/ID='Crop.Tomato'; source=GrowthStages[3].SeedItem; SeasonConfigs=[Spring, Summer].
MATCH: seed ID='Item.Crop.WaterSpinachSeed'; CropTemplate key/ID='Crop.WaterSpinach'; source=GrowthStages[3].SeedItem; SeasonConfigs=[Summer].
MATCH: seed ID='Item.Crop.WaterSpinachSeed'; CropTemplate key/ID='Crop.WaterSpinach_Eternity'; source=GrowthStages[3].SeedItem; SeasonConfigs=[Spring, Summer, Autumn, Winter].
```

These are confirmed runtime matches between item references stored in crop
templates and the listed seed IDs. They are not proof that
`GrowthStages[3].SeedItem` is the game's authoritative planting lookup or that
each matched crop is the unique crop planted by that seed. In particular,
`Item.Crop.WaterSpinachSeed` matches both `Crop.WaterSpinach` and
`Crop.WaterSpinach_Eternity`.

The same probe also reported:

```text
GrowthStages[3].SeedItem null: 0
GrowthStages[3].SeedItem resolved: 216
ReturnSeed null: 52
ReturnSeed resolved: 112
```

This confirms two important details:

* `GrowthStages[3].SeedItem` is populated on the observed crop templates.
* `ReturnSeed` is present for some entries, but not all, and therefore is not a
  safe universal replacement for the seed lookup path.

## Historical false negative: typed FarmSeedItemExt lookup

The initial focused test resolved each requested item from the loaded
`KItemTemplateSet` and called
`Extensions.Get(Il2CppType.Of<FarmSeedItemExt>(), create: false)`. That call
returned null for all six records:

```text
Item.Crop.PotatoSeed
Item.Crop.SweetPotatoSeed
Item.Crop.SunFlowerSeed
Item.Crop.TomatoSeed
Item.Crop.WaterSpinachSeed
Item.Crop.WaterSpinachSeed_Eternity
```

The null return was initially interpreted as extension absence. That
interpretation is superseded by the later extension-key enumeration below:
each of these records contains a dictionary entry keyed by
`XSandbox.Farm.FarmSeedItemExt`. The evidence now shows that the generic
`Get(Type, create:false)` retrieval method did not retrieve the entry in this
case; the precise cause is unknown. Do not use that null result as evidence
that the extension is absent.

The runtime scan also reported that:

```text
Crop-reference matches for 'Item.Crop.SunFlowerSeed': 0.
```

The earlier scan's zero crop-reference matches for Sunflower Seeds remain a
separate observation; they do not negate the extension entries found in the
loaded item records.

## Current runtime-evidence conclusion

The current evidence establishes:

1. Read the crop's actual runtime season data from `CropTemplate.SeasonConfigs`.
2. `GrowthStages[3].SeedItem` and `ReturnSeed` can be read as reverse references
   from a crop template to item templates.
3. The six inspected seed records have a `FarmSeedItemExt` entry and resolve
   through `Entity` and `Template.Get()` to the crop metadata documented
   below. The original `DataObj.Get(Type, create:false)` call returned null
   and was a retrieval false negative.
4. The current F7 diagnostic scans the complete loaded `KItemTemplateSet`
   collection for additional records with `FarmSeedItemExt`, including
   special seeds located by display name. It reports compact per-seed results
   and aggregate resolution/all-season counts. Runtime results from this
   broader catalog scan are pending.

The direct resolver has been confirmed for the six tested records, but
consistency across the entire seed catalog remains to be validated. Do not
select a crop from ambiguous reverse-reference matches or treat `ReturnSeed`
as a universal planting-seed link.

## Historical static planting-path investigation (2026-10-03)

The installed BepInEx interop assemblies were loaded and inspected with ILSpy:

* `GameWorld.dll`:
  * `Inventory.KItem.Template` is an `ItemTemplate`.
  * `Inventory.ItemTemplate.ID` is a string.
  * `XSandbox.Farm.FarmSeedItemExt` has `Entity : string` and
    `Template : Ref<KCropTemplateSet, CropTemplate>`.
  * `FarmSeedItemExt.CheckForHybrid(CFarmlandUnit, ItemTemplate)` exists as a
    public static method.
  * `GameWorld.Hunting.KFarmGunMotion` has an `m_FarmSeedItemExt` field and
    `OnSwitchBulletAction(ItemTemplate last, ItemTemplate now)`.
  * `XSandbox.Farm.KFarmUtil.RequestFarmSeed(CFarmlandUnit, string)` and
    `SyncFarmSeed(CFarmlandUnit, string seedTemplate, string cropGuid)` exist.
  * `XSandbox.Farm.CFarmlandUnit.FarmSeed(string entityTemplate, Guid)` returns
    a `CCrop`; `XSandbox.Farm.CCrop.Template` is a `CropTemplate`.
  * `CropTemplate.SeasonConfigs` is `List<GameTime.ESeason>`,
    `GrowthStages` is `List<GrowthStage>`, and `ReturnSeed` is
    `Ref<KItemTemplateSet, ItemTemplate>`.
* `GameFramework.dll`:
  * `BaseTemplateSet<TSelf, TTemplate>` exposes `GetTemplate` and
    `TryGetTemplate`.
  * `KCropTemplateSet` derives from `JsonTemplateSet<KCropTemplateSet,
    CropTemplate>`, which uses the base-template-set/singleton API. The public
    inherited `Instance` property is available.
  * `JsonTemplateSet<TSelf, TTemplate>.Ref.Get()` exists as a public
    parameterless method.

These signatures make the following possible data flow worth investigating:

```text
KItem.Template -> ItemTemplate
KFarmGunMotion.OnSwitchBulletAction(last, now)
    -> m_FarmSeedItemExt
    -> FarmSeedItemExt.Template / Entity
    -> CropTemplate / entity-template string
KFarmUtil request/sync -> CFarmlandUnit.FarmSeed(entityTemplate)
    -> CCrop.Template -> CropTemplate.SeasonConfigs
```

This was a **candidate path, not a confirmed call chain**. The interop methods
`OnSwitchBulletAction`, `CheckForHybrid`, `KFarmUtil.RequestFarmSeed`,
`SyncFarmSeed`, and `CFarmlandUnit.FarmSeed` decompile to IL2CPP
`il2cpp_runtime_invoke` wrappers. The available ILSpy analysis did not reveal a
managed caller for `CheckForHybrid`, and the generated wrappers do not show the
native method bodies. In particular, the `ItemTemplate` arguments to
`OnSwitchBulletAction` and `CheckForHybrid` do not prove how the game uses
them, and the `seedTemplate` / `entityTemplate` parameter names do not prove
whether they identify an item template or crop entity template.

The game installation has `GameAssembly.dll` and
`Starsand Island_Data\il2cpp_data\Metadata\global-metadata.dat`, but no
Cpp2IL or Il2CppDumper command was available during this inspection. The
original native planting logic and actual call sequence therefore remain
unverified.

### Water Spinach / Eternity ambiguity

The runtime findings are:

```text
Crop.WaterSpinach.GrowthStages[3].SeedItem
    -> Item.Crop.WaterSpinachSeed

Crop.WaterSpinach_Eternity.GrowthStages[3].SeedItem
    -> Item.Crop.WaterSpinachSeed

Crop.WaterSpinach_Eternity.ReturnSeed
    -> Item.Crop.WaterSpinachSeed_Eternity
```

Thus `GrowthStages[3].SeedItem` alone cannot distinguish the ordinary and
Eternity crop templates for the ordinary Water Spinach seed. `ReturnSeed`
distinguishes the Eternity seed in this observed case, but its planting role is
unconfirmed and runtime scans found it absent on some crop templates. The
loaded item records for both tested Water Spinach seeds contain no
`FarmSeedItemExt`; the controlled `FarmSeed` observations below show distinct
boundary strings and resulting crop templates, without establishing a general
resolver.

### Historical overlay assessment

`CropTemplate.SeasonConfigs` remains a suitable data source **after** the
correct crop template has been resolved. The present seed-to-crop candidates
are not sufficient for a general overlay: Water Spinach is ambiguous, and the
runtime scan found no `GrowthStages[3].SeedItem` match for
`Item.Crop.SunFlowerSeed`.

This assessment predates the focused loaded-record probe and current F7
catalog scan. The earlier proposed `KFarmGunMotion` observation is
superseded and is not part of the current diagnostic.

---

The currently confirmed reverse-direction metadata is:

```text
CropTemplate
    ├── GrowthStages[*].SeedItem : Ref<KItemTemplateSet, ItemTemplate>
    └── ReturnSeed : Ref<KItemTemplateSet, ItemTemplate>
```

These references and their runtime values are confirmed, but neither is yet
confirmed as the authoritative planting-seed association. Their gameplay
semantics must be verified before relying on them.

Runtime `SeasonConfigs` values have been observed in the loaded crop-template
set (see the distribution above). The corresponding serialized template data
has not been extracted from packed asset bundles. Do not assume that the
observed runtime values establish a serialization convention for missing or
empty season lists.

---


---

# Reverse Engineering Priorities

Investigate in approximately this order:

### Current priority — Validate the full seed catalog

The current F7 diagnostic enumerates `KItemTemplateSet.AllTemplates.Values`,
skips records without a `FarmSeedItemExt` key, converts present values with
`TryCast<FarmSeedItemExt>()`, and logs only successful seed-to-crop
resolutions. It summarizes total templates scanned, extension presence,
successful casts, crop-template successes/failures, invalid template
references, and four-season crops. It separately checks matched display names
for Blue Sleep Lily and Rampant Pasture Grass without hardcoding item IDs.

Runtime results from this catalog-wide scan are pending. In particular, do
not infer yet that the six confirmed records represent the entire seed
catalog, and do not add special rules for hybrid, Rampant, or
Eternity/Immortal crops.

### Priority 2 — Verify real crop season values

Extract or inspect the packed `.ab` assets under the installed game's
`Starsand Island_Data\StreamingAssets\Bundles` directory. Find actual
serialized crop templates and confirm `SeasonConfigs` for single-season,
multi-season, and year-round crops. Do not infer the values from item names or
display labels.

### Priority 3 — Verify season-change notification

Trace the publisher and subscription paths for
`KGameTimeEvent.TimeSystemLoaded` and
`KGameTimeEvent.CrossWideTiemSetted`. Confirm whether either fires on season
rollover. If neither does, check for season changes using
`KGameTimeUtil.Now.GetSeason()` while the inventory UI is active.

The event-argument types and handler names are present in the available
decompiled source, but no publisher/subscriber path or rollover behavior has
been confirmed. Until verified, lightweight polling of the current `ESeason`
while the inventory UI is active is the recommended fallback; refresh only
when the season value changes.

### Priority 4 — ItemBrowser data flow

Trace:

```text
CatalogEntry
```

construction and determine where:

```text
TypeName
TypeId
Template
```

come from.

This may provide a useful example of how to retrieve `ItemTemplate` data.

### Priority 5 — Inventory UI

After the crop-data problem is solved, identify the actual Starsand Island inventory UI class and determine how inventory item slots are rendered.

---


---

# Historical seed-shop F7 inspection

The first live shop test, with the seed shop visibly open, reported:

```text
Live shop scan complete: active UI views=53; visible shop views=0.
No visible UIShopView was found in KUIViewManager.ActiveViews.
```

This establishes that the shop is not represented by a visible `UIShopView` in
the manager's active-view list at the time of that test. Do not assume the shop
view has a shop-related name.

The follow-up test identified the live view:

```text
ActiveViews[52]: type='KUIViewBase', GameObject='UIShopView (Clone)',
activeSelf=True, activeInHierarchy=True, keywordMatches=[Shop]
```

This showed that the active-view wrapper's concrete runtime type did not reveal
the actual view; the underlying active GameObject name did.

An earlier F7 implementation located active `UIShopView (Clone)` GameObjects
through `KUIViewManager.ActiveViews` and inspected components on the root and its child
GameObjects. The first `GetComponents<Component>()` attempt returned wrappers
typed as `UnityEngine.Component`, so component types are now determined from
the IL2CPP native object instead of the wrapper's C# `GetType()` result.

The local referenced assemblies were inspected and confirm these APIs:

```text
GameObject.GetComponents(Il2CppSystem.Type)
GameObject.GetComponentsInChildren(Il2CppSystem.Type, Boolean)
Il2CppObjectBase.Pointer
IL2CPP.il2cpp_object_get_class(IntPtr)
IL2CPP.il2cpp_class_get_type(IntPtr)
Il2CppType.TypeFromPointer(IntPtr, String)
```

That historical diagnostic used the component pointer and IL2CPP class/type functions to
report the runtime `Il2CppSystem.Type`, full name, assembly-qualified name, and
base-type chain. For types with shop/item/controller/data/template/card names,
it reads public instance fields/properties, follows relevant objects, resolves
public parameterless `Get()` on template-reference objects, and enumerates
collections read-only (up to 100 entries per collection). The resulting
component and seed-item-to-template graph from the next in-game run remains
unknown.

## Historical follow-up: selected-seed planting call path (2026-10-03)

The latest F7 attempt was made holding Potato Seeds and standing one action
away from planting. The direct player-component path produced:

```text
[F7] MainEntity CHunter found; CurrWeaponBullet ID=.
[F7] CHunter.HunterWeapon template ID=.
[F7] CHunter.HunterMotion runtime type=<null/unavailable>.
[F7] CHunter.HunterMotionGameObject is null; lookup stops before component search.
```

The output format distinguishes a null item template (`<null>`) from an item
whose `ID` is an empty string. Therefore the observed `CurrWeaponBullet` and
`HunterWeapon.Template` were not reported as null, but neither produced a
usable ID. The active motion object and interface are null in this state.
This runtime result does not confirm a `KFarmGunMotion` instance or expose
`m_FarmSeedItemExt`; it also does not confirm that `CurrWeaponBullet` is the
selected planting seed in this state.

### Static selection-side evidence

`CHunter` exposes:

```text
CurrWeaponBullet : ItemTemplate
SetBullet(ItemTemplate)
GetBulletByIndex(int) : ItemTemplate
GetLastBullet() : ItemTemplate
GetNextBullet() : ItemTemplate
OnBulletSelectChange(int, int)
RequestSetBulletSync(string)
OnSwitchBulletAction : Action<ItemTemplate, ItemTemplate>
```

The native method table and generated wrappers show that `CHunter` has a
current-bullet template property and selection/setter methods. `OnSwitchBulletAction`
is a typed delegate taking `(last, now)` item templates. Separately,
`KFarmGunMotion.OnSwitchBulletAction(ItemTemplate last, ItemTemplate now)` has
the matching signature. This makes a callback registration from the hunter
selection event to the farming motion **plausible**, but it is not proof:
there is no managed call site or delegate subscription in the checked
decompiled source, and ILSpy's `Used By` analysis for the motion method had no
caller nodes. Do not infer that `now` is the selected seed solely from the
matching parameter types.

`KFarmGunMotion`'s method list does **not** contain `OnActionStart`. It does
contain `Init(CHunter)`, `OnSwitchBulletAction(ItemTemplate, ItemTemplate)`,
`DoShooting()`, `Throwing_OnEnter()`, and `Throwing_OnUpdate()`. The
decompiled bodies of these methods are IL2CPP invocation wrappers; ILSpy
decompilation has the same limitation. Its `Init(CHunter)` signature and the
`CHunter` callback property make the hunter/motion association likely at the
design level, but the runtime values above show that the hunter's motion
accessors were not populated in the tested ready-to-plant state.

### Planting-side evidence

`FarmSeedItemExt` exposes `Entity` and a crop-template `Template` reference.
It also defines:

```text
OnActionStart(KEcsEntity target, IInteract usingInteract)
CheckForHybrid(CFarmlandUnit unit, ItemTemplate template) : bool
```

`OnActionStart` receives no `ItemTemplate` parameter in its signature.
`CheckForHybrid` does receive one, making it an explicit seed/item-template
candidate API, but its wrapper does not reveal how the template is interpreted.
ILSpy's `Used By` analysis for `CheckForHybrid` produced no caller nodes, and
no call site appears in the decompiled source.

The farming request/sync surface is:

```text
KFarmUtil.RequestFarmSeed(CFarmlandUnit unit, string seedTemplate)
KFarmUtil.SyncFarmSeed(CFarmlandUnit unit, string seedTemplate, string cropGuid)
CFarmlandUnit.FarmSeed(string entityTemplate, Guid entityId) : CCrop
RpcRequestSeed.SeedItemTemplate : string
RpcSyncFarmSeed.SeedItemTemplate : string
```

This confirms that request/sync messages carry a string named
`SeedItemTemplate` and that the farmland planting method accepts a string
named `entityTemplate` and returns a `CCrop`. Runtime data inspection later
confirmed `FarmSeedItemExt.Entity` for six seed records; for ordinary and
Eternity Water Spinach, those values equal the `entityTemplate` strings
captured at the planting boundary. The managed source still does **not** show
how the selected `ItemTemplate` or extension value is passed into either
request/sync or planting, nor which caller invokes the planting method. The request,
sync, hybrid-check, and farmland methods all decompile to invocation wrappers;
ILSpy caller analysis for `RequestFarmSeed`, `SyncFarmSeed`, and
`CFarmlandUnit.FarmSeed` did not return caller nodes. Source-wide searches found
only their declarations/wrappers (plus the RPC handler declarations), not a
managed end-to-end call chain. The `KFarmRPCRegister` callbacks and
`FarmSeedItemExt.OnActionStart` bodies are also unavailable in the generated
wrappers.

### Historical CHunter/KFarmGunMotion evidence grades

* **CONFIRMED (historical runtime probe):** the ready-to-plant test had a `CHunter` on
  `KWorldUtil.MainEntity`; its bullet ID and weapon-template ID were empty
  strings in the reported output, and `HunterMotion` /
  `HunterMotionGameObject` were null.
* **CONFIRMED (API):** the API has an `ItemTemplate` bullet property and
  bullet-selection/setter methods on `CHunter`; the APIs and signatures alone
  do not prove that the current bullet is the planting seed.
* **LIKELY:** the matching `CHunter.OnSwitchBulletAction` delegate and
  `KFarmGunMotion.OnSwitchBulletAction` signature are intended to connect
  bullet selection to the farm motion. No registration or invocation has been
  statically confirmed.
* **CONFIRMED:** `FarmSeedItemExt` can represent an entity string and a
  `CropTemplate` reference; `CropTemplate.SeasonConfigs` is available once a
  crop template is resolved.
* **UNKNOWN (selection route):** whether `OnSwitchBulletAction` receives the
  actual selected seed as `now`, where/how `m_FarmSeedItemExt` is populated,
  and how the request's `SeedItemTemplate` becomes a crop entity/template.
  These older probes did not establish those callback details; the later
  `FarmSeed` boundary observations independently confirmed different
  arguments/results for ordinary and Eternity Water Spinach.

No seed→crop resolver was implemented as part of this historical call-path
investigation. The current one-shot `FarmSeed` observer is described below.

---

# Completed KItemTemplateSet seed-record probe (2026-10-03)

The completed focused probe inspected these IDs from the loaded
`Inventory.KItemTemplateSet`. It has since been replaced as the F7 handler by
the `CFarmlandUnit.FarmSeed(...)` boundary observer described below.

```text
Item.Crop.PotatoSeed
Item.Crop.SweetPotatoSeed
Item.Crop.SunFlowerSeed
Item.Crop.TomatoSeed
Item.Crop.WaterSpinachSeed
Item.Crop.WaterSpinachSeed_Eternity
```

For each record, that probe reported lookup success, `ItemTemplate.ID`,
`DisplayName`, whether `Extensions` was null, and the result of
`Extensions.Get(Il2CppType.Of<FarmSeedItemExt>(), create: false)`. If present,
it would report `Entity`, the `Template` reference state, the result of
`Template.Get()`, the resolved crop name, and `SeasonConfigs`. Its output marked
observed outcomes `CONFIRMED`, missing extensions `NOT PRESENT`, missing
records/references `UNRESOLVED`, and thrown lookup failures `ERROR`.

The historical lookup followed the decompiled API pattern:
`Singleton<KItemTemplateSet>.Instance` followed by the inherited
`TryGetTemplate(primaryKey, out template)`. No new `ItemTemplate` instances are
constructed. The Water Spinach and Water Spinach Eternity records are reported
separately to test whether their `FarmSeedItemExt` data differs.

**Historical confirmed result:** all six records resolved from the loaded
`KItemTemplateSet`, their `Extensions` containers were non-null, and the
`FarmSeedItemExt` `Get(Type, create:false)` call returned null. Later type-key
enumeration confirmed the entry exists on all six records; the historical
null result was a retrieval false negative, not proof of extension absence.

The source-wide decompiled search found only generated invocation wrappers for
`CFarmlandUnit.FarmSeed`, `KFarmUtil.RequestFarmSeed`, `KFarmUtil.SyncFarmSeed`,
and the `KFarmRPCRegister` handlers. `RpcSyncFarmSeed.SeedItemTemplate` is a
generated protobuf property. The wrappers call `IL2CPP.il2cpp_runtime_invoke`;
they do not reveal the native method bodies or a managed caller passing values
between these boundaries. The wrapper declaration for `FarmSeed` accepts
`string entityTemplate` and `Il2CppSystem.Guid entityId` and returns `CCrop`.
These signatures identify the boundary to observe, but do not establish the
meaning of either value.

## Confirmed planting boundary observations

An earlier F7 version armed a one-shot Harmony postfix on
`CFarmlandUnit.FarmSeed`. The supplied runtime output confirmed that postfix
fired and captured both controlled Water Spinach cases:

| Controlled planting | `entityTemplate` argument | `entityId` argument | Returned `CCrop.Template.Name` | `SeasonConfigs` | `KCropTemplateSet.TryGetTemplate(entityTemplate)` |
|---|---|---|---|---|---|
| Ordinary Water Spinach | `Object.Crop.WaterSpinach` | `00000000-0000-0000-0000-000000000000` | `Crop.WaterSpinach` | `[Summer]` | `resolved=False`, template null |
| Water Spinach Eternity | `Object.Crop.WaterSpinach_Eternity` | `00000000-0000-0000-0000-000000000000` | `Crop.WaterSpinach_Eternity` | `[Spring, Summer, Autumn, Winter]` | `resolved=False`, template null |

Both calls returned non-null `CCrop` objects, and each resulting crop exposed
its `CropTemplate` directly through `CCrop.Template`. In these two
observations, the distinct `entityTemplate` strings accompanied distinct
resulting crop templates even though neither string resolved through
`KCropTemplateSet.TryGetTemplate`. This does not establish that either string
is a `CropTemplate` key or that the relationship generalizes to other seeds.

`KFarmUtil.SyncFarmSeed` declares a separate `string cropGuid` parameter.
Do not equate that value with the `Guid entityId` argument to `FarmSeed`
without runtime or implementation evidence. Both observed `entityId` values
were the all-zero Guid. This establishes only those captured values; it does
not show whether zero is typical or whether the identifier denotes a template
or planted instance.

### RequestFarmSeed observation attempt (stopped)

The temporary F7 diagnostic added a Harmony prefix for
`KFarmUtil.RequestFarmSeed(CFarmlandUnit, string)` and retained the
`CFarmlandUnit.FarmSeed` postfix. In two controlled runs, the prefix never
produced an observation before the FarmSeed postfix. The supplied runtime
result therefore contains no observed `RequestFarmSeed.seedTemplate` value:

| Controlled planting | `RequestFarmSeed.seedTemplate` | `FarmSeed.entityTemplate` | `FarmSeed.entityId` | Returned `CCrop.Template.Name` | `SeasonConfigs` |
|---|---|---|---|---|---|
| Ordinary Water Spinach | **UNRESOLVED:** no request observed before `FarmSeed` | `Object.Crop.WaterSpinach` | `00000000-0000-0000-0000-000000000000` | `Crop.WaterSpinach` | `[Summer]` |
| Water Spinach Eternity | **UNRESOLVED:** no request observed before `FarmSeed` | `Object.Crop.WaterSpinach_Eternity` | `00000000-0000-0000-0000-000000000000` | `Crop.WaterSpinach_Eternity` | `[Spring, Summer, Autumn, Winter]` |

**CONFIRMED:** both FarmSeed calls were observed; each returned a non-null
`CCrop` whose `Template` resolved directly to the listed crop template and
season configuration. The `entityId` was the zero Guid in both runs.

**UNKNOWN:** whether `RequestFarmSeed` was not called for these operations,
whether the Harmony prefix failed to observe it, or whether another pathway
led to `FarmSeed`. The log only demonstrates that no request observation was
captured before either observed FarmSeed call. Do not infer a seed ID from the
controlled item choice. Per the current investigation direction,
`RequestFarmSeed` is not being pursued further, and the all-zero
`cropGuid/entityId` values are not being pursued as a seed/crop resolver.

### Data-model findings: seed item to crop template

The source declarations establish the following schema:

```text
KItem.Template -> ItemTemplate
ItemTemplate.ID : string
ItemTemplate.Extensions : ItemExtensionObj
FarmSeedItemExt : ItemExtension
FarmSeedItemExt.Entity : string
FarmSeedItemExt.Template : Ref<KCropTemplateSet, CropTemplate>
CropTemplate.SeasonConfigs : List<ESeason>
```

* **CONFIRMED:** `FarmSeedItemExt.Template` is a typed crop-template
  reference. If an extension instance exists and its `Template` reference is
  populated, that is a direct path to the `CropTemplate`; no entity-string
  lookup is required for this edge.
* **CONFIRMED:** `ItemTemplate` has an extension container, and
  `FarmSeedItemExt` is serializable. Its MessagePack formatter uses a custom
  formatter for the crop-template `Ref`. Runtime inspection subsequently
  confirmed the extension value and read both fields for the six seed records
  listed below.
* **CONFIRMED for these six loaded records:** their `ItemTemplate.ID` values
  map through `FarmSeedItemExt.Entity` and `FarmSeedItemExt.Template.Get()` to
  the corresponding entity string and resolved crop template shown below.
  This confirms these record values; it does not establish a universal
  convention for other items.
* **CONFIRMED:** `GrowthStage.SeedItem` and `CropTemplate.ReturnSeed` are
  crop-to-item references. The runtime scan found ordinary Water Spinach as
  `GrowthStages[3].SeedItem` on both `Crop.WaterSpinach` and
  `Crop.WaterSpinach_Eternity`; the latter has
  `ReturnSeed = Item.Crop.WaterSpinachSeed_Eternity`. These observations do
  not establish which crop the game plants from either seed.
* **CONFIRMED for the two observed cases:** ordinary and Eternity Water
  Spinach produced different `FarmSeed.entityTemplate` strings and different
  directly resolved `CCrop.Template.Name` values. Neither argument resolved
  through `KCropTemplateSet.TryGetTemplate`.
* **CONFIRMED for the six inspected records:** the loaded seed extension
  provides a direct `ItemTemplate -> FarmSeedItemExt -> CropTemplate` path,
  including Potato, Sweet Potato, Tomato, and Sunflower. No planting captures
  for those four seeds are recorded here, so this does not establish their
  observed `FarmSeed` arguments.
* **UNKNOWN (not being pursued as the resolver):** the meaning of
  `FarmSeed.entityId` and its relationship, if any, to
  `KFarmUtil.SyncFarmSeed`'s string `cropGuid`. Both observed `entityId`
  arguments were zero.

The loaded-record extension check and direct member inspection are complete
for the six tested seed records. Their `FarmSeedItemExt.Entity` values and
resolved `CropTemplate` names are documented below. The two Water Spinach
boundary observations are also complete. Resolving an `Object.Crop.*` string
through `KCropTemplateSet` remains unsuccessful; the extension's separate
`Template` reference resolves directly. The zero `entityId` is not being
pursued as the seed/crop resolver.

## F7 extension type scan and targeted FarmSeedItemExt read (2026-10-03)

The F7 type-enumeration run resolved these six records from
`Singleton<KItemTemplateSet>.Instance` using the set's existing
`TryGetTemplate(id, out template)` API:

```text
Item.Crop.PotatoSeed
Item.Crop.SweetPotatoSeed
Item.Crop.SunFlowerSeed
Item.Crop.TomatoSeed
Item.Crop.WaterSpinachSeed
Item.Crop.WaterSpinachSeed_Eternity
```

**CONFIRMED runtime result:** all six records resolved, each `Extensions`
container was non-null, and each contained exactly these same six extension
types:

```text
Inventory.OriginItemExt
Inventory.UseItemExt
XSandbox.Farm.FarmSeedItemExt
XSandbox.HoldingRepresentItemExt
XSandbox.Talent.TalentItemExt
XSandbox.UsageItemExt
```

The exact type-key enumeration showed `FarmSeedItemExt` is attached to every
tested record. This supersedes the earlier `Get(Type, create:false)` null
result as an absence test.

An initial attempt to cast the retrieved dictionary value with C# `as`
returned null because its managed proxy was exposed as the base
`Inventory.ItemExtension`. After confirming the value's native IL2CPP type was
`XSandbox.Farm.FarmSeedItemExt`, the F7 probe converted it with
`((Il2CppObjectBase)value).TryCast<FarmSeedItemExt>()` and inspected only
`Entity`, `Template`, the resolved crop name, and its seasons. It did not add
a farming hook, plant anything, or recursively inspect the value.

## FarmSeedItemExt dictionary value representation

**CONFIRMED from the user-provided runtime result:** each of the six seed
records has an extension dictionary entry keyed by
`XSandbox.Farm.FarmSeedItemExt`.

**CONFIRMED from the user-provided F7 runtime logs:** all six dictionary
values were non-null; each `TryCast<FarmSeedItemExt>()` succeeded; each
`Template` reference was non-null and valid; and each `Template.Get()` call
resolved. The exact observed values are:

| ItemTemplate.ID | `FarmSeedItemExt.Entity` | `CropTemplate.Name` | `SeasonConfigs` |
|---|---|---|---|
| `Item.Crop.PotatoSeed` | `Object.Crop.Potato` | `Crop.Potato` | `[Spring, Summer, Autumn, Winter]` |
| `Item.Crop.SweetPotatoSeed` | `Object.Crop.SweetPotato` | `Crop.SweetPotato` | `[Spring, Summer]` |
| `Item.Crop.SunFlowerSeed` | `Object.Crop.SunFlower` | `Crop.SunFlower` | `[Summer, Autumn]` |
| `Item.Crop.TomatoSeed` | `Object.Crop.Tomato` | `Crop.Tomato` | `[Spring, Summer]` |
| `Item.Crop.WaterSpinachSeed` | `Object.Crop.WaterSpinach` | `Crop.WaterSpinach` | `[Summer]` |
| `Item.Crop.WaterSpinachSeed_Eternity` | `Object.Crop.WaterSpinach_Eternity` | `Crop.WaterSpinach_Eternity` | `[Spring, Summer, Autumn, Winter]` |

This confirms the direct seed-record to crop-template relationship for these
six records. The ordinary/Eternity Water Spinach `Entity` values also match
the corresponding strings observed earlier at the `FarmSeed` boundary. No
claim is made here about other seed records or a universal mapping rule.

## Catalog scan from the loaded KItemTemplateSet runtime log

The expanded F7 catalog-validation run confirmed the same resolver pattern
across the loaded seed catalog. The runtime output followed the same
read-only flow for each item in `KItemTemplateSet`:

- `ItemTemplate` resolved from the set
- extension dictionary inspected for `XSandbox.Farm.FarmSeedItemExt`
- value retrieved from the dictionary
- `((Il2CppObjectBase)value).TryCast<FarmSeedItemExt>()`
- `FarmSeedItemExt.Entity` read
- `FarmSeedItemExt.Template` validated
- `Template.Get()` resolved
- `CropTemplate.Name` and `CropTemplate.SeasonConfigs` logged

Representative confirmed results from the loaded catalog log are:

- `Item.Crop.PotatoSeed` → `Object.Crop.Potato` → `Crop.Potato` → `[Spring, Summer, Autumn, Winter]`
- `Item.Crop.SweetPotatoSeed` → `Object.Crop.SweetPotato` → `Crop.SweetPotato` → `[Spring, Summer]`
- `Item.Crop.SunFlowerSeed` → `Object.Crop.SunFlower` → `Crop.SunFlower` → `[Summer, Autumn]`
- `Item.Crop.TomatoSeed` → `Object.Crop.Tomato` → `Crop.Tomato` → `[Spring, Summer]`
- `Item.Crop.WaterSpinachSeed` → `Object.Crop.WaterSpinach` → `Crop.WaterSpinach` → `[Summer]`
- `Item.Crop.WaterSpinachSeed_Eternity` → `Object.Crop.WaterSpinach_Eternity` → `Crop.WaterSpinach_Eternity` → `[Spring, Summer, Autumn, Winter]`

The catalog scan also emitted many additional `SEED: ...` lines, including
common and specialty crops such as Aloe Vera, Apple, Banana, Blueberry,
Carrot, Cherry Tomato, Cotton, Cucumber, Moonrise flower variants, and other
seeds. Each such line followed the same confirmed resolver chain and yielded a
resolved `CropTemplate.Name` with a valid `SeasonConfigs` array. The runtime log
therefore supports the conclusion that the game-native `FarmSeedItemExt`
resolver remains stable across the loaded seed catalog and that the key
ordinary/Eternity Water Spinach distinction is a real metadata difference in
`Entity` and `CropTemplate`, not a fallback or UI-only artifact.

## Follow-up: managed evidence for the seed-to-entity mapping

The decompiled-source search covered the farming request/sync path, RPC seed
fields, `FarmSeed` arguments, the exact runtime `Object.Crop.WaterSpinach` and
`Object.Crop.WaterSpinach_Eternity` strings, entity-template APIs, and item-to-entity
references. The exact runtime strings were not found in the available
`reference/decompiled_game/` source. The only farming declarations found for
`KFarmUtil.RequestFarmSeed`, `KFarmUtil.SyncFarmSeed`,
`KFarmRPCRegister.OnRequestFarmSeed`, `KFarmRPCRegister.OnSyncFarmSeed`,
`KFarmRPCRegister.WaitForSyncFarmSeed`, and `CFarmlandUnit.FarmSeed` are
generated IL2CPP invocation wrappers or message/property declarations; they
marshal arguments but do not expose the native conversion implementation.
`RpcRequestSeed.SeedItemTemplate` and `RpcSyncFarmSeed.SeedItemTemplate` are
message properties, but no available source call site connects them to
`CFarmlandUnit.FarmSeed`.

ILSpy analysis of the installed `GameWorld.dll` did not return caller nodes for
`KFarmUtil.RequestFarmSeed`, `KFarmRPCRegister.WaitForSyncFarmSeed`, or
`CFarmlandUnit.FarmSeed`. The source-wide call-site search likewise found
declarations only, not a managed caller chain. This is a limit of the
available managed/decompiled evidence, not proof that the conversion is
implemented only in native code.

The available entity-template API establishes a candidate data system:
`Entity.KEntityTemplateSet` derives from
`JsonTemplateSet<KEntityTemplateSet, EntityTemplate>`, and `EntityTemplate`
has a `Name` field. `KEntityUtil` also declares entity creation methods that
accept a string `templateID`. These declarations do not establish that
`Object.Crop.*` is a `KEntityTemplateSet` key, that these records are present
in that set, or that planting derives the string from a seed ID. The runtime
failure of `KCropTemplateSet.TryGetTemplate("Object.Crop...")` only rules out
that lookup path for the two tested strings; it does not test
`KEntityTemplateSet`.

Evidence grades:

* **CONFIRMED:** the exact observed `Object.Crop.*` strings have no matches in
  the available decompiled source searched for this investigation.
* **CONFIRMED:** available managed source and ILSpy caller analysis do not
  expose a call chain converting a seed identifier into the
  `FarmSeed.entityTemplate` argument.
* **LIKELY:** `KEntityTemplateSet` is the relevant managed entity-template
  system to test for `Object.Crop.*` IDs, based on its declared
  `EntityTemplate` type and the entity creation API's `templateID` parameter.
* **UNKNOWN:** whether those runtime strings resolve in `KEntityTemplateSet`,
  where their records are loaded from, and how a selected seed becomes the
  planting argument. Native/runtime-only conversion remains possible but is
  not established.

The `RequestFarmSeed` Harmony observation never fired in the two controlled
Water Spinach runs. The method itself may or may not have been called; the
runtime observation alone does not decide that. As directed, this boundary
will not be pursued further for now. The all-zero `cropGuid/entityId` values
are not being pursued as a seed/crop resolver. The current focused task is to
read the confirmed `FarmSeedItemExt` entries on the six loaded seed templates.
