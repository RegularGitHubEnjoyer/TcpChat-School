using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatCLI
{
    public enum LogType
    {
        Log = 0,
        Warning,
        Information,
        Error,
        Success
    }


    public class CLI
    {
        private string _name;
        private string _prefix;
        private bool _isRunning;

        private bool _updatingBuffer = false;
        private bool _scheduleUpdate = false;

        public string WelcomeMessage;

        public CLI(string name = "Chat CLI", string prefix = ">")
        {
            _name = name;
            _prefix = prefix;
            _isRunning = false;
        }

        public virtual void Init()
        {
            //set console title, position and default view
            Console.Title = _name;
            Console.SetWindowPosition(0, 0);
            _updateConsoleBuffer();
            if(WelcomeMessage != null) Log(LogType.Information, WelcomeMessage, false);

            //set flag to true so loop can work
            _isRunning = true;

            //create and start thread for getting and handling input
            Thread cliThread = new Thread(_cliUpdateLoop); 
            cliThread.Start();
        }

        public virtual void Stop()
        {
            //set flag to false to stop loop
            _isRunning = false;
        }

        public void ClearLogWindow()
        {
            _logsHistory.Clear();
            _updateConsoleBuffer();
        }

        private void _updateConsoleBuffer()
        {
            //quick fix for updating buffer more than once simultaneously
            if (_updatingBuffer)
            {
                _scheduleUpdate = true;
                return;
            }
            _updatingBuffer = true;

            Console.Clear();
            //if window resized fix buffer size so there is no scrollbar
            if(Console.BufferHeight != Console.WindowHeight || Console.BufferWidth != Console.WindowWidth) Console.SetBufferSize(Console.WindowWidth, Console.WindowHeight);
            foreach (var (logType, timeStamp, message) in _logsHistory.ToArray()) //display all logs
            {
                Console.Write(timeStamp);

                switch (logType) //select proper color
                {
                    case LogType.Information:
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        break;

                    case LogType.Warning:
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        break;

                    case LogType.Error:
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        break;

                    case LogType.Success:
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        break;
                }

                Console.WriteLine(message);
                Console.ResetColor();
            }

            //display input 'box'
            Console.SetCursorPosition(0, Console.BufferHeight - 1);
            Console.Write($"{_prefix} {_input}");
            Console.CursorLeft += _cursorOffset;

            //if some updates was scheduled update buffer
            _updatingBuffer = false;
            if(_scheduleUpdate)
            {
                _scheduleUpdate = false;
                _updateConsoleBuffer();
            }
        }

        private void _cliUpdateLoop()
        {
            while (_isRunning)
            {
                
            }
        }

        private void _handleInput(string input)
        {
            if (_inputHistory.Count == _inputHistoryMaxLength) _inputHistory.RemoveAt(0); //if input history is full delete the earliest entry
            _inputHistory.Add(input); //add current input to history

            if (input.StartsWith("/")) //handle command
            {
                //break down command into command name and agruments
                bool hasArguments = input.IndexOf(" ") > 0;

                string command = "";
                string[] args = null;

                if (hasArguments)
                {
                    command = input.Substring(1, input.IndexOf(" ") - 1);
                    args = input.Substring(input.IndexOf(" ") + 1).Split(' ');
                }
                else
                {
                    command = input.Substring(1);
                }

                
                if (_commands.ContainsKey(command)) 
                {
                    //perform assigned action if command exist
                    _commands[command].Invoke(args, this);
                }
                else
                {
                    Log(LogType.Warning, $"Unknown command: {command}");
                }
            }
            else //handle text input
            {
                //if input handler is specified invoke it else log input text
                if(InputHandler == null) Log(input);
                else InputHandler.Invoke(input);
            }
        }
    }
}
