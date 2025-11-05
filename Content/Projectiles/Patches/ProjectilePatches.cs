using HarmonyLib;
using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppSource.Utils;
using PvZReCoreLib.Content.Plants;
using PvZReCoreLib.Content.Plants.Behavior;
using PvZReCoreLib.Content.Plants.Behavior.CoreBehavior;
using PvZReCoreLib.Content.Plants.Mint;
using PvZReCoreLib.Util;
using Type = Il2CppSystem.Type;

namespace PvZReCoreLib.Content.Projectiles.Patches;

[HarmonyPatch(typeof(Projectile), nameof(Projectile.ProjectileInitialize))]
public class Projectile_ProjectileInitialize_Patch
{
    public static void Postfix(ref Projectile __instance)
    {
        ProjectileExtension ext = ProjectileExtension.GetOrCreateExtension<ProjectileExtension>(__instance.mController.gameObject);
        ext.source = __instance;
        
        Type behaviorType = null;
        
        if (CustomContentRegistry.IsValidCustomProjectileType(__instance.mProjectileType))
        {
            try
            {
                ProjectileDefinition plantDef = CustomContentRegistry.GetCustomProjectileDefinition(__instance.mProjectileType);
                if(plantDef.TryCast<CustomProjectileDefinition>() is { } customDef)
                {
                    behaviorType = customDef.GetCustomBehaviorType();
                }
            }
            catch (Exception e)
            {
                MelonLoader.MelonLogger.Error($"Error initializing custom projectile {__instance.mProjectileType}: {e}");
            }
        }
        
        if (behaviorType != null)
        {
            try
            {
                CustomProjectileBehaviorController comp = __instance.mController.gameObject.AddComponent(behaviorType).Cast<CustomProjectileBehaviorController>();
                comp.mObject.Value = __instance;
                comp.mBoard.Value = __instance.mBoard;
                    
                comp.PostObjectInitialize();

                ext.CustomBehaviorController = comp;
            }
            catch (Exception e)
            {
                MelonLoader.MelonLogger.Error($"Error adding custom behavior to plant {__instance.mProjectileType}: {e}");
            }
        }
    }
}