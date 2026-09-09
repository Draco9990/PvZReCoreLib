using System.Collections.Generic;
using HarmonyLib;
using Il2CppReloaded.Gameplay;
using UnityEngine;

namespace PvZReCoreLib.Content.Common.Behavior;

// Generic "push this zombie back N units" primitive - no native knockback/
// stagger system exists to hook (checked: nothing on Zombie beyond an
// unrelated mShieldRecoilCounter and mIsPushingBobsled flag), but none is
// needed - mPosX/mPosY are plain, freely-writable fields just like on
// Plant/Projectile. Drives the zombie smoothly back over a short window
// instead of teleporting it in one frame, with a parabolic vertical hop
// peaking at the midpoint (a simple stand-in for the "normal distribution"
// arc real PvZ2 knockback pops use) - the zombie's own walk/attack
// animation is untouched throughout, this only ever touches position.
public static class ZombiePushBack
{
    private class PushState
    {
        public float StartX;
        public float TargetX;
        public float BaseY;
        public float HopHeight;
        public int ElapsedTicks;
        public int TotalTicks;
    }

    private static readonly Dictionary<Zombie, PushState> Active = new Dictionary<Zombie, PushState>();

    // Cumulative push per zombie, persisted across separate Push() calls
    // (not just one in-progress push) - a single plant hitting once for 2
    // tiles is fine, but nothing stops many separate plants (or one plant
    // spammed) from each pushing the same zombie further, unboundedly, over
    // its lifetime. This caps the TOTAL a zombie can ever be pushed so it
    // can't end up flung off the visible lawn. Generic safety net, not
    // specific to any one plant's own per-hit distance.
    private static readonly Dictionary<Zombie, float> TotalPushed = new Dictionary<Zombie, float>();
    private const float MaxCumulativePush = 400f;

    // distance: world units to push back (positive = away from the house,
    // same convention as mPosX everywhere else in this codebase). ticks:
    // how many Update calls the slide+hop takes. hopHeight: peak vertical
    // offset at the midpoint of the push.
    public static void Push(Zombie zombie, float distance, int ticks = 20, float hopHeight = 30f)
    {
        if (zombie == null || zombie.IsDeadOrDying())
        {
            return;
        }

        TotalPushed.TryGetValue(zombie, out var alreadyPushed);
        float remaining = Mathf.Max(0f, MaxCumulativePush - alreadyPushed);
        float clampedDistance = Mathf.Min(distance, remaining);
        if (clampedDistance <= 0f)
        {
            return;
        }
        TotalPushed[zombie] = alreadyPushed + clampedDistance;

        // Re-pushing a zombie already mid-push restarts from its CURRENT
        // position rather than stacking on top of the old target, so two
        // quick hits don't fling it back double distance.
        Active[zombie] = new PushState
        {
            StartX = zombie.mPosX,
            TargetX = zombie.mPosX + clampedDistance,
            BaseY = zombie.mPosY,
            HopHeight = hopHeight,
            ElapsedTicks = 0,
            TotalTicks = Mathf.Max(1, ticks),
        };
    }

    // Called once per zombie per Zombie.Update() - advances and applies any
    // in-progress push, removing it once complete. No-op for a zombie that
    // isn't currently being pushed.
    public static void Advance(Zombie zombie)
    {
        if (zombie == null || !Active.TryGetValue(zombie, out var state))
        {
            return;
        }

        if (zombie.IsDeadOrDying())
        {
            Active.Remove(zombie);
            TotalPushed.Remove(zombie);
            return;
        }

        state.ElapsedTicks++;
        float t = Mathf.Clamp01((float)state.ElapsedTicks / state.TotalTicks);

        zombie.mPosX = Mathf.Lerp(state.StartX, state.TargetX, t);
        // Negative: this engine's Y increases downward on screen (confirmed
        // by FirePeashooter's mouth-offset needing the same flip), so a
        // negative offset at the midpoint is what actually reads as a hop up
        // rather than a dip down.
        zombie.mPosY = state.BaseY - 4f * state.HopHeight * t * (1f - t);

        if (t >= 1f)
        {
            zombie.mPosY = state.BaseY;
            Active.Remove(zombie);
        }
    }
}

[HarmonyPatch(typeof(Zombie), nameof(Zombie.Update))]
public class Zombie_Update_PushBack_Patch
{
    public static void Postfix(ref Zombie __instance)
    {
        ZombiePushBack.Advance(__instance);
    }
}
