using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppReloaded.Gameplay;
using MelonLoader;
using UnityEngine;

namespace PvZReCoreLib.Content.Common.Behavior;

[RegisterTypeInIl2Cpp]
public class CustomBehaviorController : MonoBehaviour
{
    public Il2CppReferenceField<Board> mBoard;
    
    public Board Board => mBoard.Value;
    
    #region Variables



    #endregion

    #region Constructors

    public CustomBehaviorController(IntPtr pointer) : base(pointer)
    {
    }
    
    public CustomBehaviorController() : base(ClassInjector.DerivedConstructorPointer<CustomBehaviorController>()) => ClassInjector.DerivedConstructorBody(this);

    #endregion

    #region Methods

    public virtual void Awake() { }
    public virtual void Update() { }
    
    public Action PostInitializeEvent;
    public virtual void PostInitialize() { PostInitializeEvent?.Invoke(); }

    public Action OnResetEvent;
    public virtual void Reset() { OnResetEvent?.Invoke(); }

    #endregion
}