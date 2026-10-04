# Copilot Instructions — Starsand Island BepInEx Modding

## Project Context

This is a BepInEx mod for **Starsand Island**, a Unity IL2CPP game.

Read `docs/MODDING_CONTEXT.md` before making architectural recommendations or implementing game-specific functionality.

This file contains the behavioral rules for working in this repo. Project facts, reverse-engineering discoveries, and game-specific design notes belong in `docs/MODDING_CONTEXT.md`.

The project is reverse-engineering the game's existing systems to build a small seasonal seed indicator mod.

The primary goal is to use **Starsand Island's existing game data and APIs**, rather than recreating or hardcoding game data.

---

# Critical Rule: Never Invent Game APIs

This is the most important instruction.

**Do not assume a Starsand Island class, method, property, field, event, enum, or API exists.**

Before suggesting or using a game-specific API:

1. Search the provided interop/decompiled assemblies.
2. Find the actual class/member definition.
3. Confirm its namespace and assembly.
4. Confirm its parameter and return types.
5. Search for usages/callers when the behavior is not obvious.
6. Only then use it in code.

If the relevant API cannot be confirmed, say so explicitly and investigate further rather than inventing an implementation.

---

## Missing Reference Handling

The reference/decompiled directory may be incomplete.

If a class, method, property, field, enum, or implementation cannot be found:

1. Do NOT invent it.
2. Determine which assembly/type appears to be missing.
3. Tell me the exact missing type or dependency.
4. Explain what information we were trying to obtain from it.
5. Recommend which assembly or DLL should be decompiled next.
6. Continue investigating other confirmed parts of the problem if possible.

Treat missing source as "unknown", not as evidence that the API does not exist.

---

# Treat Decompiled IL2CPP Code Carefully

Starsand Island uses IL2CPP.

Much of the code exposed by interop assemblies consists of generated wrappers such as:

```csharp
IL2CPP.il2cpp_runtime_invoke(...)
```

and fields such as:

```text
NativeMethodInfoPtr_...
NativeFieldInfoPtr_...
```

These are not necessarily the original game implementation.

When investigating a method:

* Identify the class and method signature.
* Determine whether the method is merely an IL2CPP wrapper.
* Search for callers/usages.
* Search related classes and data structures.
* Prefer meaningful game data and references over wrapper boilerplate.

Do not claim to know the implementation of an IL2CPP wrapper simply because its generated C# representation has a particular name.

---

# Prefer Data Flow Over Guessing

When trying to understand a feature, trace the data from its origin to its use.

Do not jump from a display name or item ID to a season choice without confirming the actual game data path.

The relevant relationship should be traced through the actual game objects and template references, not inferred from names alone.

---

## Project-specific reverse-engineering notes

Project-specific findings are organized across the focused documents linked
from `docs/MODDING_CONTEXT.md`. Keep the context file as the concise entry
point; put requirements, game API facts, UI notes, and reverse-engineering
evidence in their matching documents. Before making any project documentation
change—including documentation updates accompanying code changes—invoke and
follow the `documentation-updater` skill. Record only claims supported by
confirmed evidence or explicitly stated user requirements.

Keep this file focused on operational rules, investigation workflow, evidence requirements, and coding guardrails.

---

## Static reverse-engineering workflow

Before using ILSpy or decompiled sources to investigate a type, method, property, field, or code relationship:

1. Search the existing project references first, including `reference/decompiled_game/` and any relevant mod references.
2. Prefer existing decompiled evidence when it clearly answers the question.
3. Use installed interop DLLs through `icsharpcode.ilspy-vscode` when the answer is missing, incomplete, or needs verification.
4. Search for exact type/member names first, then follow callers/usages.
5. Treat generated IL2CPP wrappers as wrappers, not as original gameplay logic.
6. Record the exact class, method, property, and field names discovered.
7. Distinguish directly observed behavior from hypotheses.

### Evidence priority

For static investigation, prefer evidence in this order:

1. Existing decompiled implementation/call sites in the repo
2. Installed BepInEx interop DLL inspection through ILSpy
3. Runtime observations from diagnostics
4. Other project notes/documentation
5. Hypothesis/inference

Clearly label conclusions as **CONFIRMED**, **LIKELY**, or **UNKNOWN** when the evidence does not establish the behavior.

### Search strategy

When asked to discover how the game implements something, search by both:

- the type/member involved
- likely callers and consumers

Follow promising call chains until the actual data relationship is established.

### Evidence requirements

Before implementing a game-integration feature, classify findings as:

**CONFIRMED**

- Directly observed in the installed DLL through ILSpy.
- Directly observed through runtime diagnostics.
- Explicitly established by existing project documentation.

**LIKELY**

- Strongly supported by code structure or multiple observations, but not yet directly confirmed.

**UNKNOWN**

- Not established.
- Do not implement based solely on an UNKNOWN relationship when it is possible to investigate further.

Never present a hypothesis as a confirmed game API.

### Runtime diagnostics vs. ILSpy

Use ILSpy for static questions such as:

- "Who calls this?"
- "What does this method do?"
- "What types does this method use?"
- "Where is this property populated?"
- "How does the game resolve this reference?"

Use a temporary runtime probe/diagnostic build for dynamic questions such as:

- "What object exists at runtime?"
- "What value does this property contain right now?"
- "Which UI object is active?"
- "What template does this live object resolve to?"
- "Does this API actually return null for this real game item?"

Use both when appropriate.

---

# Do Not Hardcode Crop Lists

Do not implement:

```csharp
if (seedId == "PumpkinSeed")
    return Autumn;
```

or any manually maintained crop/season dictionary keyed by display name, item ID, or localization text.

The preferred implementation is data-driven.

If the game provides crop-season metadata, use it.

---

# When Multiple Implementations Are Possible

Prefer solutions in this order:

1. Existing public game API.
2. Existing game data structure.
3. Existing event/callback used by the game.
4. Existing pattern demonstrated by another working mod.
5. Reflection/runtime inspection of confirmed game types.
6. Harmony patching of an appropriate confirmed method.
7. Polling as a last resort.
8. Hardcoded game data only if no better option exists.

If using reflection or Harmony, first identify the actual target member from the assemblies.

---

# Code Quality

When writing code:

* Keep game-specific logic isolated.
* Prefer small classes with clear responsibilities.
* Avoid unnecessary reflection.
* Avoid expensive repeated searches.
* Cache stable metadata when appropriate.
* Handle missing/null game objects gracefully.
* Do not crash the game if an unexpected item lacks crop metadata.
* Log useful diagnostic information during development.
* Remove or reduce noisy debug logging once the feature is stable.

Suggested conceptual components:

```text
SeasonService
SeedCropResolver
SeedSeasonInfo
SeasonIconManager
InventoryOverlayManager
```

These names are suggestions for our own mod code, NOT existing Starsand Island APIs.

---

## Final rule

If the required behavior depends on a game relationship, prefer:

`existing game logic -> discovered API/data relationship -> mod integration`

over:

`hardcoded mod mapping -> guessed relationship`

When unsure, say so explicitly and keep investigating before writing code.
