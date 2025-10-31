using HarmonyLib;
using Il2CppTekly.TreeState;

namespace PvZReCoreLib.Util;

public class ReplantedGet
{
    #region Variables
    
    private static TreeStateManager _treeStateManager;

    #endregion

    #region Constructors



    #endregion

    #region Methods

    public static TreeStateManager TreeStateManager()
    {
        return _treeStateManager;
    }

    #endregion

    #region Patches

    [HarmonyPatch(typeof(TreeStateManager))]
    [HarmonyPatch("HandleTransition")]
    private static class TreeStateManagerCacher
    {
        // Prefix runs BEFORE the original method
        static void Prefix(TreeStateManager __instance, string transitionName)
        {
            _treeStateManager = __instance;
        }
    }

    #endregion
}