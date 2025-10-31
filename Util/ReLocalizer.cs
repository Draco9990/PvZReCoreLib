using HarmonyLib;
using Il2CppReloaded;
using Il2CppReloaded.Binders;
using Il2CppReloaded.Services;

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

    #endregion
}

[HarmonyPatch(typeof(FormattedLocalizationIdBinder), nameof(FormattedLocalizationIdBinder.BindValue))]
public class FormattedLocalizationIdBinder_BindValue_Patch
{
    public static bool Prefix(FormattedLocalizationIdBinder __instance, ref string value)
    {
        bool localized = false;
        
        if (value.StartsWith("[RAW]"))
        {
            value = value.Replace("[RAW]", "");
            localized = true;
        }
        else
        {
            int overrideValue = (int)AppCore.GetService<ISettingsService>().Cast<SettingsService>().m_currentLanguage;

            foreach (KeyValuePair<string,Dictionary<int,string>> pair in ReLocalizer.localizedStrings)
            {
                //split value by words
                string[] words = value.Split(' ');
                for (int i = 0; i < words.Length; i++)
                {
                    if (words[i].StartsWith(pair.Key))
                    {
                        words[i] = pair.Value.TryGetValue(overrideValue, out var localizedString) ? localizedString : words[i];
                        localized = true;
                        break;
                    }
                }
                
                if (localized)
                {
                    value = string.Join(" ", words);
                }
            }
        }

        if (localized)
        {
            if (__instance.m_text != null)
            {
                __instance.m_text.SetText(value);
                return false;
            }
        }
        
        return true;
    }
}