using Il2CppReloaded.Gameplay;
using MelonLoader;
using UnityEngine;

namespace PvZReCoreLib.Util;

public class UnityUtil
{
    #region Variables



    #endregion

    #region Constructors



    #endregion

    #region Methods

    public static void LogDetails(GameObject obj, int indentLevel = 0)
    {
        string indent = new string(' ', indentLevel * 2);
        
        if (obj == null)
        {
            MelonLogger.Msg($"{indent}GameObject is null");
            return;
            
        }

        MelonLogger.Msg($"{indent}--- GameObject: {obj.name} ---");
        MelonLogger.Msg($"{indent}Type: {obj.GetIl2CppType().FullName}");
        MelonLogger.Msg($"{indent}IsPlant?: {obj.TryCast<Plant>() != null}");

        Component[] components = obj.GetComponents<Component>();
        MelonLogger.Msg($"{indent}--- Components ({components.Length}) ---");
        foreach (Component comp in components)
        {
            if (comp == null)
            {
                MelonLogger.Msg("Missing Script Component");
                continue;
            }

            MelonLogger.Msg($"{indent}Component: {comp.GetIl2CppType().FullName}");
            

            // Handle common component types manually
            if (comp.TryCast<Transform>() is { } t)
            {
                MelonLogger.Msg($"{indent}Position: {t.position}");
                MelonLogger.Msg($"{indent}Rotation: {t.rotation.eulerAngles}");
                MelonLogger.Msg($"{indent}Scale: {t.localScale}");
                MelonLogger.Msg($"{indent}Child Count: {t.childCount}");
            }
            else if (comp.TryCast<Renderer>() is { } r)
            {
                MelonLogger.Msg($"{indent}Enabled: {r.enabled}");
                MelonLogger.Msg($"{indent}Material Count: {r.sharedMaterials.Length}");
                foreach (var mat in r.sharedMaterials)
                    MelonLogger.Msg($"{indent}Material: {mat?.name ?? "None"}");
            }
        }

        // Children
        MelonLogger.Msg($"{indent}--- Children ({obj.transform.childCount}) ---");
        foreach (var o in obj.transform)
        {
            var child = o.TryCast<Transform>();
            if (child is null)
            {
                continue;
            }
            
            MelonLogger.Msg($"{indent}Child: {child.name}");
            LogDetails(child.gameObject, indentLevel + 1);
        }
    }
    
    public static List<GameObject> FindDeepChildrenByName(Transform parent, string name)
    {
        var results = new List<GameObject>();
        FindChildrenRecursive(parent, name, results);
        return results;
    }

    private static void FindChildrenRecursive(Transform parent, string name, List<GameObject> results)
    {
        foreach (var o in parent)
        {
            var child = o.TryCast<Transform>();
            if (child is null)
            {
                continue;
            }

            if (child.name == name)
            {
                results.Add(child.gameObject);
            }
            FindChildrenRecursive(child, name, results);
        }
    }

    #endregion
}