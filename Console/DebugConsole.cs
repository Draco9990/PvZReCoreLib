using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace PvZReCoreLib.Console;

public class DebugConsole
{
    #region Variables

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();
    
    private static readonly ConcurrentQueue<Action> MainThreadActions = new();
    
    public static Dictionary<string, ConsoleCommand> Commands = new();

    #endregion

    #region Constructors



    #endregion

    #region Methods

    public static void Start()
    {
        // Open a separate console
        AllocConsole();

        // Start input thread so Unity doesn't freeze
        new Thread(() =>
        {
            while (true)
            {
                string input = System.Console.ReadLine();
                if (!string.IsNullOrEmpty(input))
                {
                    HandleCommand(input);
                }
            }
        }) { IsBackground = true }.Start();
    }
    
    public static void Update()
    {
        while (MainThreadActions.TryDequeue(out var action))
        {
            action(); // Safe to call Unity API here
        }
    }
    
    private static void HandleCommand(string command)
    {
        var paramz = command.Split(' ');
        if(paramz.Length == 0)
        {
            return;
        }
        
        if (!Commands.ContainsKey(paramz[0]))
        {
            System.Console.WriteLine($"Unknown command: {paramz[0]}");
            return;
        }
        
        Commands[command].Execute(paramz[1..]);
    }
    
    public static void RegisterCommand(string name, ConsoleCommand command)
    {
        Commands[name.ToLower()] = command;
    }

    #endregion
    
}