using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu.GameServer.Properties
{
    public class PropertyInfo
    {
        public string Name { get; }
        public PropertyType Type { get; }

        public PropertyInfo(string name, PropertyType type)
        {
            Name = name;
            Type = type;
        }

        public override string ToString() => $"{Name} {Type}";
    }
}
