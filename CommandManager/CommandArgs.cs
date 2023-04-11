using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandManager
{
    public class CommandArgs
    {
        private HashSet<string> _args;

        public CommandArgs(string[] args)
        {
            _args = new HashSet<string>();
            foreach (string arg in args)
            {
                _args.Add(arg);
            }
        }

        public static CommandArgs Parse(string argsString)
        {
            return new CommandArgs(argsString.Split(' '));
        }

        public bool HasArgument(string arg)
        {
            return _args.Contains(arg);
        }

        public int GetNumOfArgs()
        {
            return _args.Count;
        }

        public string GetArgsString()
        {
            return (_args.Aggregate("", (sum, next) => $"{sum + next} ")).Trim();
        }
    }
}
