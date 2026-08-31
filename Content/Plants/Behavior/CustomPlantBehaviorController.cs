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

    // Backs the Plant.IsSpiky() Harmony patch (see PlantPatches.cs) - native
    // IsSpiky() only recognizes vanilla SeedTypes (Spikeweed/SpikeRock), so it
    // always returns false for a custom plant regardless of what we set here.
    // Overriding this is what tells the native engine "let zombies walk over
    // me instead of stopping to eat me" (Zombie.CanTargetPlant excludes spiky
    // plants from normal chew-targeting) and "squish me specially every step
    // a zombie takes over me" (Zombie.CheckSquish/SquishAllInSquare, which is
    // also what makes Zomboni get destroyed instead of squishing a spiky
    // plant on contact). It's a live query, not a fixed flag, specifically so
    // a plant can be conditionally spiky - e.g. a Celery Stalker-style plant
    // that's spiky while hidden and stops being spiky once it stands up.
    //
    // Actual damage dealt to zombies walking over a spiky custom plant is
    // NOT handled by this - that's on the plant's own behavior, the same way
    // Endurian computes its own damage rather than relying on native combat.
    // This only stops the native engine from treating the plant as a normal
    // eat-target.
    public virtual bool IsSpiky()
    {
        return false;
    }

    // Backs the Zombie.CanTargetPlant Harmony patch (see PlantPatches.cs) -
    // a separate, more specific hook than IsSpiky. CanTargetPlant itself
    // calls IsSpiky internally to exclude spiky plants from normal chew
    // targeting, but SquishAllInSquare ALSO calls IsSpiky separately to
    // decide whether a driving zombie (e.g. Zomboni) gets destroyed instead
    // of squishing the plant - meaning IsSpiky alone can't grant "walk over
    // me, don't eat me" without also granting "destroy any vehicle that
    // touches me". This hook lets a plant veto being eaten per attack type
    // (Chew/DriveOver/Vault/Ladder) without ever setting IsSpiky, so e.g. a
    // hidden ambush plant can avoid being eaten while it's hidden without
    // also turning into a Zomboni-killer. A real Spikeweed-style plant that
    // wants the full vehicle-destroying treatment should use IsSpiky
    // instead.
    public virtual bool CanBeTargetedBy(ZombieAttackType attackType)
    {
        return true;
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

    // Lets SkinRegistry's PlayAnimation postfix tell "we asked for this" apart
    // from Plant.DoBlink()'s native forced PlayAnimation("idle") calls, which
    // both funnel through the exact same native method - see the postfix for
    // why that distinction matters. Safe as a plain static bool: Unity's
    // update loop is single-threaded and PlayAnimation calls are synchronous,
    // so there's never a window where two plants' calls could interleave.
    public static bool IsExecutingOwnPlayAnimation { get; private set; }

    public void PlayAnimation(string animation)
    {
        IsExecutingOwnPlayAnimation = true;
        try
        {
            Plant.mController.AnimationController.PlayAnimation(animation, CharacterTracks.NULL, 30, AnimLoopType.PlayOnce);
        }
        finally
        {
            IsExecutingOwnPlayAnimation = false;
        }
    }

    #endregion
}

[HarmonyPatch(typeof(Plant), nameof(Plant.Update))]
public class Plant_PlantUpdate_Patch
{
    public static bool Prefix(ref Plant __instance)
    {
        if(__instance.mController == null || __instance.mController.gameObject == null)
        {
            return true;
        }
        
        if(__instance.mController.gameObject.TryGetComponent(out CustomPlantBehaviorController customPlantBehavior))
        {
            return customPlantBehavior.PrePlantUpdate();
        }

        return true;
    }
    
    public static void Postfix(ref Plant __instance)
    {
        if(__instance.mController == null || __instance.mController.gameObject == null)
        {
            return;
        }
        
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
        if(__instance.mController == null || __instance.mController.gameObject == null)
        {
            return true;
        }
        
        if(__instance.mController.gameObject.TryGetComponent(out CustomPlantBehaviorController customPlantBehavior))
        {
            return customPlantBehavior.PreUpdateProduction();
        }

        return true;
    }
    
    public static void Postfix(ref Plant __instance)
    {
        if(__instance.mController == null || __instance.mController.gameObject == null)
        {
            return;
        }
        
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
        if(__instance.mController == null || __instance.mController.gameObject == null)
        {
            return true;
        }
        
        if(__instance.mController.gameObject.TryGetComponent(out CustomPlantBehaviorController customPlantBehavior))
        {
            return customPlantBehavior.PreUpdateShooter();
        }

        return true;
    }
    
    public static void Postfix(ref Plant __instance)
    {
        if(__instance.mController == null || __instance.mController.gameObject == null)
        {
            return;
        }
        
        if(__instance.mController.gameObject.TryGetComponent(out CustomPlantBehaviorController customPlantBehavior))
        {
            customPlantBehavior.PostUpdateShooter();
        }
    }
}