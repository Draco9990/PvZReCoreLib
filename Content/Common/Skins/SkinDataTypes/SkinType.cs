using Il2CppReloaded.Characters;
using Il2CppReloaded.Gameplay;
using UnityEngine;

namespace PvZReCoreLib.Content.Common.Skins.SkinDataTypes;

public abstract class SkinType
{
    #region Variables
    
    public string skinId = string.Empty;

    #endregion

    #region Constructors



    #endregion

    #region Methods
    
    public virtual void ApplySkin(GameObject obj) {}
    public virtual void CleanUpSkin(GameObject obj) {}
    
    public virtual void PlayAnimation(
        GameObject obj,
        string animationName,
        CharacterTracks track,
        float fps,
        AnimLoopType loopType
    ) {}
    
    public string GetSkinId()
    {
        return skinId;
    }

    #endregion
}