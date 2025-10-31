using HarmonyLib;
using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using MelonLoader;
using PvZReCoreLib.Util;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace PvZReCoreLib.Plants;

public class PlantRegistry : MonoBehaviour
{
    #region Variables
    
    private static Dictionary<SeedType, PlantDefinition> _customPlantDefinitions;
    private static PlantDefinition emptyPlantDefinition;
    
    public static Action OnPlantRegistryInit;
    
    #endregion

    #region Constructors

    #endregion

    #region Methods

    public static void Init()
    {
        _customPlantDefinitions = new Dictionary<SeedType, PlantDefinition>();

        emptyPlantDefinition = PersistentStorage.Store(ScriptableObject.CreateInstance<PlantDefinition>());
        
        OnPlantRegistryInit?.Invoke();
    }
    
    public static SeedType RegisterCustomPlantDefinition(PlantDefinition plantDefinition)
    {
        if (!_customPlantDefinitions.TryAdd(plantDefinition.SeedType, plantDefinition))
        {
            MelonLogger.Warning($"Overriding existing custom plant definition for SeedType {plantDefinition.SeedType}");
            _customPlantDefinitions[plantDefinition.SeedType] = plantDefinition;
        }
        
        return plantDefinition.SeedType;
    }
    
    public static bool IsCustomSeed(SeedType theSeedType)
    {
        return _customPlantDefinitions != null && _customPlantDefinitions.ContainsKey(theSeedType);
    }
    
    public static List<SeedType> GetAllCustomSeedTypes()
    {
        List<SeedType> customSeedTypes = new List<SeedType>();
        foreach (SeedType seedType in _customPlantDefinitions.Keys)
        {
            customSeedTypes.Add(seedType);
        }
        return customSeedTypes;
    }
    
    public static int GetHighestCustomSeedTypeValue()
    {
        int highestValue = -1;
        foreach (SeedType seedType in _customPlantDefinitions.Keys)
        {
            int intValue = (int)seedType;
            if (intValue > highestValue)
            {
                highestValue = intValue;
            }
        }
        return highestValue;
    }

    public static int RequestFreeSeedType()
    {
        for (int i = 1000; i < int.MaxValue; i++)
        {
            SeedType potentialSeedType = (SeedType)i;
            if (!_customPlantDefinitions.ContainsKey(potentialSeedType))
            {
                return i;
            }
        }
        
        throw new Exception("No free SeedType values available.");
    }

    [HarmonyPatch(typeof(DataService), nameof(DataService.GetPlantDefinition))]
    public class GetPlantDefinitionPatch
    {
        public static bool Prefix(SeedType seedType, ref PlantDefinition __result)
        {
            if (_customPlantDefinitions != null && _customPlantDefinitions.ContainsKey(seedType))
            {
                __result = _customPlantDefinitions[seedType];
                return false;
            }

            if (seedType >= SeedType.NumSeedsInChooser)
            {
                __result = emptyPlantDefinition;
                return false;
            }
            
            return true;
        }
    }

    #endregion
}

// Mark all custom plants as available
[HarmonyPatch(typeof(UserService), nameof(UserService.HasSeedType))]
public class UserService_HasSeedTypePatch
{
    public static bool Prefix(SeedType theSeedType, ref bool __result)
    {
        if (PlantRegistry.IsCustomSeed(theSeedType))
        {
            __result = true;
            return false;
        }
        
        if (theSeedType >= SeedType.NumSeedsInChooser)
        {
            __result = false;
            return false;
        }

        return true;
    }
}