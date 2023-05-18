using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility.Commands
{
    public class CommandArgs : List<string>
    {
        public CommandArgs(params string[] args)
        {
            foreach (string arg in args)
            {
                Add(arg);
            }
        }

        public static CommandArgs Parse(string argsString)
        {
            return new CommandArgs(argsString.Split(' '));
        }

        public bool HasArgument(string arg)
        {
            return Contains(arg);
        }

        public string GetArgsString()
        {
            return (this.Aggregate("", (sum, next) => $"{sum + next} ")).Trim();
        }
    }
}
