using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandManager
{
    public class Command
    {
        private string _name;
        private string _description;
        private string _help;

        private Action<CommandArgs> _commandAction;

        public Command(string name, Action<CommandArgs> commandAction)
        {
            _name = name;
            _commandAction = commandAction;
            _description = "";
            _help = "";
        }

        public void SetCommandDescription(string description)
        {
            _description = description;
        }

        public void SetCommandHelp(string help)
        {
            _help = help;
        }

        public string GetNameWithDescription()
        {
            return $"{GetName()}\t{GetDescription()}";
        }

        public string GetName()
        {
            return _name;
        }

        public string GetDescription()
        {
            return _description;
        }

        public string GetHelpPage()
        {
            return _help;
        }

        public void Execute(CommandArgs args)
        {
            _commandAction(args);
        }
    }
}
