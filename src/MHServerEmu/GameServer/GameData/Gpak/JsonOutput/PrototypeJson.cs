using MHServerEmu.GameServer.GameData.Gpak.FileFormats;

namespace MHServerEmu.GameServer.GameData.Gpak.JsonOutput
{
    public class PrototypeJson
    {
        public FileHeader Header { get; }
        public PrototypeDataJson Data { get; }

        public PrototypeJson(Prototype prototype, DataDirectory prototypeDir, DataDirectory curveDir, DataDirectory typeDir,
            Dictionary<ulong, string> prototypeFieldDict, Dictionary<ulong, string> assetDict, Dictionary<ulong, string> assetTypeDict)
        {
            Header = prototype.Header;
            Data = new(prototype.Data, prototypeDir, curveDir, typeDir, prototypeFieldDict, assetDict, assetTypeDict);
        }

        public class PrototypeDataJson
        {
            public byte Flags { get; }
            public string ParentId { get; }
            public PrototypeDataEntryJson[] Entries { get; }

            public PrototypeDataJson(PrototypeData data, DataDirectory prototypeDir, DataDirectory curveDir, DataDirectory typeDir,
                Dictionary<ulong, string> prototypeFieldDict, Dictionary<ulong, string> assetDict, Dictionary<ulong, string> assetTypeDict)
            {
                Flags = data.Flags;
                ParentId = (data.ParentId != 0) ? prototypeDir.IdDict[data.ParentId].FilePath : "";

                if (data.Entries != null)
                {
                    Entries = new PrototypeDataEntryJson[data.Entries.Length];
                    for (int i = 0; i < Entries.Length; i++)
                        Entries[i] = new(data.Entries[i], prototypeDir, curveDir, typeDir, prototypeFieldDict, assetDict, assetTypeDict);
                }
            }

            public class PrototypeDataEntryJson
            {
                public string Id { get; }
                public byte Field1 { get; }
                public PrototypeDataEntryElementJson[] Elements { get; }
                public PrototypeDataEntryListElementJson[] ListElements { get; }

                public PrototypeDataEntryJson(PrototypeDataEntry entry, DataDirectory prototypeDir, DataDirectory curveDir, DataDirectory typeDir,
                    Dictionary<ulong, string> prototypeFieldDict, Dictionary<ulong, string> assetDict, Dictionary<ulong, string> assetTypeDict)
                {
                    Id = (entry.Id != 0) ? prototypeDir.IdDict[entry.Id].FilePath : "";
                    Field1 = entry.Field1;

                    Elements = new PrototypeDataEntryElementJson[entry.Elements.Length];
                    for (int i = 0; i < Elements.Length; i++)
                        Elements[i] = new(entry.Elements[i], prototypeDir, curveDir, typeDir, prototypeFieldDict, assetDict, assetTypeDict);

                    ListElements = new PrototypeDataEntryListElementJson[entry.ListElements.Length];
                    for (int i = 0; i < ListElements.Length; i++)
                        ListElements[i] = new(entry.ListElements[i], prototypeDir, curveDir, typeDir, prototypeFieldDict, assetDict, assetTypeDict);
                }

                public class PrototypeDataEntryElementJson
                {
                    public string Id { get; }
                    public char Type { get; }
                    public object Value { get; }

                    public PrototypeDataEntryElementJson(PrototypeDataEntryElement element, DataDirectory prototypeDir, DataDirectory curveDir, DataDirectory typeDir,
                        Dictionary<ulong, string> prototypeFieldDict, Dictionary<ulong, string> assetDict, Dictionary<ulong, string> assetTypeDict)
                    {
                        Id = (element.Id != 0) ? prototypeFieldDict[element.Id] : "";
                        Type = (char)element.Type;

                        switch (Type)
                        {
                            case 'A':
                                Value = $"{assetDict[(ulong)element.Value]} ({assetTypeDict[(ulong)element.Value]})";
                                break;
                            case 'C':
                                Value = curveDir.IdDict[(ulong)element.Value].FilePath;
                                break;
                            case 'P':
                                Value = ((ulong)element.Value != 0) ? prototypeDir.IdDict[(ulong)element.Value].FilePath : "";
                                break;
                            case 'R':
                                Value = new PrototypeDataJson((PrototypeData)element.Value, prototypeDir, curveDir, typeDir, prototypeFieldDict, assetDict, assetTypeDict);
                                break;
                            case 'T':
                                Value = typeDir.IdDict[(ulong)element.Value].FilePath;
                                break;
                            default:
                                Value = element.Value;
                                break;
                        }
                    }
                }

                public class PrototypeDataEntryListElementJson
                {
                    public string Id { get; }
                    public char Type { get; }
                    public object[] Values { get; }

                    public PrototypeDataEntryListElementJson(PrototypeDataEntryListElement element, DataDirectory prototypeDir, DataDirectory curveDir, DataDirectory typeDir,
                        Dictionary<ulong, string> prototypeFieldDict, Dictionary<ulong, string> assetDict, Dictionary<ulong, string> assetTypeDict)
                    {
                        Id = prototypeFieldDict[element.Id];
                        Type = (char)element.Type;

                        Values = new object[element.Values.Length];
                        for (int i = 0; i < Values.Length; i++)
                        {
                            switch (Type)
                            {
                                case 'A':
                                    Values[i] = $"{assetDict[(ulong)element.Values[i]]} ({assetTypeDict[(ulong)element.Values[i]]}";
                                    break;
                                case 'C':
                                    Values[i] = curveDir.IdDict[(ulong)element.Values[i]].FilePath;
                                    break;
                                case 'P':
                                    Values[i] = ((ulong)element.Values[i] != 0) ? prototypeDir.IdDict[(ulong)element.Values[i]].FilePath : "";
                                    break;
                                case 'R':
                                    Values[i] = new PrototypeDataJson((PrototypeData)element.Values[i], prototypeDir, curveDir, typeDir, prototypeFieldDict, assetDict, assetTypeDict);
                                    break;
                                case 'T':
                                    Values[i] = typeDir.IdDict[(ulong)element.Values[i]].FilePath;
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