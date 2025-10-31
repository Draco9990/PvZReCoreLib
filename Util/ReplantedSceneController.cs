using Il2CppTekly.TreeState.Utils;

namespace PvZReCoreLib.Util;

public class ReplantedSceneController
{
    #region Variables



    #endregion

    #region Constructors



    #endregion

    #region Methods

    public static void GoTo(string state)
    {
        ReplantedGet.TreeStateManager().HandleTransition(state);
    }

    public static void GoToLevelSelect()
    {
        GoTo("LevelSelect");
    }

    public static void GoToMainMenu()
    {
        GoTo("back");
        // TODO - check if we're in game and use the game back key
    }

    public static void GoToLevel()
    {
        GoTo("Gameplay");
    }
    
    public static List<string> GetAvailableStates()
    {
        List<string> toReturn = new List<string>();
        
        Il2CppSystem.Collections.Generic.List<string> il2cppList = new Il2CppSystem.Collections.Generic.List<string>();
        TreeStateUtils.GetValidTransitions(ReplantedGet.TreeStateManager().Active, il2cppList);
        
        foreach (var state in il2cppList)
        {
            toReturn.Add(state);
        }
        
        return toReturn;
    }

    #endregion

    
}