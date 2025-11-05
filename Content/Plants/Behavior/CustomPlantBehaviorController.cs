using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppReloaded.Gameplay;
using MelonLoader;
using PvZReCoreLib.Content.Common.Behavior;
using UnityEngine;

namespace PvZReCoreLib.Content.Plants.Behavior;

[RegisterTypeInIl2Cpp]
public class CustomPlantBehaviorController : CustomBehaviorController<Plant>
{
    public bool bMintEffectActive = false;
    
    public CustomPlantBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    public static CustomPlantBehaviorController GetFor(Plant p)
    {
        if (p.mController is null || !p.mController.gameObject.TryGetComponent(out CustomPlantBehaviorController plantComp))
        {
            return null;
        }

        return plantComp;
    }
    
    #region Plant Calls
    
    public Action PrePlantUpdated;
    public virtual void PrePlantUpdate() { PrePlantUpdated?.Invoke(); }
    public Action PostPlantUpdated;
    public virtual void PostPlantUpdate() { PostPlantUpdated?.Invoke(); }
    
    public Action PreProductionPlantUpdated;
    public virtual void PreProductionPlantUpdate() { PreProductionPlantUpdated?.Invoke(); }
    public Action PostProductionPlantUpdated;
    public virtual void PostProductionPlantUpdate() { PostProductionPlantUpdated?.Invoke(); }

    public Action OnMintEffectStarted;
    public virtual void OnMintEffectStart()
    {
        bMintEffectActive = true;
        OnMintEffectStarted?.Invoke();
    }
    public Action OnMintEffectEnded;
    public virtual void OnMintEffectEnd()
    {
        OnMintEffectEnded?.Invoke();
        bMintEffectActive = false;
    }

    #endregion
}

[HarmonyPatch(typeof(Plant), nameof(Plant.Update))]
public class Plant_PlantUpdate_Patch
{
    public static bool Prefix(ref Plant __instance)
    {
        if(__instance.mController.gameObject.TryGetComponent(out CustomPlantBehaviorController customPlantBehavior))
        {
            customPlantBehavior.PrePlantUpdate();
        }

        return true;
    }
    
    public static void Postfix(ref Plant __instance)
    {
        if(__instance.mController.gameObject.TryGetComponent(out CustomPlantBehaviorController customPlantBehavior))
        {
            customPlantBehavior.PostPlantUpdate();
        }
    }
}

[HarmonyPatch(typeof(Plant), nameof(Plant.UpdateProductionPlant))]
public class Plant_PlantProductionUpdate_Patch
{
    public static bool Prefix(ref Plant __instance)
    {
        if(__instance.mController.gameObject.TryGetComponent(out CustomPlantBehaviorController customPlantBehavior))
        {
            customPlantBehavior.PreProductionPlantUpdate();
        }

        return true;
    }
    
    public static void Postfix(ref Plant __instance)
    {
        if(__instance.mController.gameObject.TryGetComponent(out CustomPlantBehaviorController customPlantBehavior))
        {
            customPlantBehavior.PostProductionPlantUpdate();
        }
    }
}