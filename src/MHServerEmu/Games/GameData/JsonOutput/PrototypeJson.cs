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
            public byte Flags { get; }
            public string ParentId { get; }
            public PrototypeEntryJson[] Entries { get; }

            public PrototypeJson(Prototype prototype)
            {
                Flags = prototype.Flags;
                ParentId = GameDatabase.GetPrototypeName(prototype.ParentId);

                if (prototype.Entries != null)
                {
                    Entries = new PrototypeEntryJson[prototype.Entries.Length];
                    for (int i = 0; i < Entries.Length; i++)
                        Entries[i] = new(prototype.Entries[i]);
                }
            }

            public class PrototypeEntryJson
            {
                public string Id { get; }
                public byte ByteField { get; }
                public PrototypeEntryElementJson[] Elements { get; }
                public PrototypeEntryListElementJson[] ListElements { get; }

                public PrototypeEntryJson(PrototypeEntry entry)
                {
                    Id = GameDatabase.GetPrototypeName(entry.Id);
                    ByteField = entry.ByteField;

                    Elements = new PrototypeEntryElementJson[entry.Elements.Length];
                    for (int i = 0; i < Elements.Length; i++)
                        Elements[i] = new(entry.Elements[i]);

                    ListElements = new PrototypeEntryListElementJson[entry.ListElements.Length];
                    for (int i = 0; i < ListElements.Length; i++)
                        ListElements[i] = new(entry.ListElements[i]);
                }

                public class PrototypeEntryElementJson
                {
                    public string Id { get; }
                    public char Type { get; }
                    public object Value { get; }

                    public PrototypeEntryElementJson(PrototypeEntryElement element)
                    {
                        Id = GameDatabase.GetBlueprintFieldName(element.Id);
                        Type = (char)element.Type;

                        switch (Type)
                        {
                            case 'A':
                                ulong assetId = (ulong)element.Value;
                                string assetName = GameDatabase.GetAssetName(assetId);
                                string assetTypeName = GameDatabase.GetAssetTypeName(GameDatabase.DataDirectory.AssetDirectory.GetAssetTypeId(assetId));
                                Value = $"{assetName} ({assetTypeName})";
                                break;
                            case 'C':
                                Value = GameDatabase.GetCurveName((ulong)element.Value);
                                break;
                            case 'P':
                                Value = GameDatabase.GetPrototypeName((ulong)element.Value);
                                break;
                            case 'R':
                                Value = new PrototypeJson((Prototype)element.Value);
                                break;
                            case 'T':
                                Value = GameDatabase.GetAssetTypeName((ulong)element.Value);
                                break;
                            default:
                                Value = element.Value;
                                break;
                        }
                    }
                }

                public class PrototypeEntryListElementJson
                {
                    public string Id { get; }
                    public char Type { get; }
                    public object[] Values { get; }

                    public PrototypeEntryListElementJson(PrototypeEntryListElement element)
                    {
                        Id = GameDatabase.GetBlueprintFieldName(element.Id);
                        Type = (char)element.Type;

                        Values = new object[element.Values.Length];
                        for (int i = 0; i < Values.Length; i++)
                        {
                            switch (Type)
                            {
                                case 'A':
                                    ulong assetId = (ulong)element.Values[i];
                                    string assetName = GameDatabase.GetAssetName(assetId);
                                    string assetTypeName = GameDatabase.GetAssetTypeName(GameDatabase.DataDirectory.AssetDirectory.GetAssetTypeId(assetId));
                                    Values[i] = $"{assetName} ({assetTypeName})";
                                    break;
                                case 'C':
                                    Values[i] = GameDatabase.GetCurveName((ulong)element.Values[i]);
                                    break;
                                case 'P':
                                    Values[i] = GameDatabase.GetPrototypeName((ulong)element.Values[i]);
                                    break;
                                case 'R':
                                    Values[i] = new PrototypeJson((Prototype)element.Values[i]);
                                    break;
                                case 'T':
                                    Values[i] = GameDatabase.GetAssetTypeName((ulong)element.Values[i]);
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