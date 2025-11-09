using Il2CppReloaded.Characters;
using PvZReCoreLib.Content.Common.Audio;
using PvZReCoreLib.Content.Plants;
using PvZReCoreLib.Util;
using UnityEngine;

namespace PvZReCoreLib.Content.Common.Skins.SkinDataTypes;

public abstract class SpriteRendererSkin : SkinType
{
    #region Variables

    public string AssetBundleId;
    public string SkinPrefabId;
    
    public Vector3 ScaleOverride = Vector3.one;

    #endregion

    #region Constructors



    #endregion

    #region Methods

    public override void ApplySkin(GameObject obj)
    {
        Action<GameObject> onSkinLoaded = (skinGameObject) =>
        {
            // TODO maybe GC errors?
            var instance = UnityEngine.Object.Instantiate(skinGameObject, obj.transform, false);
            instance.SetName("SpriteRendererSkin");
            instance.transform.localScale = ScaleOverride;

            instance.AddComponent<AnimationSoundPlayer>();
        };
        RegistryBridge.LoadAssetFromAssetBundle<GameObject>(AssetBundleId, SkinPrefabId, onSkinLoaded);
    }

    public override void CleanUpSkin(GameObject obj)
    {
        var existingChild = obj.transform.Find("SpriteRendererSkin");
        if (existingChild != null && existingChild.gameObject != null)
        {
            UnityEngine.Object.Destroy(existingChild.gameObject);
        }
    }

    public override void PlayAnimation(
        GameObject obj,
        string animationName, CharacterTracks track, float fps, AnimLoopType loopType)
    {
        var existingChild = obj.transform.Find("SpriteRendererSkin/anim");
        if (existingChild == null || existingChild.gameObject == null)
        {
            return;
        }
        
        var spriteAnimator = existingChild.GetComponent<Animator>();
        if (spriteAnimator == null)
        {
            return;
        }
        
        if(spriteAnimator.GetCurrentStateName(0) == animationName)
        {
            return;
        }
        spriteAnimator.Play(animationName);
    }

    #endregion
}