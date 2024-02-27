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

            return TypeName switch
            {
                "AssetTypeId" => $"{Name} : {GameDatabase.GetAssetTypeName((AssetTypeId)ulong.Parse(Value))} ({Value})",
                "CurveId" => $"{Name} : {GameDatabase.GetCurveName((CurveId)ulong.Parse(Value))} ({Value})",
                "AssetId" => $"{Name} : {GameDatabase.GetAssetName((AssetId)ulong.Parse(Value))} ({Value})",
                "PrototypeId" => $"{Name} : {GameDatabase.GetPrototypeName((PrototypeId)ulong.Parse(Value))} ({Value})",
                "Boolean" or "Int32" or "Int64" or "Single" => $"{Name} : {Value}",
                _ => $"{Name} : {Value} ({TypeName})",
            };
        }
    }
}
