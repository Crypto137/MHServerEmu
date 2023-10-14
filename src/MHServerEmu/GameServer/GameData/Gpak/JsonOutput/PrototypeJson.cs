using MHServerEmu.GameServer.GameData.Gpak.FileFormats;

namespace MHServerEmu.GameServer.GameData.Gpak.JsonOutput
{
    public class PrototypeFileJson
    {
        public CalligraphyHeader Header { get; }
        public PrototypeJson Prototype { get; }

        public PrototypeFileJson(PrototypeFile prototypeFile, DataDirectory prototypeDir)
        {
            Header = prototypeFile.Header;
            Prototype = new(prototypeFile.Prototype, prototypeDir);
        }

        public class PrototypeJson
        {
            public byte Flags { get; }
            public string ParentId { get; }
            public PrototypeEntryJson[] Entries { get; }

            public PrototypeJson(Prototype prototype, DataDirectory prototypeDir)
            {
                Flags = prototype.Flags;
                ParentId = (prototype.ParentId != 0) ? prototypeDir.IdDict[prototype.ParentId].FilePath : "";

                if (prototype.Entries != null)
                {
                    Entries = new PrototypeEntryJson[prototype.Entries.Length];
                    for (int i = 0; i < Entries.Length; i++)
                        Entries[i] = new(prototype.Entries[i], prototypeDir);
                }
            }

            public class PrototypeEntryJson
            {
                public string Id { get; }
                public byte ByteField { get; }
                public PrototypeEntryElementJson[] Elements { get; }
                public PrototypeEntryListElementJson[] ListElements { get; }

                public PrototypeEntryJson(PrototypeEntry entry, DataDirectory prototypeDir)
                {
                    Id = (entry.Id != 0) ? prototypeDir.IdDict[entry.Id].FilePath : "";
                    ByteField = entry.ByteField;

                    Elements = new PrototypeEntryElementJson[entry.Elements.Length];
                    for (int i = 0; i < Elements.Length; i++)
                        Elements[i] = new(entry.Elements[i], prototypeDir);

                    ListElements = new PrototypeEntryListElementJson[entry.ListElements.Length];
                    for (int i = 0; i < ListElements.Length; i++)
                        ListElements[i] = new(entry.ListElements[i], prototypeDir);
                }

                public class PrototypeEntryElementJson
                {
                    public string Id { get; }
                    public char Type { get; }
                    public object Value { get; }

                    public PrototypeEntryElementJson(PrototypeEntryElement element, DataDirectory prototypeDir)
                    {
                        Id = GameDatabase.GetBlueprintFieldName(element.Id);
                        Type = (char)element.Type;

                        switch (Type)
                        {
                            case 'A':
                                ulong assetId = (ulong)element.Value;
                                string assetName = GameDatabase.GetAssetName(assetId);
                                string assetTypeName = GameDatabase.GetAssetTypeName(GameDatabase.Calligraphy.AssetDirectory.GetAssetTypeId(assetId));
                                Value = $"{assetName} ({assetTypeName})";
                                break;
                            case 'C':
                                Value = GameDatabase.GetCurveName((ulong)element.Value);
                                break;
                            case 'P':
                                Value = ((ulong)element.Value != 0) ? prototypeDir.IdDict[(ulong)element.Value].FilePath : "";
                                break;
                            case 'R':
                                Value = new PrototypeJson((Prototype)element.Value, prototypeDir);
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

                    public PrototypeEntryListElementJson(PrototypeEntryListElement element, DataDirectory prototypeDir)
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
                                    string assetTypeName = GameDatabase.GetAssetTypeName(GameDatabase.Calligraphy.AssetDirectory.GetAssetTypeId(assetId));
                                    Values[i] = $"{assetName} ({assetTypeName})";
                                    break;
                                case 'C':
                                    Values[i] = GameDatabase.GetCurveName((ulong)element.Values[i]);
                                    break;
                                case 'P':
                                    Values[i] = ((ulong)element.Values[i] != 0) ? prototypeDir.IdDict[(ulong)element.Values[i]].FilePath : "";
                                    break;
                                case 'R':
                                    Values[i] = new PrototypeJson((Prototype)element.Values[i], prototypeDir);
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