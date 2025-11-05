using HarmonyLib;
using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using PvZReCoreLib.Content.Plants;
using PvZReCoreLib.Content.Projectiles;
using PvZReCoreLib.Util;
using UnityEngine;

namespace PvZReCoreLib.Content;

public class CustomContentRegistry
{
    #region Variables
    
    // Plants and Zombies
    private static HashSet<SeedType> _customSeedTypes = new ();
    private static Dictionary<SeedType, PlantDefinition> _customPlantDefinitions = new ();
    private static Dictionary<SeedType, ZombieDefinition> _customZombieDefinitions = new ();
    
    private static Dictionary<SeedType, AlmanacEntryData> _customAlmanacEntries = new ();
    
    public static PlantDefinition EmptyPlantDefinition;
    public static ZombieDefinition EmptyZombieDefinition;
    
    // Projectiles
    private static Dictionary<ProjectileType, ProjectileDefinition> _customProjectileDefinitions = new ();
    
    // Common
    public static Action OnCustomContentRegistryInit;
    
    #endregion

    #region Methods

    public static void Init()
    {
        EmptyPlantDefinition = PersistentStorage.Store(ScriptableObject.CreateInstance<PlantDefinition>());
        
        OnCustomContentRegistryInit?.Invoke();
    }

    #region Seed Type

    public static bool IsValidCustomSeedType(SeedType theSeedType)
    {
        return _customSeedTypes.Contains(theSeedType);
    }
    
    public static HashSet<SeedType> GetAllCustomSeedTypes()
    {
        return _customSeedTypes;
    }
    
    public static int GetHighestCustomSeedTypeValue()
    {
        if (!_customSeedTypes.Any()) return -1;
        return (int)_customSeedTypes.Last();
    }
    
    public static SeedType RequestFreeSeedType()
    {
        for (int i = 1000; i < int.MaxValue; i++)
        {
            SeedType potentialSeedType = (SeedType)i;
            if (!_customPlantDefinitions.ContainsKey(potentialSeedType))
            {
                return (SeedType)i;
            }
        }
        
        throw new Exception("No free SeedType values available.");
    }

    #endregion

    #region Custom Plants

    public static SeedType RegisterCustomPlant(CustomPlantDefinition plantData)
    {
        _customSeedTypes.Add(plantData.SeedType);
        
        _customPlantDefinitions[plantData.SeedType] = plantData;

        var almanacData = PersistentStorage.Store(ScriptableObject.CreateInstance<CustomAlmanacEntryData>());
        almanacData.LoadFrom(plantData);
        _customAlmanacEntries[plantData.SeedType] = almanacData;
        
        return plantData.SeedType;
    }

    public static bool IsValidCustomPlantType(SeedType seedType)
    {
        return _customPlantDefinitions.ContainsKey(seedType);
    }
    
    public static HashSet<SeedType> GetAllCustomPlantTypes()
    {
        return new HashSet<SeedType>(_customPlantDefinitions.Keys);
    }
    
    public static int GetHighestCustomPlantTypeValue()
    {
        if (!_customPlantDefinitions.Any()) return -1;
        return (int)_customPlantDefinitions.Keys.Last();
    }
    
    public static PlantDefinition GetCustomPlantDefinition(SeedType seedType)
    {
        if (_customPlantDefinitions.ContainsKey(seedType))
        {
            return _customPlantDefinitions[seedType];
        }

        return null;
    }

    #endregion

    #region Custom Zombies

    

    #endregion
    
    public static AlmanacEntryData GetCustomAlmanacEntry(SeedType seedType)
    {
        if (_customAlmanacEntries.ContainsKey(seedType))
        {
            return _customAlmanacEntries[seedType];
        }

        return null;
    }

    #region Custom Projectiles

    public static ProjectileType RegisterCustomProjectile(CustomProjectileDefinition projectileDefinition)
    {
        _customProjectileDefinitions[projectileDefinition.m_projectileType] = projectileDefinition;
        
        return projectileDefinition.m_projectileType;
    }
    
    public static bool IsValidCustomProjectileType(ProjectileType projectileType)
    {
        return _customProjectileDefinitions.ContainsKey(projectileType);
    }
    
    public static ProjectileDefinition GetCustomProjectileDefinition(ProjectileType projectileType)
    {
        if (_customProjectileDefinitions.ContainsKey(projectileType))
        {
            return _customProjectileDefinitions[projectileType];
        }

        return null;
    }
    
    public static ProjectileType RequestFreeProjectileType()
    {
        for (int i = 1000; i < int.MaxValue; i++)
        {
            ProjectileType potentialType = (ProjectileType)i;
            if (!_customProjectileDefinitions.ContainsKey(potentialType))
            {
                return (ProjectileType)i;
            }
        }
        
        throw new Exception("No free ProjectileType values available.");
    }

    #endregion
    
    #endregion
}

[HarmonyPatch(typeof(DataService), nameof(DataService.GetPlantDefinition))]
public class GetPlantDefinitionPatch
{
    public static bool Prefix(SeedType seedType, ref PlantDefinition __result)
    {
        if (CustomContentRegistry.IsValidCustomPlantType(seedType))
        {
            __result = CustomContentRegistry.GetCustomPlantDefinition(seedType);
            return false;
        }

        if (seedType >= SeedType.NumSeedsInChooser)
        {
            __result = CustomContentRegistry.EmptyPlantDefinition;
            return false;
        }
            
        return true;
    }
}

// Mark all custom plants as available
[HarmonyPatch(typeof(UserService), nameof(UserService.HasSeedType))]
public class UserService_HasSeedTypePatch
{
    public static bool Prefix(SeedType theSeedType, ref bool __result)
    {
        if (CustomContentRegistry.IsValidCustomSeedType(theSeedType))
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

[HarmonyPatch(typeof(UserService), nameof(UserService.IsLocked), new[] { typeof(AlmanacEntryData) })]
public class UserService_IsLockedPatch
{
    public static bool Prefix(AlmanacEntryData almanacEntryData, ref bool __result)
    {
        if (almanacEntryData is CustomAlmanacEntryData)
        {
            __result = false;
            return false;
        }

        return true;
    }
}