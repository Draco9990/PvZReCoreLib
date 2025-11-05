using Il2CppReloaded.Gameplay;
using PvZReCoreLib.Content.Plants.Behavior;
using PvZReCoreLib.Content.Plants.Mint;
using PvZReCoreLib.Content.Plants.Skins.SkinDataTypes;
using PvZReCoreLib.Content.Projectiles;
using PvZReCoreLib.Util;
using UnityEngine;

namespace PvZReCoreLib.Content.Plants;

public class ProjectileExtension : ClassExtension<GameObject>
{
    public Projectile source;
        
    public CustomProjectileBehaviorController CustomBehaviorController;
}