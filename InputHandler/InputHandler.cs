using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InputHandler
{
    public class InputHandler
    {
        private List<string> _inputHistory;
        private string _inputString;
        private int _historyCursor;
        private int _inputStringCursor;

        public event Action<string> InputProcessor;

        public InputHandler()
        {
            _inputHistory = new List<string>();
            _inputString = "";
        }

        public string GetCurrentInputString()
        {
            return _inputString;
        }

        public void RegisterKeyPress()
        {
            ConsoleKeyInfo keyInfo = Console.ReadKey(false);

            switch (keyInfo.Key)
            {
                case ConsoleKey.Enter:
                    _inputHistory.Add(_inputString);
                    _resetInputData();
                    _processInput();
                    break;

                case ConsoleKey.Backspace:
                    _deleteCharacterBeforeCursorPosition();
                    break;

                case ConsoleKey.UpArrow:
                    _setInputStringToPreviousInHistory();
                    break;

                case ConsoleKey.DownArrow:
                    _setInputStringToNextInHistoryOrEmpty();
                    break;

                case ConsoleKey.LeftArrow:
                    _moveCursorLeft();
                    break;

                case ConsoleKey.RightArrow:
                    _moveCursorRight();
                    break;

                default:
                    _appendCharacterToInputString(keyInfo.KeyChar);
                    break;
            }
        }

        private void _processInput()
        {
            InputProcessor?.Invoke(_inputHistory.Last());
        }

        private void _resetInputData()
        {
            _resetInputString();
            _resetHistoryCursor();
            _resetInputStringCursor();
        }

        private void _resetHistoryCursor()
        {
            _historyCursor = 0;
        }

        private void _resetInputStringCursor()
        {
            _inputStringCursor = 0;
        }

        private void _resetInputString()
        {
            _inputString = "";
        }

        private void _deleteCharacterBeforeCursorPosition()
        {
            if (!string.IsNullOrEmpty(_inputString))
            {
                _inputString = _inputString.Remove(_inputStringCursor - 1, 1);
            }
        }

        private void _setInputStringToPreviousInHistory()
        {
            if (_inputHistory.Count > 0)
            {
                if (_historyCursor < _inputHistory.Count) _historyCursor++;
                _inputString = _inputHistory[_inputHistory.Count - _historyCursor];
            }
            _resetInputStringCursor();
        }

        private void _setInputStringToNextInHistoryOrEmpty()
        {
            if (_historyCursor > 0) _historyCursor--;
            if (_historyCursor == 0)
            {
                _inputString = "";
            }
            else
            {
                _inputString = _inputHistory[_inputHistory.Count - _historyCursor];
            }
            _resetInputStringCursor();
        }

        private void _moveCursorLeft()
        {
            if (_inputStringCursor > -_inputString.Length)
            {
                _inputStringCursor--;
            }
        }

        private void _moveCursorRight()
        {
            if (_inputStringCursor < 0)
            {
                _inputStringCursor++;
            }
        }

        private void _appendCharacterToInputString(char newChar)
        {
            _inputString = _inputString.Insert(_inputString.Length + _inputStringCursor, $"{newChar}");
        }
    }
}
