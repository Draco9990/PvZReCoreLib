using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppReloaded.Gameplay;
using MelonLoader;
using UnityEngine;

namespace PvZReCoreLib.Content.Common.Behavior;

[RegisterTypeInIl2Cpp]
public class CustomBehaviorController<TObjectType> : MonoBehaviour where TObjectType : Il2CppObjectBase
{
    public Il2CppReferenceField<TObjectType> mObject;
    public Il2CppReferenceField<Board> mBoard;
    
    public TObjectType Object => mObject.Value;
    public Board Board => mBoard.Value;
    
    #region Variables



    #endregion

    #region Constructors

    public CustomBehaviorController(IntPtr pointer) : base(pointer)
    {
    }
    
    public CustomBehaviorController() : base(ClassInjector.DerivedConstructorPointer<CustomBehaviorController<TObjectType>>()) => ClassInjector.DerivedConstructorBody(this);

    #endregion

    #region Methods

    public virtual void Awake() { }
    public virtual void Update() { }
    
    public Action PostObjectInitialized;
    public virtual void PostObjectInitialize() { PostObjectInitialized?.Invoke(); }

    #endregion
}