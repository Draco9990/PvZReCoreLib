using Object = Il2CppSystem.Object;

namespace PvZReCoreLib.Util;

public class ClassExtension<TClassType> where TClassType : Object
{
    #region Variables
    
    private static Dictionary<TClassType, List<ClassExtension<TClassType>>> classExtensions = new();

    #endregion

    #region Constructors



    #endregion

    #region Methods

    static void PurgeNullTableKeys()
    {
        var keysToRemove = new List<TClassType>();

        foreach (var kvp in classExtensions)
        {
            if (kvp.Key == null)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            classExtensions.Remove(key);
        }
    }
    
    public static TExtensionType GetOrCreateExtension<TExtensionType>(TClassType classInstance)
        where TExtensionType : ClassExtension<TClassType>, new()
    {
        PurgeNullTableKeys();
        
        if (!classExtensions.TryGetValue(classInstance, out var extensions))
        {
            extensions = new List<ClassExtension<TClassType>>();
            classExtensions[classInstance] = extensions;
        }

        foreach (var extension in extensions)
        {
            if (extension is TExtensionType typedExtension)
            {
                return typedExtension;
            }
        }

        var newExtension = new TExtensionType();
        extensions.Add(newExtension);
        return newExtension;
    }
    
    public static TExtensionType GetExtension<TExtensionType>(TClassType classInstance)
        where TExtensionType : ClassExtension<TClassType>
    {
        PurgeNullTableKeys();
        
        if (classExtensions.TryGetValue(classInstance, out var extensions))
        {
            foreach (var extension in extensions)
            {
                if (extension is TExtensionType typedExtension)
                {
                    return typedExtension;
                }
            }
        }

        return null;
    }
    
    public static void DeleteExtensionData(TClassType classInstance)
    {
        PurgeNullTableKeys();
        
        if (classExtensions.ContainsKey(classInstance))
        {
            classExtensions.Remove(classInstance);
        }
    }

    #endregion
}