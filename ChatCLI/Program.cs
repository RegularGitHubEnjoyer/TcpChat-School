using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatCLI
{
    internal class Program
    {
        static void Main(string[] args)
        {
            CLI chat = new CLI();

            chat.WelcomeMessage = "Type /help to list all available commands or /help [command] to display help page for this command";

            chat.AddCommand("quit", "exits program", (cmdArgs, invoker) =>
            {
                invoker.Stop();
            });

            chat.AddCommand("help", "lists all available commands or displays help page", (cmdArgs, invoker) =>
            {
                if (cmdArgs != null)
                {
                    string helpPage = invoker.GetHelpPage(cmdArgs[0]);
                    if (helpPage != null) invoker.Log(helpPage);
                    else invoker.Log(LogType.Warning, $"Unknown command: {cmdArgs[0]}");
                }
                else
                {
                    invoker.Log(invoker.CommandList);
                } 
            });

            chat.AddCommand("clear", "clears command window", (cmdArgs, invoker) => invoker.ClearLogWindow());

            chat.Init();
        }
    }
}
