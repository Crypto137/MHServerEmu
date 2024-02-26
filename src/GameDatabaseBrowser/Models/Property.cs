using MHServerEmu.Games.GameData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameDatabaseBrowser.Models
{
    public class Property
    {
        public string TypeName { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }

        public override string ToString()
        {
            if(Value == "Invalid")
                return $"{Name} : {Value} ({TypeName})";

            switch (TypeName)
            {
                case "PrototypeId":
                    return $"{Name} : {GameDatabase.GetPrototypeName((PrototypeId)ulong.Parse(Value))} ({Value})";

                case "Boolean":
                    return $"{Name} : {Value}";

                default:
                    return $"{Name} : {Value} ({TypeName})";
            }
            
        }
    }
}
