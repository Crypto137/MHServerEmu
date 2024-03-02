using MHServerEmu.Games.GameData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameDatabaseBrowser.Models
{
    public class PropertyDetails
    {
        public string TypeName { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public int? Index { get; set; }

        public override string ToString()
        {
            if(Name == "Data")
                return Name;

            if (Value == "Invalid")
                return $"{Name} : {Value} ({TypeName})";

            string line = Index.HasValue ? $"[{Index.Value}] " : "";
            line += string.IsNullOrWhiteSpace(Name) ? "" : $"{Name} : ";
            string value = string.IsNullOrWhiteSpace(Value) ? "" : $" ({Value})";

            return TypeName switch
            {
                "AssetTypeId" => $"{line}{GameDatabase.GetAssetTypeName((AssetTypeId)ulong.Parse(Value))}{value}",
                "CurveId" => $"{line}{GameDatabase.GetCurveName((CurveId)ulong.Parse(Value))}{value}",
                "AssetId" => $"{line}{GameDatabase.GetAssetName((AssetId)ulong.Parse(Value))}{value}",
                "PrototypeId" => $"{line}{GameDatabase.GetPrototypeName((PrototypeId)ulong.Parse(Value))}{value}",
                "Boolean" or "Int16" or "Int32" or "Int64" or "Single" => $"{line}{Value}",
                _ => $"{line}{Value} ({TypeName})",
            };
        }
    }
}
