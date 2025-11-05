using Il2CppInterop.Runtime.Injection;
using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppSource.Utils;
using MelonLoader;
using PvZReCoreLib.Content.Common.Behavior;
using PvZReCoreLib.Util;
using UnityEngine.AddressableAssets;
using Type = Il2CppSystem.Type;

namespace PvZReCoreLib.Content.Projectiles;

[RegisterTypeInIl2Cpp]
public class CustomProjectileDefinition : ProjectileDefinition
{
    #region Variables



    #endregion

    #region Constructors

    public CustomProjectileDefinition(IntPtr pointer) : base(pointer)
    {
        var peaDefinition = ReloadedUtils.DataService.ProjectileDefinitions[0];
        
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

    #endregion
}

[RegisterTypeInIl2Cpp]
public class CustomProjectileBehaviorController : CustomBehaviorController<Projectile>
{
    #region Variables

    

    #endregion

    #region Constructors

    public CustomProjectileBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}