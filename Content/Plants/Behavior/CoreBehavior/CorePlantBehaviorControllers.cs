using Il2CppReloaded.Gameplay;
using MelonLoader;

namespace PvZReCoreLib.Content.Plants.Behavior.CoreBehavior;

[RegisterTypeInIl2Cpp]
public class PeashooterBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public PeashooterBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class SunflowerBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    private float launchTimerCache; 
    
    #endregion

    #region Constructors

    public SunflowerBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    public override void PreProductionPlantUpdate()
    {
        base.PreProductionPlantUpdate();

        launchTimerCache = Plant.mLaunchCounter;
    }

    public override void PostProductionPlantUpdate()
    {
        base.PostProductionPlantUpdate();
        
        if (Plant.mLaunchCounter > launchTimerCache && bMintEffectActive)
        {
            Board.AddCoin(Plant.mX, Plant.mY, CoinType.Sun, CoinMotion.FromPlant);
        }
    }

    #endregion
}

[RegisterTypeInIl2Cpp]
public class CherrybombBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public CherrybombBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class WallnutBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public WallnutBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class PotatomineBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public PotatomineBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class SnowpeaBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public SnowpeaBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class ChomperBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public ChomperBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class RepeaterBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public RepeaterBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class PuffshroomBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public PuffshroomBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class SunshroomBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    private float launchTimerCache;
    private int launchRateCache;

    #endregion

    #region Constructors

    public SunshroomBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    public override void OnMintEffectStart()
    {
        base.OnMintEffectStart();

        launchRateCache = Plant.mLaunchRate;
        
        Plant.mLaunchRate = 500;
        if (Plant.mState == PlantState.SunshroomSmall)
        {
            Plant.mStateCountdown = 0;
        }
    }

    public override void OnMintEffectEnd()
    {
        base.OnMintEffectEnd();

        Plant.mLaunchRate = launchRateCache;
    }
    
    public override void PreProductionPlantUpdate()
    {
        base.PreProductionPlantUpdate();

        launchTimerCache = Plant.mLaunchCounter;
    }

    public override void PostProductionPlantUpdate()
    {
        base.PostProductionPlantUpdate();
        
        if (Plant.mLaunchCounter > launchTimerCache && bMintEffectActive)
        {
            Board.AddCoin(Plant.mX, Plant.mY, CoinType.Sun, CoinMotion.FromPlant);
        }
    }

    #endregion
}

[RegisterTypeInIl2Cpp]
public class FumeshroomBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public FumeshroomBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class GravebusterBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public GravebusterBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class HypnoshroomBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public HypnoshroomBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class ScaredyshroomBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public ScaredyshroomBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class IceshroomBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public IceshroomBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class DoomshroomBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public DoomshroomBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class LilypadBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public LilypadBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class SquashBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public SquashBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class ThreepeaterBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public ThreepeaterBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class TanglekelpBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public TanglekelpBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class JalapenoBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public JalapenoBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class SpikeweedBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public SpikeweedBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class TorchwoodBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public TorchwoodBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class TallnutBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public TallnutBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class SeashroomBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public SeashroomBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class PlanternBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public PlanternBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion

    /*[HarmonyPatch(typeof(Board), "UpdateFog")]
    private static class PlanternFogMintPatch
    {
        static bool Prefix(Board __instance)
        {
            foreach (var plant in __instance.m_plants.m_list)
            {
                if (plant == null || plant.mItem == null)
                {
                    continue;
                }
                if (plant.mItem.mSeedType != SeedType.Plantern)
                {
                    continue;
                }
                if (!plant.mItem.mController.gameObject.GetComponent<CustomPlantBehaviorController>().bMintEffectActive)
                {
                    continue;
                }
                
                // Skip fog update
                return false;
            }

            return true;
        }
    }*/
}

[RegisterTypeInIl2Cpp]
public class CactusBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public CactusBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class BloverBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public BloverBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class SplitpeaBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public SplitpeaBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class StarfruitBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public StarfruitBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class PumpkinshellBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public PumpkinshellBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class MagnetshroomBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public MagnetshroomBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class CabbagepultBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public CabbagepultBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class FlowerpotBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public FlowerpotBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class KernelpultBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public KernelpultBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class InstantCoffeeBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public InstantCoffeeBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class GarlicBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public GarlicBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class UmbrellaBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public UmbrellaBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class MarigoldBehaviorController : CustomPlantBehaviorController
{
    #region Variables
    
    private float launchTimerCache; 

    #endregion

    #region Constructors

    public MarigoldBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    public override void PreProductionPlantUpdate()
    {
        base.PreProductionPlantUpdate();

        launchTimerCache = Plant.mLaunchCounter;
    }

    public override void PostProductionPlantUpdate()
    {
        base.PostProductionPlantUpdate();
        
        if (Plant.mLaunchCounter > launchTimerCache && bMintEffectActive)
        {
            Board.AddCoin(Plant.mX, Plant.mY, CoinType.Diamond, CoinMotion.FromPlant);
        }
    }

    #endregion
}

[RegisterTypeInIl2Cpp]
public class MelonpultBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public MelonpultBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class GatlingpeaBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public GatlingpeaBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class TwinsunflowerBehaviorController : CustomPlantBehaviorController
{
    #region Variables
    
    private float launchTimerCache; 

    #endregion

    #region Constructors

    public TwinsunflowerBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods
    
    public override void PreProductionPlantUpdate()
    {
        base.PreProductionPlantUpdate();

        launchTimerCache = Plant.mLaunchCounter;
    }

    public override void PostProductionPlantUpdate()
    {
        base.PostProductionPlantUpdate();
        
        if (Plant.mLaunchCounter > launchTimerCache && bMintEffectActive)
        {
            Board.AddCoin(Plant.mX, Plant.mY, CoinType.LargeSun, CoinMotion.FromPlant);
        }
    }

    #endregion
}

[RegisterTypeInIl2Cpp]
public class GloomshroomBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public GloomshroomBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class CattailBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public CattailBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class WintermelonBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public WintermelonBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class GoldMagnetBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public GoldMagnetBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class SpikerockBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public SpikerockBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class CobcannonBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public CobcannonBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class ImitaterBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public ImitaterBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}

[RegisterTypeInIl2Cpp]
public class ExplodeONutBehaviorController : CustomPlantBehaviorController
{
    #region Variables

    #endregion

    #region Constructors

    public ExplodeONutBehaviorController(IntPtr pointer) : base(pointer)
    {
    }

    #endregion

    #region Methods

    #endregion
}
