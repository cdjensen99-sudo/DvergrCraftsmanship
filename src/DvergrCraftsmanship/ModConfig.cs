using BepInEx.Configuration;

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
    }
}
