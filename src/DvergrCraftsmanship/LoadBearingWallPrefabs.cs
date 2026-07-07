using System;
using System.Collections.Generic;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace DvergrCraftsmanship;

internal static class LoadBearingWallPrefabs
{
    private const string WoodItem = "Wood";
    private const string BronzeNailsItem = "BronzeNails";
    private const string HammerPieceTable = "Hammer";
    private const float MaxSupportMultiplier = 1.4f;
    private const float HorizontalLossCap = 1f / 6f;
    private const float VerticalLossCap = 0.1f;

    private static readonly LoadBearingPieceDefinition[] PieceDefinitions =
    {
        new LoadBearingPieceDefinition(
            ModConstants.LoadBearingWoodWallPrefab,
            "woodwall",
            ModConstants.LoadBearingWoodWallName,
            ModConstants.LoadBearingWoodWallDescription,
            fallbackWoodCost: 2,
            bronzeNailsCost: 2),
        new LoadBearingPieceDefinition(
            ModConstants.LoadBearingWoodHalfWallPrefab,
            "wood_wall_half",
            ModConstants.LoadBearingWoodHalfWallName,
            ModConstants.LoadBearingWoodHalfWallDescription,
            fallbackWoodCost: 1,
            bronzeNailsCost: 1),
        new LoadBearingPieceDefinition(
            ModConstants.LoadBearingWoodQuarterWallPrefab,
            "wood_wall_quarter",
            ModConstants.LoadBearingWoodQuarterWallName,
            ModConstants.LoadBearingWoodQuarterWallDescription,
            fallbackWoodCost: 1,
            bronzeNailsCost: 1)
    };

    private static readonly Dictionary<string, LoadBearingPieceDefinition> DefinitionsByPrefabName = CreateDefinitionLookup();
    private static readonly Dictionary<string, GameObject> PrefabsByName = new Dictionary<string, GameObject>();
    private static readonly HashSet<string> RegisteredPieces = new HashSet<string>();
    private static bool registeredPrefab;

    internal static void Initialize()
    {
        PrefabManager.OnVanillaPrefabsAvailable += RegisterPrefab;
        PieceManager.OnPiecesRegistered += RegisterPiece;
        PrefabManager.OnPrefabsRegistered += RegisterPiece;
    }

    internal static bool TryApplyLoadBearingStats(WearNTear wear, ref float maxSupport, ref float horizontalLoss, ref float verticalLoss)
    {
        if (!TryGetDefinition(wear, out _))
        {
            return false;
        }

        maxSupport *= MaxSupportMultiplier;
        horizontalLoss = Mathf.Min(horizontalLoss, HorizontalLossCap);
        verticalLoss = Mathf.Min(verticalLoss, VerticalLossCap);
        return true;
    }

    private static void RegisterPrefab()
    {
        if (registeredPrefab)
        {
            return;
        }

        foreach (LoadBearingPieceDefinition definition in PieceDefinitions)
        {
            GameObject clone = PrefabManager.Instance.CreateClonedPrefab(
                definition.CustomPrefabName,
                definition.VanillaPrefabName);

            if (clone == null)
            {
                DvergrCraftsmanshipPlugin.Log?.LogError($"Failed to clone {definition.VanillaPrefabName} for {definition.CustomPrefabName}.");
                continue;
            }

            ConfigureClone(clone, definition);
            PrefabsByName[definition.CustomPrefabName] = clone;
            DvergrCraftsmanshipPlugin.Log?.LogInfo($"Registered prefab {definition.CustomPrefabName}.");
        }

        registeredPrefab = true;

        PrefabManager.OnVanillaPrefabsAvailable -= RegisterPrefab;
        RegisterPiece();
    }

    private static void RegisterPiece()
    {
        if (!registeredPrefab || PrefabsByName.Count == 0 || RegisteredPieces.Count == PrefabsByName.Count)
        {
            return;
        }

        foreach (LoadBearingPieceDefinition definition in PieceDefinitions)
        {
            if (RegisteredPieces.Contains(definition.CustomPrefabName) || !PrefabsByName.TryGetValue(definition.CustomPrefabName, out GameObject prefab))
            {
                continue;
            }

            try
            {
                PieceConfig pieceConfig = new PieceConfig
                {
                    Name = definition.DisplayName,
                    Description = definition.Description,
                    PieceTable = HammerPieceTable,
                    Icon = RenderManager.Instance.Render(prefab, RenderManager.IsometricRotation)
                };

                int woodCost = GetRequirementAmount(prefab, WoodItem, definition.FallbackWoodCost);
                pieceConfig.AddRequirement(WoodItem, woodCost);
                pieceConfig.AddRequirement(BronzeNailsItem, definition.BronzeNailsCost);

                PieceManager.Instance.AddPiece(new CustomPiece(prefab, false, pieceConfig));
                RegisteredPieces.Add(definition.CustomPrefabName);

                DvergrCraftsmanshipPlugin.Log?.LogInfo(
                    $"Registered {definition.DisplayName} in Hammer piece table ({woodCost} Wood, {definition.BronzeNailsCost} Bronze Nails).");
            }
            catch (Exception ex)
            {
                DvergrCraftsmanshipPlugin.Log?.LogWarning($"Deferring {definition.DisplayName} registration: {ex.Message}");
            }
        }

        if (RegisteredPieces.Count == PrefabsByName.Count)
        {
            PieceManager.OnPiecesRegistered -= RegisterPiece;
            PrefabManager.OnPrefabsRegistered -= RegisterPiece;
        }
    }

    private static void ConfigureClone(GameObject clone, LoadBearingPieceDefinition definition)
    {
        Piece piece = clone.GetComponent<Piece>();
        if (piece != null)
        {
            piece.m_name = definition.DisplayName;
            piece.m_description = definition.Description;
        }

        AddDecorativeBracing(clone, definition);
    }

    private static void AddDecorativeBracing(GameObject clone, LoadBearingPieceDefinition definition)
    {
        if (!TryGetLocalRendererBounds(clone, out Bounds bounds))
        {
            DvergrCraftsmanshipPlugin.Log?.LogWarning($"Unable to compute {definition.DisplayName} bounds; using conservative decorative brace placement.");
            bounds = new Bounds(new Vector3(0f, 1f, 0f), new Vector3(2f, 2f, 0.2f));
        }

        Material woodMaterial = FindFirstMaterial(clone);
        Material braceMaterial = CreateTintedMaterial(woodMaterial, new Color(0.72f, 0.52f, 0.34f, 1f));
        Material bronzeMaterial = CreateTintedMaterial(woodMaterial, new Color(0.82f, 0.46f, 0.18f, 1f));

        float width = Mathf.Max(0.5f, bounds.size.x);
        float height = Mathf.Max(0.5f, bounds.size.y);
        float braceSpanX = width * 0.74f;
        // Keep bracing inside the wall's top/bottom rails instead of crossing over them.
        float braceSpanY = height * 0.56f;
        float braceLength = Mathf.Sqrt(braceSpanX * braceSpanX + braceSpanY * braceSpanY);
        float braceAngle = Mathf.Atan2(braceSpanX, braceSpanY) * Mathf.Rad2Deg;
        float braceThickness = Mathf.Max(0.06f, Mathf.Min(width, height) * 0.055f);
        float braceDepth = Mathf.Max(0.06f, bounds.size.z * 0.18f);
        const float surfaceEmbed = 0.01f;
        float frontFaceZ = bounds.min.z;
        float braceCenterZ = frontFaceZ - (braceDepth * 0.5f) + surfaceEmbed;
        Vector3 center = bounds.center;

        AddBrace(clone.transform, "Dvergr_Brace_A", new Vector3(center.x, center.y, braceCenterZ), new Vector3(braceThickness, braceLength, braceDepth), braceAngle, braceMaterial);
        AddBrace(clone.transform, "Dvergr_Brace_B", new Vector3(center.x, center.y, braceCenterZ - 0.006f), new Vector3(braceThickness, braceLength, braceDepth), -braceAngle, braceMaterial);

        bool compactWall = height < 1.3f;
        if (!compactWall)
        {
            AddBrace(clone.transform, "Dvergr_Center_Rib", new Vector3(center.x, center.y, braceCenterZ - 0.009f), new Vector3(width * 0.82f, braceThickness, braceDepth), 0f, braceMaterial);
        }

        float plateX = width * 0.34f;
        float plateY = compactWall ? height * 0.24f : height * 0.32f;
        float topY = center.y + plateY;
        float bottomY = center.y - plateY;
        Vector3 plateScale = new Vector3(Mathf.Max(0.08f, width * 0.095f), Mathf.Max(0.08f, height * 0.095f), braceDepth * 0.55f);
        float plateZ = frontFaceZ - (plateScale.z * 0.5f) + surfaceEmbed;

        AddBronzePlate(clone.transform, "Dvergr_Nail_TopLeft", new Vector3(center.x - plateX, topY, plateZ), plateScale, bronzeMaterial);
        AddBronzePlate(clone.transform, "Dvergr_Nail_TopRight", new Vector3(center.x + plateX, topY, plateZ), plateScale, bronzeMaterial);
        AddBronzePlate(clone.transform, "Dvergr_Nail_BottomLeft", new Vector3(center.x - plateX, bottomY, plateZ), plateScale, bronzeMaterial);
        AddBronzePlate(clone.transform, "Dvergr_Nail_BottomRight", new Vector3(center.x + plateX, bottomY, plateZ), plateScale, bronzeMaterial);

        if (!compactWall)
        {
            AddBronzePlate(clone.transform, "Dvergr_Nail_MidLeft", new Vector3(center.x - plateX * 0.78f, center.y, plateZ), plateScale, bronzeMaterial);
            AddBronzePlate(clone.transform, "Dvergr_Nail_MidRight", new Vector3(center.x + plateX * 0.78f, center.y, plateZ), plateScale, bronzeMaterial);
        }
    }

    private static void AddBrace(Transform parent, string name, Vector3 localPosition, Vector3 localScale, float zRotation, Material material)
    {
        GameObject brace = GameObject.CreatePrimitive(PrimitiveType.Cube);
        brace.name = name;
        brace.transform.SetParent(parent, worldPositionStays: false);
        brace.transform.localPosition = localPosition;
        brace.transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
        brace.transform.localScale = localScale;
        StripCollider(brace);
        SetMaterial(brace, material);
    }

    private static void AddBronzePlate(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plate.name = name;
        plate.transform.SetParent(parent, worldPositionStays: false);
        plate.transform.localPosition = localPosition;
        plate.transform.localRotation = Quaternion.identity;
        plate.transform.localScale = localScale;
        StripCollider(plate);
        SetMaterial(plate, material);
    }

    private static bool TryGetLocalRendererBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
        bool hasBounds = false;
        bounds = default;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Bounds localBounds = WorldToLocalBounds(root.transform, renderer.bounds);
            if (!hasBounds)
            {
                bounds = localBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(localBounds.min);
                bounds.Encapsulate(localBounds.max);
            }
        }

        return hasBounds;
    }

    private static Bounds WorldToLocalBounds(Transform root, Bounds worldBounds)
    {
        Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        Vector3 worldMin = worldBounds.min;
        Vector3 worldMax = worldBounds.max;

        for (int x = 0; x <= 1; x++)
        {
            for (int y = 0; y <= 1; y++)
            {
                for (int z = 0; z <= 1; z++)
                {
                    Vector3 corner = new Vector3(
                        x == 0 ? worldMin.x : worldMax.x,
                        y == 0 ? worldMin.y : worldMax.y,
                        z == 0 ? worldMin.z : worldMax.z);
                    Vector3 local = root.InverseTransformPoint(corner);
                    min = Vector3.Min(min, local);
                    max = Vector3.Max(max, local);
                }
            }
        }

        Bounds result = new Bounds((min + max) * 0.5f, max - min);
        return result;
    }

    private static Material FindFirstMaterial(GameObject root)
    {
        Renderer renderer = root.GetComponentInChildren<Renderer>(includeInactive: true);
        return renderer != null ? renderer.sharedMaterial : null;
    }

    private static Material CreateTintedMaterial(Material source, Color color)
    {
        Material material = source != null
            ? new Material(source)
            : new Material(Shader.Find("Standard"));

        material.color = color;
        return material;
    }

    private static void SetMaterial(GameObject target, Material material)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private static void StripCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider != null)
        {
            UnityEngine.Object.DestroyImmediate(collider);
        }
    }

    private static bool TryGetDefinition(WearNTear wear, out LoadBearingPieceDefinition definition)
    {
        definition = null;
        if (wear == null)
        {
            return false;
        }

        string prefabName = Utils.GetPrefabName(wear.gameObject.name);
        return DefinitionsByPrefabName.TryGetValue(prefabName, out definition);
    }

    private static Dictionary<string, LoadBearingPieceDefinition> CreateDefinitionLookup()
    {
        Dictionary<string, LoadBearingPieceDefinition> definitions = new Dictionary<string, LoadBearingPieceDefinition>();
        foreach (LoadBearingPieceDefinition definition in PieceDefinitions)
        {
            definitions[definition.CustomPrefabName] = definition;
        }

        return definitions;
    }

    private static int GetRequirementAmount(GameObject prefab, string itemName, int fallbackAmount)
    {
        Piece piece = prefab.GetComponent<Piece>();
        if (piece?.m_resources == null)
        {
            return fallbackAmount;
        }

        foreach (Piece.Requirement requirement in piece.m_resources)
        {
            if (requirement?.m_resItem == null)
            {
                continue;
            }

            string prefabName = Utils.GetPrefabName(requirement.m_resItem.name);
            if (prefabName == itemName)
            {
                return Mathf.Max(1, requirement.m_amount);
            }
        }

        return fallbackAmount;
    }

    private sealed class LoadBearingPieceDefinition
    {
        internal readonly string CustomPrefabName;
        internal readonly string VanillaPrefabName;
        internal readonly string DisplayName;
        internal readonly string Description;
        internal readonly int FallbackWoodCost;
        internal readonly int BronzeNailsCost;

        internal LoadBearingPieceDefinition(
            string customPrefabName,
            string vanillaPrefabName,
            string displayName,
            string description,
            int fallbackWoodCost,
            int bronzeNailsCost)
        {
            CustomPrefabName = customPrefabName;
            VanillaPrefabName = vanillaPrefabName;
            DisplayName = displayName;
            Description = description;
            FallbackWoodCost = fallbackWoodCost;
            BronzeNailsCost = bronzeNailsCost;
        }
    }
}
