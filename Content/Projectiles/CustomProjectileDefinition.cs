using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppReloaded;
using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using Il2CppSource.Utils;
using MelonLoader;
using PvZReCoreLib.Content.Common.Behavior;
using PvZReCoreLib.Content.Common.Skins;
using PvZReCoreLib.Content.Common.Skins.SkinDataTypes.subtypes;
using PvZReCoreLib.Util;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Type = Il2CppSystem.Type;

namespace PvZReCoreLib.Content.Projectiles;

[RegisterTypeInIl2Cpp]
public class CustomProjectileDefinition : ProjectileDefinition
{
    #region Variables
    
    public ProjectileMotion m_motionType;

    public DamageRangeFlags m_damageRangeFlags;
    public DamageFlags m_damageFlags;
    
    public AudioClip m_hitSfx = null;

    public string m_defaultSkin = "";

    #endregion

    #region Constructors

    public CustomProjectileDefinition(IntPtr pointer) : base(pointer)
    {
        var peaDefinition = ReloadedUtils.DataService.GetProjectileDefinition(ProjectileType.Pea);
        
        m_projectileType = CustomContentRegistry.RequestFreeProjectileType();

        m_prefab = PersistentStorage.Store(new AssetReferenceGameObject(peaDefinition.m_prefab.m_AssetGUID));
        m_preorderPrefab = PersistentStorage.EmptyGameObjectRef;
    }
    
    public CustomProjectileDefinition() : base(ClassInjector.DerivedConstructorPointer<CustomProjectileDefinition>()) => ClassInjector.DerivedConstructorBody(this);

    #endregion

    #region Methods

    public virtual Type GetCustomBehaviorType()
    {
        return null;
    }
    
    public void SetDamageRangeFlags(params DamageRangeFlags[] flags)
    {
        int flagColl = 0;
        foreach (var flag in flags)
        {
            flagColl |= (int)flag;
        }
        this.m_damageRangeFlags = (DamageRangeFlags)flagColl;
    }
    public void SetDamageFlags(params DamageFlags[] flags)
    {
        int flagColl = 0;
        foreach (var flag in flags)
        {
            flagColl |= (int)flag;
        }
        this.m_damageFlags = (DamageFlags)flagColl;
    }

    public void SetHitSfx(string assetBundleId, string assetPath)
    {
        Action<AudioClip> sfxLoadAction = (sfx) =>
        {
            this.m_hitSfx = sfx;
        };
        RegistryBridge.LoadAssetFromAssetBundle<AudioClip>(assetBundleId, assetPath, sfxLoadAction);
    }
    
    public void RegisterSkin(IProjectileSkin skinType)
    {
        SkinRegistry.RegisterProjectileSkin(skinType);

        if (string.IsNullOrEmpty(m_defaultSkin))
        {
            m_defaultSkin = skinType.GetSkinId();
        }
    }

    #endregion
}

[RegisterTypeInIl2Cpp]
public class CustomProjectileBehaviorController : CustomBehaviorController
{
    #region Variables

    public Il2CppReferenceField<Projectile> mProjectile;
    public Projectile Projectile => mProjectile.Value;
    
    public Il2CppReferenceField<ProjectileDefinition> mProjectileDefinition;
    public ProjectileDefinition ProjectileDefinition => mProjectileDefinition.Value;

    #endregion

    #region Constructors

    public CustomProjectileBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    public Action PreUpdateEvent;
    public virtual bool PreUpdate() { PreUpdateEvent?.Invoke(); return true; }
    public Action PostUpdateEvent;
    public virtual void PostUpdate() { PostUpdateEvent?.Invoke(); }
    
    public Action PreUpdateNormalMotionEvent;
    public virtual bool PreUpdateNormalMotion() { PreUpdateNormalMotionEvent?.Invoke(); return true; }
    public Action PostUpdateNormalMotionEvent;
    public virtual void PostUpdateNormalMotion() { PostUpdateNormalMotionEvent?.Invoke(); }
    
    public Action<Zombie> PreDoImpactEvent;
    public virtual bool PreDoImpact(Zombie theZombie) { PreDoImpactEvent?.Invoke(theZombie); return true; }
    public Action<Zombie> PostDoImpactEvent;
    public virtual void PostDoImpact(Zombie theZombie) { PostDoImpactEvent?.Invoke(theZombie); }

    #endregion

    #region Helpers

    public void DamageZombie(Zombie theZombie)
    {
        var customDef = ProjectileDefinition.TryCast<CustomProjectileDefinition>();
        if (customDef == null)
        {
            var flags = Projectile.GetDamageFlags(theZombie);
            theZombie.TakeDamage(ProjectileDefinition.m_damage, flags);    
        }
        else
        {
            theZombie.TakeDamage(ProjectileDefinition.m_damage, customDef.m_damageFlags);
        }
        
        var audioSrv = AppCore.GetService<IAudioService>().Cast<AudioService>();
        if (customDef != null && customDef.m_hitSfx != null)
        {
            audioSrv.m_audioSources.GetAudioSource(Constants.Sound.SOUND_PLANT).m_audioSource.PlayOneShot(customDef.m_hitSfx);
        }
    }

    #endregion
}

[HarmonyPatch(typeof(Projectile), nameof(Projectile.Update))]
public class Projectile_Update_Patch
{
    public static bool Prefix(ref Projectile __instance)
    {
        if(__instance.mController.gameObject.TryGetComponent(out CustomProjectileBehaviorController customProjectileBehavior))
        {
            return customProjectileBehavior.PreUpdate();
        }

        return true;
    }
    
    public static void Postfix(ref Projectile __instance)
    {
        if (__instance == null || __instance.mDead || __instance.mController == null || __instance.mController.gameObject == null)
        {
            return;
        }
        
        if(__instance.mController.gameObject.TryGetComponent(out CustomProjectileBehaviorController customProjectileBehavior))
        {
            customProjectileBehavior.PostUpdate();
        }
    }
}

[HarmonyPatch(typeof(Projectile), nameof(Projectile.UpdateNormalMotion))]
public class Projectile_UpdateMotion_Patch
{
    public static bool Prefix(ref Projectile __instance)
    {
        if(__instance.mController.gameObject.TryGetComponent(out CustomProjectileBehaviorController customProjectileBehavior))
        {
            return customProjectileBehavior.PreUpdateNormalMotion();
        }

        return true;
    }
    
    public static void Postfix(ref Projectile __instance)
    {
        if (__instance == null || __instance.mDead || __instance.mController == null || __instance.mController.gameObject == null)
        {
            return;
        }
        
        if(__instance.mController.gameObject.TryGetComponent(out CustomProjectileBehaviorController customProjectileBehavior))
        {
            customProjectileBehavior.PostUpdateNormalMotion();
        }
    }
}

[HarmonyPatch(typeof(Projectile), nameof(Projectile.DoImpact))]
public class Projectile_DoImpact_Patch
{
    public static bool Prefix(ref Projectile __instance, ref Zombie theZombie)
    {
        if(__instance.mController.gameObject.TryGetComponent(out CustomProjectileBehaviorController customProjectileBehavior))
        {
            return customProjectileBehavior.PreDoImpact(theZombie);
        }

        return true;
    }
    
    public static void Postfix(ref Projectile __instance, ref Zombie theZombie)
    {
        if (__instance == null || __instance.mDead || __instance.mController == null || __instance.mController.gameObject == null)
        {
            return;
        }
        
        if(__instance.mController.gameObject.TryGetComponent(out CustomProjectileBehaviorController customProjectileBehavior))
        {
            customProjectileBehavior.PostDoImpact(theZombie);
        }
    }
}