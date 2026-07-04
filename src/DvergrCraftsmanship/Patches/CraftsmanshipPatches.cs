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
    private static void Postfix(WearNTear __instance, float maxSupport, ref float horizontalLoss, ref float verticalLoss)
    {
        CraftsmanshipService.ApplyCraftsmanshipToSupportLoss(__instance, maxSupport, ref horizontalLoss, ref verticalLoss);
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
