using MelonLoader;
using PvZReCoreLib;
using PvZReCoreLib.Console;
using PvZReCoreLib.Content;
using PvZReCoreLib.Content.Common.Skins;
using PvZReCoreLib.Content.Plants.Patches;
using PvZReCoreLib.Util;
using UnityEngine.SceneManagement;

[assembly: MelonInfo(typeof(CoreLibMod), "CoreLib", "0.2", "Draco9990")]
[assembly: MelonGame("PopCap Games", "PvZ Replanted")]

namespace PvZReCoreLib;

public class CoreLibMod : MelonMod
{
    #region Variables

    public static string ModId => "ReCodeLib";

    private static bool Initd = false;

    public static Action OnCoreLibInit;

    #endregion

    #region Constructors



    #endregion

    #region Methods

    public override void OnGUI()
    {
        base.OnGUI();

        if (!Initd)
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name == "Frontend")
            {
                FirstTimeInit();
            }
        }
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        
        DebugConsole.Update();
    }

    public void FirstTimeInit()
    {
        Initd = true;
        
        DebugConsole.Start();
        
        PersistentStorage.Init();

        RegistryBridge.Init();

        ReLocalizer.Init();

        SkinRegistry.Init();
        CustomContentRegistry.Init();

        MelonLoader.MelonLogger.Msg($"[CoreLib] Registered {CustomContentRegistry.GetAllCustomPlantTypes().Count} custom plant(s) before Frontend init.");

        try
        {
            AlmanacModel_CustomPlantEntriesPatch.Patch();
        }
        catch (Exception e)
        {
            MelonLoader.MelonLogger.Warning($"[CoreLib] AlmanacModel_CustomPlantEntriesPatch.Patch() failed (likely because AlmanacModel hasn't run yet) - will retry on first Almanac open: {e}");
        }

        RegistryBridge.RegisterAssetBundle(ModId, "Mods/CoreLib/pvzcorelibassetbundle");

        OnCoreLibInit?.Invoke();
    }

    #endregion
}