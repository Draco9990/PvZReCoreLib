using Il2CppReloaded.Gameplay;
using PvZReCoreLib.Content.Plants.Behavior;
using PvZReCoreLib.Content.Plants.Mint;
using PvZReCoreLib.Content.Plants.Skins.SkinDataTypes;
using PvZReCoreLib.Util;
using UnityEngine;

namespace PvZReCoreLib.Content.Plants;

public class PlantExtension : ClassExtension<GameObject>
{
    public Plant source;
        
    public CustomPlantBehaviorController CustomBehaviorController;
    public MintFamilyBehaviorController MintFamilyBehaviorController;

    public SkinType CurrentSkin;
}