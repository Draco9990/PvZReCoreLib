using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.TreeStateActivities;
using MelonLoader;

namespace PvZReCoreLib.Content.Plants.Patches;

// Diagnostics only: "Let's Rock" silently fails to start a level when a custom (SeedType >= 1000)
// seed is among the chosen ones. Log what's selected going in, and use a Finalizer (runs even if
// the original method throws) to surface any exception that MelonLoader might otherwise swallow.
[HarmonyPatch(typeof(SeedChooserScreen), nameof(SeedChooserScreen.OnStartButton))]
public class SeedChooserScreen_OnStartButton_DiagPatch
{
    static void Prefix(SeedChooserScreen __instance)
    {
        MelonLogger.Msg("[CoreLib] OnStartButton() called. Chosen seeds:");
        foreach (var cs in __instance.mChosenSeeds)
        {
            if (cs.mSeedState != ChosenSeedState.SeedPacketHidden && cs.mSeedState != ChosenSeedState.SeedInChooser)
            {
                MelonLogger.Msg($"[CoreLib]   seedType={cs.mSeedType} ({(int)cs.mSeedType}) state={cs.mSeedState} indexInBank={cs.mSeedIndexInBank}");
            }
        }
    }

    static System.Exception Finalizer(System.Exception __exception)
    {
        if (__exception != null)
        {
            MelonLogger.Error($"[CoreLib] OnStartButton() threw: {__exception}");
        }
        else
        {
            MelonLogger.Msg("[CoreLib] OnStartButton() completed without throwing.");
        }
        return __exception;
    }
}

[HarmonyPatch(typeof(GameplayActivity), nameof(GameplayActivity.NewGame))]
public class GameplayActivity_NewGame_DiagPatch
{
    static void Prefix()
    {
        MelonLogger.Msg("[CoreLib] GameplayActivity.NewGame() called.");
    }

    static System.Exception Finalizer(System.Exception __exception)
    {
        if (__exception != null)
        {
            MelonLogger.Error($"[CoreLib] NewGame() threw: {__exception}");
        }
        return __exception;
    }
}

[HarmonyPatch(typeof(GameplayActivity), nameof(GameplayActivity.CreatePlantController))]
public class GameplayActivity_CreatePlantController_DiagPatch
{
    static void Prefix(SeedType type)
    {
        if ((int)type >= (int)SeedType.NumSeedTypes)
        {
            MelonLogger.Msg($"[CoreLib] CreatePlantController() called for custom SeedType {type} ({(int)type}).");
        }
    }

    static System.Exception Finalizer(SeedType type, System.Exception __exception)
    {
        if (__exception != null)
        {
            MelonLogger.Error($"[CoreLib] CreatePlantController({type}) threw: {__exception}");
        }
        return __exception;
    }
}
