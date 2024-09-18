using MHServerEmu.Games.GameData;

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
            if (Name == "Data")
                return Name;

            if (Value == "Invalid")
                return $"{Name} : {Value} ({TypeName})";

            string prefix = Index.HasValue ? $"[{Index.Value}] " : "";
            prefix += string.IsNullOrWhiteSpace(Name) ? "" : $"{Name} : ";

            string value = Value ?? "null";

            string typeName = GetNameFromValue().Contains(TypeName.Replace("[]", "")) ? "" : $" ({TypeName})";

            return TypeName switch
            {
                "AssetTypeId" => $"{prefix}{GetNameFromValue()} ({value})",
                "CurveId" => $"{prefix}{GetNameFromValue()} ({value})",
                "PrototypeId" => $"{prefix}{GetNameFromValue()} ({value})",
                "AssetId" => $"{prefix}{GetNameFromValue()}" + DisplayPrototypeIdEquivalence(),
                "PrototypeGuid" => $"{prefix}{GetNameFromValue()}" + DisplayPrototypeIdEquivalence(),
                "Boolean" or "Int16" or "Int32" or "Int64" or "Single" => $"{prefix}{value}",
                _ => $"{prefix}{GetNameFromValue()}{typeName}",
            };
        }

        private string GetNameFromValue()
        {
            string name = Value ?? "null";

            switch (TypeName)
            {
                case "AssetTypeId":
                    name = GameDatabase.GetAssetTypeName((AssetTypeId)ulong.Parse(Value));
                    break;

                case "CurveId":
                    name = GameDatabase.GetCurveName((CurveId)ulong.Parse(Value));
                    break;

                case "AssetId":
                    name = GameDatabase.GetAssetName((AssetId)ulong.Parse(Value));
                    break;

                case "PrototypeId":
                    name = GameDatabase.GetPrototypeName((PrototypeId)ulong.Parse(Value));
                    break;

                default:
                    break;
            }

            return name.Replace("MHServerEmu.Games.GameData.Prototypes.", "")
                .Replace("MHServerEmu.Games.GameData.", "");
        }

        private string DisplayPrototypeIdEquivalence()
        {
            PrototypeId prototypeId = GetPrototypeIdEquivalence();

            switch (TypeName)
            {
                case "AssetId":
                case "PrototypeGuid":
                    return $" => {prototypeId} : {GameDatabase.GetPrototypeName(prototypeId)}";

                default:
                    break;
            }

            return "";
        }

        public PrototypeId GetPrototypeIdEquivalence()
        {
            if (Value == "Invalid")
                return 0;

            return TypeName switch
            {
                "AssetId" => GameDatabase.GetDataRefByAsset((AssetId)ulong.Parse(Value)),
                "PrototypeGuid" => GameDatabase.GetDataRefByPrototypeGuid((PrototypeGuid)ulong.Parse(Value)),
                "PrototypeId" => (PrototypeId)ulong.Parse(Value),
                _ => 0,
            };
        }
    }
}
