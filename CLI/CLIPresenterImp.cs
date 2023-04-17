using Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLI
{
    public class CLIPresenterImp : CLIPresenter
    {
        Logger.Logger _logger;
        InputHandler.InputHandler _inputHandler;
        string _prefix;

        public CLIPresenterImp(Logger.Logger logger, InputHandler.InputHandler inputHandler)
        {
            _logger = logger;
            _inputHandler = inputHandler;
            _prefix = ">";
        }

        public List<CLIViewItem> GetViewData()
        {
            List<CLIViewItem> view = new List<CLIViewItem>();
            int viewHeight = Console.BufferHeight - 1;

            Log[] logs = _logger.GetLastNLogsOrLess(viewHeight);
            foreach (Log log in logs.Reverse())
            {
                ConsoleColor textColor = _getColorAccordingToLogType(log.GetLogType());

                CLIViewItem viewItem = new CLIViewItem(textColor, ConsoleColor.Black, $"{log.GetMessageWithTimeStamp()}\n");
                view.Add(viewItem);
            }

            int remainingSpace = viewHeight - logs.Length;
            for (int i = 0; i < remainingSpace; i++)
            {
                CLIViewItem viewItem = new CLIViewItem(ConsoleColor.Black, ConsoleColor.Black, "\n");
                view.Add(viewItem);
            }

            CLIViewItem userInput = new CLIViewItem(ConsoleColor.White, ConsoleColor.Black, $"{_prefix} {_inputHandler.GetCurrentInputString()}");
            view.Add(userInput);

            return view;
        }

        public (int left, int top) GetCursorOffset()
        {
            return (_prefix.Length + 1 + _inputHandler.GetCursorOffset(), Console.BufferHeight - 1);
        }

        public void SetPrefix(string prefix)
        {
            _prefix = prefix;
        }

        private ConsoleColor _getColorAccordingToLogType(LogType logType)
        {
            ConsoleColor color;

            switch (logType)
            {
                case LogType.Message:
                    color = ConsoleColor.White;
                    break;
                case LogType.Warning:
                    color = ConsoleColor.DarkYellow;
                    break;
                case LogType.Info:
                    color = ConsoleColor.DarkCyan;
                    break;
                case LogType.Error:
                    color = ConsoleColor.DarkRed;
                    break;
                case LogType.Success:
                    color = ConsoleColor.DarkGreen;
                    break;
                default:
                    color = ConsoleColor.White;
                    break;
            }

            return color;
        }
    }
}
