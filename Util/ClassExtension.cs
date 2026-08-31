using Object = Il2CppSystem.Object;

namespace PvZReCoreLib.Util;

public class ClassExtension<TClassType> where TClassType : Object
{
    #region Variables

    private static Dictionary<TClassType, List<ClassExtension<TClassType>>> classExtensions = new();

    #endregion

    #region Methods

    // kvp.Key's static type here is TClassType, constrained only to Il2CppSystem.Object - so
    // `kvp.Key == null` binds to that base type's (non-virtual, static-operator) equality, NOT
    // UnityEngine.Object's overridden op_Equality that treats a destroyed-but-not-yet-GC'd native
    // object as null. That means a destroyed GameObject/Component key never satisfies this check,
    // so entries for despawned plants/projectiles/zombies were never purged - classExtensions (a
    // static dictionary living for the whole game session) grew unbounded. Explicitly checking
    // through UnityEngine.Object's operator (when the key is one, which in practice it always is
    // for this game) restores real "destroyed" detection.
    static bool IsDestroyed(TClassType key)
    {
        if (key is UnityEngine.Object unityObj)
        {
            return unityObj == null;
        }

        return key == null;
    }

    // This game pools GameObjects (ControllerPool<T>) rather than destroying most of them, so in
    // practice there's rarely anything for this to actually purge - but GetOrCreateExtension and
    // GetExtension are called very often (every SetCurrentSkin/PlayAnimation call, for every
    // plant/projectile/zombie in the game), and classExtensions grows to match peak concurrent+
    // pooled entity count and stays there. Scanning the whole dictionary unconditionally on every
    // single call was wasted work independent of whether IsDestroyed above finds anything to
    // remove. Throttling to once a second bounds that cost without materially delaying cleanup -
    // nothing else can look up a destroyed key anyway, since nothing still holds a reference to it
    // to call these methods with.
    static float _lastPurgeTime = float.NegativeInfinity;
    const float PurgeIntervalSeconds = 1f;

    static void PurgeNullTableKeys()
    {
        float now = UnityEngine.Time.realtimeSinceStartup;
        if (now - _lastPurgeTime < PurgeIntervalSeconds)
        {
            return;
        }
        _lastPurgeTime = now;

        var keysToRemove = new List<TClassType>();

        foreach (var kvp in classExtensions)
        {
            if (IsDestroyed(kvp.Key))
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
