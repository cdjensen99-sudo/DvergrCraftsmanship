using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace DvergrCraftsmanship;

internal static class CraftsmanshipService
{
    private static readonly MethodInfo CheckCanRemovePieceMethod = AccessTools.Method(typeof(Player), "CheckCanRemovePiece");
    private static readonly MethodInfo GetBuildStaminaMethod = AccessTools.Method(typeof(Player), "GetBuildStamina");
    private static readonly MethodInfo ClearCachedSupportMethod = AccessTools.Method(typeof(WearNTear), "ClearCachedSupport");
    private static readonly FieldInfo ClearCachedSupportField = AccessTools.Field(typeof(WearNTear), "m_clearCachedSupport");
    private static readonly FieldInfo SkillsPlayerField = AccessTools.Field(typeof(Skills), "m_player");
    private static readonly FieldInfo CharacterZanimField = AccessTools.Field(typeof(Character), "m_zanim");
    private static readonly FieldInfo HudHoverNameField = AccessTools.Field(typeof(Hud), "m_hoverName");

    private static bool s_hasPendingPlacement;
    private static long s_pendingPlayerId;
    private static float s_pendingSkillLevel;
    private static float s_pendingMultiplier;

    internal static void CapturePlacementSkill(Player player)
    {
        s_hasPendingPlacement = false;

        if (!IsEnabled() || player == null)
        {
            return;
        }

        float skillLevel = GetCraftingSkillLevel(player);
        s_pendingPlayerId = player.GetPlayerID();
        s_pendingSkillLevel = skillLevel;
        s_pendingMultiplier = ComputeMultiplier(skillLevel);
        s_hasPendingPlacement = true;
    }

    internal static void ClearPendingPlacement()
    {
        s_hasPendingPlacement = false;
    }

    internal static void StampPieceFromPendingPlacement(Piece piece, long creator)
    {
        if (!s_hasPendingPlacement || piece == null || creator != s_pendingPlayerId)
        {
            return;
        }

        try
        {
            WearNTear wear = piece.GetComponent<WearNTear>();
            if (wear != null)
            {
                StampWearNTear(wear, s_pendingSkillLevel, s_pendingMultiplier, force: true);
            }
        }
        finally
        {
            ClearPendingPlacement();
        }
    }

    internal static void RegisterReinforceRpc(WearNTear wear)
    {
        if (wear == null)
        {
            return;
        }

        ZNetView nview = wear.GetComponent<ZNetView>();
        if (nview == null || nview.GetZDO() == null)
        {
            return;
        }

        nview.Register<float, float>(
            ModConstants.RpcReinforce,
            (_, skillLevel, multiplier) => TryApplyReinforcement(wear, skillLevel, multiplier));
    }

    internal static void ApplyCraftsmanshipToSupportLoss(WearNTear wear, float maxSupport, ref float horizontalLoss, ref float verticalLoss)
    {
        if (!IsEnabled() || wear == null || maxSupport <= 0f)
        {
            return;
        }

        ZNetView nview = wear.GetComponent<ZNetView>();
        bool hasView = nview != null;
        bool isValid = hasView && nview.IsValid();
        bool isOwner = isValid && nview.IsOwner();
        ZDO zdo = isValid ? nview.GetZDO() : null;
        string pieceName = Utils.GetPrefabName(wear.gameObject.name);

        if (zdo == null)
        {
            SupportTraceLog(
                $"Support material patch hit for {pieceName}: no valid ZDO, material={wear.m_materialType}, owner={isOwner}, validView={isValid}, baseMax={maxSupport:0.###}, frame={Time.frameCount}");
            return;
        }

        float multiplier = GetStoredMultiplier(zdo, 1f);
        float skillLevel = zdo.GetFloat(ModConstants.ZdoCraftSkillHash, 0f);
        float support = zdo.GetFloat(ZDOVars.s_support, -1f);
        if (multiplier <= 1f)
        {
            SupportTraceLog(
                $"Support material patch hit for {pieceName}: no bonus, material={wear.m_materialType}, owner={isOwner}, skill={skillLevel:0.#}, multiplier={multiplier:0.###}, max={maxSupport:0.###}, hLoss={horizontalLoss:0.###}, vLoss={verticalLoss:0.###}, currentSupport={support:0.###}, frame={Time.frameCount}");
            return;
        }

        float oldHorizontalLoss = horizontalLoss;
        float oldVerticalLoss = verticalLoss;
        horizontalLoss /= multiplier;
        verticalLoss /= multiplier;

        SupportTraceLog(
            $"Support material patch hit for {pieceName}: material={wear.m_materialType}, owner={isOwner}, skill={skillLevel:0.#}, multiplier={multiplier:0.###}, max={maxSupport:0.###}, hLoss={oldHorizontalLoss:0.###}->{horizontalLoss:0.###}, vLoss={oldVerticalLoss:0.###}->{verticalLoss:0.###}, currentSupport={support:0.###}, frame={Time.frameCount}");
    }

    internal static bool TryHandleReinforceRepair(Player player, ItemDrop.ItemData toolItem)
    {
        if (!IsEnabled() || !ModConfig.EnableReinforce.Value || player == null || toolItem == null)
        {
            return false;
        }

        if (!player.InPlaceMode())
        {
            return false;
        }

        Piece hoveringPiece = player.GetHoveringPiece();
        if (hoveringPiece == null)
        {
            return false;
        }

        WearNTear wear = hoveringPiece.GetComponent<WearNTear>();
        if (wear == null || !IsFullHealth(wear))
        {
            return false;
        }

        float skillLevel = GetCraftingSkillLevel(player);
        float multiplier = ComputeMultiplier(skillLevel);
        if (!CanImprove(wear, skillLevel, multiplier))
        {
            return false;
        }

        if (!HasRepairAccess(player, hoveringPiece))
        {
            return true;
        }

        if (!RequestReinforcement(wear, skillLevel, multiplier))
        {
            return false;
        }

        ConsumeRepairSwing(player, hoveringPiece, toolItem, skillLevel, multiplier);
        return true;
    }

    internal static int GetCraftingSkillLevelForTierNotification(Skills skills)
    {
        if (!IsEnabled() || !ModConfig.EnableTierNotifications.Value || skills == null)
        {
            return -1;
        }

        return Mathf.FloorToInt(skills.GetSkillLevel(Skills.SkillType.Crafting));
    }

    internal static void ShowTierNotification(Skills skills, int oldLevel)
    {
        if (oldLevel < 0 || !IsEnabled() || !ModConfig.EnableTierNotifications.Value || skills == null)
        {
            return;
        }

        int newLevel = Mathf.FloorToInt(skills.GetSkillLevel(Skills.SkillType.Crafting));
        int threshold = GetCrossedTierThreshold(oldLevel, newLevel);
        if (threshold <= 0)
        {
            return;
        }

        Player player = SkillsPlayerField?.GetValue(skills) as Player;
        if (player == null)
        {
            return;
        }

        player.Message(
            MessageHud.MessageType.TopLeft,
            $"Dvergr Craftsmanship: {GetTierName(threshold)} reached. Future builds gain stronger integrity.",
            0,
            null);
    }

    internal static void AppendIntegrityHoverText(Hud hud, Player player)
    {
        if (!IsEnabled() || ModConfig.ShowIntegrityHoverText?.Value != true || hud == null || player == null)
        {
            return;
        }

        Piece hoveringPiece = player.GetHoveringPiece();
        if (hoveringPiece == null)
        {
            return;
        }

        WearNTear wear = hoveringPiece.GetComponent<WearNTear>();
        ZDO zdo = GetZdo(wear);
        if (zdo == null)
        {
            return;
        }

        float multiplier = GetStoredMultiplier(zdo, 0f);
        if (multiplier <= 0f)
        {
            return;
        }

        float skillLevel = zdo.GetFloat(ModConstants.ZdoCraftSkillHash, 0f);
        string pieceName = Localization.instance != null ? Localization.instance.Localize(hoveringPiece.m_name) : hoveringPiece.m_name;
        string bonus = FormatBonusPercent(GetSupportLossReductionPercent(multiplier));
        string details = $"{bonus}% support loss (Crafting {skillLevel:0})";

        object hoverName = HudHoverNameField?.GetValue(hud);
        if (hoverName == null)
        {
            return;
        }

        PropertyInfo textProperty = hoverName.GetType().GetProperty("text");
        if (textProperty == null || !textProperty.CanRead || !textProperty.CanWrite)
        {
            return;
        }

        string current = textProperty.GetValue(hoverName) as string;
        string updated = string.IsNullOrWhiteSpace(current)
            ? $"{pieceName}\n{details}"
            : $"{current}\n{details}";

        textProperty.SetValue(hoverName, updated);
    }

    private static bool IsEnabled()
    {
        return ModConfig.EnableMod?.Value == true;
    }

    private static float GetStoredMultiplier(ZDO zdo, float defaultValue)
    {
        return zdo?.GetFloat(ModConstants.ZdoIntegrityMultiplierHash, defaultValue) ?? defaultValue;
    }

    private static float GetSupportLossReductionPercent(float multiplier)
    {
        return multiplier <= 0f ? 0f : (1f - 1f / multiplier) * 100f;
    }

    private static float GetCraftingSkillLevel(Player player)
    {
        return Mathf.Clamp(player.GetSkills()?.GetSkillLevel(Skills.SkillType.Crafting) ?? 0f, 0f, 100f);
    }

    private static float ComputeMultiplier(float skillLevel)
    {
        float bonusPercent = Mathf.Clamp(ModConfig.MaxIntegrityBonusPercent.Value, 20f, 60f);
        return 1f + Mathf.Clamp01(skillLevel / 100f) * (bonusPercent / 100f);
    }

    private static void StampWearNTear(WearNTear wear, float skillLevel, float multiplier, bool force)
    {
        ZNetView nview = wear.GetComponent<ZNetView>();
        if (nview == null || !nview.IsValid() || !nview.IsOwner())
        {
            return;
        }

        ZDO zdo = nview.GetZDO();
        if (zdo == null)
        {
            return;
        }

        if (!force && !CanImprove(zdo, skillLevel, multiplier))
        {
            return;
        }

        zdo.Set(ModConstants.ZdoCraftSkillHash, skillLevel);
        zdo.Set(ModConstants.ZdoIntegrityMultiplierHash, multiplier);
        MarkSupportDirty(wear);

        DebugLog($"Stamped {wear.name}: Crafting {skillLevel:0}, integrity x{multiplier:0.###}");
    }

    private static bool RequestReinforcement(WearNTear wear, float skillLevel, float multiplier)
    {
        ZNetView nview = wear.GetComponent<ZNetView>();
        if (nview == null || !nview.IsValid())
        {
            return false;
        }

        ZDO zdo = nview.GetZDO();
        if (zdo == null || !CanImprove(zdo, skillLevel, multiplier))
        {
            return false;
        }

        if (nview.IsOwner())
        {
            return TryApplyReinforcement(wear, skillLevel, multiplier);
        }

        nview.InvokeRPC(ModConstants.RpcReinforce, skillLevel, multiplier);
        return true;
    }

    private static bool TryApplyReinforcement(WearNTear wear, float skillLevel, float multiplier)
    {
        ZNetView nview = wear.GetComponent<ZNetView>();
        if (nview == null || !nview.IsValid() || !nview.IsOwner())
        {
            return false;
        }

        ZDO zdo = nview.GetZDO();
        if (zdo == null || !CanImprove(zdo, skillLevel, multiplier))
        {
            return false;
        }

        zdo.Set(ModConstants.ZdoCraftSkillHash, skillLevel);
        zdo.Set(ModConstants.ZdoIntegrityMultiplierHash, multiplier);
        MarkSupportDirty(wear);

        DebugLog($"Reinforced {wear.name}: Crafting {skillLevel:0}, integrity x{multiplier:0.###}");
        return true;
    }

    private static bool CanImprove(WearNTear wear, float skillLevel, float multiplier)
    {
        ZDO zdo = GetZdo(wear);
        return zdo != null && CanImprove(zdo, skillLevel, multiplier);
    }

    private static bool CanImprove(ZDO zdo, float skillLevel, float multiplier)
    {
        float oldSkill = zdo.GetFloat(ModConstants.ZdoCraftSkillHash, 0f);
        float oldMultiplier = zdo.GetFloat(ModConstants.ZdoIntegrityMultiplierHash, 1f);
        int minimumDelta = Mathf.Max(1, ModConfig.MinimumReinforceSkillDelta.Value);

        return skillLevel >= oldSkill + minimumDelta && multiplier > oldMultiplier;
    }

    private static bool HasRepairAccess(Player player, Piece piece)
    {
        if (!PrivateArea.CheckAccess(piece.transform.position))
        {
            return false;
        }

        if (CheckCanRemovePieceMethod == null)
        {
            DvergrCraftsmanshipPlugin.Log?.LogWarning("Unable to validate build-station repair access; reinforcement skipped.");
            return false;
        }

        object result = CheckCanRemovePieceMethod.Invoke(player, new object[] { piece });
        return result is bool canRemove && canRemove;
    }

    private static bool IsFullHealth(WearNTear wear)
    {
        ZDO zdo = GetZdo(wear);
        return zdo != null && zdo.GetFloat(ZDOVars.s_health, wear.m_health) >= wear.m_health;
    }

    private static ZDO GetZdo(WearNTear wear)
    {
        ZNetView nview = wear.GetComponent<ZNetView>();
        if (nview == null || !nview.IsValid())
        {
            return null;
        }

        return nview.GetZDO();
    }

    private static void MarkSupportDirty(WearNTear wear)
    {
        ClearCachedSupportField?.SetValue(wear, true);
        ClearCachedSupportMethod?.Invoke(wear, Array.Empty<object>());
    }

    private static void ConsumeRepairSwing(Player player, Piece piece, ItemDrop.ItemData toolItem, float skillLevel, float multiplier)
    {
        player.FaceLookDirection();
        TriggerRepairAnimation(player, toolItem);
        piece.m_placeEffect.Create(piece.transform.position, piece.transform.rotation);

        string pieceName = Localization.instance != null ? Localization.instance.Localize(piece.m_name) : piece.m_name;
        float bonusPercent = GetSupportLossReductionPercent(multiplier);
        player.Message(
            MessageHud.MessageType.TopLeft,
            $"Reinforced {pieceName}: Crafting {skillLevel:0}, support loss -{bonusPercent:0.#}%",
            0,
            null);

        ConsumeBuildStamina(player);
        player.UseEitr(toolItem.m_shared.m_attack.m_attackEitr);

        if (toolItem.m_shared.m_useDurability)
        {
            toolItem.m_durability -= toolItem.m_shared.m_useDurabilityDrain;
        }
    }

    private static void TriggerRepairAnimation(Player player, ItemDrop.ItemData toolItem)
    {
        object zanim = CharacterZanimField?.GetValue(player);
        if (zanim == null)
        {
            return;
        }

        MethodInfo setTrigger = AccessTools.Method(zanim.GetType(), "SetTrigger", new[] { typeof(string) });
        setTrigger?.Invoke(zanim, new object[] { toolItem.m_shared.m_attack.m_attackAnimation });
    }

    private static void ConsumeBuildStamina(Player player)
    {
        if (GetBuildStaminaMethod?.Invoke(player, Array.Empty<object>()) is float stamina)
        {
            player.UseStamina(stamina);
        }
    }

    private static int GetCrossedTierThreshold(int oldLevel, int newLevel)
    {
        int crossed = 0;
        int[] thresholds = { 25, 50, 75, 100 };
        foreach (int threshold in thresholds)
        {
            if (oldLevel < threshold && newLevel >= threshold)
            {
                crossed = threshold;
            }
        }

        return crossed;
    }

    private static string GetTierName(int threshold)
    {
        switch (threshold)
        {
            case 25:
                return "Apprentice Craftsman";
            case 50:
                return "Journeyman Craftsman";
            case 75:
                return "Artisan Craftsman";
            case 100:
                return "Master Craftsman";
            default:
                return "Craftsman";
        }
    }

    private static string FormatBonusPercent(float value)
    {
        return Math.Abs(value - Mathf.Round(value)) < 0.05f
            ? value.ToString("+0;-0;0")
            : value.ToString("+0.#;-0.#;0");
    }

    private static void DebugLog(string message)
    {
        if (ModConfig.DebugLogging?.Value == true)
        {
            DvergrCraftsmanshipPlugin.Log?.LogInfo(message);
        }
    }

    private static void SupportTraceLog(string message)
    {
        DebugLog(message);
    }
}
