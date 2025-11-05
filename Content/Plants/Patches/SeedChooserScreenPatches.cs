using HarmonyLib;
using Il2CppReloaded.Gameplay;
using PvZReCoreLib.Util;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        
        // Add all custom seeds
        for (int i = (int)SeedType.NumSeedsInChooser; i <= CustomContentRegistry.GetHighestCustomPlantTypeValue(); i++)
        {
            // Init regular seeds
            {
                ChosenSeed customSeed = new ChosenSeed();
                customSeed.mSeedType = (SeedType)i;
                customSeed.mSeedState = CustomContentRegistry.IsValidCustomPlantType((SeedType)i) ? ChosenSeedState.SeedInChooser : ChosenSeedState.SeedPacketHidden;
                customSeed.mImitaterType = SeedType.None;
        
                // Calculate position in grid (7 or 8 columns, multiple rows)
                int index = 12;
                int column = index % 8;
                int row = index / 8;

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
        }
        for (int i = (int)SeedType.Twinsunflower; i <= CustomContentRegistry.GetHighestCustomPlantTypeValue(); i++)
        {
            // Init Imitater Seeds
            {
                ChosenSeed customSeed = new ChosenSeed();
                customSeed.mSeedType = (SeedType)i;
                customSeed.mSeedState = CustomContentRegistry.IsValidCustomPlantType((SeedType)i) ? ChosenSeedState.SeedInChooser : ChosenSeedState.SeedPacketHidden;
                customSeed.mImitaterType = SeedType.None;
        
                // Calculate position in grid (7 or 8 columns, multiple rows)
                int index = 12;

                customSeed.mY = ((__instance.mImitaterDialog.mWidth / 2) + (index & 7) * 0x33 - 0xD2);
                customSeed.mY = index / 8 * 0x47 + 0x72;

                customSeed.mStartX = customSeed.mX;
                customSeed.mStartY = customSeed.mY;
                customSeed.mEndX = customSeed.mX;
                customSeed.mEndY = customSeed.mY;
        
                __instance.mImitaterDialog.mChosenSeeds.Add(customSeed);
            }
        }
        
        // Add custom buttons to the scene
        Scene activeScene = SceneManager.GetActiveScene();
        foreach (GameObject root in activeScene.GetRootGameObjects())
        {
            var targetChildren = UnityUtil.FindDeepChildrenByName(root.transform, "P_SeedChooser");
            if (targetChildren.Count == 0)
            {
                continue;
            }
            
            var seedChooserRoot = targetChildren.First();
            var seedChooser = seedChooserRoot.transform.FindChild("Canvas/Layout/Center/Panel/SeedChooser");
            if(seedChooser == null)
            {
                continue;
            }
            
            var grid = seedChooser.FindChild("Grid");
            if(grid == null)
            {
                continue;
            }
            
            UnityUtil.InjectScrollbar(seedChooser, grid, __instance.Has7Rows() ? 7 : 6);

            break;
        }
        
        return true;
    }
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
    
        // Manually update any custom seeds
        for (int i = (int)SeedType.NumSeedsInChooser; i <= CustomContentRegistry.GetHighestCustomPlantTypeValue(); i++)
        {
            SeedType seedType = (SeedType)i;
            if (__instance.mApp.HasSeedType(seedType))
            {
                var chosenSeed = __instance.mChosenSeeds._items[i];
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