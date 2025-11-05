using Il2CppReloaded.Characters;
using Il2CppReloaded.Gameplay;

namespace PvZReCoreLib.Content.Plants.Skins.SkinDataTypes;

public class SkinType
{
    #region Variables

    public SeedType ForSeedType = SeedType.None;
    public string skinId = string.Empty;

    #endregion

    #region Constructors



    #endregion

    #region Methods
    
    public virtual void ApplySkin(CharacterSkinController skinController) {}
    public virtual void CleanUpSkin(CharacterSkinController skinController) {}
    
    public virtual void PlayAnimation(CharacterSkinController skinController, CharacterAnimationController animationController,
        string animationName,
        CharacterTracks track,
        float fps,
        AnimLoopType loopType
    ) {}

    #endregion
}