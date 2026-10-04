# Starsand Island Seasonal Seed Display

This is the concise entry point for the mod's requirements, game API facts,
UI integration, and reverse-engineering evidence.

## Project goal

Show a small image indicator on a seed when its crop can grow in the current
season and is not growable in all four seasons. Read the game's crop metadata;
do not maintain a hardcoded crop list.

## Current implementation

- The BepInEx plugin is named **Seed Season Display** and builds as
  `StarsandIsland.SeedSeasonDisplay.dll` from
  `src/SeedSeasonDisplay.csproj`. Its identifier remains
  `com.starsandisland.seedseason-indicator`.
- Seed resolution uses `ItemTemplate.Extensions`, an IL2CPP
  `TryCast<FarmSeedItemExt>()`, `FarmSeedItemExt.Template.Get()`, and
  `CropTemplate.SeasonConfigs : List<GameTime.ESeason>`.
- The current season comes from `KGameTimeUtil.Now.GetSeason()`. All-four-season
  crops, out-of-season crops, and non-seeds have no indicator.
- A reusable `UnityEngine.UI.Image` displays the current season's PNG sprite:
  Spring `spring.png`, Summer `summer.png`, Autumn `fall.png`, Winter
  `winter.png`. The four PNGs are embedded in the assembly and loaded/cached
  as Unity textures and sprites.
- The same indicator code covers player inventory, storage, and seed shop.
  The developer has confirmed that all three work in-game.
- The manager checks the season every frame and refreshes visible inventory and
  shop cells on a season change or every 0.75 seconds. The periodic refresh
  also updates cells that have been rebound to different items.
- There is no F7 diagnostic or debug hotkey in the current plugin source.

## Confirmed data findings

- `FarmSeedItemExt` is present and resolves to a `CropTemplate` across the
  loaded seed catalog: 5,927 item templates were scanned, with 219 seed
  extensions, 219 successful IL2CPP casts, and 219 resolved crop templates.
- Crop seasons use `GameTime.ESeason` values in
  `CropTemplate.SeasonConfigs`. A crop with all four actual seasons is treated
  as year-round for indicator purposes.
- The managed planting-boundary data flow that supplies
  `CFarmlandUnit.FarmSeed(...).entityTemplate` remains unknown and is not
  needed by the display mod.

## Focused documents

- [Requirements and implementation status](MODDING_REQUIREMENTS.md)
- [Game data and API reference](GAME_DATA_REFERENCE.md)
- [UI integration notes](UI_INTEGRATION_NOTES.md)
- [Reverse-engineering log](REVERSE_ENGINEERING_LOG.md)

The reverse-engineering log retains historical probe details. Its old F7
diagnostic descriptions refer to development probes, not code shipped in the
current plugin.
