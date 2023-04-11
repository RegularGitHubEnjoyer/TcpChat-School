using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLI
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Logger.Logger logger = new Logger.Logger();
            CommandManager.CommandManager commandManager = new CommandManager.CommandManager();

            CLI ui = new CLI(commandManager, logger);

            CommandManager.Command quitCmd = new CommandManager.Command("quit", cmdArgs => ui.Stop());
            CommandManager.Command logErrorCmd = new CommandManager.Command("error", cmdArgs => logger.LogError(cmdArgs.GetArgsString()));
            CommandManager.Command logSuccessCmd = new CommandManager.Command("success", cmdArgs => logger.LogSuccess(cmdArgs.GetArgsString()));
            CommandManager.Command logInfoCmd = new CommandManager.Command("info", cmdArgs => logger.LogInfo(cmdArgs.GetArgsString()));

            commandManager.AddCommand(quitCmd);
            commandManager.AddCommand(logErrorCmd);
            commandManager.AddCommand(logSuccessCmd);
            commandManager.AddCommand(logInfoCmd);

            ui.Init();
        }
    }
}
