using MelonLoader;
using PvZReCoreLib;
using PvZReCoreLib.Content;
using PvZReCoreLib.Content.Common.Skins;
using PvZReCoreLib.Content.Plants.Patches;
using PvZReCoreLib.Util;
using UnityEngine.SceneManagement;

[assembly: MelonInfo(typeof(CoreLibMod), "CoreLib", "0.1", "Draco9990")]
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

    public void FirstTimeInit()
    {
        Initd = true;
        
        PersistentStorage.Init();
        
        RegistryBridge.Init();
        
        ReLocalizer.Init();
        
        SkinRegistry.Init();
        CustomContentRegistry.Init();
        
        AlmanacModel_CustomPlantEntriesPatch.Patch();
        
        RegistryBridge.RegisterAssetBundle(ModId, "Mods/CoreLib/pvzcorelibassetbundle");
        
        OnCoreLibInit?.Invoke();
    }


    #endregion
}