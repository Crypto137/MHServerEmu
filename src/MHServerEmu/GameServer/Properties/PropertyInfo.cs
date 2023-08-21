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
        public PropertyValueType ValueType { get; }

        public PropertyInfo(string name, PropertyValueType valueType)
        {
            Name = name;
            ValueType = valueType;
        }

        public override string ToString() => $"{Name} {ValueType}";
    }
}
