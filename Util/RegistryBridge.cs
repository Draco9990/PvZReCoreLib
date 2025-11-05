using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using File = Il2CppSystem.IO.File;
using Object = Il2CppSystem.Object;
using Type = Il2CppSystem.Type;

namespace PvZReCoreLib.Util;

[RegisterTypeInIl2CppWithInterfaces(typeof(IResourceLocator))]
public class CustomResourceLocator : Object
{
    public string LocatorId { get; } = "CustomLocator";

    private Il2CppSystem.Collections.Generic.Dictionary<Object, IResourceLocation> _locations = new();
    public Il2CppSystem.Collections.Generic.IEnumerable<Object> Keys => _locations._keys.Cast<Il2CppSystem.Collections.Generic.IEnumerable<Object>>();
    
    public CustomResourceLocator(IntPtr pointer) : base(pointer) { }
    public CustomResourceLocator() : base(ClassInjector.DerivedConstructorPointer<CustomResourceLocator>()) => ClassInjector.DerivedConstructorBody(this);

    public bool Locate(Object key, Type type, out Il2CppSystem.Collections.Generic.IList<IResourceLocation> locations)
    {
        if (_locations.TryGetValue(key, out var location))
        {
            var newList = new Il2CppSystem.Collections.Generic.List<IResourceLocation>();
            newList.Add(location);
            locations = newList.Cast<Il2CppSystem.Collections.Generic.IList<IResourceLocation>>();
            return true;
        }
        locations = null;
        return false;
    }

    public void AddLocation(Object key, IResourceLocation location)
    {
        _locations.Add(key, location);
    }
}

[RegisterTypeInIl2Cpp]
public class PreloadedAssetProvider : ResourceProviderBase
{
    public static System.Collections.Generic.Dictionary<string, Object> PreloadedAssets = new();
    
    public PreloadedAssetProvider(IntPtr pointer) : base(pointer) { }
    public PreloadedAssetProvider() : base(ClassInjector.DerivedConstructorPointer<PreloadedAssetProvider>()) => ClassInjector.DerivedConstructorBody(this);

    public override bool CanProvide(Type t, IResourceLocation location)
    {
        bool bResult = PreloadedAssets.ContainsKey(location.InternalId);
        return bResult;
    }

    public override void Release(IResourceLocation location, Object obj)
    {
    }
    
    public override void Provide(ProvideHandle provideHandle)
    {
        provideHandle.Complete(PreloadedAssets[provideHandle.Location.InternalId], true, null);
    }
}

public class RegistryBridge
{
    #region Variables
    
    public static System.Collections.Generic.Dictionary<string, Il2CppAssetBundle> AssetBundlesMap = new();

    public static CustomResourceLocator CustomResourceLocator;
    
    public static Action OnRegistryBridgeInit;

    #endregion

    #region Constructors

    public static void Init()
    {
        CustomResourceLocator = PersistentStorage.Store(new CustomResourceLocator());
        Addressables.AddResourceLocator(CustomResourceLocator.Cast<IResourceLocator>());
        Addressables.ResourceManager.m_ResourceProviders.Add(PersistentStorage.Store(new PreloadedAssetProvider()).Cast<IResourceProvider>());
        
        OnRegistryBridgeInit?.Invoke();
    }

    #endregion

    #region Methods

    public static Il2CppAssetBundle RegisterAssetBundle(string assetBundleKey, string assetBundlePath)
    {
        var stream = File.OpenRead(assetBundlePath);
        AssetBundlesMap.Add(assetBundleKey, Il2CppAssetBundleManager.LoadFromStream(stream));
        stream.Close();
        
        return AssetBundlesMap[assetBundleKey];
    }
    
    public static void RegisterSpriteFromAssetBundle(string assetBundleKey, string assetPath, 
        Action<string> resultCallback = null, Vector2? pivot = null, string keyOverride = null)
    {
        Action<Sprite> spriteCallback = (Sprite sprite) =>
        {
            var regRes = RegisterObject(sprite, keyOverride);
            resultCallback?.Invoke(regRes);
        };
        LoadSpriteFromAssetBundle(assetBundleKey, assetPath, spriteCallback, pivot);
    }
    public static void LoadSpriteFromAssetBundle(string assetBundleKey, string assetPath, Action<Sprite> resultCallback, 
        Vector2? pivot = null)
    {
        Action<Texture2D> textureCallback = (Texture2D texture) =>
        {
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                pivot ?? new Vector2(0.5f, 0.5f),
                1f
            );
            PersistentStorage.Store(sprite);
            resultCallback.Invoke(sprite);
        };
        LoadAssetFromAssetBundle<Texture2D>(assetBundleKey, assetPath, textureCallback);
    }
    
    public static void RegisterAssetFromAssetBundle<T>(string assetBundleKey, string assetPath,
        Action<string> resultCallback = null, string keyOverride = null) where T : Object
    {
        Action<T> assetCallback = (T asset) =>
        {
            var regRes = RegisterObject(asset, keyOverride);
            resultCallback?.Invoke(regRes);
        };
        LoadAssetFromAssetBundle<T>(assetBundleKey, assetPath, assetCallback);
    }
    public static void LoadAssetFromAssetBundle<T>(string assetBundleKey, string assetPath, Action<T> resultCallback) where T : Object
    {
        Il2CppAssetBundle assetBundle = AssetBundlesMap[assetBundleKey];
        
        var assetRequest = assetBundle.LoadAssetWithSubAssetsAsync(assetPath);
        assetRequest.m_completeCallback += (Il2CppSystem.Action<AsyncOperation>)((AsyncOperation operation) =>
        {
            T asset = operation.Cast<AssetBundleRequest>().allAssets.First().Cast<T>();
            PersistentStorage.Store(asset);
            resultCallback.Invoke(asset);
        });
    }
    
    public static string RegisterObject(Object obj, string keyOverride = null)
    {
        string key = keyOverride ?? Guid.NewGuid().ToString();
        
        PreloadedAssetProvider.PreloadedAssets.Add(key, obj);
        var loc = PersistentStorage.Store(new ResourceLocationBase(key, key, typeof(PreloadedAssetProvider).FullName, obj.GetIl2CppType()));
        CustomResourceLocator.AddLocation(key, loc.Cast<IResourceLocation>());
        
        return key;
    }

    #endregion
}