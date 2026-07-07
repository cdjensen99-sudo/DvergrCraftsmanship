using BepInEx.Configuration;
using UnityEngine;

namespace DvergrCraftsmanship;

internal static class ModConfig
{
    internal static ConfigEntry<bool> EnableMod;
    internal static ConfigEntry<float> MaxIntegrityBonusPercent;
    internal static ConfigEntry<bool> EnableReinforce;
    internal static ConfigEntry<int> MinimumReinforceSkillDelta;
    internal static ConfigEntry<bool> EnableTierNotifications;
    internal static ConfigEntry<bool> ShowIntegrityHoverText;
    internal static ConfigEntry<bool> DebugLogging;
    internal static ConfigEntry<KeyboardShortcut> CycleStructuralWoodKey;
    internal static ConfigEntry<KeyboardShortcut> CycleStructuralMetalKey;

    internal static ConfigEntry<bool> EnableAnalysisMode;
    internal static ConfigEntry<bool> ShowPrefabName;
    internal static ConfigEntry<bool> ShowRawStructuralValue;
    internal static ConfigEntry<bool> ShowCurrentStructuralValue;
    internal static ConfigEntry<bool> ShowCraftingLossReduction;

    internal static void Bind(ConfigFile config)
    {
        EnableMod = config.Bind(
            ModConstants.ConfigFolder,
            "EnableMod",
            true,
            "Enable Crafting-skill-based structural integrity bonuses.");

        MaxIntegrityBonusPercent = config.Bind(
            ModConstants.ConfigFolder,
            "MaxIntegrityBonusPercent",
            40f,
            new ConfigDescription(
                "Maximum craftsmanship bonus at Crafting skill 100. Default 40 means support loss is divided by 1.4 at skill 100.",
                new AcceptableValueRange<float>(20f, 60f)));

        EnableReinforce = config.Bind(
            ModConstants.ConfigFolder,
            "EnableReinforce",
            true,
            "Allow hammer repair on full-health pieces to reinforce them when the repairer's Crafting skill is high enough.");

        MinimumReinforceSkillDelta = config.Bind(
            ModConstants.ConfigFolder,
            "MinimumReinforceSkillDelta",
            5,
            new ConfigDescription(
                "Minimum Crafting skill improvement required to reinforce an already-crafted piece.",
                new AcceptableValueRange<int>(1, 100)));

        EnableTierNotifications = config.Bind(
            ModConstants.ConfigFolder,
            "EnableTierNotifications",
            true,
            "Show a short message when Crafting reaches Dvergr Craftsmanship thresholds: 25, 50, 75, and 100.");

        ShowIntegrityHoverText = config.Bind(
            ModConstants.ConfigFolder,
            "ShowIntegrityHoverText",
            true,
            "Show a concise Crafting skill and support-loss reduction line while hammer-hovering crafted pieces.");

        DebugLogging = config.Bind(
            ModConstants.ConfigFolder,
            "DebugLogging",
            false,
            "Enable verbose debug logging for placement stamps and reinforcement.");

        CycleStructuralWoodKey = config.Bind(
            ModConstants.ConfigFolder,
            "CycleStructuralWoodKey",
            new KeyboardShortcut(KeyCode.LeftBracket),
            "While placing a Structural Wall, cycle the wood body (Wood / Core Wood). Default [ (LeftBracket). Supports modifier combos via BepInEx config.");

        CycleStructuralMetalKey = config.Bind(
            ModConstants.ConfigFolder,
            "CycleStructuralMetalKey",
            new KeyboardShortcut(KeyCode.RightBracket),
            "While placing a Structural Wall, cycle the metal accent (Bronze Nails / Iron Nails). Default ] (RightBracket). Supports modifier combos via BepInEx config.");

        EnableAnalysisMode = config.Bind(
            ModConstants.AnalysisConfigFolder,
            "EnableAnalysisMode",
            false,
            "Show a verbose [Analysis] structural diagnostics block while hammer-hovering any WearNTear piece (research tool, off by default).");

        ShowPrefabName = config.Bind(
            ModConstants.AnalysisConfigFolder,
            "ShowPrefabName",
            true,
            "In analysis mode, show localized display name and real prefab name.");

        ShowRawStructuralValue = config.Bind(
            ModConstants.AnalysisConfigFolder,
            "ShowRawStructuralValue",
            true,
            "In analysis mode, show vanilla baseline maxSupport/minSupport/loss values from material type (unmodified by this mod).");

        ShowCurrentStructuralValue = config.Bind(
            ModConstants.AnalysisConfigFolder,
            "ShowCurrentStructuralValue",
            true,
            "In analysis mode, show the piece's live support value from WearNTear.GetSupport().");

        ShowCraftingLossReduction = config.Bind(
            ModConstants.AnalysisConfigFolder,
            "ShowCraftingLossReduction",
            true,
            "In analysis mode, show this mod's Crafting-skill loss reduction when ZDO data exists, otherwise 'no data'.");
    }
}
