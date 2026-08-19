using HarmonyLib;
using Il2CppReloaded;
using Il2CppReloaded.Characters;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using PvZReCoreLib.Content.Common.Skins.SkinDataTypes;
using PvZReCoreLib.Content.Common.Skins.SkinDataTypes.subtypes;
using PvZReCoreLib.Content.Plants;
using PvZReCoreLib.Content.Plants.Behavior;
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
        var requestedSkin = __instance.m_currentSkin;
        if (!SkinRegistry.PlantSkins.TryGetValue(requestedSkin, out var skinType))
        {
            return;
        }

        // SetCurrentSkin is called from CharacterSkinController.LateUpdate() every
        // frame, not just when the skin actually changes - without this guard we
        // were tearing down and reinstantiating the whole skin GameObject
        // (Animator included) 60x/second for every custom-skinned plant. That's
        // invisible for a plant whose resting animation is the Animator's own
        // default state, but it visibly snaps any plant whose current resting
        // state differs from that default (e.g. Bamboo Spartan sitting in
        // "battle_trance_idle1" post-shield-break) back to the default pose on
        // the very next frame.
        if (ReferenceEquals(extension.CurrentSkin, skinType))
        {
            return;
        }

        if (extension.CurrentSkin != null)
        {
            extension.CurrentSkin.CleanUpSkin(__instance.gameObject);
        }

        skinType.ApplySkin(__instance.gameObject);
        extension.CurrentSkin = (SkinType)skinType;
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

        // Plant.DoBlink() - a native cosmetic eye-blink system, random-rolled
        // (Common.Rand/RandRangeInt) on every plant's update tick regardless of
        // skin - calls this exact PlayAnimation overload to reset to "idle" as
        // part of its own routine (confirmed via the game's IL2CPP call-graph
        // metadata: PlayAnimation(string, CharacterTracks, float, AnimLoopType)
        // lists Plant.DoBlink as one of its 16 callers). Harmless for native
        // Spine-skeleton plants, whose "idle" really is their resting
        // animation and whose blink is a separate eye slot/texture overlay -
        // but any custom sprite-based plant has no such separate blink layer,
        // so this call instead yanks its entire full-body sprite swap back to
        // "idle" regardless of what its own CustomPlantBehaviorController
        // currently wants shown. Those controllers are the sole authority over
        // their own animation state, so blink's forced "idle" is dropped for
        // them - but only blink's: a custom plant that legitimately has its
        // own state literally named "idle" (e.g. Endurian's tier-1 pose) still
        // needs to reach the animator when IT is the one asking, which is
        // exactly what IsExecutingOwnPlayAnimation distinguishes.
        if (animationName == "idle"
            && plantExtension.CustomBehaviorController != null
            && !CustomPlantBehaviorController.IsExecutingOwnPlayAnimation)
        {
            return;
        }

        plantExtension.CurrentSkin.PlayAnimation(__instance.gameObject, animationName, track, fps, loopType);
    }
}