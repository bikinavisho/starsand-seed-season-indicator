**Project docs:** [Modding context](MODDING_CONTEXT.md) · [Requirements and design](MODDING_REQUIREMENTS.md) · [Game data reference](GAME_DATA_REFERENCE.md) · [UI integration notes](UI_INTEGRATION_NOTES.md) · [Reverse-engineering log](REVERSE_ENGINEERING_LOG.md)

---

# Modding Requirements and Design

## Project Overview

This project is a small BepInEx mod for **Starsand Island**.

The goal is to add a small visual indicator to seed items in the player's inventory, storage, and seed shop showing the **current season**, but only when the crop is seasonal and can grow in that season.

The mod should rely on the game's own crop/item data rather than maintaining a manually hardcoded list of crops.

## Implementation status (2026-10-04)

The player inventory, storage, and seed-shop indicators are implemented and
have been confirmed working in-game by the developer. The current
implementation uses a separate `UnityEngine.UI.Image` overlay, not text or
emoji. It loads four PNG assets embedded in `StarsandIsland.SeedSeasonDisplay.dll`:

| Current season | Embedded asset |
| -------------- | -------------- |
| Spring         | `spring.png` |
| Summer         | `summer.png` |
| Autumn         | `fall.png` |
| Winter         | `winter.png` |

The overlay is reused on each cell. The manager compares the current
`GameTime.ESeason` with the previously observed season, and scans visible
inventory cells and shop cards when the season changes or every 0.75 seconds.
This periodic refresh also handles recycled/rebound cells. The four decoded
sprites are cached. The source assets remain under `src/Assets`; they are
compiled into the DLL as embedded resources.

The latest documented build in this work sequence succeeded with zero
warnings and zero errors. There is no F7 diagnostic hotkey or diagnostic
handler in the current plugin source.

---

### Important distinction: seasonal vs year-round crops

The mod must distinguish:

```text
Crop grows only in Autumn
```

from:

```text
Crop grows in every season
```

The desired behavior is:

```text
Autumn-only crop → Autumn icon
Spring-only crop → Spring icon
Summer-only crop → Summer icon
Winter-only crop → Winter icon
All-season crop → NO icon
```

The implementation treats a crop as year-round only when
`SeasonConfigs` contains Spring, Summer, Autumn, and Winter. A missing crop or
season list does not establish year-round behavior and produces no icon.


---

### Inventory UI integration

The implemented mod places a small seasonal image on seed cells in player
inventory and storage, and on cards in the seed shop.

Current integrations use `UI.KUICell_Item` for inventory/storage item cells and
`UI.KUIShopItemCard` for seed-shop cards. Their existing `Preview` image is not
replaced or modified; the indicator is a separate child image.

### UI requirements

The desired indicator is small and unobtrusive.

Conceptually:

```text
┌─────────┐
│ [icon]  │
│         │
│  SEED   │
│         │
└─────────┘
```

The season indicator should overlay the existing seed icon rather than replace it.

The current icon is positioned at the upper-right of the preview, displayed at
28×28 UI units, with aspect preserved. Its PNG sprite uses the full 64×64
source image.

The implementation should avoid:

* Large UI elements.
* Blocking clicks.
* Interfering with item tooltips.
* Interfering with stack counts.
* Duplicating icons when inventory slots are refreshed.

### Season changes

The icon should update when the in-game season changes.

Preferred approach:

1. Identify an existing season-change event/callback if one exists.
2. If no suitable event exists, identify a safe update mechanism.
3. Recalculate visible seed indicators.
4. Remove indicators that are no longer appropriate.

Do not assume that checking the season every frame is necessary.

Avoid per-frame work unless investigation shows it is required.

### Mod compatibility

The mod should ideally work with crops added by other mods when those crops use Starsand Island's normal crop/item systems.

Avoid assumptions based on:

* Specific mod names.
* Specific crop names.
* Specific item IDs.
* Display names.
* Localization text.

Prefer underlying game data.

If a third-party crop uses a different system and cannot be detected, document that limitation rather than adding an arbitrary special case.


---

# Desired Mod Behavior

For every seed item displayed in the player's inventory:

### Seasonal crops

If a crop does not grow in all four seasons, it is a seasonal crop. Display a small icon for the **current season** over/near its seed item's inventory icon only when the crop can grow in that current season.

The icon represents the current season, not a season chosen once from the crop's growing-season list. For example, a crop that grows in both Spring and Summer shows the Summer icon while it is Summer, and the Spring icon while it is Spring. If the current season is not one of that crop's growing seasons, display no icon.

| Current growing season | Asset |
| ---------------------- | ----- |
| Spring                 | `spring.png` |
| Summer                 | `summer.png` |
| Autumn                 | `fall.png` |
| Winter                 | `winter.png` |

For example:

```text
[ Crop Seeds that grow in Autumn; current season is Autumn ]
       [fall.png]
```

The exact visual implementation can be refined later.

### Year-round crops

If a crop can grow in **all four seasons**, display **NO season icon**.

This applies even if the current season is one of the crop's growing seasons: year-round crops never display an icon. This distinction is extremely important.

The mod must NOT simply determine the icon from the seed's category/name unless that can be proven to represent the actual crop growing-season data.

For example:

```text
Spring/Summer Crop Seeds → current season is Summer → `summer.png`
Spring/Summer Crop Seeds → current season is Autumn → no icon
Some Year-Round Seed → All Seasons → no icon
```

---

# Functional Requirements

## 1. Detect the current season

The current-season API is confirmed in `GameWorld.dll`:

```text
GameTime.KGameTimeUtil.Now : GameTime.KGameTime
GameTime.KGameTimeUtil.GetSeason(GameTime.KGameTime) : GameTime.ESeason
```

`GetSeason` is an extension method, so the current season can be read as:

```text
KGameTimeUtil.Now.GetSeason()
```

The crop-template season metadata uses `GameTime.ESeason`, not the flags enum.
`GameTime.ESeason` has `Spring`, `Summer`, `Autumn`, `Winter`, and `Total`.

Other season representations exist:

```text
GameTime.ESeasonFlag
Gameworld.ESeasonType
```

Do not treat these season representations as interchangeable. `ESeasonFlag`
is a `[Flags]` enum, but it is not the type used by
`CropTemplate.SeasonConfigs`.

No reliable season-change event has been confirmed. `KGameTimeEvent` declares
`TimeSystemLoaded` and `CrossWideTiemSetted` event-argument types, and game
classes expose handlers with those names, but the available decompiled source
does not show publisher wiring or establish that either event fires on season
rollover. The plugin reads `KGameTimeUtil.Now.GetSeason()` and compares the
value with its last observation. It refreshes visible cells when the season
changes and also every 0.75 seconds to account for reused/rebound cells.

---

## 2. Determine whether a seed is seasonal

The mod should inspect the game's actual seed/crop metadata.

Do NOT hardcode something like:

```csharp
if (seedId == "PumpkinSeed")
    season = Autumn;
```

The desired implementation should automatically work with:

* Existing vanilla crops
* Crops added by other mods, if they use the game's normal crop/item systems
* Future crops
* Additional seed items

The ideal solution is to ask the game's own crop definition which seasons it supports. If it supports all four seasons, display no icon; otherwise, display the icon for the current season only when that season is in the crop's supported seasons.

---

## 3. Update automatically when the season changes

The icon should reflect the current season without requiring:

* Game restart
* Mod restart
* Reloading the inventory
* Manually reloading configuration

The implementation should detect when the game's current season changes and update the displayed icons accordingly.

No reliable season-change event has been confirmed. `KGameTimeEvent` declares
`TimeSystemLoaded` and `CrossWideTiemSetted`, but the available source does not
show that either is published on season rollover. The plugin compares `KGameTimeUtil.Now.GetSeason()` with its last
observed value and refreshes on a change. It also refreshes visible cells every
0.75 seconds so reused/rebound cells do not retain stale indicators.

---


---

# Hard Requirements

## Do not hardcode crop names

Avoid:

```csharp
Dictionary<string, Season>
```

containing manually entered crop IDs unless absolutely unavoidable.

The game already appears to contain the required crop metadata.

Use the game's metadata whenever possible.

---

## Do not infer seasons from display names

Do not rely on:

```text
"Pumpkin Seeds"
"Autumn Crop"
```

as growing-season data. The implemented resolver uses `FarmSeedItemExt` and
`CropTemplate.SeasonConfigs` instead.

---

## Do not assume ItemType == growing season

The fact that ItemBrowser displays:

```text
Autumn Crop
```

is an item category in that UI, not the crop's growing-season definition. The
indicator does not use it.

---

## Do not invent APIs

When writing the mod, only use APIs/classes/methods that have been confirmed from:

* Starsand Island interop assemblies
* Decompiled game assemblies
* Existing working Starsand Island mods
* BepInEx/Unity APIs appropriate to this game

If an API is uncertain, investigate it first.

---

# Current Implementation Structure

## Season provider

The current-season API is confirmed:

```text
GameTime.KGameTimeUtil.Now : GameTime.KGameTime
GameTime.KGameTimeUtil.GetSeason(GameTime.KGameTime) : GameTime.ESeason
```

`GetSeason` is an extension method; read the current value using
`KGameTimeUtil.Now.GetSeason()`.

---

## Seed/crop metadata resolver

For an item template, the implemented resolver follows:

```text
ItemTemplate
    → Extensions
    → FarmSeedItemExt (IL2CPP TryCast)
    → Template.Get()
    → CropTemplate
    → SeasonConfigs : List<GameTime.ESeason>
```

The display decision uses the resolved crop seasons and the current season:

```csharp
if (growingSeasons contains all four actual seasons)
    return no icon;

if (growingSeasons includes currentSeason)
    return sprite for currentSeason;

return no icon;
```

Do not select a single fixed season from a multi-season crop's metadata. A
crop that grows in Spring and Summer shows the current one. A missing crop
template or season list produces no icon; it is not interpreted as a
year-round crop.

## Inventory and shop UI

The plugin applies the same indicator logic to `UI.KUICell_Item` cells and
`UI.KUIShopItemCard` cards. Player inventory, storage, and seed shop have been
confirmed working. The seasonal `Image` is a separate overlay and does not
replace the item's preview.

## Seasonal image resources

The four original 64×64 PNGs under `src/Assets` are embedded in the project
assembly as resources:

```text
Spring  → spring.png
Summer  → summer.png
Autumn  → fall.png
Winter  → winter.png
```

The plugin loads image bytes into `Texture2D`, creates sprites from the full
source rectangle, and caches the resulting sprites. It displays them at a
small UI size rather than at raw 64-pixel size.

## Dynamic refresh

The manager reads `KGameTimeUtil.Now.GetSeason()` and compares it to the last
observed `GameTime.ESeason`. Active item cells and shop cards are refreshed on
a season change or every 0.75 seconds, so recycled cells can be recalculated
for their current item. A dedicated live test through an actual season
rollover has not been recorded.

---

## Historical design notes

The initial design and investigation emphasized discovery before UI
integration. Those steps are complete; the details below are retained as
design rationale rather than outstanding tasks.

### Earlier metadata path proposal

The data flow now implemented is:

```text
Item
    ↓
ItemTemplate
    ↓
FarmSeedItemExt
    ↓
CropTemplate
    ↓
SeasonConfigs : List<GameTime.ESeason>
```

# Development Philosophy

The developer of this mod is comfortable with programming but is relatively new to:

* Unity modding
* BepInEx
* IL2CPP
* Reverse engineering compiled Unity games
* Navigating interop assemblies

Therefore, explanations should be practical and explicit.

When suggesting an investigation:

1. Say exactly what class/member to search for.
2. Explain briefly why it matters.
3. Identify what information to look for.
4. Avoid assuming the developer already understands IL2CPP internals.
5. Prefer small, verifiable discoveries over large speculative implementations.

When proposing code:

* Explain where the code belongs.
* Identify required references.
* Distinguish confirmed APIs from assumptions.
* Do not fabricate Starsand Island APIs.

---


---

# Important Principle

The ultimate goal is not merely:

> "Put an autumn icon on Pumpkin Seeds."

The goal is:

> **Build a generic, data-driven seasonal indicator that asks Starsand Island's own crop system which seasons a seed's crop can grow in.**

If the game adds a new crop tomorrow and that crop correctly uses the game's normal crop metadata, the mod should ideally recognize it automatically without any code changes.

---
