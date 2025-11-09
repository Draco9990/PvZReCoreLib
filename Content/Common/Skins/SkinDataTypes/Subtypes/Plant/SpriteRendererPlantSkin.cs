using Il2CppReloaded.Characters;
using PvZReCoreLib.Content.Common.Skins.SkinDataTypes.subtypes;
using PvZReCoreLib.Content.Plants;
using UnityEngine;

namespace PvZReCoreLib.Content.Common.Skins.SkinDataTypes.Subtypes.Plant;

public class SpriteRendererPlantSkin : SpriteRendererSkin, IPlantSkin
{
    #region Variables



    #endregion

    #region Constructors



    #endregion

    #region Methods

    public override void ApplySkin(GameObject obj)
    {
        base.ApplySkin(obj);
        
        obj.TryGetComponent(out CharacterSkinController skinController);
        if (skinController != null)
        {
            skinController.m_skeleton.Skeleton.a = 0f;
        }
        
        PlantExtension ext = PlantExtension.GetOrCreateExtension<PlantExtension>(obj.transform.parent.gameObject);
        ext.CurrentSkin = this;
    }

    public override void CleanUpSkin(GameObject obj)
    {
        base.CleanUpSkin(obj);
        
        obj.TryGetComponent(out CharacterSkinController skinController);
        if (skinController != null)
        {
            skinController.m_skeleton.Skeleton.a = 1f;
        }
        
        PlantExtension ext = PlantExtension.GetOrCreateExtension<PlantExtension>(skinController.gameObject.transform.parent.gameObject);
        ext.CurrentSkin = null;
    }

    #endregion
}