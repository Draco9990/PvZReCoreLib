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

    // Builds the reactive models from scratch via the game's own UpdateEntries() - this is the
    // only place that ever happens, exactly once per screen-open (called from TryRefresh above).
    // Page turns never call this again - see ApplyPageVisibility, which reuses what this builds
    // instead of rebuilding it.
    public static void ForceRefresh(SeedChooserDataModel dataModel, SeedChooserScreen screen)
    {
        if (UpdateEntriesMethod == null)
        {
            MelonLoader.MelonLogger.Warning("[CoreLib] Could not find SeedChooserDataModel.UpdateEntries() via reflection - custom seeds will not appear in the chooser grid.");
            return;
        }

        // UpdateEntries() only ever adds - it was only ever meant to run once per screen open, and
        // it already ran once (naturally, via UpdateSeedChooserScreen) before mChosenSeeds had our
        // custom entries. Calling it again without clearing first duplicates every built-in entry
        // on top of the originals. Clear both models ourselves so this rebuild starts clean.
        // Using ClearNoDispose rather than Clear: Clear() disposes each contained model, which may
        // cancel in-flight async thumbnail loads for plants that hadn't finished loading yet at
        // this point (we're forcing this refresh within ~100ms of the screen opening).
        dataModel.m_entriesModel.ClearNoDispose();
        dataModel.m_entriesUnlockedModel.ClearNoDispose();
        hiddenEntries.Clear();
        hiddenUnlockedEntries.Clear();
        visibleEntries.Clear();

        UpdateEntriesMethod.Invoke(dataModel, null);

        // UpdateEntries() rebuilds straight from mChosenSeeds and ignores mSeedState entirely -
        // confirmed live: every custom entry showed up in m_entriesUnlockedModel regardless of
        // state. So whatever's off the initial page has to be pulled back out of both models
        // directly, keyed by the same string(SeedType-as-int) key ObjectModel uses (confirmed
        // live: key='88' for seedType 88) - captured into the hidden* caches rather than just
        // discarded, so ApplyPageVisibility can restore it later without rebuilding it.
        //
        // Deliberately checks SeedChooserPaging.ShouldBeVisible, NOT mSeedState - mSeedState is an
        // unrelated native mechanism (a plant can be legitimately hidden/grayed for its own
        // reasons - roof restrictions, locked, etc. - independent of which page it's on), and an
        // earlier version of this wrongly keyed removal off it, which deleted a real native
        // SeedPacketHidden entry (a roof-restricted custom plant on a rooftop level) from the
        // model entirely instead of leaving it for native code to render however it intended.
        //
        // ShouldBeVisible also prunes VANILLA entries while a custom page is showing (not just
        // custom entries while the vanilla page is showing) - an earlier version only ever pruned
        // custom entries, so viewing "page 2" rendered as 48 vanilla + up to 48 custom stacked
        // together instead of swapping to a clean, custom-only page.
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

        MelonLoader.MelonLogger.Msg($"[CoreLib] Forced SeedChooserDataModel.UpdateEntries() refresh - entriesModel now has {dataModel.m_entriesModel.m_models.Count}, unlockedModel now has {dataModel.m_entriesUnlockedModel.m_models.Count}.");
    }

    // The lightweight page-turn path: moves already-built entries between the live models and the
    // hidden* caches based on SeedChooserPaging.ShouldBeVisible, without ever calling
    // UpdateEntries() again. This is what actually fixed the ~8 second per-page-turn stall -
    // ForceRefresh's full rebuild was re-loading every entry's thumbnail from scratch every time.
    public static void ApplyPageVisibility(SeedChooserDataModel dataModel)
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
