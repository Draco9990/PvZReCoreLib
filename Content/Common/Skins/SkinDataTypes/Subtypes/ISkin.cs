using Il2CppReloaded.Characters;
using UnityEngine;

namespace PvZReCoreLib.Content.Common.Skins.SkinDataTypes.subtypes;

public interface ISkin
{
    string GetSkinId();

    public void ApplySkin(GameObject obj);
    public void CleanUpSkin(GameObject obj);

    public void PlayAnimation(
        GameObject obj,
        string animationName,
        CharacterTracks track,
        float fps,
        AnimLoopType loopType
    );
}