using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu.GameServer.Properties
{
    public enum PropertyValueType
    {
        Boolean,
        Float,
        Integer,
        Prototype,
        Curve,
        Asset,
        UnknownType6,
        Time,   // Gazillion::Time
        UnknownType8,
        UnknownType9,
        Ulong
    }

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
