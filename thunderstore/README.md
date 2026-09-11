# Dvergr Craftsmanship

**Build like a master craftsman.** Dvergr Craftsmanship makes the structural support of pieces you place scale with your current **Crafting** skill.

The mod does **not** change how Crafting levels up. Valheim already grants Crafting skill from building and repairing; this mod only reads your current skill and uses it to improve the pieces you place.

## What It Does

When you place a supported building piece, Dvergr Craftsmanship snapshots your current Crafting skill onto that piece. Higher-skill builders create pieces that lose less structural support as that support travels through connected pieces.

In practical terms:

- A beginner's building pieces behave almost like vanilla pieces.
- A more experienced craftsman gets better spans and more forgiving structural chains.
- The bonus is stored on each piece when it is placed.
- Existing pieces do not automatically improve just because your skill rises later.
- Lower-skilled players can still repair higher-skilled pieces without damaging their bonus.

## Current Release Scope

This **1.0.0** release is Valheim 1.0 / Unity 6 compliant and includes the structural-integrity system:

- Crafting-skill-based support-loss reduction
- Permanent per-piece skill snapshot
- Hammer reinforcement for full-health pieces
- Hammer-hover display showing the stored support-loss reduction
- Optional Structural Analysis hover diagnostics
- Crafting tier notifications

This release does **not** add new build pieces. Load-bearing walls, nail-gated variants, firebreak walls, headers, and similar pieces belong in separate mods (for example Dvergr Forge).

## How The Bonus Works

The configured bonus creates a multiplier from your Crafting skill:

```text
multiplier = 1 + (CraftingSkill / 100) * (MaxIntegrityBonusPercent / 100)
```

Dvergr Craftsmanship then reduces structural support loss:

```text
effectiveSupportLoss = vanillaSupportLoss / multiplier
```

With the default `MaxIntegrityBonusPercent = 40`:

- Crafting 25 reduces support loss by about 9.1%
- Crafting 50 reduces support loss by about 16.7%
- Crafting 75 reduces support loss by about 23.1%
- Crafting 100 reduces support loss by about 28.6%

The result is subtle at low skill and more noticeable as support travels through longer chains.

## Reinforcing Existing Pieces

If `EnableReinforce` is enabled, you can improve an existing full-health piece with the hammer:

1. Equip the hammer.
2. Select the **Repair** build action.
3. Aim at a full-health building piece.
4. Use repair.
5. If your current Crafting skill is high enough above the piece's stored skill (default +5), the piece is reinforced.

Reinforcement never lowers a piece's stored bonus. A lower-skilled player can still perform ordinary repairs, but they cannot downgrade a master-built piece.

## Hover Display

With the hammer equipped, hover a supported piece to see its stored bonus:

```text
+16.7% support loss (Crafting 50)
```

Vanilla or unstamped pieces are not given noisy `0%` text.

### Structural Analysis

When Analysis mode is enabled (default on), hammer-hover also shows a diagnostics block:

```text
[Analysis]
Wood Wall | prefab: woodwall
Raw: max=100 min=10 hLoss=0.2 vLoss=0.125 (Wood)
Current support: 100
Crafting reduction: 16.7% support loss (Crafting 50)
```

This works on any hovered piece with `WearNTear`, including vanilla pieces without a Crafting stamp.

## Config Options

The config file is generated at:

```text
BepInEx/config/com.cdjensen.dvergrcraftsmanship.cfg
```

| Setting | Default | Description |
| --- | ---: | --- |
| `EnableMod` | `true` | Enables or disables all Dvergr Craftsmanship mechanics. |
| `MaxIntegrityBonusPercent` | `40` | Main tuning knob. Allowed range is `20-60`. At skill 100, this controls the multiplier used to reduce support loss. |
| `EnableReinforce` | `true` | Allows hammer repair on full-health pieces to improve their stored Crafting bonus. |
| `MinimumReinforceSkillDelta` | `5` | Required Crafting skill improvement before reinforcement can upgrade a piece. |
| `EnableTierNotifications` | `true` | Shows a short message when Crafting reaches 25, 50, 75, and 100. |
| `ShowIntegrityHoverText` | `true` | Shows support-loss reduction while hammer-hovering stamped pieces. |
| `DebugLogging` | `false` | Enables verbose diagnostic logging for placement, reinforcement, and support calculations. |
| `EnableAnalysisMode` | `true` | Shows the `[Analysis]` structural diagnostics block on hammer hover. |
| `ShowPrefabName` | `true` | In analysis mode, show display name and prefab name. |
| `ShowRawStructuralValue` | `true` | In analysis mode, show vanilla baseline support/loss values. |
| `ShowCurrentStructuralValue` | `true` | In analysis mode, show live support value. |
| `ShowCraftingLossReduction` | `true` | In analysis mode, show this mod's Crafting reduction or `no data`. |

## Compatibility Notes

Dvergr Craftsmanship patches Valheim's `WearNTear.GetMaterialProperties` to reduce structural support loss. Other mods that change structural integrity may stack with or overwrite similar behavior.

Requires **Valheim 1.0** and **BepInExPack Valheim 5.4.2350** (Unity 6 pack) or newer.

If using Valheim Plus or another structural-integrity mod, check that only the behavior you want is enabled.

## Installation

Install with a mod manager, or manually place `DvergrCraftsmanship.dll` into:

```text
BepInEx/plugins/DvergrCraftsmanship/
```

Requires BepInExPack Valheim.

## Support

**Feedback and bug reports:** [Team Extreme Discord](https://discord.gg/cCNG8xKXMn) — use `#dvergrcraftsmanship` and `#bug-reports`

GitHub issues: [Dvergr Craftsmanship](https://github.com/cdjensen99-sudo/DvergrCraftsmanship/issues)
