using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InputHandler;
using Logger;
using CommandManager;

namespace CLI
{
    public class CLI
    {
        private InputHandler.InputHandler _inputHandler;
        private CommandManager.CommandManager _commandManager;
        private Logger.Logger _logger;

        private bool _active;

        public CLI(CommandManager.CommandManager commandManager, Logger.Logger logger)
        {
            _inputHandler = new InputHandler.InputHandler();
            _commandManager = commandManager;
            _logger = logger;
            _active = false;
        }

        public void Init()
        {
            _active = true;
            _inputHandler.InputProcessor += _processInput;
            _logger.LogHistoryChanged += _updateConsoleBuffer;

            Console.SetBufferSize(Console.WindowWidth, Console.WindowHeight);

            _cliMainLoop();
        }

        public void Stop()
        {
            _active = false;
            _inputHandler.InputProcessor -= _processInput;
            _logger.LogHistoryChanged -= _updateConsoleBuffer;
        }

        private void _cliMainLoop()
        {
            while (_active)
            {
                _inputHandler.RegisterKeyPress();
                _updateConsoleBuffer();
            }
        }

        private void _updateConsoleBuffer()
        {
            Console.Clear();
            _displayNLogs(Console.BufferHeight - 2);
            Console.SetCursorPosition(0, Console.BufferHeight - 1);
            _displayUserInput();
        }

        private void _displayNLogs(int logsAmmount)
        {
            Log[] logs = _logger.GetLastNLogsOrLess(logsAmmount);
            foreach (Log log in logs.Reverse())
            {
                _formatAccordingToLogType(log.GetLogType());
                Console.WriteLine(log.GetMessageWithTimeStamp());
            }
            _clearFormating();
        }

        private void _formatAccordingToLogType(LogType logType)
        {
            switch (logType)
            {
                case LogType.Message:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogType.Warning:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    break;
                case LogType.Info:
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    break;
                case LogType.Error:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;
                case LogType.Success:
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }
        }

        private void _clearFormating()
        {
            Console.ResetColor();
        }

        private void _displayUserInput()
        {
            string userInput = _inputHandler.GetCurrentInputString();
            Console.Write($"> {userInput}");
        }

        private void _processInput(string input)
        {
            if (input.StartsWith("/"))
            {
                _processCommand(input.Substring(1));
            }
            else
            {
                _logger.LogMessage(input);
            }
        }

        private void _processCommand(string input)
        {
            try
            {
                if(input.IndexOf(' ') > -1)
                {
                    string command = input.Substring(0, input.IndexOf(' '));
                    CommandArgs args = CommandArgs.Parse(input.Substring(input.IndexOf(' ') + 1));
                    _commandManager.ExecuteCommand(command, args);
                }
                else
                {
                    string command = input;
                    CommandArgs args = new CommandArgs(new string[0]);
                    _commandManager.ExecuteCommand(command, args);
                }
            }
            catch(Exception e)
            {
                _logger.LogError(e.Message);
            }
        }


    }
}
