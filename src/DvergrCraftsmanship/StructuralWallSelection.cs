using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace DvergrCraftsmanship;

internal static class StructuralWallSelection
{
    private const string BronzeNailsItem = "BronzeNails";
    private const string IronNailsItem = "IronNails";
    private const int BronzeNailsCost = 2;
    private const int IronNailsCost = 2;
    private const float BronzeMetalMultiplier = 1.4f;
    private const float IronMetalMultiplier = 1.75f;
    private const float BronzeHorizontalLossCap = 1f / 6f;
    private const float BronzeVerticalLossCap = 0.1f;
    private const float IronHorizontalLossCap = 0.14f;
    private const float IronVerticalLossCap = 0.085f;

    private static readonly FieldInfo PlacementGhostField = AccessTools.Field(typeof(Player), "m_placementGhost");
    private static readonly FieldInfo KnownStationsField = AccessTools.Field(typeof(Player), "m_knownStations");
    private static readonly FieldInfo KnownMaterialField = AccessTools.Field(typeof(Player), "m_knownMaterial");
    private static readonly FieldInfo BuildSelectionField = AccessTools.Field(typeof(Hud), "m_buildSelection");
    private static readonly FieldInfo PieceDescriptionField = AccessTools.Field(typeof(Hud), "m_pieceDescription");
    private static readonly FieldInfo RequirementItemsField = AccessTools.Field(typeof(Hud), "m_requirementItems");

    private static StructuralWoodType s_pendingWood = StructuralWoodType.Wood;
    private static StructuralMetalType s_pendingMetal = StructuralMetalType.BronzeNails;
    private static string s_activeGhostName;

    internal static void OnPlacementGhostSetup(Player player)
    {
        if (!IsEnabled() || player == null || !IsStructuralWallSelected(player))
        {
            s_activeGhostName = null;
            return;
        }

        ResetPendingSelection();
        RefreshGhostVisual(player);
        DebugLog(
            $"Structural Wall ghost setup: selected='{DescribeGameObject(player.GetSelectedPiece()?.gameObject)}' " +
            $"ghost='{DescribeGameObject(GetPlacementGhost(player))}' pending={s_pendingWood}+{s_pendingMetal}");
    }

    internal static void HandleHotkeys(Player player, bool takeInput)
    {
        if (!takeInput || !IsEnabled() || player == null || !player.InPlaceMode() || !IsStructuralWallSelected(player))
        {
            return;
        }

        GameObject ghost = GetPlacementGhost(player);
        if (ghost == null || !ghost.activeSelf)
        {
            return;
        }

        if (Console.IsVisible() || (Chat.instance != null && Chat.instance.HasFocus()))
        {
            return;
        }

        TrackGhostIdentity(ghost);

        bool trace = ModConfig.DebugLogging?.Value == true;
        if (trace)
        {
            LogHotkeyProbe(player, takeInput);
        }

        TryCycleWood(player);
        TryCycleMetal(player);
    }

    private static void LogHotkeyProbe(Player player, bool takeInput)
    {
        KeyboardShortcut woodShortcut = StructuralWallHotkeys.NormalizeForGameplay(ModConfig.CycleStructuralWoodKey.Value);
        KeyboardShortcut metalShortcut = StructuralWallHotkeys.NormalizeForGameplay(ModConfig.CycleStructuralMetalKey.Value);
        bool woodActivity = StructuralWallHotkeys.ProbeAnyActivity(woodShortcut.MainKey, woodShortcut.Modifiers);
        bool metalActivity = StructuralWallHotkeys.ProbeAnyActivity(metalShortcut.MainKey, metalShortcut.Modifiers);
        if (!woodActivity && !metalActivity && Time.frameCount % 120 != 0)
        {
            return;
        }

        bool pieceSelectionVisible = Hud.IsPieceSelectionVisible();
        DebugLog(
            $"[HotkeyTrace] site=UpdatePlacement.prefix frame={Time.frameCount} takeInput={takeInput} " +
            $"zinput={(ZInput.instance != null)} pieceMenu={pieceSelectionVisible} pending={s_pendingWood}+{s_pendingMetal}");

        LogShortcutTrace("wood", ModConfig.CycleStructuralWoodKey.Value);
        LogShortcutTrace("metal", ModConfig.CycleStructuralMetalKey.Value);
    }

    private static void LogShortcutTrace(string label, KeyboardShortcut shortcut)
    {
        KeyboardShortcut normalized = StructuralWallHotkeys.NormalizeForGameplay(shortcut);
        StructuralWallHotkeys.Evaluate(shortcut, out bool pressed, true, out string trace);
        DebugLog(
            $"[HotkeyTrace] {label} raw='{shortcut}' normalized='{normalized}' {trace} pressed={pressed}");
    }

    private static string DescribeGameObject(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return "<null>";
        }

        return Utils.GetPrefabName(gameObject.name);
    }

    private static void TryCycleWood(Player player)
    {
        if (!StructuralWallHotkeys.WasPressed(ModConfig.CycleStructuralWoodKey.Value))
        {
            return;
        }

        s_pendingWood = s_pendingWood == StructuralWoodType.Wood
            ? StructuralWoodType.CoreWood
            : StructuralWoodType.Wood;
        RefreshGhostVisual(player);
        RefreshBuildHud(player);
        DebugLog($"Cycled wood to {s_pendingWood}");
    }

    private static void TryCycleMetal(Player player)
    {
        if (!StructuralWallHotkeys.WasPressed(ModConfig.CycleStructuralMetalKey.Value))
        {
            return;
        }

        s_pendingMetal = s_pendingMetal == StructuralMetalType.BronzeNails
            ? StructuralMetalType.IronNails
            : StructuralMetalType.BronzeNails;
        RefreshGhostVisual(player);
        RefreshBuildHud(player);
        DebugLog($"Cycled metal to {s_pendingMetal}");
    }

    internal static bool TryEvaluateRequirements(Player player, Piece piece, Player.RequirementMode mode, ref bool result)
    {
        if (!IsEnabled() || player == null || !StructuralWallPrefabs.IsStructuralWall(piece))
        {
            return false;
        }

        if (!PassesVanillaPlacementGates(player, piece, mode))
        {
            result = false;
            return true;
        }

        if (mode != Player.RequirementMode.IsKnown && ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey()))
        {
            result = true;
            return true;
        }

        GetPendingSelection(player, out StructuralWoodType woodType, out StructuralMetalType metalType);
        result = HasDynamicResources(player, woodType, metalType, mode);
        return true;
    }

    internal static bool TryConsumeResources(Player player)
    {
        if (!IsEnabled() || player == null || !IsStructuralWallSelected(player))
        {
            return false;
        }

        GetPendingSelection(player, out StructuralWoodType woodType, out StructuralMetalType metalType);
        ConsumeDynamicResources(player, woodType, metalType);
        return true;
    }

    internal static void StampPlacedPiece(Piece piece)
    {
        if (!IsEnabled() || piece == null || !StructuralWallPrefabs.IsStructuralWall(piece))
        {
            return;
        }

        ZNetView nview = piece.GetComponent<ZNetView>();
        if (nview == null || !nview.IsValid() || !nview.IsOwner())
        {
            return;
        }

        ZDO zdo = nview.GetZDO();
        if (zdo == null)
        {
            return;
        }

        zdo.Set(ModConstants.ZdoWoodTypeHash, (int)s_pendingWood);
        zdo.Set(ModConstants.ZdoMetalTypeHash, (int)s_pendingMetal);
        StructuralWallPrefabs.ApplyVisualVariant(piece.gameObject, s_pendingWood, s_pendingMetal);

        DebugLog($"Stamped structural wall: wood={s_pendingWood}, metal={s_pendingMetal}");
    }

    internal static bool TryApplyStructuralStats(
        WearNTear wear,
        ref float maxSupport,
        ref float horizontalLoss,
        ref float verticalLoss)
    {
        if (!IsEnabled() || wear == null || !StructuralWallPrefabs.IsStructuralWall(wear.gameObject))
        {
            return false;
        }

        ZDO zdo = wear.GetComponent<ZNetView>()?.GetZDO();
        StructuralWoodType woodType = GetStoredWoodType(zdo);
        StructuralMetalType metalType = GetStoredMetalType(zdo);

        ApplyWoodBaseline(woodType, ref maxSupport, ref horizontalLoss, ref verticalLoss);
        ApplyMetalBonus(metalType, ref maxSupport, ref horizontalLoss, ref verticalLoss);
        return true;
    }

    internal static void AppendBuildHudInfo(Hud hud, Piece piece)
    {
        if (!IsEnabled() || hud == null || !StructuralWallPrefabs.IsStructuralWall(piece))
        {
            return;
        }

        Player player = Player.m_localPlayer;
        if (player == null)
        {
            return;
        }

        GetPendingSelection(player, out StructuralWoodType woodType, out StructuralMetalType metalType);
        float maxSupport = GetPreviewMaxSupport(woodType, metalType);
        float skillLevel = GetCraftingSkillLevel(player);
        float multiplier = ComputeMultiplier(skillLevel);
        float bonusPercent = GetSupportLossReductionPercent(multiplier);

        object buildSelection = BuildSelectionField?.GetValue(hud);
        object pieceDescription = PieceDescriptionField?.GetValue(hud);
        PropertyInfo buildTextProperty = buildSelection?.GetType().GetProperty("text");
        PropertyInfo descriptionTextProperty = pieceDescription?.GetType().GetProperty("text");
        if (buildTextProperty != null && buildTextProperty.CanWrite)
        {
            buildTextProperty.SetValue(buildSelection, $"{Localization.instance.Localize(piece.m_name)}\n{GetWoodLabel(woodType)} + {GetMetalLabel(metalType)}");
        }

        if (descriptionTextProperty != null && descriptionTextProperty.CanWrite)
        {
            descriptionTextProperty.SetValue(
                pieceDescription,
                $"{Localization.instance.Localize(piece.m_description)}\n" +
                $"Max support: {maxSupport:0}\n" +
                $"{FormatBonusPercent(bonusPercent)}% support loss (Crafting {skillLevel:0})\n" +
                $"Cost: {GetWoodCostLabel(woodType)}, {GetMetalCostLabel(metalType)}\n" +
                $"[{StructuralWallHotkeys.FormatHint(ModConfig.CycleStructuralWoodKey.Value)}: wood] [{StructuralWallHotkeys.FormatHint(ModConfig.CycleStructuralMetalKey.Value)}: metal]");
        }

        UpdateRequirementSlots(hud, player, woodType, metalType);
    }

    internal static void AppendPlacedHoverText(Piece hoveringPiece, ref string currentText)
    {
        if (!IsEnabled() || !StructuralWallPrefabs.IsStructuralWall(hoveringPiece))
        {
            return;
        }

        WearNTear wear = hoveringPiece.GetComponent<WearNTear>();
        ZDO zdo = wear?.GetComponent<ZNetView>()?.GetZDO();
        if (zdo == null)
        {
            return;
        }

        StructuralWoodType woodType = GetStoredWoodType(zdo);
        StructuralMetalType metalType = GetStoredMetalType(zdo);
        float maxSupport = 0f;
        float horizontalLoss = 0f;
        float verticalLoss = 0f;
        ApplyWoodBaseline(woodType, ref maxSupport, ref horizontalLoss, ref verticalLoss);
        ApplyMetalBonus(metalType, ref maxSupport, ref horizontalLoss, ref verticalLoss);

        currentText = string.IsNullOrWhiteSpace(currentText)
            ? $"{GetWoodLabel(woodType)} + {GetMetalLabel(metalType)}\nMax support: {maxSupport:0}"
            : $"{currentText}\n{GetWoodLabel(woodType)} + {GetMetalLabel(metalType)}\nMax support: {maxSupport:0}";
    }

    private static void ResetPendingSelection()
    {
        s_pendingWood = StructuralWoodType.Wood;
        s_pendingMetal = StructuralMetalType.BronzeNails;
    }

    private static void TrackGhostIdentity(GameObject ghost)
    {
        string ghostName = ghost.name;
        if (s_activeGhostName != ghostName)
        {
            s_activeGhostName = ghostName;
            ResetPendingSelection();
            RefreshGhostVisual(Player.m_localPlayer);
            DebugLog($"Structural Wall ghost identity changed: '{DescribeGameObject(ghost)}'");
        }
    }

    private static void RefreshGhostVisual(Player player)
    {
        GameObject ghost = GetPlacementGhost(player);
        if (ghost == null)
        {
            return;
        }

        StructuralWallPrefabs.ApplyVisualVariant(ghost, s_pendingWood, s_pendingMetal);
    }

    private static void RefreshBuildHud(Player player)
    {
        if (Hud.instance == null || player == null)
        {
            return;
        }

        Piece selectedPiece = player.GetSelectedPiece();
        if (StructuralWallPrefabs.IsStructuralWall(selectedPiece))
        {
            AppendBuildHudInfo(Hud.instance, selectedPiece);
        }
    }

    private static void GetPendingSelection(Player player, out StructuralWoodType woodType, out StructuralMetalType metalType)
    {
        if (IsStructuralWallSelected(player) && GetPlacementGhost(player) != null)
        {
            woodType = s_pendingWood;
            metalType = s_pendingMetal;
            return;
        }

        woodType = StructuralWoodType.Wood;
        metalType = StructuralMetalType.BronzeNails;
    }

    private static bool IsStructuralWallSelected(Player player)
    {
        return StructuralWallPrefabs.IsStructuralWall(player?.GetSelectedPiece());
    }

    private static GameObject GetPlacementGhost(Player player)
    {
        return PlacementGhostField?.GetValue(player) as GameObject;
    }

    private static bool PassesVanillaPlacementGates(Player player, Piece piece, Player.RequirementMode mode)
    {
        if (piece.m_craftingStation != null)
        {
            if (mode == Player.RequirementMode.IsKnown || mode == Player.RequirementMode.CanAlmostBuild)
            {
                if (KnownStationsField?.GetValue(player) is System.Collections.Generic.Dictionary<string, int> knownStations &&
                    !knownStations.ContainsKey(piece.m_craftingStation.m_name))
                {
                    return false;
                }
            }
            else if (!CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, player.transform.position) &&
                     !ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoWorkbench))
            {
                return false;
            }
        }

        if (piece.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(piece.m_dlc))
        {
            return false;
        }

        if (mode != Player.RequirementMode.IsKnown && ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey()))
        {
            return true;
        }

        return true;
    }

    private static bool HasDynamicResources(
        Player player,
        StructuralWoodType woodType,
        StructuralMetalType metalType,
        Player.RequirementMode mode)
    {
        string woodItem = StructuralWallPrefabs.GetWoodItemName(woodType);
        string metalItem = GetMetalItemName(metalType);
        int woodAmount = StructuralWallPrefabs.GetWoodCost(woodType);
        int metalAmount = GetMetalCost(metalType);

        return CheckRequirement(player, GetItemSharedName(woodItem), woodAmount, mode) &&
               CheckRequirement(player, GetItemSharedName(metalItem), metalAmount, mode);
    }

    private static bool CheckRequirement(Player player, string itemName, int amount, Player.RequirementMode mode)
    {
        switch (mode)
        {
            case Player.RequirementMode.IsKnown:
                return KnownMaterialField?.GetValue(player) is HashSet<string> knownMaterials &&
                       knownMaterials.Contains(itemName);
            case Player.RequirementMode.CanAlmostBuild:
                return player.GetInventory().HaveItem(itemName);
            default:
                return player.GetInventory().CountItems(itemName) >= amount;
        }
    }

    private static void ConsumeDynamicResources(Player player, StructuralWoodType woodType, StructuralMetalType metalType)
    {
        string woodItem = GetItemSharedName(StructuralWallPrefabs.GetWoodItemName(woodType));
        string metalItem = GetItemSharedName(GetMetalItemName(metalType));
        player.GetInventory().RemoveItem(woodItem, StructuralWallPrefabs.GetWoodCost(woodType));
        player.GetInventory().RemoveItem(metalItem, GetMetalCost(metalType));
    }

    private static string GetItemSharedName(string itemPrefabName)
    {
        if (ObjectDB.instance == null)
        {
            return itemPrefabName;
        }

        GameObject prefab = ObjectDB.instance.GetItemPrefab(itemPrefabName);
        ItemDrop itemDrop = prefab?.GetComponent<ItemDrop>();
        return itemDrop?.m_itemData?.m_shared?.m_name ?? itemPrefabName;
    }

    private static void ApplyWoodBaseline(
        StructuralWoodType woodType,
        ref float maxSupport,
        ref float horizontalLoss,
        ref float verticalLoss)
    {
        if (woodType == StructuralWoodType.CoreWood)
        {
            maxSupport = 140f;
            horizontalLoss = 0.166667f;
            verticalLoss = 0.1f;
            return;
        }

        maxSupport = 100f;
        horizontalLoss = 0.2f;
        verticalLoss = 0.125f;
    }

    private static void ApplyMetalBonus(
        StructuralMetalType metalType,
        ref float maxSupport,
        ref float horizontalLoss,
        ref float verticalLoss)
    {
        float multiplier = metalType == StructuralMetalType.IronNails ? IronMetalMultiplier : BronzeMetalMultiplier;
        maxSupport *= multiplier;

        if (metalType == StructuralMetalType.IronNails)
        {
            horizontalLoss = Mathf.Min(horizontalLoss, IronHorizontalLossCap);
            verticalLoss = Mathf.Min(verticalLoss, IronVerticalLossCap);
            return;
        }

        horizontalLoss = Mathf.Min(horizontalLoss, BronzeHorizontalLossCap);
        verticalLoss = Mathf.Min(verticalLoss, BronzeVerticalLossCap);
    }

    private static StructuralWoodType GetStoredWoodType(ZDO zdo)
    {
        if (zdo == null || !zdo.GetInt(ModConstants.ZdoWoodTypeHash, out int storedWood))
        {
            return StructuralWoodType.Wood;
        }

        return storedWood == (int)StructuralWoodType.CoreWood
            ? StructuralWoodType.CoreWood
            : StructuralWoodType.Wood;
    }

    private static StructuralMetalType GetStoredMetalType(ZDO zdo)
    {
        if (zdo == null || !zdo.GetInt(ModConstants.ZdoMetalTypeHash, out int storedMetal))
        {
            return StructuralMetalType.BronzeNails;
        }

        return storedMetal == (int)StructuralMetalType.IronNails
            ? StructuralMetalType.IronNails
            : StructuralMetalType.BronzeNails;
    }

    private static float GetPreviewMaxSupport(StructuralWoodType woodType, StructuralMetalType metalType)
    {
        float maxSupport = 0f;
        float horizontalLoss = 0f;
        float verticalLoss = 0f;
        ApplyWoodBaseline(woodType, ref maxSupport, ref horizontalLoss, ref verticalLoss);
        ApplyMetalBonus(metalType, ref maxSupport, ref horizontalLoss, ref verticalLoss);
        return maxSupport;
    }

    private static void UpdateRequirementSlots(Hud hud, Player player, StructuralWoodType woodType, StructuralMetalType metalType)
    {
        if (RequirementItemsField?.GetValue(hud) is not GameObject[] requirementItems)
        {
            return;
        }

        Piece.Requirement woodRequirement = CreateRequirement(StructuralWallPrefabs.GetWoodItemName(woodType), StructuralWallPrefabs.GetWoodCost(woodType));
        Piece.Requirement metalRequirement = CreateRequirement(GetMetalItemName(metalType), GetMetalCost(metalType));

        if (requirementItems.Length > 0)
        {
            requirementItems[0].SetActive(true);
            InventoryGui.SetupRequirement(requirementItems[0].transform, woodRequirement, player, false, 0);
        }

        if (requirementItems.Length > 1)
        {
            requirementItems[1].SetActive(true);
            InventoryGui.SetupRequirement(requirementItems[1].transform, metalRequirement, player, false, 0);
        }

        for (int i = 2; i < requirementItems.Length; i++)
        {
            requirementItems[i].SetActive(false);
        }
    }

    private static Piece.Requirement CreateRequirement(string itemName, int amount)
    {
        GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemName);
        return new Piece.Requirement
        {
            m_resItem = itemPrefab != null ? itemPrefab.GetComponent<ItemDrop>() : null,
            m_amount = amount,
            m_amountPerLevel = 0
        };
    }

    private static string GetMetalItemName(StructuralMetalType metalType)
    {
        return metalType == StructuralMetalType.IronNails ? IronNailsItem : BronzeNailsItem;
    }

    private static int GetMetalCost(StructuralMetalType metalType)
    {
        return metalType == StructuralMetalType.IronNails ? IronNailsCost : BronzeNailsCost;
    }

    private static string GetWoodLabel(StructuralWoodType woodType)
    {
        return woodType == StructuralWoodType.CoreWood ? "Core Wood" : "Wood";
    }

    private static string GetMetalLabel(StructuralMetalType metalType)
    {
        return metalType == StructuralMetalType.IronNails ? "Iron Nails" : "Bronze Nails";
    }

    private static string GetWoodCostLabel(StructuralWoodType woodType)
    {
        return $"{StructuralWallPrefabs.GetWoodCost(woodType)} {GetWoodLabel(woodType)}";
    }

    private static string GetMetalCostLabel(StructuralMetalType metalType)
    {
        return $"{GetMetalCost(metalType)} {GetMetalLabel(metalType)}";
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

    private static float GetSupportLossReductionPercent(float multiplier)
    {
        return multiplier <= 0f ? 0f : (1f - 1f / multiplier) * 100f;
    }

    private static string FormatBonusPercent(float value)
    {
        return Mathf.Abs(value - Mathf.Round(value)) < 0.05f
            ? value.ToString("+0;-0;0")
            : value.ToString("+0.#;-0.#;0");
    }

    private static bool IsEnabled()
    {
        return ModConfig.EnableMod?.Value == true;
    }

    private static void DebugLog(string message)
    {
        if (ModConfig.DebugLogging?.Value == true)
        {
            DvergrCraftsmanshipPlugin.Log?.LogInfo(message);
        }
    }
}
