using System.Linq;
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

        UpdateEntriesMethod.Invoke(dataModel, null);
        MelonLoader.MelonLogger.Msg($"[CoreLib] Forced SeedChooserDataModel.UpdateEntries() refresh - entriesModel now has {dataModel.m_entriesModel.m_models.Count}, unlockedModel now has {dataModel.m_entriesUnlockedModel.m_models.Count}.");

        DumpModelOrder(dataModel);
    }

    static void DumpModelOrder(SeedChooserDataModel dataModel)
    {
        var models = dataModel.m_entriesUnlockedModel.m_models;
        for (int i = 0; i < models.Count; i++)
        {
            var reference = models[i];
            string seedTypeStr = "?";
            var entryModel = reference.Model.TryCast<SeedChooserEntryModel>();
            if (entryModel != null)
            {
                seedTypeStr = entryModel.m_chosenSeed.mSeedType.ToString();
            }
            MelonLoader.MelonLogger.Msg($"[CoreLib]   unlockedModel[{i}] key='{reference.Key}' seedType={seedTypeStr}");
        }
    }
}
