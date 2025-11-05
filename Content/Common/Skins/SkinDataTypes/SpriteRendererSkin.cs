using Il2CppReloaded.Characters;
using PvZReCoreLib.Content.Common.Audio;
using PvZReCoreLib.Content.Plants;
using PvZReCoreLib.Util;
using UnityEngine;

namespace PvZReCoreLib.Content.Common.Skins.SkinDataTypes;

public class SpriteRendererSkin : SkinType
{
    #region Variables

    public string AssetBundleId;
    public string SkinPrefabId;

    #endregion

    #region Constructors



    #endregion

    #region Methods

    public override void ApplySkin(CharacterSkinController skinController)
    {
        var go = skinController.gameObject;
        
        Action<GameObject> onSkinLoaded = (skinGameObject) =>
        {
            // TODO maybe GC errors?
            var instance = UnityEngine.Object.Instantiate(skinGameObject, go.transform, false);
            instance.SetName("SpriteRendererSkin");

            instance.AddComponent<AnimationSoundPlayer>();
        };
        RegistryBridge.LoadAssetFromAssetBundle<GameObject>(AssetBundleId, SkinPrefabId, onSkinLoaded);

        skinController.m_skeleton.Skeleton.a = 0f;

        PlantExtension ext = PlantExtension.GetOrCreateExtension<PlantExtension>(skinController.gameObject);
        ext.CurrentSkin = this;
    }

    public override void CleanUpSkin(CharacterSkinController skinController)
    {
        var go = skinController.gameObject;
        var existingChild = go.transform.Find("SpriteRendererSkin");
        if (existingChild != null && existingChild.gameObject != null)
        {
            UnityEngine.Object.Destroy(existingChild.gameObject);
        }

        skinController.m_skeleton.Skeleton.a = 1f;
        
        PlantExtension ext = PlantExtension.GetOrCreateExtension<PlantExtension>(skinController.gameObject);
        ext.CurrentSkin = null;
    }

    public override void PlayAnimation(CharacterSkinController skinController, CharacterAnimationController animationController,
        string animationName, CharacterTracks track, float fps, AnimLoopType loopType)
    {
        var go = animationController.gameObject;
        var existingChild = go.transform.Find("SpriteRendererSkin");
        if (existingChild == null || existingChild.gameObject == null)
        {
            return;
        }
        
        var spriteAnimator = existingChild.GetComponent<Animator>();
        if (spriteAnimator == null)
        {
            return;
        }
        
        spriteAnimator.Play(animationName);
    }

    #endregion
}