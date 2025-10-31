using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppReloaded.Gameplay;
using PvZReCoreLib.Util;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PvZReCoreLib.Plants.CustomPlantLib.Patches;

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
        for (int i = (int)SeedType.NumSeedsInChooser; i <= PlantRegistry.GetHighestCustomSeedTypeValue(); i++)
        {
            // Init regular seeds
            {
                ChosenSeed customSeed = new ChosenSeed();
                customSeed.mSeedType = (SeedType)i;
                customSeed.mSeedState = PlantRegistry.IsCustomSeed((SeedType)i) ? ChosenSeedState.SeedInChooser : ChosenSeedState.SeedPacketHidden;
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
        for (int i = (int)SeedType.Twinsunflower; i <= PlantRegistry.GetHighestCustomSeedTypeValue(); i++)
        {
            // Init Imitater Seeds
            {
                ChosenSeed customSeed = new ChosenSeed();
                customSeed.mSeedType = (SeedType)i;
                customSeed.mSeedState = PlantRegistry.IsCustomSeed((SeedType)i) ? ChosenSeedState.SeedInChooser : ChosenSeedState.SeedPacketHidden;
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
            
            InjectScrollbar(__instance, seedChooser, grid);

            break;
        }
        
        return true;
    }

    private static void InjectScrollbar(SeedChooserScreen __instance, Transform seedChooser, Transform gridTransform)
    {
        RectTransform grid = gridTransform.Cast<RectTransform>();
        
        // Store original grid size before modifications
        Vector2 originalOffsetMin = grid.offsetMin;
        Vector2 originalOffsetMax = grid.offsetMax;
        
        float height = grid.offsetMax.y - grid.offsetMin.y;
        float newHeight = __instance.Has7Rows() ? height * 7 : height * 6;
        originalOffsetMin.y = -newHeight;
        
        // Create Scroll View root
        var scrollGO = new GameObject("GridScrollView", Il2CppType.Of<RectTransform>(), Il2CppType.Of<ScrollRect>());
        scrollGO.transform.SetParent(seedChooser, false);
        scrollGO.transform.SetSiblingIndex(grid.GetSiblingIndex());

        var scrollRect = scrollGO.GetComponent<RectTransform>().Cast<RectTransform>();
        scrollRect.anchorMin = grid.anchorMin;
        scrollRect.anchorMax = grid.anchorMax;
        scrollRect.pivot = grid.pivot;
        scrollRect.offsetMin = originalOffsetMin;
        scrollRect.offsetMax = originalOffsetMax;

        // Create Viewport
        var viewportGO = new GameObject("Viewport", Il2CppType.Of<RectTransform>(), Il2CppType.Of<Mask>(), Il2CppType.Of<Image>());
        viewportGO.transform.SetParent(scrollGO.transform, false);

        var viewportRect = viewportGO.GetComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0, 0);
        viewportRect.anchorMax = new Vector2(1, 1);
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        var mask = viewportGO.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        var viewportImage = viewportGO.GetComponent<Image>();
        //viewportImage.color = new Color(0, 0, 0, 0); // Fully transparent

        // Reset grid anchors/offsets and move under viewport
        grid.SetParent(viewportGO.transform, false);
        grid.anchorMin = new Vector2(0, 1); // Anchor to top-left
        grid.anchorMax = new Vector2(1, 1);
        grid.pivot = new Vector2(0.5f, 1);
        grid.anchoredPosition = Vector2.zero;
        
        // Let the GridLayoutGroup control the size
        // Don't set offsetMin/offsetMax - let it size itself

        // Configure ScrollRect
        var sr = scrollGO.GetComponent<ScrollRect>();
        sr.viewport = viewportRect;
        sr.content = grid;
        sr.horizontal = false;
        sr.vertical = true;

        // Add scrollbar with Handle
        var scrollbarGO = new GameObject("Scrollbar", Il2CppType.Of<RectTransform>(), Il2CppType.Of<Image>(), Il2CppType.Of<Scrollbar>());
        scrollbarGO.transform.SetParent(scrollGO.transform, false);

        var scrollbarRect = scrollbarGO.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1, 0);
        scrollbarRect.anchorMax = new Vector2(1, 1);
        scrollbarRect.pivot = new Vector2(1, 0.5f);
        scrollbarRect.sizeDelta = new Vector2(20, 0);
        scrollbarRect.anchoredPosition = new Vector2(0, 0); // Position at right edge

        var scrollbarImage = scrollbarGO.GetComponent<Image>();
        scrollbarImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f); // Dark semi-transparent background

        // Create Scrollbar Handle (required for scrollbar to work)
        var handleGO = new GameObject("Handle", Il2CppType.Of<RectTransform>(), Il2CppType.Of<Image>());
        handleGO.transform.SetParent(scrollbarGO.transform, false);

        var handleRect = handleGO.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;

        var handleImage = handleGO.GetComponent<Image>();
        handleImage.color = new Color(0.5f, 0.5f, 0.5f, 0.8f); // Lighter handle

        var scrollbar = scrollbarGO.GetComponent<Scrollbar>();
        scrollbar.handleRect = handleRect;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        sr.verticalScrollbar = scrollbar;
        sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        
        // Force layout rebuild
        LayoutRebuilder.ForceRebuildLayoutImmediate(grid);
    }
}

[HarmonyPatch(typeof(SeedChooserScreen), nameof(SeedChooserScreen.GetChosenSeedFromType))]
public class SeedChooserScreen_GetChosenSeedFromType_Patch
{
    public static bool Prefix(ref SeedChooserScreen __instance, SeedType theSeedType, ref ChosenSeed __result)
    {
        if (PlantRegistry.IsCustomSeed(theSeedType))
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
        for (int i = (int)SeedType.NumSeedsInChooser; i <= PlantRegistry.GetHighestCustomSeedTypeValue(); i++)
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