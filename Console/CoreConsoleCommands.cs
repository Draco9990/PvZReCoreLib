using UnityEngine;

namespace PvZReCoreLib.Console;

public class HelpCommand : ConsoleCommand
{
    #region Variables



    #endregion

    #region Constructors



    #endregion

    #region Methods

    public override void Execute(string[] paramz)
    {
        System.Console.WriteLine("Available Commands:");
        foreach (var command in DebugConsole.Commands)
        {
            var key = command.Key;
            var cmd = command.Value;
            if (cmd.IsHidden())
            {
                continue;
            }
            
            System.Console.WriteLine($"* {key}: {cmd.GetHint()}");
        }
    }

    public override bool IsHidden()
    {
        return true;
    }

    #endregion
}