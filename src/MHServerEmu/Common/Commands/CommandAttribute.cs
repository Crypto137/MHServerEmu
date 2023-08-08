using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu.Common.Commands
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandGroupAttribute : Attribute
    {
        public string Name { get; private set; }
        public string Help { get; private set; }

        public CommandGroupAttribute(string name, string help)
        {
            Name = name.ToLower();
            Help = help;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public string Name { get; private set; }
        public string Help { get; private set; }

        public CommandAttribute(string name, string help)
        {
            Name = name;
            Help = help;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class DefaultCommand : CommandAttribute
    {
        public DefaultCommand() : base("", "")
        {
        }
    }
}
