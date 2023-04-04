using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using ChatCLI;

namespace TcpClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //create chat client object
            ChatClient client = new ChatClient();

            //set welcome message and commands
            client.WelcomeMessage = "Type /help to list all available commands or /help [command] to display help page for this command";

            client.AddCommand("help", "lists all available commands or displays help page", HelpCmd);

            client.AddCommand("quit", "exits program", (cmdArgs, invoker) => invoker.Stop());

            client.AddCommand("connect", "Connects to server", ConnectCmd, "Connects to server on specified endtpoint and with the given name\n/connect [ip] [port] [username]\te.g. /connect 127.0.0.1 11000 Guest\n/connect [ip:port] [username]\te.g. /connect 127.0.0.1:11000 Guest");

            client.AddCommand("disconnect", "Disconnects from server", (cmdArgs, invoker) => (invoker as ChatClient).RequestDisconnect());

            client.AddCommand("clear", "clears command window", (cmdArgs, invoker) => invoker.ClearLogWindow());

            client.AddCommand("pm", "send private message", SendPrivateMessageCmd, "/pm [receiver] [message]\te.g. /pm user123 private message");
            //start client CLI
            client.Init();
        }

        private static void SendPrivateMessageCmd(string[] cmdArgs, CLI invoker)
        {
            if(cmdArgs == null || cmdArgs.Length < 2)
            {
                invoker.Log(LogType.Warning, "private message requires receiver and message. Type /help pm to get more info");
            }
            else
            {
                (invoker as ChatClient)?.SendPrivateChatMessage(cmdArgs[0], string.Join(" ", cmdArgs.Skip(1)));
            }
        }

        private static void HelpCmd(string[] cmdArgs, CLI invoker)
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
        }

        private static void ConnectCmd(string[] cmdArgs, CLI invoker)
        {
            ChatClient client = invoker as ChatClient;

            if(cmdArgs == null || cmdArgs.Length < 2)
            {
                client.Log(LogType.Warning, "You have to specify endpoint and username tyo connect to a server. Type /help start to get more info.");
            }
            else if(cmdArgs.Length == 2)
            {
                if (cmdArgs[0].IndexOf(":") == -1)
                {
                    client.Log(LogType.Warning, $"Wrong format of argument: {cmdArgs[0]}. Type /help start to get more info.");
                    return;
                }

                IPAddress ip;
                int port;

                string ipString = cmdArgs[0].Substring(0, cmdArgs[0].IndexOf(":"));
                string portString = cmdArgs[0].Substring(cmdArgs[0].IndexOf(":") + 1);

                if (IPAddress.TryParse(ipString, out ip))
                {
                    if (int.TryParse(portString, out port))
                    {
                        client.Connect(new IPEndPoint(ip, port), cmdArgs[1]);
                    }
                    else
                    {
                        client.Log(LogType.Warning, $"Couldn't parse port: {portString}. Type /help connect to get more info.");
                    }
                }
                else
                {
                    client.Log(LogType.Warning, $"Couldn't parse ip: {ipString}. Type /help connect to get more info.");
                }
            }
            else if(cmdArgs.Length == 3)
            {
                IPAddress ip;
                int port;

                string ipString = cmdArgs[0];
                string portString = cmdArgs[1];

                if (IPAddress.TryParse(ipString, out ip))
                {
                    if (int.TryParse(portString, out port))
                    {
                        client.Connect(new IPEndPoint(ip, port), cmdArgs[2]);
                    }
                    else
                    {
                        client.Log(LogType.Warning, $"Couldn't parse port: {portString}. Type /help connect to get more info.");
                    }
                }
                else
                {
                    client.Log(LogType.Warning, $"Couldn't parse ip: {ipString}. Type /help connect to get more info.");
                }
            }
            else
            {
                client.Log(LogType.Warning, $"Command connect takes max 3 arguments but {cmdArgs.Length} were provided. Type /help connect to get more info.");
            }
        }
    }
}
