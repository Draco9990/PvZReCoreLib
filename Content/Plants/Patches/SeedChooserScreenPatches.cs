using HarmonyLib;
using Il2CppReloaded.Gameplay;
using PvZReCoreLib.Util;
using UnityEngine;

namespace PvZReCoreLib.Content.Plants.Patches;

// Add all custom seeds to our custom seed picker
[HarmonyPatch(typeof(SeedChooserScreen), nameof(SeedChooserScreen.Update))]
public partial class SeedChooserScreen_CustomSeedsPatch
{
    public static bool Prefix(ref SeedChooserScreen __instance)
    {
        var patchMarker = PatchMarker<SeedChooserScreen>.GetOrCreateExtension<PatchMarker<SeedChooserScreen>>(__instance);
        if (patchMarker.IsPatched)
        {
            return true;
        }
        patchMarker.IsPatched = true;

        int customPlantCount = 0;

        // Add an entry for every numeric value from the current end of mChosenSeeds up through the
        // highest registered custom SeedType, not just the actually-registered ones. Decompiling
        // the game confirmed FindSeedInBank() and CloseSeedChooser's internal callback both index
        // mChosenSeeds DIRECTLY by raw SeedType value (mChosenSeeds[(int)seedType]), not by
        // searching for a matching entry - so list position must equal SeedType value for every
        // index up to the highest one we hand out, or those lookups go out of range.
        //
        // Deliberately starting from mChosenSeeds.Count rather than (int)SeedType.NumSeedTypes:
        // logging showed the vanilla list is only densely filled up to Count (e.g. 49), while
        // NumSeedTypes itself reads higher (e.g. 53) - there are a handful of reserved/unused
        // SeedType values between the last real plant and the NumSeedTypes sentinel. Starting the
        // loop at NumSeedTypes skipped that gap and put every custom entry 4 slots off from its own
        // SeedType value, which is exactly what was overflowing CloseSeedChooser's lookup.
        for (int i = __instance.mChosenSeeds.Count; i <= CustomContentRegistry.GetHighestCustomPlantTypeValue(); i++)
        {
            ChosenSeed customSeed = new ChosenSeed();
            customSeed.mSeedType = (SeedType)i;
            bool isValid = CustomContentRegistry.IsValidCustomPlantType((SeedType)i);
            customSeed.mSeedState = isValid ? ChosenSeedState.SeedInChooser : ChosenSeedState.SeedPacketHidden;
            customSeed.mImitaterType = SeedType.None;
            if (isValid)
            {
                customPlantCount++;
            }

            // Calculate position in grid (7 or 8 columns, multiple rows). List position == i here
            // by construction, since we're appending sequentially starting at the list's own count.
            int column = i % 8;
            int row = i / 8;

            customSeed.mX = column * 0x35 + 0x16;

            if (__instance.Has7Rows())
            {
                customSeed.mY = row * 0x46 + 0x7b;
            }
            else
            {
                customSeed.mY = row * 0x49 + 0x80;
            }

            customSeed.mStartX = customSeed.mX;
            customSeed.mStartY = customSeed.mY;
            customSeed.mEndX = customSeed.mX;
            customSeed.mEndY = customSeed.mY;

            __instance.mChosenSeeds.Add(customSeed);
        }
        int imitaterIndex = __instance.mImitaterDialog.mChosenSeeds.Count;
        for (int i = (int)SeedType.Twinsunflower; i <= CustomContentRegistry.GetHighestCustomPlantTypeValue(); i++)
        {
            // Init Imitater Seeds
            {
                ChosenSeed customSeed = new ChosenSeed();
                customSeed.mSeedType = (SeedType)i;
                customSeed.mSeedState = CustomContentRegistry.IsValidCustomPlantType((SeedType)i) ? ChosenSeedState.SeedInChooser : ChosenSeedState.SeedPacketHidden;
                customSeed.mImitaterType = SeedType.None;

                // Calculate position in grid (7 or 8 columns, multiple rows)
                int index = imitaterIndex;

                customSeed.mY = ((__instance.mImitaterDialog.mWidth / 2) + (index & 7) * 0x33 - 0xD2);
                customSeed.mY = index / 8 * 0x47 + 0x72;

                customSeed.mStartX = customSeed.mX;
                customSeed.mStartY = customSeed.mY;
                customSeed.mEndX = customSeed.mX;
                customSeed.mEndY = customSeed.mY;

                __instance.mImitaterDialog.mChosenSeeds.Add(customSeed);
                imitaterIndex++;
            }
        }

        MelonLoader.MelonLogger.Msg($"[SeedChooserScreen] Added {customPlantCount} custom seed(s) to chooser (registry reports {CustomContentRegistry.GetAllCustomPlantTypes().Count} registered, highest value {CustomContentRegistry.GetHighestCustomPlantTypeValue()}).");

        // UpdateSeedChooserScreen already ran once before mChosenSeeds had these entries in it
        // (it only fires once per screen open, not every frame), so force it to try again now
        // that the data it needs actually exists.
        var cachedDataModel = SeedChooserDataModel_CustomPlantEntriesPatch.CachedDataModel;
        if (cachedDataModel != null)
        {
            SeedChooserDataModel_CustomPlantEntriesPatch.TryRefresh(cachedDataModel, __instance);
        }

        return true;
    }

    // Scrollbar injection removed for now: SeedChooserScreen.Update() keeps firing well past the
    // seed-chooser menu closing (confirmed live - a Postfix here was still running every frame
    // deep into active gameplay), so the full scene root-object enumeration + recursive deep-child
    // search this used to do on every failed attempt ran unconditionally every single frame for
    // the rest of the session once the grid stopped being found (e.g. once gameplay starts and the
    // menu's UI is gone) - cost scaling with total scene object count, which was the actual cause
    // of reported choppiness that got worse as more zombies piled up. The feature never actually
    // succeeded in practice either (extra seeds are visible in one row, but not reachable via
    // scroll), so this is worth rebuilding properly - gated on actually being on the seed-chooser
    // screen, not on Update() continuing to fire - rather than re-adding the same unbounded retry.
}

[HarmonyPatch(typeof(SeedChooserScreen), nameof(SeedChooserScreen.GetChosenSeedFromType))]
public class SeedChooserScreen_GetChosenSeedFromType_Patch
{
    public static bool Prefix(ref SeedChooserScreen __instance, SeedType theSeedType, ref ChosenSeed __result)
    {
        if (CustomContentRegistry.IsValidCustomPlantType(theSeedType))
        {
            foreach (ChosenSeed chosenSeed in __instance.mChosenSeeds)
            {
                if (chosenSeed.mSeedType == theSeedType)
                {
                    __result = chosenSeed;
                    return false;
                }
            }
        }

        return true;
    }
}

[HarmonyPatch(typeof(SeedChooserScreen), nameof(SeedChooserScreen.UpdateSeedChooserScreen))]
public class SeedChooserScreen_ClickedSeedInChooser_Patch
{
    public static bool Prefix(ref SeedChooserScreen __instance)
    {
        float deltaTime = Time.deltaTime;

        // Manually update any custom seeds. mChosenSeeds is only densely packed for the built-in
        // roster (list index == SeedType value there) - custom entries live wherever
        // SeedChooserScreen_CustomSeedsPatch appended them, so look them up by mSeedType instead
        // of indexing directly by the (much larger) numeric SeedType value.
        foreach (SeedType seedType in CustomContentRegistry.GetAllCustomPlantTypes())
        {
            if (!__instance.mApp.HasSeedType(seedType))
            {
                continue;
            }

            ChosenSeed chosenSeed = null;
            foreach (var cs in __instance.mChosenSeeds)
            {
                if (cs.mSeedType == seedType)
                {
                    chosenSeed = cs;
                    break;
                }
            }

            if (chosenSeed == null)
            {
                continue;
            }

            if (chosenSeed.mSeedState == ChosenSeedState.SeedFlyingToBank || chosenSeed.mSeedState == ChosenSeedState.SeedFlyingToChooser)
            {
                chosenSeed.mTimeInMotion += deltaTime;

                if (chosenSeed.mTimeInMotion >= chosenSeed.mDurationOfMotion)
                {
                    __instance.LandFlyingSeed(chosenSeed);
                }
            }
        }
        return true;
    }
}

[HarmonyPatch(typeof(SeedChooserScreen), nameof(SeedChooserScreen.FindSeedInBank))]
public class SeedChooserScreen_FindSeedInBank_Patch
{
    public static void Postfix(ref SeedChooserScreen __instance, int theIndexInBank, int playerIndex, ref SeedType __result)
    {
        if (__result == SeedType.None)
        {
            foreach (var chosenSeed in __instance.mChosenSeeds)
            {
                if (chosenSeed.mSeedState == ChosenSeedState.SeedInBank && chosenSeed.mSeedIndexInBank == theIndexInBank)
                {
                    __result = chosenSeed.mSeedType;
                    return;
                }
            }
        }
    }
}