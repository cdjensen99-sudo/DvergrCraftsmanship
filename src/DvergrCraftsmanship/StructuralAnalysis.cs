using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace DvergrCraftsmanship;

internal static class StructuralAnalysis
{
    private static readonly FieldInfo HudHoverNameField = AccessTools.Field(typeof(Hud), "m_hoverName");
    private static readonly MethodInfo GetSupportMethod = AccessTools.Method(typeof(WearNTear), "GetSupport");

    internal static void AppendHoverText(Hud hud, Player player)
    {
        if (ModConfig.EnableAnalysisMode?.Value != true || hud == null || player == null)
        {
            return;
        }

        Piece hoveringPiece = player.GetHoveringPiece();
        if (hoveringPiece == null)
        {
            return;
        }

        WearNTear wear = hoveringPiece.GetComponent<WearNTear>();
        if (wear == null)
        {
            return;
        }

        if (!TryGetHoverTextProperty(hud, out object hoverName, out PropertyInfo textProperty, out string current))
        {
            return;
        }

        List<string> lines = new List<string> { "[Analysis]" };

        if (ModConfig.ShowPrefabName?.Value == true)
        {
            string displayName = Localization.instance != null
                ? Localization.instance.Localize(hoveringPiece.m_name)
                : hoveringPiece.m_name;
            string prefabName = Utils.GetPrefabName(wear.gameObject.name);
            lines.Add($"{displayName} | prefab: {prefabName}");
        }

        if (ModConfig.ShowRawStructuralValue?.Value == true)
        {
            GetVanillaBaseline(
                wear.m_materialType,
                out float maxSupport,
                out float minSupport,
                out float horizontalLoss,
                out float verticalLoss);
            lines.Add(
                $"Raw: max={maxSupport:0.###} min={minSupport:0.###} " +
                $"hLoss={horizontalLoss:0.###} vLoss={verticalLoss:0.###} ({wear.m_materialType})");
        }

        if (ModConfig.ShowCurrentStructuralValue?.Value == true)
        {
            lines.Add($"Current support: {GetCurrentSupport(wear):0.###}");
        }

        if (ModConfig.ShowCraftingLossReduction?.Value == true)
        {
            lines.Add(FormatCraftingReduction(wear));
        }

        if (lines.Count <= 1)
        {
            return;
        }

        string block = string.Join("\n", lines);
        string updated = string.IsNullOrWhiteSpace(current) ? block : $"{current}\n{block}";
        textProperty.SetValue(hoverName, updated);
    }

    private static bool TryGetHoverTextProperty(
        Hud hud,
        out object hoverName,
        out PropertyInfo textProperty,
        out string current)
    {
        hoverName = HudHoverNameField?.GetValue(hud);
        textProperty = null;
        current = null;

        if (hoverName == null)
        {
            return false;
        }

        textProperty = hoverName.GetType().GetProperty("text");
        if (textProperty == null || !textProperty.CanRead || !textProperty.CanWrite)
        {
            return false;
        }

        current = textProperty.GetValue(hoverName) as string;
        return true;
    }

    private static float GetCurrentSupport(WearNTear wear)
    {
        if (wear == null || GetSupportMethod == null)
        {
            return 0f;
        }

        return GetSupportMethod.Invoke(wear, null) is float support ? support : 0f;
    }

    private static string FormatCraftingReduction(WearNTear wear)
    {
        ZNetView nview = wear.GetComponent<ZNetView>();
        if (nview == null || !nview.IsValid())
        {
            return "Crafting reduction: no data";
        }

        ZDO zdo = nview.GetZDO();
        if (zdo == null)
        {
            return "Crafting reduction: no data";
        }

        float multiplier = zdo.GetFloat(ModConstants.ZdoIntegrityMultiplierHash, 0f);
        if (multiplier <= 1f)
        {
            return "Crafting reduction: no data";
        }

        float skillLevel = zdo.GetFloat(ModConstants.ZdoCraftSkillHash, 0f);
        float bonusPercent = (1f - 1f / multiplier) * 100f;
        return $"Crafting reduction: {bonusPercent:0.#}% support loss (Crafting {skillLevel:0})";
    }

    // Mirrors vanilla WearNTear.GetMaterialProperties() — intentionally bypasses mod postfix for research baseline.
    private static void GetVanillaBaseline(
        WearNTear.MaterialType materialType,
        out float maxSupport,
        out float minSupport,
        out float horizontalLoss,
        out float verticalLoss)
    {
        switch (materialType)
        {
        case WearNTear.MaterialType.Wood:
            maxSupport = 100f;
            minSupport = 10f;
            verticalLoss = 0.125f;
            horizontalLoss = 0.2f;
            break;
        case WearNTear.MaterialType.HardWood:
            maxSupport = 140f;
            minSupport = 10f;
            verticalLoss = 0.1f;
            horizontalLoss = 1f / 6f;
            break;
        case WearNTear.MaterialType.Stone:
            maxSupport = 1000f;
            minSupport = 100f;
            verticalLoss = 0.125f;
            horizontalLoss = 1f;
            break;
        case WearNTear.MaterialType.Iron:
            maxSupport = 1500f;
            minSupport = 20f;
            verticalLoss = 1f / 13f;
            horizontalLoss = 1f / 13f;
            break;
        case WearNTear.MaterialType.Marble:
            maxSupport = 1500f;
            minSupport = 100f;
            verticalLoss = 0.125f;
            horizontalLoss = 0.5f;
            break;
        case WearNTear.MaterialType.Ashstone:
            maxSupport = 2000f;
            minSupport = 100f;
            verticalLoss = 0.1f;
            horizontalLoss = 1f / 3f;
            break;
        case WearNTear.MaterialType.Ancient:
            maxSupport = 5000f;
            minSupport = 100f;
            verticalLoss = 1f / 15f;
            horizontalLoss = 0.25f;
            break;
        default:
            maxSupport = 0f;
            minSupport = 0f;
            verticalLoss = 0f;
            horizontalLoss = 0f;
            break;
        }
    }
}
