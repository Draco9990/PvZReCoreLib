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

    // Called by Animation Events
    public void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null)
        {
            return;
        }
        
        audioSource.PlayOneShot(clip);
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