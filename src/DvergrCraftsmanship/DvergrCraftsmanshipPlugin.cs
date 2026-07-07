using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn;

namespace DvergrCraftsmanship;

[BepInPlugin(ModConstants.ModGuid, ModConstants.ModName, ModConstants.ModVersion)]
[BepInDependency(Main.ModGuid)]
public sealed class DvergrCraftsmanshipPlugin : BaseUnityPlugin
{
    internal static ManualLogSource Log;

    private Harmony harmony;

    private void Awake()
    {
        Log = Logger;
        ModConfig.Bind(Config);
        LoadBearingWallPrefabs.Initialize();
        StructuralWallPrefabs.Initialize();

        harmony = new Harmony(ModConstants.ModGuid);
        harmony.PatchAll(typeof(DvergrCraftsmanshipPlugin).Assembly);

        Log.LogInfo($"{ModConstants.ModName} {ModConstants.ModVersion} loaded.");
    }

    private void OnDestroy()
    {
        harmony?.UnpatchSelf();
    }
}
