using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandManager
{
    public class CommandManager
    {
        private Dictionary<string, Command> _commands;

        public CommandManager()
        {
            _commands = new Dictionary<string, Command>();
        }

        public void AddCommand(Command command)
        {
            _commands.Add(command.GetName(), command);
        }

        public void ExecuteCommand(string commandName, CommandArgs args)
        {
            if(CommandAvailable(commandName))
            {
                _commands[commandName].Execute(args);
            }
            else
            {
                throw new Exception($"Command '{commandName}' not found!");
            }
        }

        public bool CommandAvailable(string command)
        {
            return _commands.ContainsKey(command);
        }

        public List<string> GetAvailableCommandsNamesList()
        {
            return _commands.Keys.ToList();
        }

        public List<string> GetAvaliableCommandsWithDescription()
        {
            List<string> commandsWithDescription = new List<string>();
            foreach (Command command in _commands.Values)
            {
                commandsWithDescription.Add(command.GetNameWithDescription());
            }
            return commandsWithDescription;
        }
    }
}
