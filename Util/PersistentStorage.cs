using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppSystem.Runtime.InteropServices;
using MelonLoader;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace PvZReCoreLib.Util;

[RegisterTypeInIl2Cpp]
public class PersistentStorage : MonoBehaviour
{
    #region Variables

    public static GameObject Keeper;
    public static PersistentStorage Instance;

    private bool firstTimeInit = true;
    
    public Il2CppReferenceField<Il2CppSystem.Collections.Generic.List<Il2CppSystem.Object>> StoredObjects;
    private GCHandle StoredObjectsHandle;

    public Il2CppReferenceField<Il2CppSystem.Collections.Generic.List<IntPtr>> StoredStrings;
    private GCHandle StoredStringsHandle;

    public static AssetReferenceSprite EmptySpriteRef;
    public static AssetReferenceGameObject EmptyGameObjectRef;
    
    public static Action OnPersistentStorageInit;

    #endregion

    #region Constructors

    public PersistentStorage(IntPtr pointer) : base(pointer)
    {
        Instance = this;
    }
    public PersistentStorage() : base(ClassInjector.DerivedConstructorPointer<PersistentStorage>()) => ClassInjector.DerivedConstructorBody(this);

    private void Awake()
    {
        if (firstTimeInit)
        {
            firstTimeInit = false;

            var objectList = new Il2CppSystem.Collections.Generic.List<Il2CppSystem.Object>();
            StoredObjectsHandle = GCHandle.Alloc(objectList);
            StoredObjects.Set(objectList);
            
            var stringList = new Il2CppSystem.Collections.Generic.List<IntPtr>();
            StoredStringsHandle = GCHandle.Alloc(stringList);
            StoredStrings.Set(stringList);

            EmptySpriteRef = Store(new AssetReferenceSprite(""));
            EmptyGameObjectRef = Store(new AssetReferenceGameObject(""));
        }
    }

    public static void Init()
    {
        Keeper = new GameObject("Il2CppKeeperRoot");
        DontDestroyOnLoad(Keeper);
        DontDestroyOnLoad_Injected(Keeper.m_CachedPtr);

        Keeper.AddComponent<PersistentStorage>();
        
        OnPersistentStorageInit?.Invoke();
    }

    #endregion

    #region Methods
    
    public static T Store<T>(T toStore) where T : Il2CppSystem.Object
    {
        Instance.StoredObjects.Value.Add(toStore);
        return toStore;
    }

    public static Il2CppSystem.String Store(string str)
    {
        // 1. Create a new IL2CPP string
        IntPtr il2cppStr = IL2CPP.il2cpp_string_new(str);

        // 2. Root (pin) it
        IntPtr handle = IL2CPP.il2cpp_gchandle_new(il2cppStr, true);

        // 3. Keep the handle alive
        Instance.StoredStrings.Value.Add(handle);

        // 4. Return the managed Il2CppSystem.String wrapper
        return Store(new Il2CppSystem.String(IL2CPP.il2cpp_gchandle_get_target(handle)));
    }

    public static void Assign(Il2CppSystem.Object toObject, string fieldName, string str)
    {
        IntPtr il2cppStr = IL2CPP.il2cpp_string_new(str);

        // Root it so the GC can’t move or collect it
        var rootedStringHandle = IL2CPP.il2cpp_gchandle_new(il2cppStr, true);
        Instance.StoredStrings.Value.Add(rootedStringHandle);

        // Get the real pointer back (in case the GC handle wrapper changes)
        IntPtr rootedPtr = IL2CPP.il2cpp_gchandle_get_target(rootedStringHandle);
        
        // Get The object ptr
        IntPtr objPtr = IL2CPP.Il2CppObjectBaseToPtrNotNull(toObject); // get raw pointer
        IntPtr nativeClassPtr = IL2CPP.il2cpp_object_get_class(objPtr); // Il2CppClass* for this instan

        // Assign to your field
        IntPtr plantPtr = IL2CPP.Il2CppObjectBaseToPtrNotNull(toObject);
        uint fieldOffset = IL2CPP.il2cpp_field_get_offset(IL2CPP.GetIl2CppField(nativeClassPtr, fieldName));
        IL2CPP.il2cpp_gc_wbarrier_set_field(plantPtr, plantPtr + (int)fieldOffset, rootedPtr);
    }

    #endregion

    
}