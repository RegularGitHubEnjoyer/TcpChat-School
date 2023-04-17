using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLI
{
    public class CLIViewItem
    {
        private ConsoleColor _textColor;
        private ConsoleColor _backgroundColor;
        private string _message;

        public CLIViewItem(ConsoleColor textColor, ConsoleColor backgroundColor, string message)
        {
            _textColor = textColor;
            _backgroundColor = backgroundColor;
            _message = message;
        }   

        public void Display()
        {
            Console.ForegroundColor = _textColor;
            Console.BackgroundColor = _backgroundColor;
            Console.Write(_message);
        }
    }
}
