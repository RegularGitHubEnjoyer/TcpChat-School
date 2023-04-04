using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using ChatCLI;

namespace TcpServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //create chat server object
            ChatServer server = new ChatServer();
            //set welcome message and commands
            server.WelcomeMessage = "Type /help to list all available commands or /help [command] to display help page for this command";

            server.AddCommand("help", "lists all available commands or displays help page", HelpCmd);

            server.AddCommand("quit", "exits program", (cmdArgs, invoker) => invoker.Stop());

            server.AddCommand("start", "starts the server", StartCmd, "Starts server on specified endpoint:\n/start [ip:port]\te.g. /start 127.0.0.1:11000\n/start [ip] [port]\te.g. /start 127.0.0.1 11000");

            server.AddCommand("stop", "stops the server", StopCmd);

            server.AddCommand("clear", "clears command window", (cmdArgs, invoker) => invoker.ClearLogWindow());
            //start server CLI
            server.Init();
        }

        private static void StopCmd(string[] cmdArgs, CLI invoker)
        {
            ChatServer server = invoker as ChatServer;

            if (server == null)
            {
                invoker.Log(LogType.Error, "Something went wrong when creating a server!");
                return;
            }

            server.StopServer();
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

        private static void StartCmd(string[] cmdArgs, CLI invoker)
        {
            ChatServer server = invoker as ChatServer;

            if(cmdArgs == null || cmdArgs.Length == 0)
            {
                server.Log(LogType.Warning, "You have to specify endpoint to start a server. Type /help start to get more info.");
            }
            else if(cmdArgs.Length == 1)
            {
                if(cmdArgs[0].IndexOf(":") == -1)
                {
                    server.Log(LogType.Warning, $"Wrong format of argument: {cmdArgs[0]}. Type /help start to get more info.");
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
                        server.Start(new IPEndPoint(ip, port));
                    }
                    else
                    {
                        server.Log(LogType.Warning, $"Couldn't parse port: {portString}. Type /help start to get more info.");
                    }
                }
                else
                {
                    server.Log(LogType.Warning, $"Couldn't parse ip: {ipString}. Type /help start to get more info.");
                }
            }
            else if(cmdArgs.Length == 2)
            {
                IPAddress ip;
                int port;

                string ipString = cmdArgs[0];
                string portString = cmdArgs[1];

                if (IPAddress.TryParse(ipString, out ip))
                {
                    if (int.TryParse(portString, out port))
                    {
                        server.Start(new IPEndPoint(ip, port));
                    }
                    else
                    {
                        server.Log(LogType.Warning, $"Couldn't parse port: {portString}. Type /help start to get more info.");
                    }
                }
                else
                {
                    server.Log(LogType.Warning, $"Couldn't parse ip: {ipString}. Type /help start to get more info.");
                }
            }
            else
            {
                server.Log(LogType.Warning, $"Command start takes max 2 arguments but {cmdArgs.Length} were provided. Type /help start to get more info.");
            }
        }
    }
}
