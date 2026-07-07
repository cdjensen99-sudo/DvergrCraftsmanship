# Changelog

## 0.1.0.1
- Added Team Extreme Discord link for feedback and bug reports (`https://discord.gg/cCNG8xKXMn`)

## 0.1.12
- Added toggleable in-game Structural Analysis diagnostics (`[Analysis]` config section, off by default)
- Hammer-hover overlay shows prefab name, vanilla baseline stats, live support, and Crafting reduction on any WearNTear piece
- Independent of existing ShowIntegrityHoverText; works on untouched vanilla pieces without Dvergr ZDO data

## 0.1.11
- Moved Structural Wall hotkey polling from `Player.Update` postfix to `Player.UpdatePlacement` prefix (vanilla placement input hook, before GUI input consumption)
- Unified debug and live hotkey code paths; trace now logs only on key activity or periodic heartbeat

## 0.1.10
- Fixed Structural Wall hotkey detection: use Valheim `ZInput.GetKeyDown`/`GetKey` instead of BepInEx UnityInput (always returned false in-game)
- Auto-normalize KeyboardShortcut config order so letter keys are main and Ctrl/Shift/Alt are modifiers (`LeftControl + N` in cfg works as hold-Ctrl tap-N)

## 0.1.9
- Fixed hotkey cycling blocked during normal placement: removed incorrect `Hud.IsPieceSelectionVisible()` guard (build menu is visible whenever the ghost is active)

## 0.1.8
- Added per-frame Structural Wall hotkey trace logging (DebugLogging=true): guard chain, prefab-name match, and WasPressed results for wood/metal shortcuts

## 0.1.7
- Fixed Structural Wall hotkey cycling not firing: BepInEx KeyboardShortcut.IsDown() rejects input when any other key (e.g. WASD) is held; now uses gameplay-safe main-key + modifier check
- Moved hotkey polling to Player.Update so cycling works reliably during ghost preview

## 0.1.6
- Fixed Structural Wall missing from Hammer build menu: dynamic requirement checks now use Valheim item shared names (e.g. `$item_wood`) instead of ObjectDB prefab names
- Fixed crafting-station known check to use the correct player known-stations dictionary type

## 0.1.5
- Fixed Structural Wall hotkey conflicts: switched from plain KeyCode (Z/X) to BepInEx KeyboardShortcut with modifier support
- New defaults: `[` (LeftBracket) cycles wood, `]` (RightBracket) cycles metal — conflict-free against vanilla and scanned mod configs

## 0.1.4
- Added Structural Wall prototype with hotkey material cycling before placement
- Wood/Core Wood body and Bronze/Iron nail accents selectable via configurable Z/X defaults
- Live ghost bracing tint, build HUD preview, dynamic recipe cost, and ZDO material stamps
- Structural stats use wood baseline x metal multiplier lookup tables (Bronze x1.4, Iron x1.75)

## 0.1.3
- Added Bronze Load-Bearing Half Wall
- Added Bronze Load-Bearing 1x1 Wall based on the vanilla quarter wall prefab
- Refactored load-bearing wall registration into data-driven piece definitions
- Changed load-bearing wall support from an absolute max-support floor to a relative max-support multiplier
- Scaled Bronze Nail costs for smaller load-bearing wall pieces

## 0.1.2
- Tuned Bronze Load-Bearing Wood Wall bracing to fit within the wall rails
- Repositioned decorative braces and bronze plates to sit flush against the wall face
- Updated custom build-menu icon rendering to use the modified braced prefab

## 0.1.1
- Added Bronze Load-Bearing Wood Wall proof-of-concept piece (2 Wood + 2 Bronze Nails)
- Added Jotunn dependency for custom piece registration
- Fixed vanilla wood wall clone base prefab name (`woodwall`)

## 0.1.0
- Initial release
- Building pieces snapshot the placer's Crafting skill when placed
- Crafting skill reduces structural support loss through connected pieces
- Full-health hammer repair can reinforce older pieces without downgrading high-skill work
- Hammer-hover display shows stored support-loss reduction and Crafting skill
- Optional Crafting tier notifications at 25, 50, 75, and 100
