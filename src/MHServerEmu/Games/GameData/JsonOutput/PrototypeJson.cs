using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData.JsonOutput
{
    public class PrototypeFileJson
    {
        public CalligraphyHeader Header { get; }
        public PrototypeJson Prototype { get; }

        public PrototypeFileJson(PrototypeFile prototypeFile)
        {
            Header = prototypeFile.Header;
            Prototype = new(prototypeFile.Prototype);
        }

        public class PrototypeJson
        {
            public PrototypeDataHeader Header { get; }
            public PrototypeFieldGroupJson[] FieldGroups { get; }

            public PrototypeJson(Prototype prototype)
            {
                Header = prototype.Header;

                if (prototype.FieldGroups != null)
                {
                    FieldGroups = new PrototypeFieldGroupJson[prototype.FieldGroups.Length];
                    for (int i = 0; i < FieldGroups.Length; i++)
                        FieldGroups[i] = new(prototype.FieldGroups[i]);
                }
            }

            public class PrototypeFieldGroupJson
            {
                public string DeclaringBlueprintId { get; }
                public byte BlueprintCopyNumber { get; }
                public PrototypeSimpleFieldJson[] SimpleFields { get; }
                public PrototypeListFieldJson[] ListFields { get; }

                public PrototypeFieldGroupJson(PrototypeFieldGroup entry)
                {
                    DeclaringBlueprintId = GameDatabase.GetPrototypeName(entry.DeclaringBlueprintId);
                    BlueprintCopyNumber = entry.BlueprintCopyNumber;

                    SimpleFields = new PrototypeSimpleFieldJson[entry.SimpleFields.Length];
                    for (int i = 0; i < SimpleFields.Length; i++)
                        SimpleFields[i] = new(entry.SimpleFields[i]);

                    ListFields = new PrototypeListFieldJson[entry.ListFields.Length];
                    for (int i = 0; i < ListFields.Length; i++)
                        ListFields[i] = new(entry.ListFields[i]);
                }

                public class PrototypeSimpleFieldJson
                {
                    public string Id { get; }
                    public char Type { get; }
                    public object Value { get; }

                    public PrototypeSimpleFieldJson(PrototypeSimpleField element)
                    {
                        Id = GameDatabase.GetBlueprintFieldName((StringId)element.Id);
                        Type = (char)element.Type;

                        switch (Type)
                        {
                            case 'A':
                                var assetId = (StringId)element.Value;
                                string assetName = GameDatabase.GetAssetName(assetId);
                                string assetTypeName = GameDatabase.GetAssetTypeName(GameDatabase.DataDirectory.AssetDirectory.GetAssetTypeId(assetId));
                                Value = $"{assetName} ({assetTypeName})";
                                break;
                            case 'C':
                                Value = GameDatabase.GetCurveName((CurveId)element.Value);
                                break;
                            case 'P':
                                Value = GameDatabase.GetPrototypeName((PrototypeId)element.Value);
                                break;
                            case 'R':
                                Value = new PrototypeJson((Prototype)element.Value);
                                break;
                            case 'T':
                                Value = GameDatabase.GetAssetTypeName((AssetTypeId)element.Value);
                                break;
                            default:
                                Value = element.Value;
                                break;
                        }
                    }
                }

                public class PrototypeListFieldJson
                {
                    public string Id { get; }
                    public char Type { get; }
                    public object[] Values { get; }

                    public PrototypeListFieldJson(PrototypeListField element)
                    {
                        Id = GameDatabase.GetBlueprintFieldName((StringId)element.Id);
                        Type = (char)element.Type;

                        Values = new object[element.Values.Length];
                        for (int i = 0; i < Values.Length; i++)
                        {
                            switch (Type)
                            {
                                case 'A':
                                    var assetId = (StringId)element.Values[i];
                                    string assetName = GameDatabase.GetAssetName(assetId);
                                    string assetTypeName = GameDatabase.GetAssetTypeName(GameDatabase.DataDirectory.AssetDirectory.GetAssetTypeId(assetId));
                                    Values[i] = $"{assetName} ({assetTypeName})";
                                    break;
                                case 'C':
                                    Values[i] = GameDatabase.GetCurveName((CurveId)element.Values[i]);
                                    break;
                                case 'P':
                                    Values[i] = GameDatabase.GetPrototypeName((PrototypeId)element.Values[i]);
                                    break;
                                case 'R':
                                    Values[i] = new PrototypeJson((Prototype)element.Values[i]);
                                    break;
                                case 'T':
                                    Values[i] = GameDatabase.GetAssetTypeName((AssetTypeId)element.Values[i]);
                                    break;
                                default:
                                    Values[i] = element.Values[i];
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }
}