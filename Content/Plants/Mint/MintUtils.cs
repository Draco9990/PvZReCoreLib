using Il2CppInterop.Runtime;
using Il2CppReloaded.Gameplay;
using PvZReCoreLib.Content.Plants.Mint.Controllers;
using Type = Il2CppSystem.Type;

namespace PvZReCoreLib.Content.Plants.Mint;

public class MintUtils
{
    #region Variables



    #endregion

    #region Constructors



    #endregion

    #region Methods

    public static MintFamily GetMintFamilyForBasePlants(SeedType seedType)
    {
        switch (seedType)
        {
            case SeedType.Jalapeno:
                return MintFamily.PepperMint;
            
            case SeedType.Snowpea:
            case SeedType.Wintermelon:
            case SeedType.Iceshroom:
                return MintFamily.WinterMint;
            
            case SeedType.Wallnut:
            case SeedType.Tallnut:
            case SeedType.Pumpkinshell:
            case SeedType.GiantWallnut:
            case SeedType.Flowerpot:
                return MintFamily.ReinforceMint;
            
            case SeedType.Potatomine:
            case SeedType.Cherrybomb:
            case SeedType.ExplodeONut:
            case SeedType.Doomshroom:
                return MintFamily.BombardMint;
            
            case SeedType.Puffshroom:
            case SeedType.Fumeshroom:
            case SeedType.Scaredyshroom:
            case SeedType.Garlic:
            case SeedType.Seashroom:
            case SeedType.Gloomshroom:
                return MintFamily.AilMint;
            
            case SeedType.Hypnoshroom:
            case SeedType.Imitater:
            case SeedType.InstantCoffee:
                return MintFamily.EnchantMint;
            
            case SeedType.Gravebuster:
            case SeedType.Lilypad:
            case SeedType.Blover:
            case SeedType.Magnetshroom:
            case SeedType.GoldMagnet:
            case SeedType.Umbrella:
                return MintFamily.ContainMint;
            
            case SeedType.Chomper:
            case SeedType.Squash:
            case SeedType.Tanglekelp:
                return MintFamily.EnforceMint;
            
            case SeedType.Cabbagepult:
            case SeedType.Kernelpult:
            case SeedType.Cobcannon:
            case SeedType.Melonpult:
                return MintFamily.ArmaMint;
            
            case SeedType.Cactus:
            case SeedType.Spikeweed:
            case SeedType.Spikerock:
            case SeedType.Cattail:
                return MintFamily.SpearMint;
            
            case SeedType.Peashooter:
            case SeedType.Repeater:
            case SeedType.Threepeater:
            case SeedType.Torchwood:
            case SeedType.Splitpea:
            case SeedType.Starfruit:
            case SeedType.Gatlingpea:
            case SeedType.Leftpeater:
                return MintFamily.AppeaseMint;
            
            case SeedType.Sunflower:
            case SeedType.Twinsunflower:
            case SeedType.Sunshroom:
            case SeedType.Plantern:
            case SeedType.Marigold:
                return MintFamily.EnlightenMint;
                
            default:
                return MintFamily.None;
        }
    }

    public static List<MintFamilyBehaviorController> GetAllControllersForFamily(Board b, MintFamily mintFamily)
    {
        List<MintFamilyBehaviorController> controllers = new List<MintFamilyBehaviorController>();
        foreach (var plantData in b.m_plants.m_list)
        {
            if (plantData == null || plantData.mItem == null)
            {
                continue;
            }
            
            MintFamilyBehaviorController mintFamilyController = MintFamilyBehaviorController.GetFor(plantData.mItem);
            if (mintFamilyController != null && mintFamilyController.mMintFamily == mintFamily)
            {
                controllers.Add(mintFamilyController);
            }
        }
        return controllers;
    }

    public static Type GetMintFamilyControllerType(MintFamily mintFamily)
    {
        if (mintFamily == MintFamily.EnlightenMint)
        {
            return Il2CppType.Of<EnlightenMintFamilyController>();
        }

        return null;
    }

    #endregion
}