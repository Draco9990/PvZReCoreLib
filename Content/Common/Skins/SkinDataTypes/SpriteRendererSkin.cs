using Il2CppReloaded.Characters;
using Il2CppSource.Controllers;
using MelonLoader;
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

            instance.transform.Find("anim").gameObject.AddComponent<AnimationScripts>();

            // ReloadedController.InitSorting() only scans for Renderer components once, at Awake -
            // well before this async asset-bundle load finishes and the skin's own renderers exist.
            // Those renderers never get registered with the game's depth-sorting system as a result,
            // so they render at Unity's default sortingOrder/layer - which sits above fog and other
            // properly-managed layers. Re-running InitSorting() now that the renderers actually
            // exist fixes that; plants don't move once placed, so a one-time re-scan is sufficient.
            ReloadedController reloadedController = obj.GetComponent<ReloadedController>();
            if (reloadedController == null && obj.transform.parent != null)
            {
                reloadedController = obj.transform.parent.GetComponent<ReloadedController>();
            }

            if (reloadedController != null)
            {
                reloadedController.InitSorting();
            }
            else
            {
                MelonLogger.Warning($"[CoreLib] SpriteRendererSkin.ApplySkin: could not find a ReloadedController on '{obj.name}' or its parent to refresh sorting - custom skin sprites may render on the wrong layer.");
            }
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