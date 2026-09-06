using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Il2CppSource.DataModels;
using Il2CppUI.Scripts;
using PvZReCoreLib.Util;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PvZReCoreLib.Content.Plants.Patches;

// The seed-chooser grid is a fixed 8-column x 7-or-8-row area (SeedChooserScreen.Has7Rows()) with
// no native scrolling or paging - vanilla plants already come close to filling it, and every
// custom plant on top eventually overflows the visible area entirely (rows exist in mChosenSeeds
// but nothing on screen reaches them). An earlier attempt hand-built a Unity ScrollRect from
// scratch to work around this and was reverted: it ran an unbounded scene-wide search every frame
// long after the seed chooser closed (severe perf regression), and even when it "worked" the
// overflow content was visible but never actually reachable by scrolling into view.
//
// This replaces that with paging instead of scrolling: only one page's worth of custom plants is
// ever present in the reactive models at a time, added/removed directly (see ForceRefresh in
// SeedChooserDataModelPatches.cs) - the same ClearNoDispose + UpdateEntries rebuild already used
// elsewhere in this codebase to get custom plants showing up in the first place, just windowed.
//
// Deliberately does NOT touch ChosenSeedState at all. mSeedState (SeedInChooser/SeedPacketHidden/
// etc.) is an existing, unrelated native mechanism - a plant can be legitimately hidden or grayed
// out for its own reasons (roof-only restrictions, not-yet-unlocked, and others), independent of
// which page it's on, and still needs to render normally (grayed, locked, whatever native
// decides) whenever it IS on the current page. An earlier version of this both overwrote
// mSeedState to force page membership AND used mSeedState as its own removal criterion - which
// stomped a real native SeedPacketHidden (a roof-restricted custom plant on a rooftop level) and
// made it disappear entirely instead of rendering however native code intended. Page membership
// is tracked independently instead (CurrentPageCustomTypes) and is the only thing paging ever
// decides.
//
// Next/previous buttons are cloned from AlmanacArchive's own "arrows" GameObject (found live via
// Resources.FindObjectsOfTypeAll, since the Almanac screen may not be currently instantiated) so
// this doesn't need to hand-build new button art - only AlmanacArchive's own onClick wiring is
// stripped and replaced with page-turn handlers.
public static class SeedChooserPaging
{
    private const int GridColumns = 8;

    // Confirmed empirically (screenshot: 6 full rows of 8, zero remainder for the real 48 vanilla
    // plants) - a fixed page size matching vanilla's own real layout, not derived from
    // Has7Rows() (that was never a capacity signal - see the type comment above). Custom plants
    // always start on their own fresh page boundary after every vanilla page, never sharing a
    // page with vanilla even if some future vanilla roster doesn't divide evenly by this. With
    // this fixed at 48 and only ~8 custom plants registered right now, everything currently fits
    // on a single custom page - multi-page custom navigation only actually matters once the
    // custom roster exceeds 48 (there's a stated intent to eventually port ~180 plants).
    private const int PageSize = 48;

    private static int currentPage = 0;
    private static int lastPageCount = 1;

    // How many of mChosenSeeds' leading entries are native's own real plants (captured right
    // before SeedChooserScreen_CustomSeedsPatch's injection loop appends anything) - used only to
    // tell a real vanilla entry apart from the unrelated reserved/minigame-ID range (SeedTypes
    // 54-81, Beghouled/Slot Machine buttons then zombie cursor pseudo-types) that same injection
    // loop also fills in as hidden placeholder slots on its way up to the highest registered
    // custom SeedType. Neither camp is "custom," so IsValidCustomPlantType alone can't
    // distinguish them.
    private static int VanillaCount = -1;

    // One flat, ordered list of every SeedType that actually occupies a real page slot - real
    // vanilla plants followed by real custom plants, in mChosenSeeds' own order (which is
    // ascending by SeedType per the position==value invariant elsewhere in this codebase). Simple
    // count-based paging falls out of this directly: whichever PageSize-sized slice of this list
    // an entry's index lands in IS its page, for vanilla and custom alike - no separate "vanilla
    // page" vs "custom page" concept needed. Rebuilt by ApplyPaging every screen-open and page
    // turn.
    private static readonly List<SeedType> orderedTypes = new();
    private static readonly Dictionary<SeedType, int> pageIndexByType = new();

    // Called once per screen-open from SeedChooserScreen_CustomSeedsPatch, right after it finishes
    // appending every custom SeedType (valid or not) to mChosenSeeds - vanillaCount is the list
    // length *before* that loop ran, i.e. how many on-screen slots the base game's own plants
    // occupy.
    public static void OnScreenOpened(int vanillaCount)
    {
        VanillaCount = vanillaCount;
        currentPage = 0;
    }

    // The single source of truth ForceRefresh prunes the reactive model against - true means
    // "keep this entry in the model." A SeedType not in pageIndexByType at all (the reserved
    // minigame-ID gap range) is never visible on any page.
    public static bool ShouldBeVisible(SeedType seedType)
    {
        return pageIndexByType.TryGetValue(seedType, out int page) && page == currentPage;
    }

    // Recomputes mX/mY (grid position) for whichever entries land past page 0 - never mSeedState,
    // see the type-level comment above for why.
    public static void ApplyPaging(SeedChooserScreen screen)
    {
        if (VanillaCount < 0)
        {
            return;
        }

        orderedTypes.Clear();
        pageIndexByType.Clear();

        foreach (var cs in screen.mChosenSeeds)
        {
            bool isRealVanilla = (int)cs.mSeedType < VanillaCount;
            bool isRealCustom = CustomContentRegistry.IsValidCustomPlantType(cs.mSeedType);
            if (!isRealVanilla && !isRealCustom)
            {
                continue; // reserved minigame-ID gap slot - doesn't consume a page slot at all
            }

            orderedTypes.Add(cs.mSeedType);
        }

        lastPageCount = Math.Max(1, (orderedTypes.Count + PageSize - 1) / PageSize);
        currentPage = Math.Clamp(currentPage, 0, lastPageCount - 1);

        for (int i = 0; i < orderedTypes.Count; i++)
        {
            pageIndexByType[orderedTypes[i]] = i / PageSize;
        }

        // Only entries past page 0 ever need repositioning - native already laid the first
        // PageSize out correctly on its own (that's the single page it always assumed existed).
        // Everything from PageSize onward (custom plants, or a 49th+ "vanilla" entry like the
        // Imitater slot if VanillaCount ever exceeds PageSize) restarts at row 0/column 0 of
        // whichever fresh page it lands on.
        foreach (var cs in screen.mChosenSeeds)
        {
            int combinedIndex = orderedTypes.IndexOf(cs.mSeedType);
            if (combinedIndex < PageSize)
            {
                continue;
            }

            int indexOnPage = combinedIndex % PageSize;
            int column = indexOnPage % GridColumns;
            int row = indexOnPage / GridColumns;

            cs.mX = column * 0x35 + 0x16;
            cs.mY = screen.Has7Rows() ? row * 0x46 + 0x7b : row * 0x49 + 0x80;
            cs.mStartX = cs.mX;
            cs.mStartY = cs.mY;
            cs.mEndX = cs.mX;
            cs.mEndY = cs.mY;
        }

        MelonLoader.MelonLogger.Msg($"[CoreLib] Seed chooser paging: page {currentPage + 1}/{lastPageCount}, {orderedTypes.Count} total plant(s) across all pages.");
    }

    public static void GoToNextPage(SeedChooserDataModel dataModel, SeedChooserScreen screen)
    {
        MelonLoader.MelonLogger.Msg($"[CoreLib] GoToNextPage called: currentPage={currentPage}, lastPageCount={lastPageCount}.");

        if (currentPage + 1 >= lastPageCount)
        {
            return;
        }

        currentPage++;
        ApplyPaging(screen);
        SeedChooserDataModel_CustomPlantEntriesPatch.ApplyPageVisibility(dataModel);
    }

    public static void GoToPreviousPage(SeedChooserDataModel dataModel, SeedChooserScreen screen)
    {
        MelonLoader.MelonLogger.Msg($"[CoreLib] GoToPreviousPage called: currentPage={currentPage}, lastPageCount={lastPageCount}.");

        if (currentPage <= 0)
        {
            return;
        }

        currentPage--;
        ApplyPaging(screen);
        SeedChooserDataModel_CustomPlantEntriesPatch.ApplyPageVisibility(dataModel);
    }
}

// Separate marker class from PatchMarker<SeedChooserScreen> (used by
// SeedChooserScreen_CustomSeedsPatch) - GetOrCreateExtension looks up its single stored instance
// purely by generic type per target object, so sharing the same PatchMarker<SeedChooserScreen>
// type here would silently read/write the *other* patch's IsPatched flag instead of this one's.
public class SeedChooserPagingButtonsMarker : ClassExtension<SeedChooserScreen>
{
    public bool IsPatched;
    public int Attempts;
}

[HarmonyPatch(typeof(SeedChooserScreen), nameof(SeedChooserScreen.Update))]
public class SeedChooserScreen_InjectPagingButtons_Patch
{
    // AccessTools.Field(typeof(AlmanacArchive), "arrows") returns null in practice - confirmed
    // live that the IL2CPP interop assembly this mod actually links against only exposes an
    // opaque NativeFieldInfoPtr_arrows bookkeeping entry for this private, un-accessed
    // [SerializeField] field, not a readable value or a generated property. That's an IL2CPP
    // interop generation gap, not a wrong field name - normal .NET reflection can't reach a
    // private native field with no accessor at all. Finding the GameObject by name in the
    // hierarchy instead (AlmanacArchive genuinely is a MonoBehaviour, unlike SeedChooserScreen,
    // so it has a real Transform to search from).
    private static readonly string[] ArrowsCandidateNames = { "Arrows", "arrows", "ArrowButtons", "NavigationArrows" };

    // SeedChooserScreen isn't a Component itself (Widget/WidgetContainer, its base classes, hold
    // no Transform/GameObject) - reaching its actual visible UI needs the same scene-root search
    // the earlier scrollbar attempt used (UnityUtil.FindDeepChildrenByName is a recursive
    // descendant search, run per root GameObject in the scene - real work, not free). That
    // attempt's actual failure was letting this retry completely unbounded, every single Update()
    // call, forever, including deep into gameplay long after the chooser closed.
    //
    // Confirmed live that neither "haven't succeeded yet" nor "CachedDataModel != null" are safe
    // gates for that: SeedChooserScreen.Update() keeps firing during totally unrelated TreeState
    // transitions (SpeechBubble/PlayGame) on the way into a level, AND CachedDataModel is a
    // static field set once on the very first seed-chooser open of the whole session and never
    // cleared - so once true, it's true forever, including during plain gameplay with no chooser
    // anywhere on screen. An attempt counter gated behind either one just silently burns its
    // entire budget in the background before the chooser is ever actually visible again.
    //
    // ReplantedGet.TreeStateManager().Active.name is the actual "what screen is this" signal
    // already used elsewhere in this codebase (see Plant_PlantInitialize_Patch's "AmanacPlants"
    // check) - a plain string compare against a cached reference, cheap enough to run
    // unconditionally every Update() call for the whole session. Gating on it first means the
    // expensive scene search below only ever runs while the seed chooser is genuinely the active
    // screen, not "at some point after it first became true."
    private const int MaxAttempts = 1200;

    // Confirmed live that the seed chooser is reachable through more than one named TreeState
    // depending on game mode/entry point - "SeedChooser" is the transient name passed to
    // TransitionTo, "ChooseSeeds" is the settled name most contexts actually land on (confirmed
    // working across minigames and adventure mode). More may need adding if another mode uses a
    // third name.
    private static readonly string[] SeedChooserStateNames = { "SeedChooser", "ChooseSeeds" };

    public static void Postfix(SeedChooserScreen __instance)
    {
        var patchMarker = SeedChooserPagingButtonsMarker.GetOrCreateExtension<SeedChooserPagingButtonsMarker>(__instance);
        if (patchMarker.IsPatched)
        {
            return;
        }

        var activeState = ReplantedGet.TreeStateManager()?.Active;

        if (activeState == null || Array.IndexOf(SeedChooserStateNames, activeState.name) < 0)
        {
            // Not actually on the seed chooser screen right now - don't spend anything, and
            // don't count this against the attempt budget below.
            return;
        }

        var dataModel = SeedChooserDataModel_CustomPlantEntriesPatch.CachedDataModel;
        if (dataModel == null)
        {
            // Same "runs before the data we need exists" timing issue the custom-entry
            // injection already has to work around - keep retrying (uncounted) until the
            // cached data model shows up. Bounded regardless now, since we only reach here
            // while genuinely on the seed chooser screen.
            return;
        }

        patchMarker.Attempts++;
        if (patchMarker.Attempts > MaxAttempts)
        {
            MelonLoader.MelonLogger.Warning("[CoreLib] Gave up looking for the seed chooser grid / an AlmanacArchive instance after " + MaxAttempts + " attempts while on the seed chooser screen - paging buttons will not be added this session.");
            patchMarker.IsPatched = true;
            return;
        }

        AlmanacArchive archiveTemplate = null;
        foreach (var candidate in Resources.FindObjectsOfTypeAll<AlmanacArchive>())
        {
            archiveTemplate = candidate;
            break;
        }

        if (archiveTemplate == null)
        {
            // The Almanac screen's prefab may genuinely not be loaded yet this session (e.g.
            // player hasn't opened it) - keep retrying (bounded) rather than giving up on the
            // first miss.
            return;
        }

        GameObject arrowsSource = null;
        foreach (var candidateName in ArrowsCandidateNames)
        {
            var found = UnityUtil.FindDeepChildrenByName(archiveTemplate.transform, candidateName);
            if (found.Count > 0)
            {
                arrowsSource = found[0].gameObject;
                break;
            }
        }

        if (arrowsSource == null)
        {
            // TEMP DIAGNOSTIC - none of the guessed names matched. Dump the real hierarchy under
            // the found AlmanacArchive so the actual child name can be confirmed instead of
            // guessed again. Remove once found.
            var childNames = new List<string>();
            void DumpChildren(Transform t, int depth)
            {
                foreach (Transform child in t)
                {
                    childNames.Add(new string(' ', depth * 2) + child.name);
                    DumpChildren(child, depth + 1);
                }
            }
            DumpChildren(archiveTemplate.transform, 0);
            MelonLoader.MelonLogger.Warning("[CoreLib] Could not find an Arrows-like child under AlmanacArchive - seed chooser paging buttons will not be added. Actual hierarchy:\n" + string.Join("\n", childNames));
            patchMarker.IsPatched = true;
            return;
        }

        // Locate the seed chooser's own panel Transform to parent the cloned buttons under -
        // same scene-root search path the earlier scrollbar attempt used to find "Grid".
        Transform seedChooserPanel = null;
        var activeScene = SceneManager.GetActiveScene();
        foreach (var root in activeScene.GetRootGameObjects())
        {
            var targetChildren = UnityUtil.FindDeepChildrenByName(root.transform, "P_SeedChooser");
            if (targetChildren.Count == 0)
            {
                continue;
            }

            var candidatePanel = targetChildren[0].transform.FindChild("Canvas/Layout/Center/Panel/SeedChooser");
            if (candidatePanel == null)
            {
                continue;
            }

            var grid = candidatePanel.FindChild("Grid");
            if (grid == null || grid.childCount == 0)
            {
                // UI not built yet on this pass - try again next frame (bounded by MaxAttempts).
                return;
            }

            seedChooserPanel = candidatePanel;
            break;
        }

        if (seedChooserPanel == null)
        {
            // Keep retrying (bounded) - the seed chooser's own UI may not have opened/built yet.
            return;
        }

        patchMarker.IsPatched = true;

        var arrowsClone = UnityEngine.Object.Instantiate(arrowsSource, seedChooserPanel);
        arrowsClone.name = "CoreLib_SeedChooserPagingArrows";
        arrowsClone.SetActive(true);

        var buttons = arrowsClone.GetComponentsInChildren<Button>(true);

        // Confirmed live: sibling order 0/1 matches left/right position and previous/next handler
        // assignment correctly - only the icons themselves needed fixing (see the localScale flip
        // below), not this mapping.
        for (int i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];

            // RemoveAllListeners() only clears RUNTIME listeners, not PERSISTENT ones (Unity's own
            // documented behavior) - the clone still carries AlmanacArchive's serialized
            // nextEntry()/previousEntry() persistent listener pointing at the ORIGINAL Archive
            // instance in the wrong context. If that throws when invoked here, it can abort
            // UnityEvent's invocation list before our own listener (added after it) ever runs -
            // the likely reason clicks produced zero log output despite many attempts. Replacing
            // the whole event object discards persistent listeners too, since there's no public
            // runtime API to remove them individually outside the Editor.
            button.onClick = new Button.ButtonClickedEvent();

            // Confirmed live: handlers/positions are correct (left button already fires
            // GoToPreviousPage, right fires GoToNextPage) - only the icon each one displays reads
            // backwards (left looks like a "next"/right-pointing arrow and vice versa). Mirroring
            // each button's own rendered content horizontally swaps its apparent direction without
            // touching which handler is attached to which position - sprite-agnostic, so it works
            // whether the two icons are separate sprites or one mirrored via scale already.
            var iconTransform = button.transform;
            iconTransform.localScale = new Vector3(-iconTransform.localScale.x, iconTransform.localScale.y, iconTransform.localScale.z);

            bool isPrevious = i == 0;
            button.onClick.AddListener((UnityAction)(() =>
            {
                if (isPrevious)
                {
                    SeedChooserPaging.GoToPreviousPage(dataModel, __instance);
                }
                else
                {
                    SeedChooserPaging.GoToNextPage(dataModel, __instance);
                }
            }));
        }

        // Anchored bottom-right of the seed chooser panel, clear of the grid itself - a rough
        // starting position, will very likely need live tuning against the real layout.
        var rect = arrowsClone.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-20, 20);
        }
    }
}
