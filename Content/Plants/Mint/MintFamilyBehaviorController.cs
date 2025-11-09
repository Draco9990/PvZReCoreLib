using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppReloaded.Gameplay;
using MelonLoader;
using PvZReCoreLib.Content.Common.Behavior;
using PvZReCoreLib.Content.Plants.Behavior;
using PvZReCoreLib.Util;
using UnityEngine;

namespace PvZReCoreLib.Content.Plants.Mint;

[RegisterTypeInIl2Cpp]
public class MintFamilyBehaviorController : CustomBehaviorController
{
    public MintFamily mMintFamily;
    
    public Il2CppReferenceField<Plant> mPlant;
    public Plant Plant => mPlant.Value;
    
    public bool bIsMintEffectActive = false;
    
    public MintFamilyBehaviorController(IntPtr pointer) : base(pointer)
    {
    }
    
    public static MintFamilyBehaviorController GetFor(Plant p)
    {
        if (p.mController is null || !p.mController.gameObject.TryGetComponent(out MintFamilyBehaviorController plantComp))
        {
            return null;
        }

        return plantComp;
    }

    public virtual void StartBuffEffect()
    {
        bIsMintEffectActive = true;
        
        Action<GameObject> prefabCaller = (GameObject prefab) =>
        {
            var glowEffect = Plant.mApp.InstantiateOffscreen(prefab, Plant.mController.gameObject.transform);
            glowEffect.SetName("MintGlowEffect");
            glowEffect.transform.SetLocalPositionAndRotation(new Vector3(105, -135, 0), Quaternion.identity);
            glowEffect.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        };
        RegistryBridge.LoadAssetFromAssetBundle(CoreLibMod.ModId, "assets/vfx/mintfx/prefab/mint_fx.prefab", prefabCaller);
        
        Plant.mController.gameObject.GetComponent<CustomPlantBehaviorController>().OnMintEffectStart();
    }

    public virtual void FinishBuffEffect()
    {
        if (!bIsMintEffectActive)
        {
            return;
        }
        bIsMintEffectActive = false;
        
        var glowEffect = Plant.mController.gameObject.transform.Find("MintGlowEffect");
        if (glowEffect != null)
        {
            Animator animator = glowEffect.GetComponent<Animator>();
            if (animator != null)
            {
                animator.Play("outro");
            }
            Destroy(glowEffect, 1.1f);
        }
        
        Plant.mController.gameObject.GetComponent<CustomPlantBehaviorController>().OnMintEffectEnd();
    }

    public override void Reset()
    {
        base.Reset();

        bIsMintEffectActive = false;
    }
}