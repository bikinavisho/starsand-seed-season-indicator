# Starsand Island Seasonal Seed Icon Mod

This document is the entry point for the mod's design and reverse-engineering
notes. Use the focused documents below for the full requirements, game API
reference, UI investigation, and evidence log.

## Project goal

Show a small icon over an inventory seed when its crop is seasonal and can grow
in the current in-game season. Show no icon for year-round crops or when the
crop cannot grow in the current season. Use the game's crop metadata rather
than a hardcoded list of crop names.

## Current findings

- `CropTemplate.SeasonConfigs` is runtime data containing
  `GameTime.ESeason` values and can distinguish year-round, single-season, and
  multi-season crops.
- Extension-key enumeration confirmed all six tested seed records have
  non-null `Extensions` containers with six types, including
  `XSandbox.Farm.FarmSeedItemExt`. The earlier typed `Get` call returned null
  despite the matching extension key being present; do not treat that return
  as evidence that the extension is absent.
- Earlier F7 planting captures showed distinct `CFarmlandUnit.FarmSeed(...)`
  `entityTemplate` arguments and directly resolved `CCrop.Template` values:
  ordinary `Object.Crop.WaterSpinach` resolves to `Crop.WaterSpinach` (Summer),
  while `Object.Crop.WaterSpinach_Eternity` resolves to
  `Crop.WaterSpinach_Eternity` (all four seasons). Both captured `entityId`
  values were the zero Guid; its meaning remains unknown. These are historical
  runtime observations, not the current F7 probe.
- `RequestFarmSeed` was not observed before either controlled Water Spinach
  `FarmSeed` call. That route is not being pursued further for now, and the
  all-zero `entityId` is not being pursued as a seed/crop resolver.
- The latest F7 runtime logs confirmed that `TryCast<FarmSeedItemExt>()`
  succeeds for all six tested seed records. `FarmSeedItemExt.Entity` and
  `Template.Get()` resolve the listed `Object.Crop.*` IDs and `CropTemplate`
  records, respectively. For example, ordinary Water Spinach maps to
  `Object.Crop.WaterSpinach` / `Crop.WaterSpinach` (`[Summer]`), while Eternity
  maps to `Object.Crop.WaterSpinach_Eternity` /
  `Crop.WaterSpinach_Eternity` (all four seasons). The larger catalog scan
  emitted the same pattern for many additional loaded seed items, including
  Aloe Vera, Apple, Banana, Blueberry, Carrot, Cotton, Cucumber, and flower
  variants. Full results are in the [reverse-engineering log](REVERSE_ENGINEERING_LOG.md).
- The earlier reverse-reference scan alone provided candidate matches rather
  than a unique mapping; runtime `FarmSeedItemExt` inspection now directly
  confirms the relationship for the six listed records. In that reverse scan,
  Water Spinach had an ambiguous match and Sunflower had no match.
- The direct seed-extension-to-crop-template mapping is confirmed for the six
  inspected records. `FarmSeed.entityTemplate` strings still did not resolve
  through `KCropTemplateSet.TryGetTemplate` in the two Water Spinach planting
  captures, even though both the seed extension's `Template.Get()` and the
  returned `CCrop.Template` resolved.
- Current F7 has been expanded to scan `KItemTemplateSet.AllTemplates.Values`
  for `FarmSeedItemExt`, log only resolved seed records, and summarize
  extension/cast/template-resolution counts plus four-season crops. It also
  checks matching `DisplayName` values for Blue Sleep Lily and Rampant Pasture
  Grass. Runtime results from this catalog-wide scan are pending; no special
  behavior is implemented for either crop type.
- The seed-to-entity values are confirmed in `FarmSeedItemExt.Entity` for the
  six inspected records. Searches of available decompiled source and installed
  interop metadata have not established the managed caller/data flow that
  carries the selected seed's value to `FarmSeed.entityTemplate`.
  `KEntityTemplateSet` remains a candidate entity-template store, but
  `Object.Crop.*` resolution through that set is not confirmed.

## Focused documents

- [Requirements and proposed architecture](MODDING_REQUIREMENTS.md) — desired
  behavior, constraints, refresh expectations, and design outline.
- [Game data and API reference](GAME_DATA_REFERENCE.md) — relevant game
  classes, member types, season APIs, and interop/API caveats.
- [UI integration notes](UI_INTEGRATION_NOTES.md) — ItemBrowser observations
  and inventory UI investigation.
- [Reverse-engineering log](REVERSE_ENGINEERING_LOG.md) — runtime probe
  results, evidence grades, unresolved questions, and investigation priorities.

Each focused document links back here and to the other documents for
cross-navigation.
