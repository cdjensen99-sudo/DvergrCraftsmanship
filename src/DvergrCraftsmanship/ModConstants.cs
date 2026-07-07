namespace DvergrCraftsmanship;

internal static class ModConstants
{
    internal const string ModName = "Dvergr Craftsmanship";
    internal const string ModVersion = "0.1.0.1";
    internal const string ModGuid = "com.cdjensen.dvergrcraftsmanship";
    internal const string ConfigFolder = "DvergrCraftsmanship";
    internal const string AnalysisConfigFolder = "Analysis";

    internal const string ZdoIntegrityMultiplier = "DvergrCraft_IntegrityMult";
    internal const string ZdoCraftSkill = "DvergrCraft_CraftSkill";
    internal const string RpcReinforce = "DvergrCraft_Reinforce";

    internal const string LoadBearingWoodWallPrefab = "dvergr_load_bearing_wood_wall";
    internal const string LoadBearingWoodWallName = "Load-Bearing Wood Wall";
    internal const string LoadBearingWoodWallDescription = "A bronze-fastened wall braced for stronger structural support.";
    internal const string LoadBearingWoodHalfWallPrefab = "dvergr_load_bearing_wood_half_wall";
    internal const string LoadBearingWoodHalfWallName = "Load-Bearing Wood Half Wall";
    internal const string LoadBearingWoodHalfWallDescription = "A half-height bronze-fastened wall braced for stronger structural support.";
    internal const string LoadBearingWoodQuarterWallPrefab = "dvergr_load_bearing_wood_quarter_wall";
    internal const string LoadBearingWoodQuarterWallName = "Load-Bearing Wood 1x1 Wall";
    internal const string LoadBearingWoodQuarterWallDescription = "A compact bronze-fastened wall braced for stronger structural support.";

    internal const string StructuralWallPrefab = "dvergr_structural_wall";
    internal const string StructuralWallName = "Structural Wall";
    internal const string StructuralWallDescription = "A configurable braced wall. Cycle wood and metal with hotkeys while placing.";

    internal const string ZdoWoodType = "DvergrCraft_WoodType";
    internal const string ZdoMetalType = "DvergrCraft_MetalType";

    internal static readonly int ZdoWoodTypeHash = ZdoWoodType.GetStableHashCode();
    internal static readonly int ZdoMetalTypeHash = ZdoMetalType.GetStableHashCode();

    internal static readonly int ZdoIntegrityMultiplierHash = ZdoIntegrityMultiplier.GetStableHashCode();
    internal static readonly int ZdoCraftSkillHash = ZdoCraftSkill.GetStableHashCode();
}
