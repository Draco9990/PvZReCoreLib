using MelonLoader;
using UnityEngine;

namespace PvZReCoreLib.Content.Common.Audio;

[RegisterTypeInIl2Cpp]
public class AnimationSoundPlayer : MonoBehaviour
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
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip);
    }
}