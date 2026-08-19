using HarmonyLib;
using Il2CppReloaded;
using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using Il2CppReloaded.TreeStateActivities;
using Il2CppSource.Controllers;
using Il2CppSource.Utils;
using PvZReCoreLib.Content.Common.Data;
using PvZReCoreLib.Content.Plants.Behavior;
using PvZReCoreLib.Content.Plants.Behavior.CoreBehavior;
using PvZReCoreLib.Content.Plants.Mint;
using PvZReCoreLib.Util;
using UnityEngine;
using Object = UnityEngine.Object;
using Type = Il2CppSystem.Type;

namespace PvZReCoreLib.Content.Plants.Patches;

[HarmonyPatch(typeof(Plant), nameof(Plant.PlantInitialize))]
public class Plant_PlantInitialize_Patch
{
    public static void Postfix(ref Plant __instance)
    { 
        if (ReplantedGet.TreeStateManager().Active.name == "AmanacPlants")
        {
            return;
        }
        
        UniqueIdExtension uniqueIdExt = UniqueIdExtension.GetOrCreateExtension<UniqueIdExtension>(__instance);
        uniqueIdExt.RandomizeUniqueId();
        
        PlantExtension ext = PlantExtension.GetOrCreateExtension<PlantExtension>(__instance.mController.gameObject);
        ext.source = __instance;
        
        MintFamily mintFamily = MintFamily.None;
        Type behaviorType = null;
        
        PlantDefinition plantDef = AppCore.GetService<IDataService>().Cast<DataService>().GetPlantDefinition(__instance.mSeedType);
        if (CustomContentRegistry.IsValidCustomPlantType(__instance.mSeedType))
        {
            try
            {
                if(plantDef.TryCast<CustomPlantDefinition>() is { } customDef)
                {
                    __instance.mPlantMaxHealth = customDef.m_health;
                    __instance.mPlantHealth = customDef.m_health;
                
                    mintFamily = customDef.m_mintFamily;
                    behaviorType = customDef.GetCustomBehaviorType();
                }
            }
            catch (Exception e)
            {
                MelonLoader.MelonLogger.Error($"Error initializing custom plant {__instance.mSeedType}: {e}");
            }
        }
        else
        {
            mintFamily = MintUtils.GetMintFamilyForBasePlants(__instance.mSeedType);
            behaviorType = CorePlantBehaviorUtils.GetBehaviorType(__instance.mSeedType);
        }
        
        if (behaviorType != null)
        {
            try
            {
                CustomPlantBehaviorController comp;
                if (__instance.mController.gameObject.TryGetComponent(out CustomPlantBehaviorController existingComp))
                {
                    comp = existingComp;
                    comp.Reset();
                }
                else
                {
                    comp = __instance.mController.gameObject.AddComponent(behaviorType).Cast<CustomPlantBehaviorController>();
                }
                
                comp.mPlant.Value = __instance;
                comp.mBoard.Value = __instance.mBoard;
                comp.mPlantDefinition.Value = plantDef;
                    
                comp.PostInitialize();

                ext.CustomBehaviorController = comp;
            }
            catch (Exception e)
            {
                MelonLoader.MelonLogger.Error($"Error adding custom behavior to plant {__instance.mSeedType}: {e}");
            }
        }

        if (mintFamily != MintFamily.None)
        {
            try
            {
                Type mintFamilyControllerType = MintUtils.GetMintFamilyControllerType(mintFamily);
                if (mintFamilyControllerType != null)
                {
                    MintFamilyBehaviorController comp;
                    if (__instance.mController.gameObject.TryGetComponent(out MintFamilyBehaviorController existingComp))
                    {
                        comp = existingComp;
                        comp.Reset();
                    }
                    else
                    {
                        comp = __instance.mController.gameObject.AddComponent(mintFamilyControllerType).Cast<MintFamilyBehaviorController>();
                    }
                    
                    comp.mPlant.Value = __instance;
                    comp.mBoard.Value = __instance.mBoard;
                    comp.mMintFamily = mintFamily;
                    comp.PostInitialize();

                    ext.MintFamilyBehaviorController = comp;
                }
            }
            catch (Exception e)
            {
                MelonLoader.MelonLogger.Error($"Error adding mint family behavior to plant {__instance.mSeedType}: {e}");
            }
        }
    }
}

[HarmonyPatch(typeof(Board), nameof(Board.PostDeserialize))]
public class Board_PostDeserialize_ReattachCustomPlantBehavior_Patch
{
    // Plant.PlantInitialize is what normally attaches CustomPlantBehaviorController
    // (see Plant_PlantInitialize_Patch above) - but restoring a save game rebuilds
    // Plant instances through Board's own deserialize/PostDeserialize path, which
    // never calls PlantInitialize. Result: every custom plant that was already on
    // the board when the save was written comes back with its sprite/skin showing
    // fine (that's driven by a separate, more general skin hook) but with zero
    // custom behavior attached - it just sits there and gets eaten, since nothing
    // is left to drive its attacks/production. This reattaches the missing
    // component without touching anything PlantInitialize would normally reset
    // (health, unique id) - those already came back correctly through the plant's
    // own deserialized fields, and stomping them here would undo the restore
    // instead of fixing it.
    public static void Postfix(Board __instance)
    {
        if (__instance == null || __instance.m_plants == null)
        {
            return;
        }

        foreach (var item in __instance.m_plants.m_list)
        {
            var plant = item.mItem;
            if (plant == null || plant.mController == null || plant.mController.gameObject == null)
            {
                continue;
            }

            if (!CustomContentRegistry.IsValidCustomPlantType(plant.mSeedType))
            {
                continue;
            }

            if (plant.mController.gameObject.TryGetComponent(out CustomPlantBehaviorController _))
            {
                // Already attached - some other path (e.g. planted fresh after
                // load) already handled this one. Don't Reset() an
                // already-correct controller.
                continue;
            }

            try
            {
                PlantDefinition plantDef = AppCore.GetService<IDataService>().Cast<DataService>().GetPlantDefinition(plant.mSeedType);
                if (plantDef.TryCast<CustomPlantDefinition>() is not { } customDef)
                {
                    continue;
                }

                Type behaviorType = customDef.GetCustomBehaviorType();
                if (behaviorType == null)
                {
                    continue;
                }

                PlantExtension ext = PlantExtension.GetOrCreateExtension<PlantExtension>(plant.mController.gameObject);
                ext.source = plant;

                var comp = plant.mController.gameObject.AddComponent(behaviorType).Cast<CustomPlantBehaviorController>();
                comp.mPlant.Value = plant;
                comp.mBoard.Value = plant.mBoard;
                comp.mPlantDefinition.Value = plantDef;
                comp.PostInitialize();
                ext.CustomBehaviorController = comp;

                if (customDef.m_mintFamily != MintFamily.None
                    && !plant.mController.gameObject.TryGetComponent(out MintFamilyBehaviorController _))
                {
                    Type mintFamilyControllerType = MintUtils.GetMintFamilyControllerType(customDef.m_mintFamily);
                    if (mintFamilyControllerType != null)
                    {
                        var mintComp = plant.mController.gameObject.AddComponent(mintFamilyControllerType).Cast<MintFamilyBehaviorController>();
                        mintComp.mPlant.Value = plant;
                        mintComp.mBoard.Value = plant.mBoard;
                        mintComp.mMintFamily = customDef.m_mintFamily;
                        mintComp.PostInitialize();
                        ext.MintFamilyBehaviorController = mintComp;
                    }
                }
            }
            catch (Exception e)
            {
                MelonLoader.MelonLogger.Error($"Error reattaching custom plant behavior after deserialize for {plant.mSeedType}: {e}");
            }
        }
    }
}

[HarmonyPatch(typeof(Plant), nameof(Plant.IsSpiky))]
public class Plant_IsSpiky_Patch
{
    // Native IsSpiky() only recognizes vanilla SeedTypes (Spikeweed/SpikeRock)
    // and has no data-driven field to opt a custom plant into, so it always
    // returns false for one regardless of what we do. Route custom plants
    // through their own behavior controller instead - see
    // CustomPlantBehaviorController.IsSpiky for what this actually gates.
    public static bool Prefix(Plant __instance, ref bool __result)
    {
        if (__instance == null || __instance.mController == null || __instance.mController.gameObject == null)
        {
            return true;
        }

        if (!__instance.mController.gameObject.TryGetComponent(out CustomPlantBehaviorController comp))
        {
            return true;
        }

        __result = comp.IsSpiky();
        return false;
    }
}

[HarmonyPatch(typeof(Zombie), nameof(Zombie.CanTargetPlant), new[] { typeof(Plant), typeof(ZombieAttackType) })]
public class Zombie_CanTargetPlant_Patch
{
    // Separate from Plant_IsSpiky_Patch on purpose - see
    // CustomPlantBehaviorController.CanBeTargetedBy for why. Only intervenes
    // (skips native) when the plant's own controller explicitly vetoes being
    // targeted; otherwise falls through to native logic unchanged, which
    // already handles pools/ladders/flowerpots/IsSpiky correctly on its own.
    public static bool Prefix(Zombie __instance, Plant thePlant, ZombieAttackType theAttackType, ref bool __result)
    {
        if (thePlant == null || thePlant.mController == null || thePlant.mController.gameObject == null)
        {
            return true;
        }

        if (!thePlant.mController.gameObject.TryGetComponent(out CustomPlantBehaviorController comp))
        {
            return true;
        }

        if (comp.CanBeTargetedBy(theAttackType))
        {
            return true;
        }

        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(Board), nameof(Board.CanPlantAt), new[] { typeof(int), typeof(int), typeof(SeedType) })]
public class Board_CanPlantAt_RequiresGround_Patch
{
    // Postfix, not Prefix - native CanPlantAt already handles plenty of other
    // rejection reasons (occupied square, not past the line, needs upgrade,
    // etc.) that must stay intact. This only adds the ground-only check on
    // top, and only overrides an otherwise-Ok result.
    public static void Postfix(Board __instance, int theGridX, int theGridY, SeedType theType, ref PlantingReason __result)
    {
        if (__result != PlantingReason.Ok)
        {
            return;
        }

        if (!CustomContentRegistry.IsValidCustomPlantType(theType))
        {
            return;
        }

        var plantDef = AppCore.GetService<IDataService>().Cast<DataService>().GetPlantDefinition(theType);
        if (plantDef.TryCast<CustomPlantDefinition>() is not { } customDef || !customDef.m_requiresGround)
        {
            return;
        }

        if (__instance.IsPoolSquare(theGridX, theGridY))
        {
            __result = PlantingReason.NeedsGround;
        }
    }
}

public class PlantPreviewWorkaround
{
    private static SeedType cachedOverride = SeedType.None;
    
    [HarmonyPatch(typeof(GameplayActivity), nameof(GameplayActivity.CreatePreviewController), new []{ typeof(SeedType), typeof(ReloadedObject) })]
    public class GameplayActivity_CreatePreviewController_Patch
    {
        public static bool Prefix(GameplayActivity __instance, ref SeedType seedType, ReloadedObject cursorPreview)
        {
            if (CustomContentRegistry.IsValidCustomPlantType(seedType))
            {
                cachedOverride = seedType;
                seedType = SeedType.Peashooter;
            }

            return true;
        }
    }
    
    [HarmonyPatch(typeof(PreviewController), nameof(PreviewController.Set), new [] { typeof(SeedType) })]
    public class PreviewController_Set_Patch
    {
        public static bool Prefix(PreviewController __instance, ref SeedType seedPacket)
        {
            if (cachedOverride != SeedType.None)
            {
                seedPacket = cachedOverride;
                cachedOverride = SeedType.None;
            }

            return true;
        }
    }
}