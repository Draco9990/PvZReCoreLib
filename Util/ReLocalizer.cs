using HarmonyLib;
using Il2CppReloaded;
using Il2CppReloaded.Binders;
using Il2CppReloaded.DataModels;
using Il2CppReloaded.Services;
using PvZReCoreLib.Content.Plants;

namespace PvZReCoreLib.Util;

public class ReLocalizer
{
    #region Variables

    public static Dictionary<string, Dictionary<int, string>> localizedStrings = new();
    
    public static Action OnReLocalizerInit;

    #endregion

    #region Constructors



    #endregion

    #region Methods
    
    public static void Init()
    {
        OnReLocalizerInit?.Invoke();
    }

    public static void AddLocalizedString(string key, Dictionary<int, string> overrides)
    {
        localizedStrings[key] = overrides;
    }

    public static bool Localize(string key, out string localized)
    {
        if(key.StartsWith("[RAW]"))
        {
            localized = key.Replace("[RAW]", "");
            return true;
        }
        else
        {
            int overrideValue = (int)AppCore.GetService<ISettingsService>().Cast<SettingsService>().m_currentLanguage;

            bool changed = false;
            foreach (KeyValuePair<string,Dictionary<int,string>> pair in ReLocalizer.localizedStrings)
            {
                bool hasChanges = false;
                
                //split value by words
                string[] words = key.Split(' ');
                for (int i = 0; i < words.Length; i++)
                {
                    if (words[i].StartsWith(pair.Key))
                    {
                        words[i] = pair.Value.TryGetValue(overrideValue, out var localizedString) ? localizedString : words[i];
                        hasChanges = true;
                        break;
                    }
                }
                
                if (hasChanges)
                {
                    key = string.Join(" ", words);
                    changed = true;
                }
            }
            
            localized = key;
            return changed;
        }
    }

    #endregion
}

[HarmonyPatch(typeof(FormattedLocalizationIdBinder), nameof(FormattedLocalizationIdBinder.BindValue))]
public class FormattedLocalizationIdBinder_BindValue_Patch
{
    public static bool Prefix(FormattedLocalizationIdBinder __instance, ref string value)
    {
        if (ReLocalizer.Localize(value, out string localizedVal))
        {
            if (__instance.m_text != null)
            {
                __instance.m_text.SetText(localizedVal);
                return false;
            }
        }
        
        return true;
    }
}

[HarmonyPatch(typeof(AlmanacEntryModel), nameof(AlmanacEntryModel.UpdateModelData))]
public class AlmanacDescriptionBinder_BindString_Patch
{
    public static void Postfix(AlmanacEntryModel __instance)
    {
        CustomAlmanacEntryData entryData = __instance.m_entryData.TryCast<CustomAlmanacEntryData>();
        if (entryData == null)
        {
            return;
        }

        ReLocalizer.Localize(entryData.m_descriptionHeader, out string localizedHeader);
        ReLocalizer.Localize(entryData.m_entryDescription, out string localizedDesc);
        
        string merged = $"{localizedHeader}\n\n{localizedDesc}";
        __instance.m_descriptionModel.Value = merged;
    }
}