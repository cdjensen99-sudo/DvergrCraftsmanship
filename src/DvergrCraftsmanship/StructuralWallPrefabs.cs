using System;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace DvergrCraftsmanship;

internal static class StructuralWallPrefabs
{
    private const string WoodItem = "Wood";
    private const string RoundLogItem = "RoundLog";
    private const string BronzeNailsItem = "BronzeNails";
    private const string HammerPieceTable = "Hammer";
    private const string VanillaWallPrefab = "woodwall";
    private const int FallbackWoodCost = 2;
    private const int FallbackRoundLogCost = 2;
    private const int DefaultBronzeNailsCost = 2;

    private static GameObject structuralWallPrefab;
    private static bool registeredPrefab;
    private static bool registeredPiece;

    internal static void Initialize()
    {
        PrefabManager.OnVanillaPrefabsAvailable += RegisterPrefab;
        PieceManager.OnPiecesRegistered += RegisterPiece;
        PrefabManager.OnPrefabsRegistered += RegisterPiece;
    }

    internal static bool IsStructuralWall(Piece piece)
    {
        if (piece == null)
        {
            return false;
        }

        return Utils.GetPrefabName(piece.gameObject.name) == ModConstants.StructuralWallPrefab;
    }

    internal static bool IsStructuralWall(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return false;
        }

        return Utils.GetPrefabName(gameObject.name) == ModConstants.StructuralWallPrefab;
    }

    internal static void ApplyVisualVariant(GameObject target, StructuralWoodType woodType, StructuralMetalType metalType)
    {
        if (target == null)
        {
            return;
        }

        Material woodMaterial = FindFirstMaterial(target);
        Material braceMaterial = CreateTintedMaterial(woodMaterial, GetWoodTint(woodType));
        Material metalMaterial = CreateTintedMaterial(woodMaterial, GetMetalTint(metalType));

        foreach (Transform child in target.GetComponentsInChildren<Transform>(includeInactive: true))
        {
            if (child == null || child == target.transform)
            {
                continue;
            }

            string childName = child.name;
            if (!childName.StartsWith("Dvergr_", StringComparison.Ordinal))
            {
                continue;
            }

            Material material = childName.IndexOf("Nail", StringComparison.Ordinal) >= 0 ? metalMaterial : braceMaterial;
            SetMaterial(child.gameObject, material);
        }
    }

    internal static int GetWoodCost(StructuralWoodType woodType)
    {
        return woodType == StructuralWoodType.CoreWood ? FallbackRoundLogCost : FallbackWoodCost;
    }

    internal static string GetWoodItemName(StructuralWoodType woodType)
    {
        return woodType == StructuralWoodType.CoreWood ? RoundLogItem : WoodItem;
    }

    private static void RegisterPrefab()
    {
        if (registeredPrefab)
        {
            return;
        }

        GameObject clone = PrefabManager.Instance.CreateClonedPrefab(
            ModConstants.StructuralWallPrefab,
            VanillaWallPrefab);

        if (clone == null)
        {
            DvergrCraftsmanshipPlugin.Log?.LogError($"Failed to clone {VanillaWallPrefab} for {ModConstants.StructuralWallPrefab}.");
            return;
        }

        ConfigureClone(clone);
        structuralWallPrefab = clone;
        registeredPrefab = true;

        PrefabManager.OnVanillaPrefabsAvailable -= RegisterPrefab;
        DvergrCraftsmanshipPlugin.Log?.LogInfo($"Registered prefab {ModConstants.StructuralWallPrefab}.");
        RegisterPiece();
    }

    private static void RegisterPiece()
    {
        if (!registeredPrefab || structuralWallPrefab == null || registeredPiece)
        {
            return;
        }

        try
        {
            int woodCost = GetRequirementAmount(structuralWallPrefab, WoodItem, FallbackWoodCost);
            PieceConfig pieceConfig = new PieceConfig
            {
                Name = ModConstants.StructuralWallName,
                Description = ModConstants.StructuralWallDescription,
                PieceTable = HammerPieceTable,
                Icon = RenderManager.Instance.Render(structuralWallPrefab, RenderManager.IsometricRotation)
            };

            pieceConfig.AddRequirement(WoodItem, woodCost);
            pieceConfig.AddRequirement(BronzeNailsItem, DefaultBronzeNailsCost);

            if (!PieceManager.Instance.AddPiece(new CustomPiece(structuralWallPrefab, false, pieceConfig)))
            {
                DvergrCraftsmanshipPlugin.Log?.LogError($"Failed to queue {ModConstants.StructuralWallName} with Jotunn PieceManager.");
                return;
            }

            registeredPiece = true;

            DvergrCraftsmanshipPlugin.Log?.LogInfo(
                $"Registered {ModConstants.StructuralWallName} in Hammer piece table ({woodCost} Wood, {DefaultBronzeNailsCost} Bronze Nails default recipe).");

            PieceManager.OnPiecesRegistered -= RegisterPiece;
            PrefabManager.OnPrefabsRegistered -= RegisterPiece;
        }
        catch (Exception ex)
        {
            DvergrCraftsmanshipPlugin.Log?.LogWarning($"Deferring {ModConstants.StructuralWallName} registration: {ex.Message}");
        }
    }

    private static void ConfigureClone(GameObject clone)
    {
        Piece piece = clone.GetComponent<Piece>();
        if (piece != null)
        {
            piece.m_name = ModConstants.StructuralWallName;
            piece.m_description = ModConstants.StructuralWallDescription;
        }

        AddDecorativeBracing(clone);
        ApplyVisualVariant(clone, StructuralWoodType.Wood, StructuralMetalType.BronzeNails);
    }

    private static Color GetWoodTint(StructuralWoodType woodType)
    {
        return woodType == StructuralWoodType.CoreWood
            ? new Color(0.55f, 0.38f, 0.28f, 1f)
            : new Color(0.72f, 0.52f, 0.34f, 1f);
    }

    private static Color GetMetalTint(StructuralMetalType metalType)
    {
        return metalType == StructuralMetalType.IronNails
            ? new Color(0.65f, 0.68f, 0.72f, 1f)
            : new Color(0.82f, 0.46f, 0.18f, 1f);
    }

    private static void AddDecorativeBracing(GameObject clone)
    {
        if (!TryGetLocalRendererBounds(clone, out Bounds bounds))
        {
            DvergrCraftsmanshipPlugin.Log?.LogWarning($"Unable to compute {ModConstants.StructuralWallName} bounds; using conservative decorative brace placement.");
            bounds = new Bounds(new Vector3(0f, 1f, 0f), new Vector3(2f, 2f, 0.2f));
        }

        Material woodMaterial = FindFirstMaterial(clone);
        Material braceMaterial = CreateTintedMaterial(woodMaterial, GetWoodTint(StructuralWoodType.Wood));
        Material bronzeMaterial = CreateTintedMaterial(woodMaterial, GetMetalTint(StructuralMetalType.BronzeNails));

        float width = Mathf.Max(0.5f, bounds.size.x);
        float height = Mathf.Max(0.5f, bounds.size.y);
        float braceSpanX = width * 0.74f;
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
        AddBrace(clone.transform, "Dvergr_Center_Rib", new Vector3(center.x, center.y, braceCenterZ - 0.009f), new Vector3(width * 0.82f, braceThickness, braceDepth), 0f, braceMaterial);

        float plateX = width * 0.34f;
        float plateY = height * 0.32f;
        float topY = center.y + plateY;
        float bottomY = center.y - plateY;
        Vector3 plateScale = new Vector3(Mathf.Max(0.08f, width * 0.095f), Mathf.Max(0.08f, height * 0.095f), braceDepth * 0.55f);
        float plateZ = frontFaceZ - (plateScale.z * 0.5f) + surfaceEmbed;

        AddBronzePlate(clone.transform, "Dvergr_Nail_TopLeft", new Vector3(center.x - plateX, topY, plateZ), plateScale, bronzeMaterial);
        AddBronzePlate(clone.transform, "Dvergr_Nail_TopRight", new Vector3(center.x + plateX, topY, plateZ), plateScale, bronzeMaterial);
        AddBronzePlate(clone.transform, "Dvergr_Nail_BottomLeft", new Vector3(center.x - plateX, bottomY, plateZ), plateScale, bronzeMaterial);
        AddBronzePlate(clone.transform, "Dvergr_Nail_BottomRight", new Vector3(center.x + plateX, bottomY, plateZ), plateScale, bronzeMaterial);
        AddBronzePlate(clone.transform, "Dvergr_Nail_MidLeft", new Vector3(center.x - plateX * 0.78f, center.y, plateZ), plateScale, bronzeMaterial);
        AddBronzePlate(clone.transform, "Dvergr_Nail_MidRight", new Vector3(center.x + plateX * 0.78f, center.y, plateZ), plateScale, bronzeMaterial);
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

        return new Bounds((min + max) * 0.5f, max - min);
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
}
