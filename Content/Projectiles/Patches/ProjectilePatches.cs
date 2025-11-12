using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppReloaded;
using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using Il2CppSource.Utils;
using MelonLoader;
using PvZReCoreLib.Content.Common.Data;
using PvZReCoreLib.Content.Common.Skins;
using PvZReCoreLib.Content.Common.Skins.SkinDataTypes.subtypes;
using PvZReCoreLib.Content.Plants;
using PvZReCoreLib.Content.Plants.Behavior;
using PvZReCoreLib.Content.Plants.Behavior.CoreBehavior;
using PvZReCoreLib.Content.Plants.Mint;
using PvZReCoreLib.Util;
using UnityEngine;
using Object = UnityEngine.Object;
using Type = Il2CppSystem.Type;

namespace PvZReCoreLib.Content.Projectiles.Patches;

[HarmonyPatch(typeof(Projectile), nameof(Projectile.ProjectileInitialize))]
public class Projectile_ProjectileInitialize_Patch
{
    public static void Postfix(ref Projectile __instance)
    {
        UniqueIdExtension uniqueIdExt = UniqueIdExtension.GetOrCreateExtension<UniqueIdExtension>(__instance);
        uniqueIdExt.RandomizeUniqueId();
        
        ProjectileExtension ext = ProjectileExtension.GetOrCreateExtension<ProjectileExtension>(__instance.mController.gameObject);
        ext.source = __instance;
        
        Type behaviorType = null;
        
        ProjectileDefinition projectileDef = AppCore.GetService<IDataService>().Cast<DataService>().GetProjectileDefinition(__instance.mProjectileType);
        CustomProjectileDefinition customDef = projectileDef.TryCast<CustomProjectileDefinition>();
        if (CustomContentRegistry.IsValidCustomProjectileType(__instance.mProjectileType))
        {
            if(customDef != null)
            {
                behaviorType = customDef.GetCustomBehaviorType();
                __instance.mMotionType = customDef.m_motionType;
                __instance.mDamageRangeFlags = customDef.m_damageRangeFlags;

                if (ext.CurrentSkin != null)
                {
                    ext.CurrentSkin.CleanUpSkin(__instance.mController.gameObject);
                }
                
                if (SkinRegistry.ProjectileSkins.ContainsKey(customDef.m_defaultSkin))
                {
                    IProjectileSkin skin = SkinRegistry.ProjectileSkins[customDef.m_defaultSkin];
                    skin.ApplySkin(__instance.mController.gameObject);
                }
            }
        }
        
        if (behaviorType != null)
        {
            CustomProjectileBehaviorController comp;
            if (__instance.mController.gameObject.TryGetComponent(out CustomProjectileBehaviorController existingComp))
            {
                comp = existingComp;
                comp.Reset();
            }
            else
            {
                comp = __instance.mController.gameObject.AddComponent(behaviorType).Cast<CustomProjectileBehaviorController>();
            }
            
            comp.mProjectile.Value = __instance;
            comp.mBoard.Value = __instance.mBoard;
            comp.mProjectileDefinition.Value = projectileDef;
                    
            comp.PostInitialize();

            ext.CustomBehaviorController = comp;
        }
    }
}

[HarmonyPatch(typeof(Plant), nameof(Plant.Fire))]
public class Plant_Fire_Patch
{
    public static bool Prefix(ref Plant __instance, Zombie theTargetZombie, int theRow, PlantWeapon thePlantWeapon)
    {
        if ((int)__instance.mSeedType >= 1000)
        {
            return false;
        }

        return true;
    }
}