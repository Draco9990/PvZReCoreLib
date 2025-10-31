using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppReloaded;
using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using UnityEngine;

namespace PvZReCoreLib.Util;

public class Jumpstart
{
    #region Variables



    #endregion

    #region Constructors



    #endregion

    #region Methods

    public static LevelEntryData MakeMultiplayerLevelData()
    {
        LevelEntryData levelData = AppCore.GetService<IDataService>().AdventureLevelsData[0];

        LevelEntryData toReturn = ScriptableObject.CreateInstance<LevelEntryData>();
        toReturn.m_backgroundPrefab = levelData.m_backgroundPrefab;
        toReturn.m_entryThumbnail = levelData.m_entryThumbnail;
        toReturn.m_entryType = ChallengeEntryType.None;
        toReturn.m_gameArea = GameArea.Day;
        toReturn.m_gameMode = GameMode.Adventure;
        toReturn.m_gameplayService = levelData.m_gameplayService;
        toReturn.m_initialSun = 200;
        toReturn.m_isSpecial = false;
        toReturn.m_isLimboContent = false;
        toReturn.m_levelName = "Adventure 1-1 Wink Wink";
        toReturn.m_levelNumber = 6;
        toReturn.m_order = levelData.m_order;
        toReturn.m_preloadLabels = levelData.m_preloadLabels;
        toReturn.m_reloadedGameMode = ReloadedGameMode.None;
        toReturn.m_subIndex = levelData.m_subIndex;
        toReturn.m_unlockThumbnail = levelData.m_unlockThumbnail;
        toReturn.m_zombieWaves = 20;
        toReturn.name = "DracosCustomLevelz";

        ZombieType[] zombieTypes = {
            ZombieType.Normal,
            ZombieType.Balloon,
            ZombieType.Bungee,
            ZombieType.Door,
            ZombieType.Football,
            ZombieType.Gargantuar,
        };
        levelData.m_zombieTypes = new Il2CppStructArray<ZombieType>(zombieTypes);

        return toReturn;
    }

    #endregion
}