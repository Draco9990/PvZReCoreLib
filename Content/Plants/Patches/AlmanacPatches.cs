using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppReloaded.Data;
using Il2CppReloaded.DataModels;
using Il2CppReloaded.Gameplay;
using Il2CppTekly.DataModels.Models;
using Il2CppTekly.TreeState;
using PvZReCoreLib.Util;
using UnityEngine;
using Object = Il2CppSystem.Object;

namespace PvZReCoreLib.Content.Plants.Patches;

[HarmonyPatch(typeof(AlmanacModel), nameof(AlmanacModel.OnAlmanacDataUpdated))]
public partial class AlmanacModel_CustomPlantEntriesPatch
{
    static AlmanacModel activeInstance;
    public static bool Prefix(ref AlmanacModel __instance)
    {
        activeInstance = __instance;
        Patch();
        return true;
    }

    public static void Patch()
    {
        if (activeInstance == null)
        {
            MelonLoader.MelonLogger.Msg("[CoreLib] AlmanacModel_CustomPlantEntriesPatch.Patch() called before AlmanacModel exists - deferring until Almanac is opened.");
            return;
        }

        var patchMarker = PatchMarker<AlmanacModel>.GetOrCreateExtension<PatchMarker<AlmanacModel>>(activeInstance);
        if (patchMarker.IsPatched)
        {
            return;
        }
        patchMarker.IsPatched = true;

        MelonLoader.MelonLogger.Msg($"[CoreLib] Patching AlmanacModel with {CustomContentRegistry.GetAllCustomPlantTypes().Count} custom plant entries.");

        int index = activeInstance.m_plantsModel.m_entriesModel.m_models.Count;
        foreach (SeedType seedType in CustomContentRegistry.GetAllCustomPlantTypes())
        {
            AlmanacEntryData almanacData = CustomContentRegistry.GetCustomAlmanacEntry(seedType);
            if (almanacData == null)
            {
                continue;
            }
            
            var entryModel = PersistentStorage.Store(new AlmanacEntryModel(
                almanacData, 
                activeInstance.m_plantsModel, 
                false, false, 
                index));
            entryModel.UpdateModelData();

            activeInstance.m_plantsModel.m_entriesModel.Add(index, entryModel.Cast<IModel>());
            
            index++;
        }

        activeInstance.m_plantsModel.m_totalModel.Value += CustomContentRegistry.GetAllCustomPlantTypes().Count;
        
        activeInstance.UpdateEntriesLockedState();
    }
}

[HarmonyPatch(typeof(TreeStateManager))]
[HarmonyPatch(nameof(TreeStateManager.TransitionTo))]
public static class TreeStateManagerTransitionLogger
{
    static void Postfix(TreeStateManager __instance, TreeState treeState)
    {
        MelonLoader.MelonLogger.Msg($"[CoreLib] TreeState transition -> '{treeState.name}'");

        if (treeState.name == "AmanacPlants")
        {
            foreach (Object o in UnityEngine.Object.FindObjectsOfTypeAll(Il2CppType.Of<GameObject>()))
            {
                var go = o.TryCast<GameObject>();
                if (go != null && go.name == "P_Almanac_Plants")
                {
                    var plantList = go.transform.FindChild("Canvas/Layout/Center/Panel");
                    if(plantList == null)
                    {
                        continue;
                    }
            
                    var grid = plantList.FindChild("ItemGrid");
                    if(grid == null)
                    {
                        continue;
                    }
            
                    UnityUtil.InjectScrollbar(plantList, grid, widthAdd: 70f);
                    return;
                }
            }
        }
    }
}

[HarmonyPatch(typeof(AlmanacEntryModel), nameof(AlmanacEntryModel.UpdateModelData))]
public static class AlmanacEntryModel_UpdateModelData_EnlightenMintPatch
{
    static bool Prefix(AlmanacEntryModel __instance)
    {
        return true;
    }
    
    static void Postfix(AlmanacEntryModel __instance)
    {
        int i = 0;
    }
}