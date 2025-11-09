using PvZReCoreLib.Content.Common.Skins.SkinDataTypes.subtypes;
using PvZReCoreLib.Content.Projectiles;
using UnityEngine;

namespace PvZReCoreLib.Content.Common.Skins.SkinDataTypes.Subtypes.Projectile;

public class SpriteRendererProjectileSkin : SpriteRendererSkin, IProjectileSkin
{
    #region Variables



    #endregion

    #region Constructors



    #endregion

    #region Methods

    public override void ApplySkin(GameObject obj)
    {
        base.ApplySkin(obj);

        foreach (var o in obj.transform)
        {
            var goT = o.TryCast<Transform>();
            if (goT == null)
            {
                continue;
            }
            
            var go = goT.gameObject;
            if (go == null || go.name == "Shadow")
            {
                continue;
            }

            go.SetActive(false);
        }
        
        ProjectileExtension ext = ProjectileExtension.GetOrCreateExtension<ProjectileExtension>(obj);
        ext.CurrentSkin = this;
    }

    public override void CleanUpSkin(GameObject obj)
    {
        base.CleanUpSkin(obj);
        
        ProjectileExtension ext = ProjectileExtension.GetOrCreateExtension<ProjectileExtension>(obj);
        ext.CurrentSkin = null;
    }

    #endregion
}