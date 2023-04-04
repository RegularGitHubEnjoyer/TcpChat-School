using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

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
        private List<(LogType, string, string)> _logsHistory;
        private List<string> _inputHistory;
        private Dictionary<string, Action<string[], CLI>> _commands; //command name - command action
        private Dictionary<string, string> _commandDescriptions; //command name - command description
        private Dictionary<string, string> _commandHelp; //command name - command help

        private string _input;
        private int _inputHistoryMaxLength;

        private string _name;
        private string _prefix;
        private bool _isRunning;

        private bool _updatingBuffer = false;
        private bool _scheduleUpdate = false;

        public string CommandList
        {
            get
            {
                int l = _commands.Keys.Max(cmd => cmd.Length); //Get length of the longest command
                return _commandDescriptions.Aggregate("All available commands:\n", (result, next) => result += $"{next.Key.PadRight(l, ' ')}\t{next.Value}\n"); //Put together all commands with their description
            }
        }

        public string WelcomeMessage;
        public event Action<string> InputHandler; //Action that specifies what to do when input doesn't start with '/'

        public CLI(string name = "Chat CLI", string prefix = ">")
        {
            _logsHistory = new List<(LogType, string, string)>();
            _inputHistory = new List<string>();
            _commands = new Dictionary<string, Action<string[], CLI>>();
            _commandDescriptions = new Dictionary<string, string>();
            _commandHelp = new Dictionary<string, string>();

            _input = "";
            _name = name;
            _prefix = prefix;
            _isRunning = false;

            _inputHistoryMaxLength = 20;
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

        public void Log(string message, bool timeStamp = true)
        {
            Log(LogType.Log, message, timeStamp);
        }

        public void Log(LogType logType, string message, bool timeStamp = true)
        {
            if (_logsHistory.Count >= Console.BufferHeight - 2) //handles resizing window
                _logsHistory.RemoveRange(0, _logsHistory.Count - Console.BufferHeight + 3); //remove excess logs

            string timeStampString = timeStamp ? $"[{DateTime.Now.ToLongTimeString()}] " : ""; //if timestamp is required create one

            //add new log to log history so it can be displayed
            _logsHistory.Add((logType, timeStampString, message));
            _updateConsoleBuffer();
        }

        public void ClearLogWindow()
        {
            _logsHistory.Clear();
            _updateConsoleBuffer();
        }

        public void AddCommand(string commandName, string commandDescription, Action<string[], CLI> action, string commandHelp = "There is no help page for this command!")
        {
            if (_commands.ContainsKey(commandName)) throw new Exception($"Command: {commandName} already exists!");
            
            _commands[commandName] = action;
            _commandDescriptions.Add(commandName, commandDescription);
            _commandHelp.Add(commandName, commandHelp);
        }

        public string GetHelpPage(string command)
        {
            if (_commandHelp.ContainsKey(command))
            {
                return $"help page for {command} command:\n{_commandHelp[command]}";
            }
            else
            {
                return null;
            }
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
            int historyCursor = 0; //pointer for going back and forth in input history
            while (_isRunning)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(false); //register pressed key and DON'T display it in console

                switch (keyInfo.Key) //handle different keys
                {
                    case ConsoleKey.Enter: //process input
                        string i = _input;
                        _input = "";
                        _handleInput(i);
                        historyCursor = 0;
                        break;

                    case ConsoleKey.Backspace: //delete characters from input before cursor position
                        if(!string.IsNullOrEmpty(_input)) _input = _input.Substring(0, _input.Length - 1);
                        _updateConsoleBuffer();
                        break;

                    case ConsoleKey.UpArrow: //go back in input history
                        if(_inputHistory.Count > 0)
                        {
                            if (historyCursor < _inputHistory.Count) historyCursor++;
                            _input = _inputHistory[_inputHistory.Count - historyCursor];
                        }
                        _updateConsoleBuffer();
                        break;

                    case ConsoleKey.DownArrow: //go forward in input history
                        if (historyCursor > 0) historyCursor--;
                        if(historyCursor == 0)
                        {
                            _input = "";
                        }
                        else
                        {
                            _input = _inputHistory[_inputHistory.Count - historyCursor];
                        }
                        _updateConsoleBuffer();
                        break;

                    default: //append character to input string
                        _input += keyInfo.KeyChar;
                        _updateConsoleBuffer();
                        break;
                }
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
