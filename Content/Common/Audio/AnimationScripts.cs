using Il2CppReloaded;
using Il2CppReloaded.Services;
using MelonLoader;
using PvZReCoreLib.Content.Plants;
using PvZReCoreLib.Content.Projectiles;
using UnityEngine;

namespace PvZReCoreLib.Content.Common.Audio;

[RegisterTypeInIl2Cpp]
public class AnimationScripts : MonoBehaviour
{ 
    private AudioSource audioSource;

    private void Awake()
    {
        if (!audioSource)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    // Called by Animation Events with a clip baked in as the event's object reference.
    public void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null)
        {
            return;
        }

        var sfxVolume = AppCore.GetService<ISettingsService>().SoundEffectVolume;
        audioSource.PlayOneShot(clip, sfxVolume);
    }

    // Called by Animation Events with a semantic name baked in as the event's string
    // parameter (e.g. "punch"). Forwards to whichever custom controller owns this
    // plant/projectile so it can pick a random clip from a registered pool - see
    // CustomPlantBehaviorController/CustomProjectileBehaviorController.RegisterSoundPool.
    public void PlaySoundEvent(string eventName)
    {
        var mainGo = GetMainGameObject();

        PlantExtension ple = PlantExtension.GetExtension<PlantExtension>(mainGo);
        if (ple?.CustomBehaviorController != null)
        {
            ple.CustomBehaviorController.OnAnimationSoundEvent(eventName);
            return;
        }

        ProjectileExtension pre = ProjectileExtension.GetExtension<ProjectileExtension>(mainGo);
        if (pre?.CustomBehaviorController != null)
        {
            pre.CustomBehaviorController.OnAnimationSoundEvent(eventName);
        }
    }

    public void ExecuteAttack()
    {
        
    }

    public void OwnerDie()
    {
        var mainGo = GetMainGameObject();

        PlantExtension ple = PlantExtension.GetExtension<PlantExtension>(mainGo);
        if (ple != null)
        {
            ple.source.Die();
            return;
        }
        
        ProjectileExtension pre = ProjectileExtension.GetExtension<ProjectileExtension>(mainGo);
        if (pre != null)
        {
            pre.source.Die();
            return;
        }
    }

    private GameObject GetMainGameObject()
    {
        // This script is on the 'anim' child of a Renderer, which itself is a child of a Render field. The correct parent is 3 up
        return gameObject.transform.parent.parent.parent.gameObject;
    }
}