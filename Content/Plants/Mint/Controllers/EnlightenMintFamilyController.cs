using MelonLoader;

namespace PvZReCoreLib.Content.Plants.Mint.Controllers;

[RegisterTypeInIl2Cpp]
public class EnlightenMintFamilyController : MintFamilyBehaviorController
{
    #region Variables
    

    #endregion

    #region Constructors

    public EnlightenMintFamilyController(IntPtr pointer) : base(pointer)
    {
        
    }

    #endregion

    #region Methods

    public override void StartBuffEffect()
    {
        base.StartBuffEffect();

        // TODO migrate glow effect
        /*Plant.mLaunchRate = Common.RandRangeInt(Plant.mLaunchRate - 150, Plant.mLaunchRate);
        
        EnlightenMintDefinition mintDef = ReloadedUtils.GetPlantDefinition(CustomSeedType.ElightenMint).Cast<EnlightenMintDefinition>();
        if (mintDef.m_GlowEffectPrefab != null)
        {
            var glowEffect = Plant.mApp.InstantiateOffscreen(mintDef.m_GlowEffectPrefab, Plant.mController.gameObject.transform);
            
            glowEffect.transform.SetLocalPositionAndRotation(new Vector3(105, -105, 0), Quaternion.identity);
            glowEffect.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            Destroy(glowEffect, 5);
        }*/
    }

    #endregion
}