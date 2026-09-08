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

    // Set by SeedChooserScreen_InjectPagingButtons_Patch right after it confirms a non-empty,
    // sane Grid (its own search already prefers the smallest candidate when more than one
    // P_SeedChooser exists - see its comment). Reusing this one verified reference instead of
    // re-searching the scene fresh every call avoids redoing that same search repeatedly.
    public static Transform CachedGrid { get; set; }

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
    // minigame-ID gap range) is never visible on any page - except Imitater, which paging
    // excludes from pageIndexByType entirely (see ApplyPaging) but must still never be pruned,
    // since it isn't a page-consuming grid entry at all and needs to render exactly as native
    // always has, on every page.
    public static bool ShouldBeVisible(SeedType seedType)
    {
        if (seedType == SeedType.Imitater)
        {
            return true;
        }

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
            // Imitater sits inside the "vanilla" SeedType range (48, right after the 48 real
            // plants - confirmed live VanillaCount is 49, counting it) but isn't a real grid
            // entry at all: it renders in its own dedicated slot outside the Grid hierarchy
            // entirely, using this same ChosenSeed's mX/mY for something other than a page
            // position. Treating it as page 49's overflow moved its coordinates onto Peashooter's
            // real grid slot (confirmed live: the Imitater slot started showing Peashooter's
            // tooltip/icon) - excluded here so paging never touches it at all.
            if (cs.mSeedType == SeedType.Imitater)
            {
                continue;
            }

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
        RewireVisibleGridNavigation();
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
        RewireVisibleGridNavigation();
    }

    // Confirmed live (via an on-demand nav-dump diagnostic) the actual root cause of a grid
    // keyboard/controller navigation freeze: native's own GridNavigationContainer reconfigures a
    // resized grid's up/down reasonably, but its left/right sibling-to-sibling chain doesn't get
    // rebuilt - cells kept pointing at the SAME 48-cell-page siblings even after we pruned down to
    // an 8-cell custom page, so the moment navigation tried to follow one of those now-destroyed
    // references, Unity's EventSystem selection collapsed to null - and once null, there's no
    // "from" point left for ANY further keyboard/controller input to navigate from, which is why
    // this reads as a total, permanent-for-the-session freeze rather than an occasional misstep.
    //
    // Since it's OUR OWN paging that resizes the grid, we own fixing the chain afterward rather
    // than trusting native to notice - re-linking every CURRENTLY VISIBLE cell to its real
    // same-page row/column neighbors. Deliberately only overwrites a direction when a same-page
    // neighbor actually exists in that direction (see SetHorizontalNav's same pattern above) -
    // boundary cells (first/last row, first/last column) keep whatever native already wired for
    // them (e.g. the bottom row's "down" into the Let's Rock button), since those were never the
    // stale/broken links in the first place.
    public static void RewireVisibleGridNavigation()
    {
        // Prefer the cached reference (see CachedGrid's own comment) - only fall back to a fresh
        // search if nothing's been cached yet (e.g. this runs before
        // SeedChooserScreen_InjectPagingButtons_Patch ever
        // successfully found one this session).
        Transform grid = CachedGrid;
        if (grid == null)
        {
            // Same "prefer the smallest sane non-empty Grid across every P_SeedChooser
            // candidate" logic as SeedChooserScreen_InjectPagingButtons_Patch - see its own
            // comment for why taking the first match isn't safe (stale, orphan-accumulated
            // copies from earlier restarts can coexist in the scene at once).
            var activeScene = SceneManager.GetActiveScene();
            foreach (var root in activeScene.GetRootGameObjects())
            {
                foreach (var candidateRoot in UnityUtil.FindDeepChildrenByName(root.transform, "P_SeedChooser"))
                {
                    var candidateGrid = candidateRoot.transform.FindChild("Canvas/Layout/Center/Panel/SeedChooser/Grid");
                    if (candidateGrid == null || candidateGrid.childCount == 0)
                    {
                        continue;
                    }

                    if (grid == null || candidateGrid.childCount < grid.childCount)
                    {
                        grid = candidateGrid;
                    }
                }
            }
        }

        if (grid == null || grid.childCount == 0)
        {
            // Not ready yet (or genuinely not found) - safe to skip, a later page-turn or the
            // next UpdateEntries() call will call this again.
            return;
        }

        // Filtering to active cells only, rather than trusting raw sibling index, is cheap
        // insurance against anything else ever transiently inflating grid.childCount beyond
        // what's really on screen (confirmed live this could happen: each individual
        // Add/RemoveModel call in ApplyPageVisibility used to fire its own "Modified" event,
        // and the view's reaction to it was a full re-sync rather than a surgical diff - 48
        // separate RemoveModel calls in one page turn meant 48 separate re-syncs, ballooning to
        // over a thousand children in a single page turn. That's fixed at the source now
        // (ApplyPageVisibility/PruneToCurrentPage batch all their changes behind
        // DisableModifiedTrigger and emit exactly one Modified at the end), but this filter costs
        // nothing and remains a reasonable safety net.
        var activeSelectables = new List<Selectable>();
        for (int i = 0; i < grid.childCount; i++)
        {
            var child = grid.GetChild(i);
            if (!child.gameObject.activeInHierarchy)
            {
                continue;
            }

            var selectable = child.GetComponentInChildren<Selectable>();
            if (selectable != null)
            {
                activeSelectables.Add(selectable);
            }
        }

        var selectables = activeSelectables.ToArray();
        int count = selectables.Length;

        for (int i = 0; i < count; i++)
        {
            var selectable = selectables[i];
            if (selectable == null)
            {
                continue;
            }

            int column = i % GridColumns;

            Selectable up = i >= GridColumns ? selectables[i - GridColumns] : null;
            Selectable down = (i + GridColumns) < count ? selectables[i + GridColumns] : null;
            Selectable left = column > 0 ? selectables[i - 1] : null;
            Selectable right = (column < GridColumns - 1 && i + 1 < count) ? selectables[i + 1] : null;

            var nav = selectable.navigation;
            nav.mode = Navigation.Mode.Explicit;
            if (up != null) nav.selectOnUp = up;
            if (down != null) nav.selectOnDown = down;
            if (left != null) nav.selectOnLeft = left;
            if (right != null) nav.selectOnRight = right;
            selectable.navigation = nav;
        }
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
            // None of the guessed names matched - dump the real hierarchy under the found
            // AlmanacArchive so the actual child name can be read from the log instead of
            // guessed again (e.g. if a future game update renames this asset).
            var childNames = new List<string>();
            void DumpChildren(Transform t, int depth)
            {
                // Index-based, not foreach - foreach (Transform child in t) throws
                // InvalidCastException live under IL2Cpp interop (confirmed the hard way
                // elsewhere in this file - see the Buttons-bar diagnostic below).
                for (int i = 0; i < t.childCount; i++)
                {
                    var child = t.GetChild(i);
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
        //
        // Multiple "P_SeedChooser" instances can in principle coexist in the scene (e.g. a stale
        // copy left behind by an earlier restart never torn down) - considering every candidate
        // across every root and preferring the smallest sane non-empty Grid, rather than whichever
        // happens to be first by scene root enumeration order, means a stale/oversized leftover
        // never gets picked over the real, currently-visible one.
        Transform seedChooserPanel = null;
        Transform seedChooserRoot = null;
        Transform bestGrid = null;
        var activeScene = SceneManager.GetActiveScene();
        foreach (var root in activeScene.GetRootGameObjects())
        {
            foreach (var candidateRoot in UnityUtil.FindDeepChildrenByName(root.transform, "P_SeedChooser"))
            {
                var candidatePanel = candidateRoot.transform.FindChild("Canvas/Layout/Center/Panel/SeedChooser");
                if (candidatePanel == null)
                {
                    continue;
                }

                var grid = candidatePanel.FindChild("Grid");
                if (grid == null || grid.childCount == 0)
                {
                    continue;
                }

                if (bestGrid == null || grid.childCount < bestGrid.childCount)
                {
                    bestGrid = grid;
                    seedChooserRoot = candidateRoot.transform;
                    seedChooserPanel = candidatePanel;
                }
            }
        }

        if (seedChooserPanel == null)
        {
            // Keep retrying (bounded) - the seed chooser's own UI may not have opened/built yet.
            return;
        }

        SeedChooserPaging.CachedGrid = bestGrid;

        patchMarker.IsPatched = true;

        // Parented under the ViewStore/ViewAlmanac row, not seedChooserPanel - seedChooserPanel's
        // own bounds/anchoring never let this land anywhere but stuck near/overlapping the
        // Imitator slot no matter what offset was tried, since the panel doesn't actually extend
        // out to the open margin between it and Shop the way a naive "just anchor further right"
        // assumption expected. This row does reach that open space; the actual bugs were the
        // scale (0.35x read as "too small") and the button separation not producing a real visible
        // gap in practice - fixing both below with a diagnostic to confirm the real result this
        // time instead of guessing blind again.
        // NotCoop is a mode-conditional row (a co-op session uses a different, "Coop" one
        // instead) - fall back to the panel itself if it's not the active one this session.
        var notCoopRow = seedChooserRoot.FindChild("Canvas/Layout/Center/Buttons/NotCoop");
        var buttonsRow = (notCoopRow != null && notCoopRow.gameObject.activeInHierarchy) ? notCoopRow : seedChooserPanel;

        var arrowsClone = UnityEngine.Object.Instantiate(arrowsSource, buttonsRow);
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

        // Force explicit separation between the two cloned buttons - confirmed live via a layout
        // dump each one is a real 330x300 rect (they clone from AlmanacArchive's own "arrows",
        // sized for that screen). This offset is in the group's own unscaled local space, so it
        // scales down together with the buttons themselves below - at 0.5 scale, +-220 renders as
        // +-110 with each button's own rendered half-width at 82.5, leaving a real ~55-unit visible
        // gap between them instead of the near-zero gap +-180/0.35 apparently produced live.
        if (buttons.Length >= 2)
        {
            var previousRect = buttons[0].GetComponent<RectTransform>();
            var nextRect = buttons[1].GetComponent<RectTransform>();
            if (previousRect != null)
            {
                previousRect.anchoredPosition = new Vector2(-220, 0);
            }

            if (nextRect != null)
            {
                nextRect.anchoredPosition = new Vector2(220, 0);
            }
        }

        // Looked up here (rather than only later, for nav-wiring) so positioning below can be
        // computed relative to ViewStore's own REAL, currently-live position instead of a
        // hardcoded guess - confirmed live that a fixed world-space offset that measured out
        // correctly in one game mode rendered somewhere else entirely in another (Onslaught),
        // so anything mode-specific about scale/layout there is sidestepped by reading ViewStore's
        // own anchoredPosition fresh every time instead of assuming it never moves.
        var imitatorSlot = seedChooserPanel.FindChild("Imitator/P_GamePlay_SeedChooser_Item (1)/Offset/SeedBackground")?.GetComponent<Selectable>();
        var viewStore = seedChooserRoot.FindChild("Canvas/Layout/Center/Buttons/NotCoop/ViewStore")?.GetComponent<Selectable>();
        var viewStoreRect = viewStore?.GetComponent<RectTransform>();

        // Same row as ViewStore/ViewAlmanac (or seedChooserPanel as a fallback - see buttonsRow
        // above), using their own anchor convention (anchorMin/Max=(0,1), pivot=(0,0)). 0.5 scale
        // (was 0.35, read as "too small") brings each 330x300 button down to a more visible
        // ~165x150; the pair (each button's own half-width 82.5, plus 220 separation, times the
        // 0.5 scale) extends 192.5 either side of this group's own local origin.
        var rect = arrowsClone.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = new Vector3(0.5f, 0.5f, 1f);

            if (buttonsRow == notCoopRow && viewStoreRect != null)
            {
                const float pairHalfWidth = 192.5f;
                const float gapBeforeViewStore = 40f;
                float groupX = viewStoreRect.anchoredPosition.x - gapBeforeViewStore - pairHalfWidth;

                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 0);
                rect.anchoredPosition = new Vector2(groupX, viewStoreRect.anchoredPosition.y);
            }
            else
            {
                // Fallback for screens with no NotCoop row (e.g. a minigame with no Shop/Almanac
                // access) - anchored using the Imitator slot's own convention (anchorMin/Max=(1,0),
                // pivot=(0,0)), well clear of its x=[-18,237] local span.
                rect.anchorMin = new Vector2(1, 0);
                rect.anchorMax = new Vector2(1, 0);
                rect.pivot = new Vector2(0, 0);
                rect.anchoredPosition = new Vector2(550, 120);
            }
        }

        // Our buttons clone from AlmanacArchive with mode=None (confirmed live via a one-time
        // hierarchy dump - not reachable by gamepad/keyboard, mouse-only). This screen's whole
        // menu uses plain Unity Explicit Selectable.navigation for its left/right chain (a custom
        // layer on top just decides which chain is "current," per Il2CppReloaded.Input's
        // *NavigationContainer types) - confirmed live the real chain runs
        // Grid -> Imitator's seed slot -> ViewStore -> ViewAlmanac -> ViewLawnButton, all via
        // left/right only (no vertical links at this row). Splices our two buttons into that
        // chain between the Imitator slot and ViewStore, matching where the user asked for them.
        if (buttons.Length >= 2)
        {
            if (imitatorSlot != null && viewStore != null)
            {
                var previousButton = buttons[0];
                var nextButton = buttons[1];

                SetHorizontalNav(imitatorSlot, right: previousButton);
                SetHorizontalNav(previousButton, left: imitatorSlot, right: nextButton);
                SetHorizontalNav(nextButton, left: previousButton, right: viewStore);
                SetHorizontalNav(viewStore, left: nextButton);
            }
            else
            {
                MelonLoader.MelonLogger.Warning("[CoreLib] Could not find the Imitator seed slot and/or ViewStore button to splice seed chooser paging buttons into the gamepad/keyboard navigation chain - paging buttons will stay mouse-only this session.");
            }
        }
    }

    // Only overwrites the directions passed - null leaves whatever that Selectable already had
    // (e.g. the Imitator slot's own left=Grid, or ViewStore's own right=ViewAlmanac, both stay
    // untouched here).
    static void SetHorizontalNav(Selectable selectable, Selectable left = null, Selectable right = null)
    {
        var nav = selectable.navigation;
        nav.mode = Navigation.Mode.Explicit;
        if (left != null) nav.selectOnLeft = left;
        if (right != null) nav.selectOnRight = right;
        selectable.navigation = nav;
    }
}

