using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InputHandler
{
    public class InputHandler
    {
        private string _inputString;
        private int _inputStringCursor;

        private Stack<string> _previousInputEntries;
        private Stack<string> _redoInputEntries;

        public event Action<string> InputProcessor;

        public InputHandler()
        {
            _previousInputEntries = new Stack<string>();
            _redoInputEntries = new Stack<string>();

            _setInputString("");
        }

        public string GetCurrentInputString()
        {
            return _inputString;
        }

        public int GetCursorOffset()
        {
            return _inputStringCursor;
        }

        public void RegisterKeyPress()
        {
            ConsoleKeyInfo keyInfo = Console.ReadKey(false);

            switch (keyInfo.Key)
            {
                case ConsoleKey.Enter:
                    string input = _inputString;
                    _updateInputEntryHistory();
                    _previousInputEntries.Push(_inputString);
                    _setInputString("");
                    InputProcessor?.Invoke(input);
                    _setInputStringCursor(0);
                    break;

                case ConsoleKey.Backspace:
                    _deleteCharacterBeforeCursorPosition();
                    _moveCursorLeft();
                    break;

                case ConsoleKey.UpArrow:
                    _setInputStringToPreviousInHistory();
                    _setInputStringCursor(_inputString.Length);
                    break;

                case ConsoleKey.DownArrow:
                    _setInputStringToNextInHistoryOrEmpty();
                    _setInputStringCursor(_inputString.Length);
                    break;

                case ConsoleKey.LeftArrow:
                    _moveCursorLeft();
                    break;

                case ConsoleKey.RightArrow:
                    _moveCursorRight();
                    break;

                default:
                    _appendCharacterToInputString(keyInfo.KeyChar);
                    _moveCursorRight();
                    break;
            }
        }

        private void _updateInputEntryHistory()
        {
            while(_redoInputEntries.Count > 0)
            {
                _previousInputEntries.Push(_redoInputEntries.Pop());
            }
        }

        private void _setInputString(string value)
        {
            _inputString = value;
        }

        private void _setInputStringCursor(int value)
        {
            _inputStringCursor = value;
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
            if(_previousInputEntries.Count > 0)
            {
                if (!string.IsNullOrEmpty(_inputString)) _redoInputEntries.Push(_inputString);
                _setInputString(_previousInputEntries.Pop());
            }            
        }

        private void _setInputStringToNextInHistoryOrEmpty()
        {
            if(!string.IsNullOrEmpty(_inputString)) _previousInputEntries.Push(_inputString);
            if (_redoInputEntries.Count > 0) _setInputString(_redoInputEntries.Pop());
            else _setInputString("");
        }

        private void _moveCursorLeft()
        {
            if (_inputStringCursor > 0)
            {
                _inputStringCursor--;
            }
        }

        private void _moveCursorRight()
        {
            if (_inputStringCursor < _inputString.Length)
            {
                _inputStringCursor++;
            }
        }

        private void _appendCharacterToInputString(char newChar)
        {
            _inputString = _inputString.Insert(_inputStringCursor, $"{newChar}");
        }
    }
}
