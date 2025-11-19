using Il2CppInterop.Runtime.Injection;
using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppSource.Utils;
using MelonLoader;
using PvZReCoreLib.Content.Common.Skins;
using PvZReCoreLib.Content.Common.Skins.SkinDataTypes;
using PvZReCoreLib.Content.Common.Skins.SkinDataTypes.subtypes;
using PvZReCoreLib.Content.Plants.Mint;
using PvZReCoreLib.Util;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Guid = Il2CppSystem.Guid;
using Type = Il2CppSystem.Type;

namespace PvZReCoreLib.Content.Plants;

[RegisterTypeInIl2Cpp]
public class CustomPlantDefinition : PlantDefinition
{
    #region Variables

    public int m_health = 300;
    public MintFamily m_mintFamily = MintFamily.None;

    public string m_description = "";

    public AssetReferenceSprite m_almanacBackground;

    #endregion
    
    public CustomPlantDefinition(IntPtr pointer) : base(pointer)
    {
        PlantDefinition peashooterDef = ReloadedUtils.GetPlantDefinition(SeedType.Peashooter);

        m_seedType = CustomContentRegistry.RequestFreeSeedType();
        m_animationType = ReanimationType.Peashooter;
        m_packetIndex = (int)m_seedType;
        
        m_versusImage = null;
        m_chinaPlantImage = null;

        m_preview = null;

        m_chinaPreviewSprite = null;

        m_prefab = PersistentStorage.Store(new AssetReferenceGameObject(peashooterDef.m_prefab.m_AssetGUID)); // ! Needs to be redone
        m_preorderGameObject = null;
        m_chinaGameObject = null;
        
        m_previewSpriteOffset = Vector2.zero;
        m_previewSpriteScale = 1f;
        
        m_easterEggGameObject = null;
        m_easterEggChance100 = 0;
    }
    
    public CustomPlantDefinition() : base(ClassInjector.DerivedConstructorPointer<CustomPlantDefinition>()) => ClassInjector.DerivedConstructorBody(this);
    
    public virtual Type GetCustomBehaviorType()
    {
        return null;
    }
    
    private void SetTextLocal(string text, bool localize, string fieldName)
    {
        string n = text;
        if (!localize)
        {
            n = "[RAW]" + text;
        }
        
        PersistentStorage.Assign(this, fieldName, n);
    }

    public void SetSeedPacketImage(string assetBundleId, string assetPath)
    {
        m_plantImage = PersistentStorage.Store(new AssetReferenceSprite(Guid.NewGuid().ToString()));
        RegistryBridge.RegisterSpriteFromAssetBundle(assetBundleId, assetPath, keyOverride: m_plantImage.m_AssetGUID);
    }
    
    public void SetPreviewImage(string assetBundleId, string assetPath)
    {
        m_previewSprite = PersistentStorage.Store(new AssetReferenceSprite(Guid.NewGuid().ToString()));
        RegistryBridge.RegisterSpriteFromAssetBundle(assetBundleId, assetPath, keyOverride: m_previewSprite.m_AssetGUID, pivot: new Vector2(0, 1));
    }

    public void SetName(string text, bool localize = true)
    {
        SetTextLocal(text, localize, nameof(m_plantName));
    }
    public void SetTooltip(string text, bool localize = true)
    {
        SetTextLocal(text, localize, nameof(m_plantToolTip));
    }
    public void SetDescription(string text, bool localize = true)
    {
        string n = text;
        if (!localize)
        {
            n = "[RAW]" + text;
        }

        m_description = n;
    }

    public void RegisterSkin(IPlantSkin skinType)
    {
        SkinRegistry.RegisterPlantSkin(skinType);

        if (string.IsNullOrEmpty(m_defaultSkin))
        {
            PersistentStorage.Assign(this, nameof(m_defaultSkin), skinType.GetSkinId());
        }
    }
    
    public void SetAlmanacBackground(string assetBundleId, string assetPath)
    {
        m_almanacBackground = PersistentStorage.Store(new AssetReferenceSprite(Guid.NewGuid().ToString()));
        RegistryBridge.RegisterSpriteFromAssetBundle(assetBundleId, assetPath, keyOverride: m_almanacBackground.m_AssetGUID);
    }
}

[RegisterTypeInIl2Cpp]
public class CustomAlmanacEntryData : AlmanacEntryData
{
    public string m_descriptionHeader;
    
    public CustomAlmanacEntryData(IntPtr pointer) : base(pointer)
    {
    }
    
    public CustomAlmanacEntryData() : base(ClassInjector.DerivedConstructorPointer<CustomAlmanacEntryData>())
    {
        ClassInjector.DerivedConstructorBody(this);
    }

    public void LoadFrom(CustomPlantDefinition plantDef)
    {
        SetTextLocal(plantDef.m_plantName, false, nameof(m_entryName));
        SetTextLocal(plantDef.m_description, false, nameof(m_entryDescription));
        m_descriptionHeader = plantDef.m_plantToolTip;

        if (plantDef.m_refreshTime > 3000)
        {
            SetTextLocal("Very Slow", false, nameof(m_entryRecharge));
        }
        else if (plantDef.m_refreshTime > 2000)
        {
            SetTextLocal("Slow", false, nameof(m_entryRecharge));
        }
        else if (plantDef.m_refreshTime > 1000)
        {
            SetTextLocal("Medium", false, nameof(m_entryRecharge));
        }
        else
        {
            SetTextLocal("Fast", false, nameof(m_entryRecharge));
        }

        m_entryThumbnail = PersistentStorage.EmptySpriteRef;
        m_entryThumbnailBackground = PersistentStorage.EmptySpriteRef;
        
        m_entrySunCost = plantDef.m_seedCost;

        m_entryPortrait = PersistentStorage.Store(new AssetReferenceSprite(plantDef.m_plantImage.m_AssetGUID));
        
        m_entryThumbnail = PersistentStorage.Store(new AssetReferenceSprite(plantDef.m_plantImage.m_AssetGUID));
        m_entryThumbnailBackground = PersistentStorage.Store(new AssetReferenceSprite(plantDef.m_almanacBackground.m_AssetGUID));

        m_entryType = AlmanacEntryType.Plant;
        m_seedType = plantDef.m_seedType;
        m_zombieType = ZombieType.Invalid;
    }

    private void SetTextLocal(string text, bool localize, string fieldName)
    {
        string n = text;
        if (!localize)
        {
            n = "[RAW]" + text;
        }
        
        PersistentStorage.Assign(this, fieldName, n);
    }
}