using System;
using HarmonyLib;

namespace DvergrCraftsmanship.Patches;

[HarmonyPatch(typeof(Player), "PlacePiece")]
internal static class PlayerPlacePiecePatch
{
    private static void Prefix(Player __instance)
    {
        CraftsmanshipService.CapturePlacementSkill(__instance);
    }

    private static void Finalizer()
    {
        CraftsmanshipService.ClearPendingPlacement();
    }
}

[HarmonyPatch(typeof(Piece), "SetCreator")]
internal static class PieceSetCreatorPatch
{
    private static void Postfix(Piece __instance, long uid)
    {
        CraftsmanshipService.StampPieceFromPendingPlacement(__instance, uid);
        StructuralWallSelection.StampPlacedPiece(__instance);
    }
}

[HarmonyPatch(typeof(WearNTear), "Awake")]
internal static class WearNTearAwakePatch
{
    private static void Postfix(WearNTear __instance)
    {
        CraftsmanshipService.RegisterReinforceRpc(__instance);
    }
}

[HarmonyPatch(typeof(WearNTear), "GetMaterialProperties")]
internal static class WearNTearGetMaterialPropertiesPatch
{
    private static void Postfix(WearNTear __instance, ref float maxSupport, ref float horizontalLoss, ref float verticalLoss)
    {
        CraftsmanshipService.ApplyCraftsmanshipToSupportLoss(__instance, ref maxSupport, ref horizontalLoss, ref verticalLoss);
    }
}

[HarmonyPatch(typeof(Player), "Repair")]
internal static class PlayerRepairPatch
{
    private static bool Prefix(Player __instance, ItemDrop.ItemData toolItem)
    {
        return !CraftsmanshipService.TryHandleReinforceRepair(__instance, toolItem);
    }
}

[HarmonyPatch(typeof(Hud), "UpdateCrosshair")]
internal static class HudUpdateCrosshairPatch
{
    private static void Postfix(Hud __instance, Player player)
    {
        CraftsmanshipService.AppendIntegrityHoverText(__instance, player);
        StructuralAnalysis.AppendHoverText(__instance, player);
    }
}

[HarmonyPatch(typeof(Skills), "RaiseSkill")]
internal static class SkillsRaiseSkillPatch
{
    private static void Prefix(Skills __instance, Skills.SkillType skillType, ref int __state)
    {
        __state = skillType == Skills.SkillType.Crafting
            ? CraftsmanshipService.GetCraftingSkillLevelForTierNotification(__instance)
            : -1;
    }

    private static void Postfix(Skills __instance, Skills.SkillType skillType, int __state)
    {
        if (skillType == Skills.SkillType.Crafting)
        {
            CraftsmanshipService.ShowTierNotification(__instance, __state);
        }
    }
}

[HarmonyPatch(typeof(Player), "UpdatePlacement")]
internal static class PlayerUpdatePlacementPatch
{
    private static void Prefix(Player __instance, bool takeInput, float dt)
    {
        if (__instance == Player.m_localPlayer)
        {
            StructuralWallSelection.HandleHotkeys(__instance, takeInput);
        }
    }
}

[HarmonyPatch(typeof(Player), "SetupPlacementGhost")]
internal static class PlayerSetupPlacementGhostPatch
{
    private static void Postfix(Player __instance)
    {
        StructuralWallSelection.OnPlacementGhostSetup(__instance);
    }
}

[HarmonyPatch(typeof(Player), "HaveRequirements", typeof(Piece), typeof(Player.RequirementMode))]
internal static class PlayerHaveRequirementsPatch
{
    private static bool Prefix(Player __instance, Piece piece, Player.RequirementMode mode, ref bool __result)
    {
        return !StructuralWallSelection.TryEvaluateRequirements(__instance, piece, mode, ref __result);
    }
}

[HarmonyPatch(typeof(Player), "ConsumeResources", typeof(Piece.Requirement[]), typeof(int), typeof(int), typeof(int))]
internal static class PlayerConsumeResourcesPatch
{
    private static bool Prefix(Player __instance)
    {
        return !StructuralWallSelection.TryConsumeResources(__instance);
    }
}

[HarmonyPatch(typeof(Hud), "SetupPieceInfo")]
internal static class HudSetupPieceInfoPatch
{
    private static void Postfix(Hud __instance, Piece piece)
    {
        StructuralWallSelection.AppendBuildHudInfo(__instance, piece);
    }
}
