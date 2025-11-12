using MelonLoader;

namespace PvZReCoreLib.Console;

public abstract class ConsoleCommand
{
    #region Variables



    #endregion

    #region Constructors



    #endregion

    #region Methods

    public abstract void Execute(string[] paramz);

    public virtual bool IsHidden()
    {
        return false;
    }
    public virtual string GetHint()
    {
        return "No hint available for this command.";
    }

    #endregion
}