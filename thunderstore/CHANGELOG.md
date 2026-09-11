# Changelog

## 1.0.0
- Valheim 1.0 / Unity 6 compliance (BepInExPack 5.4.2350)
- Retargeted `WearNTear.GetMaterialProperties` postfix for the four-parameter out signature (`maxSupport`, `minSupport`, `horizontalLoss`, `verticalLoss`)
- Fixed hammer reinforce for Valheim 1.0: repair targets the hovered world piece, not the hammer Repair menu entry
- Fixed Crafting tier notifications for the updated `Character.Message` signature
- Restored optional Structural Analysis hover diagnostics (`[Analysis]` config section; on by default)
- Analysis baseline table updated for Valheim 1.0 material types (`Ice`, `Timberwood`)

## 0.1.1
- Added Team Extreme Discord link for feedback and bug reports (`https://discord.gg/cCNG8xKXMn`)

## 0.1.0
- Initial release
- Building pieces snapshot the placer's Crafting skill when placed
- Crafting skill reduces structural support loss through connected pieces
- Full-health hammer repair can reinforce older pieces without downgrading high-skill work
- Hammer-hover display shows stored support-loss reduction and Crafting skill
- Optional Crafting tier notifications at 25, 50, 75, and 100
