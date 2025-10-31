using Il2CppInterop.Runtime.Injection;
using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppSource.Utils;
using MelonLoader;
using PvZReCoreLib.Plants.CustomPlantLib.Mint;
using PvZReCoreLib.Util;
using UnityEngine.AddressableAssets;
using Type = Il2CppSystem.Type;

namespace PvZReCoreLib.Plants.CustomPlantLib;

[RegisterTypeInIl2Cpp]
public class CustomPlantDefinition : PlantDefinition
{
    public int m_health = 300;
    public MintFamily m_mintFamily = MintFamily.None;
    
    public CustomPlantDefinition(IntPtr pointer) : base(pointer)
    {
        PlantDefinition peashooterDef = ReloadedUtils.GetPlantDefinition(SeedType.Peashooter);
        
        m_versusImage = null;
        m_chinaPlantImage = null;

        m_preview = null;

        m_chinaPreviewSprite = null;

        m_prefab = PersistentStorage.Store(new AssetReferenceGameObject(peashooterDef.m_prefab.m_AssetGUID)); // ! Needs to be redone
        m_preorderGameObject = null;
        m_chinaGameObject = null;

        PersistentStorage.Assign(this, nameof(m_defaultSkin), "PeaShooter"); // ! Needs to be redone
        
        m_easterEggGameObject = null;
        m_easterEggChance100 = 0;
    }
    
    public CustomPlantDefinition() : base(ClassInjector.DerivedConstructorPointer<CustomPlantDefinition>()) => ClassInjector.DerivedConstructorBody(this);
    
    public virtual Type GetCustomBehaviorType()
    {
        return null;
    }
}