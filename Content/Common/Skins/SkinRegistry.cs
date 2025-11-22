using HarmonyLib;
using Il2CppReloaded;
using Il2CppReloaded.Characters;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using PvZReCoreLib.Content.Common.Skins.SkinDataTypes;
using PvZReCoreLib.Content.Common.Skins.SkinDataTypes.subtypes;
using PvZReCoreLib.Content.Plants;
using PvZReCoreLib.Util;

namespace PvZReCoreLib.Content.Common.Skins;

public class SkinRegistry
{
    #region Variables
    
    public static Dictionary<string, IPlantSkin> PlantSkins = new Dictionary<string, IPlantSkin>();
    public static Dictionary<string, IProjectileSkin> ProjectileSkins = new Dictionary<string, IProjectileSkin>();

    #endregion

    #region Constructors

    public static void Init()
    {
        // Register all default skins
        for(int i = 0; i < (int)SeedType.NumSeedTypes; i++)
        {
            SeedType seedType = (SeedType)i;
            var definition = AppCore.GetService<IDataService>().Cast<DataService>().GetPlantDefinition(seedType);
            var defaultSkin = new BaseSpriterPlantSkin()
            {
                skinId = $"{definition.m_defaultSkin}"
            };
            RegisterPlantSkin(defaultSkin);
        }
    }

    #endregion

    #region Methods
    
    public static string RegisterPlantSkin(IPlantSkin skin)
    {
        if(!PlantSkins.ContainsKey(skin.GetSkinId()))
        {
            PlantSkins.Add(skin.GetSkinId(), skin);
        }

        return skin.GetSkinId();
    }
    
    public static string RegisterProjectileSkin(IProjectileSkin skin)
    {
        if(!ProjectileSkins.ContainsKey(skin.GetSkinId()))
        {
            ProjectileSkins.Add(skin.GetSkinId(), skin);
        }

        return skin.GetSkinId();
    }

    #endregion
}

[HarmonyPatch(typeof(CharacterSkinController), nameof(CharacterSkinController.SetCurrentSkin))]
public class CustomSkinRegistry_SetCurrentSkin_Patch
{
    public static void Postfix(CharacterSkinController __instance)
    {
        var extension = PlantExtension.GetOrCreateExtension<PlantExtension>(__instance.gameObject);
        if(extension.CurrentSkin != null)
        {
            extension.CurrentSkin.CleanUpSkin(__instance.gameObject);
        }
        
        var requestedSkin = __instance.m_currentSkin;
        if(SkinRegistry.PlantSkins.TryGetValue(requestedSkin, out var skinType))
        {
            skinType.ApplySkin(__instance.gameObject);
            extension.CurrentSkin = (SkinType)skinType;
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
        if (plantExtension == null || plantExtension.CurrentSkin == null)
        {
            return;
        }
        
        plantExtension.CurrentSkin.PlayAnimation(__instance.gameObject, animationName, track, fps, loopType);
    }
}