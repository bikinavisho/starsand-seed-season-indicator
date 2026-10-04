**Project docs:** [Modding context](MODDING_CONTEXT.md) · [Requirements and design](MODDING_REQUIREMENTS.md) · [Game data reference](GAME_DATA_REFERENCE.md) · [UI integration notes](UI_INTEGRATION_NOTES.md) · [Reverse-engineering log](REVERSE_ENGINEERING_LOG.md)

---

# Modding Requirements and Design

## Project Overview

This project is a small BepInEx mod for **Starsand Island**.

The goal is to add a small visual indicator to seed items in the player's inventory, storage, and seed shop showing the **current season**, but only when the crop is seasonal and can grow in that season.

The mod should rely on the game's own crop/item data rather than maintaining a manually hardcoded list of crops.


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

Do not treat the absence of a recognized season as evidence that a crop is year-round.

Determine how the game itself represents this distinction.


---

### Inventory UI investigation

The final mod needs to place a small seasonal icon on seed inventory slots.

Do not assume the inventory UI class or slot implementation.

First identify:

* The actual inventory UI class.
* How inventory item slots are represented.
* How item icons are created.
* How items are refreshed.
* Whether slots are pooled/reused.
* Whether there is an existing tooltip/item-display system we can hook into.

Prefer integrating with the existing UI rather than creating a separate inventory window.

### UI requirements

The desired indicator is small and unobtrusive.

Conceptually:

```text
┌─────────┐
│ 🍂      │
│         │
│  SEED   │
│         │
└─────────┘
```

The season indicator should overlay the existing seed icon rather than replace it.

The actual artwork and exact placement can be determined later.

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

| Growing season | Icon    |
| -------------- | ------- |
| Spring         | Flower  |
| Summer         | Sun     |
| Autumn/Fall    | Leaf    |
| Winter         | Snowman |

For example:

```text
[ Crop Seeds that grow in Autumn; current season is Autumn ]
       🍂
```

The exact visual implementation can be refined later.

### Year-round crops

If a crop can grow in **all four seasons**, display **NO season icon**.

This applies even if the current season is one of the crop's growing seasons: year-round crops never display an icon. This distinction is extremely important.

The mod must NOT simply determine the icon from the seed's category/name unless that can be proven to represent the actual crop growing-season data.

For example:

```text
Spring/Summer Crop Seeds → current season is Summer → ☀️
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
rollover. Until that behavior is confirmed, compare
`KGameTimeUtil.Now.GetSeason()` during an appropriate active-UI update and
refresh only when the season value changes.

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
show that either is published on season rollover. Until verified, compare
`KGameTimeUtil.Now.GetSeason()` during an appropriate active-UI update and
refresh only when its value changes.

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

unless investigation proves that these values directly represent the crop's growing-season metadata.

---

## Do not assume ItemType == growing season

The fact that ItemBrowser displays:

```text
Autumn Crop
```

does not yet prove that this is the crop's actual growing-season definition.

It may simply be an item category.

Trace the data.

---

## Do not invent APIs

When writing the mod, only use APIs/classes/methods that have been confirmed from:

* Starsand Island interop assemblies
* Decompiled game assemblies
* Existing working Starsand Island mods
* BepInEx/Unity APIs appropriate to this game

If an API is uncertain, investigate it first.

---

# Desired Mod Architecture

The final mod will likely need several conceptual pieces.

## Season Provider

The current-season API is confirmed:

```text
GameTime.KGameTimeUtil.Now : GameTime.KGameTime
GameTime.KGameTimeUtil.GetSeason(GameTime.KGameTime) : GameTime.ESeason
```

`GetSeason` is an extension method; read the current value using
`KGameTimeUtil.Now.GetSeason()`.

---

## Seed/Crop Metadata Resolver

Given an inventory item:

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

The display decision should use the resolved crop's configured seasons
together with the current season:

```csharp
if (growingSeasons contains all four seasons)
    return no icon;

if (growingSeasons includes currentSeason)
    return icon for currentSeason;

return no icon;
```

Do not select a single fixed season from a multi-season crop's metadata. A
crop that grows in Spring and Summer should show whichever of those seasons is
current. The year-round `SeasonConfigs` convention is not verified; do not
treat an empty or missing list as year-round without checking actual game data.

---

## Inventory UI Integration

Find the game's inventory UI and determine the appropriate place to add a small overlay/icon.

The icon should be attached to the seed item's UI representation rather than replacing the existing item icon.

Ideally:

```text
┌──────────┐
│ [season] │
│          │
│  [seed]  │
│          │
└──────────┘
```

The exact positioning can be refined later.

---

## Season Icon Manager

Maintain the relationship:

```text
Spring  → flower
Summer  → sun
Autumn  → leaf
Winter  → snowman
```

Choose the icon from the current season, not from a fixed season assigned to the crop. Show it only if the crop is seasonal (does not grow in all four seasons) and supports the current season. The actual art assets can be added later.

---

## Dynamic Refresh

When the game's season changes:

1. Detect the change.
2. Recalculate the current season.
3. Update visible seed overlays.
4. Remove overlays from crops that do not grow in the new current season, as well as from year-round crops.
5. Do not require a game restart.

---


---

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
