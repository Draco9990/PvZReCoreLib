using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppReloaded.Gameplay;
using MelonLoader;
using PvZReCoreLib.Plants.CustomPlantLib.Behavior;
using UnityEngine;

namespace PvZReCoreLib.Plants.CustomPlantLib.Mint;

[RegisterTypeInIl2Cpp]
public class MintFamilyBehaviorController : MonoBehaviour
{
    public MintFamily mMintFamily;
    
    public Il2CppReferenceField<Plant> mPlant;
    public Il2CppReferenceField<Board> mBoard;
    
    public Plant Plant => mPlant.Value;
    public Board Board => mBoard.Value;
    
    public MintFamilyBehaviorController(IntPtr pointer) : base(pointer)
    {
    }
    
    public MintFamilyBehaviorController() : base(ClassInjector.DerivedConstructorPointer<MintFamilyBehaviorController>()) => ClassInjector.DerivedConstructorBody(this);
    
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
        Plant.mController.gameObject.GetComponent<CustomPlantBehaviorController>().OnMintEffectStart();
    }

    public virtual void FinishBuffEffect()
    {
        Plant.mController.gameObject.GetComponent<CustomPlantBehaviorController>().OnMintEffectEnd();
    }
}