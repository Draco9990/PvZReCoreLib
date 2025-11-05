using HarmonyLib;
using Il2CppReloaded;
using Il2CppReloaded.Characters;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using PvZReCoreLib.Content.Common.Skins.SkinDataTypes;
using PvZReCoreLib.Content.Plants;
using PvZReCoreLib.Util;

namespace PvZReCoreLib.Content.Common.Skins;

class SkinExtension : ClassExtension<CharacterSkinController>
{
    public SkinType CurrentSkin = null;
}

public class SkinRegistry
{
    #region Variables
    
    public static Dictionary<string, SkinType> RegisteredSkins = new Dictionary<string, SkinType>();

    #endregion

    #region Constructors

    public static void Init()
    {
        // Register all default skins
        for(int i = 0; i < (int)SeedType.NumSeedsInChooser; i++)
        {
            SeedType seedType = (SeedType)i;
            var definition = AppCore.GetService<IDataService>().Cast<DataService>().GetPlantDefinition(seedType);
            var defaultSkin = new BaseSpriterSkin()
            {
                ForSeedType = seedType,
                skinId = $"{definition.m_defaultSkin}"
            };
            RegisterSkin(defaultSkin);
        }
    }

    #endregion

    #region Methods
    
    public static string RegisterSkin(SkinType skinType)
    {
        if(!RegisteredSkins.ContainsKey(skinType.skinId))
        {
            RegisteredSkins.Add(skinType.skinId, skinType);
        }

        return skinType.skinId;
    }

    #endregion
}

[HarmonyPatch(typeof(CharacterSkinController), nameof(CharacterSkinController.SetCurrentSkin))]
public class CustomSkinRegistry_SetCurrentSkin_Patch
{
    public static void Postfix(CharacterSkinController __instance)
    {
        var extension = SkinExtension.GetOrCreateExtension<SkinExtension>(__instance);
        if(extension.CurrentSkin != null)
        {
            extension.CurrentSkin.CleanUpSkin(__instance);
            extension.CurrentSkin = null;
        }
        
        var requestedSkin = __instance.m_currentSkin;
        if(SkinRegistry.RegisteredSkins.TryGetValue(requestedSkin, out var skinType))
        {
            skinType.ApplySkin(__instance);
            extension.CurrentSkin = skinType;
        }
    }
}

[HarmonyPatch(typeof(CharacterAnimationController), nameof(CharacterAnimationController.PlayAnimation))]
public class CustomSkinRegistry_PlayAnimation_Patch
{
    public static void Postfix(CharacterAnimationController __instance,
        string animationName,
        CharacterTracks track,
        float fps,
        AnimLoopType loopType)
    {
        var plantExtension = PlantExtension.GetOrCreateExtension<PlantExtension>(__instance.gameObject.transform.parent.gameObject);
        if (plantExtension == null || plantExtension.source == null)
        {
            return;
        }
        
        var skinExtension = SkinExtension.GetOrCreateExtension<SkinExtension>(plantExtension.source.mController.m_skinController);
        if (skinExtension == null || skinExtension.CurrentSkin == null)
        {
            return;
        }
        skinExtension.CurrentSkin.PlayAnimation(plantExtension.source.mController.m_skinController, __instance, animationName, track, fps, loopType);
    }
}