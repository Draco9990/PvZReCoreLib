using Il2CppInterop.Runtime;
using Il2CppReloaded.Gameplay;

namespace PvZReCoreLib.Content.Plants.Behavior.CoreBehavior;

public class CorePlantBehaviorUtils
{
    #region Variables



    #endregion

    #region Constructors



    #endregion

    #region Methods

    public static Il2CppSystem.Type GetBehaviorType(SeedType seedType)
    {
        switch (seedType)
        {
            case SeedType.Peashooter:      return Il2CppType.Of<PeashooterBehaviorController>();
            case SeedType.Sunflower:       return Il2CppType.Of<SunflowerBehaviorController>();
            case SeedType.Cherrybomb:      return Il2CppType.Of<CherrybombBehaviorController>();
            case SeedType.Wallnut:         return Il2CppType.Of<WallnutBehaviorController>();
            case SeedType.Potatomine:      return Il2CppType.Of<PotatomineBehaviorController>();
            case SeedType.Snowpea:         return Il2CppType.Of<SnowpeaBehaviorController>();
            case SeedType.Chomper:         return Il2CppType.Of<ChomperBehaviorController>();
            case SeedType.Repeater:        return Il2CppType.Of<RepeaterBehaviorController>();
            case SeedType.Puffshroom:      return Il2CppType.Of<PuffshroomBehaviorController>();
            case SeedType.Sunshroom:       return Il2CppType.Of<SunshroomBehaviorController>();
            case SeedType.Fumeshroom:      return Il2CppType.Of<FumeshroomBehaviorController>();
            case SeedType.Gravebuster:     return Il2CppType.Of<GravebusterBehaviorController>();
            case SeedType.Hypnoshroom:     return Il2CppType.Of<HypnoshroomBehaviorController>();
            case SeedType.Scaredyshroom:   return Il2CppType.Of<ScaredyshroomBehaviorController>();
            case SeedType.Iceshroom:       return Il2CppType.Of<IceshroomBehaviorController>();
            case SeedType.Doomshroom:      return Il2CppType.Of<DoomshroomBehaviorController>();
            case SeedType.Lilypad:         return Il2CppType.Of<LilypadBehaviorController>();
            case SeedType.Squash:          return Il2CppType.Of<SquashBehaviorController>();
            case SeedType.Threepeater:     return Il2CppType.Of<ThreepeaterBehaviorController>();
            case SeedType.Tanglekelp:      return Il2CppType.Of<TanglekelpBehaviorController>();
            case SeedType.Jalapeno:        return Il2CppType.Of<JalapenoBehaviorController>();
            case SeedType.Spikeweed:       return Il2CppType.Of<SpikeweedBehaviorController>();
            case SeedType.Torchwood:       return Il2CppType.Of<TorchwoodBehaviorController>();
            case SeedType.Tallnut:         return Il2CppType.Of<TallnutBehaviorController>();
            case SeedType.Seashroom:       return Il2CppType.Of<SeashroomBehaviorController>();
            case SeedType.Plantern:        return Il2CppType.Of<PlanternBehaviorController>();
            case SeedType.Cactus:          return Il2CppType.Of<CactusBehaviorController>();
            case SeedType.Blover:          return Il2CppType.Of<BloverBehaviorController>();
            case SeedType.Splitpea:        return Il2CppType.Of<SplitpeaBehaviorController>();
            case SeedType.Starfruit:       return Il2CppType.Of<StarfruitBehaviorController>();
            case SeedType.Pumpkinshell:    return Il2CppType.Of<PumpkinshellBehaviorController>();
            case SeedType.Magnetshroom:    return Il2CppType.Of<MagnetshroomBehaviorController>();
            case SeedType.Cabbagepult:     return Il2CppType.Of<CabbagepultBehaviorController>();
            case SeedType.Flowerpot:       return Il2CppType.Of<FlowerpotBehaviorController>();
            case SeedType.Kernelpult:      return Il2CppType.Of<KernelpultBehaviorController>();
            case SeedType.InstantCoffee:   return Il2CppType.Of<InstantCoffeeBehaviorController>();
            case SeedType.Garlic:          return Il2CppType.Of<GarlicBehaviorController>();
            case SeedType.Umbrella:        return Il2CppType.Of<UmbrellaBehaviorController>();
            case SeedType.Marigold:        return Il2CppType.Of<MarigoldBehaviorController>();
            case SeedType.Melonpult:       return Il2CppType.Of<MelonpultBehaviorController>();
            case SeedType.Gatlingpea:      return Il2CppType.Of<GatlingpeaBehaviorController>();
            case SeedType.Twinsunflower:   return Il2CppType.Of<TwinsunflowerBehaviorController>();
            case SeedType.Gloomshroom:     return Il2CppType.Of<GloomshroomBehaviorController>();
            case SeedType.Cattail:         return Il2CppType.Of<CattailBehaviorController>();
            case SeedType.Wintermelon:     return Il2CppType.Of<WintermelonBehaviorController>();
            case SeedType.GoldMagnet:      return Il2CppType.Of<GoldMagnetBehaviorController>();
            case SeedType.Spikerock:       return Il2CppType.Of<SpikerockBehaviorController>();
            case SeedType.Cobcannon:       return Il2CppType.Of<CobcannonBehaviorController>();
            case SeedType.Imitater:        return Il2CppType.Of<ImitaterBehaviorController>();
            case SeedType.ExplodeONut:     return Il2CppType.Of<ExplodeONutBehaviorController>();
            default:
                return null;
        }
    }

    #endregion
}