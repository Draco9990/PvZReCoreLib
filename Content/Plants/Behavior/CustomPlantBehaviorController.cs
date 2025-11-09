using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppReloaded;
using Il2CppReloaded.Characters;
using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using MelonLoader;
using PvZReCoreLib.Content.Common.Behavior;
using PvZReCoreLib.Content.Common.Skins;
using PvZReCoreLib.Content.Projectiles;
using UnityEngine;

namespace PvZReCoreLib.Content.Plants.Behavior;

[RegisterTypeInIl2Cpp]
public class CustomPlantBehaviorController : CustomBehaviorController
{
    #region Variables

    public Il2CppReferenceField<Plant> mPlant;
    public Plant Plant => mPlant.Value;
    
    public Il2CppReferenceField<PlantDefinition> mPlantDefinition;
    public PlantDefinition PlantDefinition => mPlantDefinition.Value;
    
    public bool bMintEffectActive = false;
    public bool bLaunchCounterFiredThisFrame = false;
    
    private float launchCounterCache;

    #endregion
    
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
    
    public Action PreUpdateEvent;
    public virtual bool PrePlantUpdate()
    {
        bLaunchCounterFiredThisFrame = false;
        PreUpdateEvent?.Invoke(); 
        return true;
    }
    public Action PostUpdateEvent;
    public virtual void PostPlantUpdate()
    {
        PostUpdateEvent?.Invoke();
    }
    
    public Action PreUpdateProductionEvent;
    public virtual bool PreUpdateProduction()
    {
        launchCounterCache = Plant.mLaunchCounter;
        PreUpdateProductionEvent?.Invoke(); 
        return true;
    }
    public Action PostUpdateProductionEvent;
    public virtual void PostUpdateProduction()
    {
        if (Plant.mLaunchCounter > launchCounterCache)
        {
            OnLaunchCounterTriggered();    
        }
        
        PostUpdateProductionEvent?.Invoke();
    }
    
    public Action PreUpdateShooterEvent;
    public virtual bool PreUpdateShooter()
    {
        launchCounterCache = Plant.mLaunchCounter;
        PreUpdateShooterEvent?.Invoke();
        return true;
    }
    public Action PostUpdateShooterEvent;
    public virtual void PostUpdateShooter()
    {
        if (Plant.mLaunchCounter > launchCounterCache)
        {
            OnLaunchCounterTriggered();    
        }
        
        PostUpdateShooterEvent?.Invoke();
    }

    public Action OnLaunchCounterTriggeredEvent;
    public virtual void OnLaunchCounterTriggered()
    {
        bLaunchCounterFiredThisFrame = true;
        OnLaunchCounterTriggeredEvent?.Invoke();
    }

    public Action OnMintEffectStartEvent;
    public virtual void OnMintEffectStart()
    {
        bMintEffectActive = true;
        OnMintEffectStartEvent?.Invoke();
    }
    public Action OnMintEffectEndEvent;
    public virtual void OnMintEffectEnd()
    {
        OnMintEffectEndEvent?.Invoke();
        bMintEffectActive = false;
    }

    public override void Reset()
    {
        base.Reset();

        bMintEffectActive = false;
        bLaunchCounterFiredThisFrame = false;
    }

    #endregion

    #region Helpers

    public Projectile SpawnProjectile(ProjectileType projectileType)
    {
        var renderOrder = Board.MakeRenderOrder(RenderLayer.Projectile, Plant.mRow, 1);
        return Board.AddProjectile(Plant.mX, Plant.mY, renderOrder, Plant.mRow, projectileType);
    }
    
    public void DamageZombie(Zombie theZombie, int damage, DamageFlags damageFlags, AudioClip hitSfx = null)
    {
        theZombie.TakeDamage(damage, damageFlags);

        if (hitSfx != null)
        {
            PlayAudio(hitSfx);
        }
    }

    public void PlayAudio(AudioClip sfx)
    {
        var audioSrv = AppCore.GetService<IAudioService>().Cast<AudioService>();
        audioSrv.m_audioSources.GetAudioSource(Constants.Sound.SOUND_PLANT).m_audioSource.PlayOneShot(sfx);
    }

    public void PlayAnimation(string animation)
    {
        Plant.mController.AnimationController.PlayAnimation(animation, CharacterTracks.NULL, 30, AnimLoopType.PlayOnce);
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
            return customPlantBehavior.PrePlantUpdate();
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
            return customPlantBehavior.PreUpdateProduction();
        }

        return true;
    }
    
    public static void Postfix(ref Plant __instance)
    {
        if(__instance.mController.gameObject.TryGetComponent(out CustomPlantBehaviorController customPlantBehavior))
        {
            customPlantBehavior.PostUpdateProduction();
        }
    }
}

[HarmonyPatch(typeof(Plant), nameof(Plant.UpdateShooter))]
public class Plant_PlantShooterUpdate_Patch
{
    public static bool Prefix(ref Plant __instance)
    {
        if(__instance.mController.gameObject.TryGetComponent(out CustomPlantBehaviorController customPlantBehavior))
        {
            return customPlantBehavior.PreUpdateShooter();
        }

        return true;
    }
    
    public static void Postfix(ref Plant __instance)
    {
        if(__instance.mController.gameObject.TryGetComponent(out CustomPlantBehaviorController customPlantBehavior))
        {
            customPlantBehavior.PostUpdateShooter();
        }
    }
}