using System.Reflection;
using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Il2CppSource.DataModels;
using Il2CppTekly.DataModels.Models;

namespace PvZReCoreLib.Content.Plants.Patches;

// The visible seed-chooser grid is driven by SeedChooserDataModel's reactive models, not directly
// by SeedChooserScreen.mChosenSeeds. Rather than hand-building SeedChooserEntryModel instances
// ourselves (which only touched m_entriesModel and left the separate m_entriesUnlockedModel that
// the view actually watches untouched), we force the game's own private UpdateEntries() to rerun
// once mChosenSeeds has our custom entries - it rebuilds both models straight from mChosenSeeds,
// exactly like it does for the built-in plants.
[HarmonyPatch(typeof(SeedChooserDataModel), nameof(SeedChooserDataModel.UpdateSeedChooserScreen))]
public class SeedChooserDataModel_CustomPlantEntriesPatch
{
    static readonly MethodInfo UpdateEntriesMethod = AccessTools.Method(typeof(SeedChooserDataModel), "UpdateEntries");

    // UpdateSeedChooserScreen only fires once (when the screen opens), not every frame - and it
    // fires BEFORE SeedChooserScreen_CustomSeedsPatch has populated mChosenSeeds for this session.
    // Cache the instance here so that patch can force a retry once mChosenSeeds is actually ready,
    // instead of relying on a "next tick" that never comes.
    public static SeedChooserDataModel CachedDataModel { get; private set; }

    // Every entry pruned out of the live models, keyed by SeedType, so a later page turn can put
    // it straight back instead of asking UpdateEntries() to build a fresh one. Confirmed live:
    // each SeedChooserEntryModel loads an Addressables thumbnail sprite in its constructor, and
    // re-running UpdateEntries() on every page turn (the original approach) re-triggered that load
    // for every single entry all over again - the actual cause of an ~8 second stall per page
    // turn. RemoveModel never disposes what it removes (only Clear()/OnDispose do - see the
    // ClearNoDispose comment below), so a removed entry's thumbnail stays loaded in memory the
    // whole time it sits in these caches, ready to be reattached instantly.
    private static readonly Dictionary<SeedType, (IModel Model, ReferenceType RefType)> hiddenEntries = new();
    private static readonly Dictionary<SeedType, (IModel Model, ReferenceType RefType)> hiddenUnlockedEntries = new();
    private static readonly HashSet<SeedType> visibleEntries = new();

    public static void Postfix(SeedChooserDataModel __instance, SeedChooserScreen seedChooserScreen)
    {
        CachedDataModel = __instance;
        TryRefresh(__instance, seedChooserScreen);
    }

    public static void TryRefresh(SeedChooserDataModel dataModel, SeedChooserScreen seedChooserScreen)
    {
        var patchMarker = PatchMarker<SeedChooserDataModel>.GetOrCreateExtension<PatchMarker<SeedChooserDataModel>>(dataModel);
        if (patchMarker.IsPatched)
        {
            return;
        }

        var allCustomTypes = CustomContentRegistry.GetAllCustomPlantTypes();
        if (allCustomTypes.Count == 0)
        {
            patchMarker.IsPatched = true;
            return;
        }

        foreach (SeedType seedType in allCustomTypes)
        {
            bool found = false;
            foreach (var cs in seedChooserScreen.mChosenSeeds)
            {
                if (cs.mSeedType == seedType)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                // SeedChooserScreen_CustomSeedsPatch hasn't populated mChosenSeeds for this
                // seed type yet - try again once it has.
                MelonLoader.MelonLogger.Msg($"[CoreLib] SeedChooserDataModel patch: mChosenSeeds missing {seedType} so far - deferring UpdateEntries() refresh.");
                return;
            }
        }

        patchMarker.IsPatched = true;
        ForceRefresh(dataModel, seedChooserScreen);
    }

    // Builds the reactive models from scratch via the game's own UpdateEntries() - the actual
    // clear-then-rebuild-then-prune sequence now lives in
    // SeedChooserDataModel_UpdateEntries_AlwaysCleanRebuildPatch's Prefix/Postfix below, which
    // fires around EVERY call to UpdateEntries(), not just this one - see that patch's comment for
    // why. This just triggers that call and logs the result.
    public static void ForceRefresh(SeedChooserDataModel dataModel, SeedChooserScreen screen)
    {
        if (UpdateEntriesMethod == null)
        {
            MelonLoader.MelonLogger.Warning("[CoreLib] Could not find SeedChooserDataModel.UpdateEntries() via reflection - custom seeds will not appear in the chooser grid.");
            return;
        }

        UpdateEntriesMethod.Invoke(dataModel, null);

        MelonLoader.MelonLogger.Msg($"[CoreLib] Forced SeedChooserDataModel.UpdateEntries() refresh - entriesModel now has {dataModel.m_entriesModel.m_models.Count}, unlockedModel now has {dataModel.m_entriesUnlockedModel.m_models.Count}.");
    }

    // Shared by the always-on UpdateEntries Prefix below and (transitively, via ForceRefresh) our
    // own first triggered call.
    public static void ResetPagingCaches()
    {
        hiddenEntries.Clear();
        hiddenUnlockedEntries.Clear();
        visibleEntries.Clear();
    }

    // Pulls whatever's off the current page back out of both models directly, keyed by the same
    // string(SeedType-as-int) key ObjectModel uses (confirmed live: key='88' for seedType 88) -
    // captured into the hidden* caches rather than just discarded, so ApplyPageVisibility can
    // restore it later without rebuilding it.
    //
    // Deliberately checks SeedChooserPaging.ShouldBeVisible, NOT mSeedState - mSeedState is an
    // unrelated native mechanism (a plant can be legitimately hidden/grayed for its own reasons -
    // roof restrictions, locked, etc. - independent of which page it's on), and an earlier version
    // of this wrongly keyed removal off it, which deleted a real native SeedPacketHidden entry (a
    // roof-restricted custom plant on a rooftop level) from the model entirely instead of leaving
    // it for native code to render however it intended.
    //
    // ShouldBeVisible also prunes VANILLA entries while a custom page is showing (not just custom
    // entries while the vanilla page is showing) - an earlier version only ever pruned custom
    // entries, so viewing "page 2" rendered as 48 vanilla + up to 48 custom stacked together
    // instead of swapping to a clean, custom-only page.
    public static void PruneToCurrentPage(SeedChooserDataModel dataModel, SeedChooserScreen screen)
    {
        // Same batching as ApplyPageVisibility, and for the same reason - see its own comment for
        // the measured cascade this avoids. This can remove dozens of entries in one pass (e.g.
        // pruning a freshly-rebuilt ~56-entry model down to 48), so it's just as exposed.
        dataModel.m_entriesModel.DisableModifiedTrigger = true;
        dataModel.m_entriesUnlockedModel.DisableModifiedTrigger = true;
        try
        {
            foreach (var cs in screen.mChosenSeeds)
            {
                if (SeedChooserPaging.ShouldBeVisible(cs.mSeedType))
                {
                    visibleEntries.Add(cs.mSeedType);
                    continue;
                }

                CaptureAndRemove(dataModel.m_entriesModel, cs.mSeedType, hiddenEntries);
                CaptureAndRemove(dataModel.m_entriesUnlockedModel, cs.mSeedType, hiddenUnlockedEntries);
            }
        }
        finally
        {
            dataModel.m_entriesModel.DisableModifiedTrigger = false;
            dataModel.m_entriesUnlockedModel.DisableModifiedTrigger = false;
        }

        dataModel.m_entriesModel.EmitModified();
        dataModel.m_entriesUnlockedModel.EmitModified();
    }

    // The lightweight page-turn path: moves already-built entries between the live models and the
    // hidden* caches based on SeedChooserPaging.ShouldBeVisible, without ever calling
    // UpdateEntries() again. This is what actually fixed the ~8 second per-page-turn stall -
    // ForceRefresh's full rebuild was re-loading every entry's thumbnail from scratch every time.
    // Confirmed live (a before/after Grid.childCount bracket around the old version of this
    // method) that each individual Add/RemoveModel call fires the model's "Modified" event
    // immediately, and the view's own reaction to it isn't a surgical diff - it's a full re-sync
    // against whatever's currently in the model. Calling RemoveModel 48 times in a row (a normal
    // page turn) meant 48 separate re-syncs, each re-rendering however many entries were STILL
    // present at that moment - a triangular-number cascade (47+46+45+...) that measured out to
    // 1128 extra renders for one page turn alone (48 -> 1176 after the hide phase, then -> 1212
    // after 8 more Add calls in the show phase). DisableModifiedTrigger defers all of that to a
    // single EmitModified() after every change in this call is already applied, so the view
    // re-syncs exactly once per page turn instead of once per individual entry.
    public static void ApplyPageVisibility(SeedChooserDataModel dataModel)
    {
        dataModel.m_entriesModel.DisableModifiedTrigger = true;
        dataModel.m_entriesUnlockedModel.DisableModifiedTrigger = true;
        try
        {
            var toHide = new List<SeedType>();
            foreach (var seedType in visibleEntries)
            {
                if (!SeedChooserPaging.ShouldBeVisible(seedType))
                {
                    toHide.Add(seedType);
                }
            }

            foreach (var seedType in toHide)
            {
                CaptureAndRemove(dataModel.m_entriesModel, seedType, hiddenEntries);
                CaptureAndRemove(dataModel.m_entriesUnlockedModel, seedType, hiddenUnlockedEntries);
                visibleEntries.Remove(seedType);
            }

            var toShow = new List<SeedType>();
            foreach (var seedType in hiddenEntries.Keys)
            {
                if (SeedChooserPaging.ShouldBeVisible(seedType))
                {
                    toShow.Add(seedType);
                }
            }

            foreach (var seedType in toShow)
            {
                string key = ((int)seedType).ToString();

                if (hiddenEntries.Remove(seedType, out var entry))
                {
                    dataModel.m_entriesModel.Add(key, entry.Model, entry.RefType);
                }

                if (hiddenUnlockedEntries.Remove(seedType, out var unlockedEntry))
                {
                    dataModel.m_entriesUnlockedModel.Add(key, unlockedEntry.Model, unlockedEntry.RefType);
                }

                visibleEntries.Add(seedType);
            }
        }
        finally
        {
            dataModel.m_entriesModel.DisableModifiedTrigger = false;
            dataModel.m_entriesUnlockedModel.DisableModifiedTrigger = false;
        }

        dataModel.m_entriesModel.EmitModified();
        dataModel.m_entriesUnlockedModel.EmitModified();

        MelonLoader.MelonLogger.Msg($"[CoreLib] Applied page visibility - entriesModel now has {dataModel.m_entriesModel.m_models.Count}, unlockedModel now has {dataModel.m_entriesUnlockedModel.m_models.Count}.");
    }

    // Captures the ModelReference's Model/ReferenceType before removing it, so ApplyPageVisibility
    // can put the exact same instance back later instead of asking UpdateEntries() to build a new
    // one (and re-trigger its thumbnail load) from scratch.
    static void CaptureAndRemove(ObjectModel model, SeedType seedType, Dictionary<SeedType, (IModel Model, ReferenceType RefType)> cache)
    {
        string key = ((int)seedType).ToString();
        foreach (var reference in model.m_models)
        {
            if (reference.Key == key)
            {
                cache[seedType] = (reference.Model, reference.ReferenceType);
                break;
            }
        }

        model.RemoveModel(key);
    }
}

// UpdateEntries() only ever appends - its ObjectModel storage (m_models) is a plain List with no
// key-uniqueness check, so calling it a second time without clearing first creates genuine
// duplicate ModelReference entries for every SeedType, each rendering its own extra card.
// Confirmed live: a stray extra call to UpdateEntries() - seemingly tied to cycling through the
// Imitater dialog a few times before restarting a Last Stand run - doubled the total from a
// correct 48 to exactly 104 (48 + 56, a second unpruned copy of every vanilla and custom entry)
// with no exception or warning anywhere. The trigger for that extra call isn't identified, but
// clearing immediately before EVERY call to UpdateEntries() - not just the one
// SeedChooserDataModel_CustomPlantEntriesPatch.ForceRefresh explicitly triggers - guarantees
// duplicates can never accumulate no matter who calls it or why.
[HarmonyPatch(typeof(SeedChooserDataModel), "UpdateEntries")]
public class SeedChooserDataModel_UpdateEntries_AlwaysCleanRebuildPatch
{
    // ClearNoDispose, not Clear(): Clear() disposes each contained model, which may cancel an
    // in-flight async thumbnail load for a plant that hadn't finished loading yet.
    static void Prefix(SeedChooserDataModel __instance)
    {
        __instance.m_entriesModel.ClearNoDispose();
        __instance.m_entriesUnlockedModel.ClearNoDispose();
        SeedChooserDataModel_CustomPlantEntriesPatch.ResetPagingCaches();
    }

    // Re-applies page-visibility pruning after ANY UpdateEntries() call, not just the one we
    // explicitly trigger ourselves - without this, a stray native call (see Prefix above) would
    // still be duplicate-free after the fix above, but would leave every entry visible again
    // regardless of whatever page was actually selected.
    static void Postfix(SeedChooserDataModel __instance)
    {
        var screen = __instance.m_seedChooserScreen;
        if (screen == null)
        {
            return;
        }

        SeedChooserPaging.ApplyPaging(screen);
        SeedChooserDataModel_CustomPlantEntriesPatch.PruneToCurrentPage(__instance, screen);

        // Same reason as the page-turn call sites in SeedChooserPaging.GoToNextPage/
        // GoToPreviousPage - whatever just rebuilt the grid's actual cell count needs its
        // left/right sibling chain rebuilt too, or navigation eventually walks into a
        // now-destroyed cell and permanently loses selection. See RewireVisibleGridNavigation's
        // own comment for the full story.
        SeedChooserPaging.RewireVisibleGridNavigation();
    }
}
