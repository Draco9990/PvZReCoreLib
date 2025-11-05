using Il2CppReloaded.Gameplay;
using PvZReCoreLib.Util;
using UnityEngine;

namespace PvZReCoreLib.Content.Projectiles;

public class ProjectileExtension : ClassExtension<GameObject>
{
    public Projectile source;
        
    public CustomProjectileBehaviorController CustomBehaviorController;
}