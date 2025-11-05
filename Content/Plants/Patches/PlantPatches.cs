using HarmonyLib;
using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.TreeStateActivities;
using Il2CppSource.Controllers;
using Il2CppSource.Utils;
using PvZReCoreLib.Content.Plants.Behavior;
using PvZReCoreLib.Content.Plants.Behavior.CoreBehavior;
using PvZReCoreLib.Content.Plants.Mint;
using PvZReCoreLib.Util;
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
        
        PlantExtension ext = PlantExtension.GetOrCreateExtension<PlantExtension>(__instance.mController.gameObject);
        ext.source = __instance;
        
        MintFamily mintFamily = MintFamily.None;
        Type behaviorType = null;
        
        if (CustomContentRegistry.IsValidCustomPlantType(__instance.mSeedType))
        {
            try
            {
                PlantDefinition plantDef = ReloadedUtils.GetPlantDefinition(__instance.mSeedType);
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
                CustomPlantBehaviorController comp = __instance.mController.gameObject.AddComponent(behaviorType).Cast<CustomPlantBehaviorController>();
                comp.mObject.Value = __instance;
                comp.mBoard.Value = __instance.mBoard;
                    
                comp.PostObjectInitialize();

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
                    MintFamilyBehaviorController comp = __instance.mController.gameObject.AddComponent(mintFamilyControllerType).Cast<MintFamilyBehaviorController>();
                    comp.mPlant.Value = __instance;
                    comp.mBoard.Value = __instance.mBoard;
                    comp.mMintFamily = mintFamily;

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