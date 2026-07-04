namespace DvergrCraftsmanship;

internal static class ModConstants
{
    internal const string ModName = "Dvergr Craftsmanship";
    internal const string ModVersion = "0.1.0";
    internal const string ModGuid = "com.cdjensen.dvergrcraftsmanship";
    internal const string ConfigFolder = "DvergrCraftsmanship";

    internal const string ZdoIntegrityMultiplier = "DvergrCraft_IntegrityMult";
    internal const string ZdoCraftSkill = "DvergrCraft_CraftSkill";
    internal const string RpcReinforce = "DvergrCraft_Reinforce";

    internal static readonly int ZdoIntegrityMultiplierHash = ZdoIntegrityMultiplier.GetStableHashCode();
    internal static readonly int ZdoCraftSkillHash = ZdoCraftSkill.GetStableHashCode();
}
