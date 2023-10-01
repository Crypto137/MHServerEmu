using MHServerEmu.GameServer.GameData.Gpak.FileFormats;

namespace MHServerEmu.GameServer.GameData.Gpak.JsonOutput
{
    public class PrototypeFileJson
    {
        public FileHeader Header { get; }
        public PrototypeJson Prototype { get; }

        public PrototypeFileJson(PrototypeFile prototypeFile, DataDirectory prototypeDir, DataDirectory curveDir, DataDirectory typeDir,
            Dictionary<ulong, string> prototypeFieldDict, Dictionary<ulong, string> assetDict, Dictionary<ulong, string> assetTypeDict)
        {
            Header = prototypeFile.Header;
            Prototype = new(prototypeFile.Prototype, prototypeDir, curveDir, typeDir, prototypeFieldDict, assetDict, assetTypeDict);
        }

        public class PrototypeJson
        {
            public byte Flags { get; }
            public string ParentId { get; }
            public PrototypeEntryJson[] Entries { get; }

            public PrototypeJson(Prototype prototype, DataDirectory prototypeDir, DataDirectory curveDir, DataDirectory typeDir,
                Dictionary<ulong, string> prototypeFieldDict, Dictionary<ulong, string> assetDict, Dictionary<ulong, string> assetTypeDict)
            {
                Flags = prototype.Flags;
                ParentId = (prototype.ParentId != 0) ? prototypeDir.IdDict[prototype.ParentId].FilePath : "";

                if (prototype.Entries != null)
                {
                    Entries = new PrototypeEntryJson[prototype.Entries.Length];
                    for (int i = 0; i < Entries.Length; i++)
                        Entries[i] = new(prototype.Entries[i], prototypeDir, curveDir, typeDir, prototypeFieldDict, assetDict, assetTypeDict);
                }
            }

            public class PrototypeEntryJson
            {
                public string Id { get; }
                public byte Field1 { get; }
                public PrototypeEntryElementJson[] Elements { get; }
                public PrototypeEntryListElementJson[] ListElements { get; }

                public PrototypeEntryJson(PrototypeEntry entry, DataDirectory prototypeDir, DataDirectory curveDir, DataDirectory typeDir,
                    Dictionary<ulong, string> prototypeFieldDict, Dictionary<ulong, string> assetDict, Dictionary<ulong, string> assetTypeDict)
                {
                    Id = (entry.Id != 0) ? prototypeDir.IdDict[entry.Id].FilePath : "";
                    Field1 = entry.Field1;

                    Elements = new PrototypeEntryElementJson[entry.Elements.Length];
                    for (int i = 0; i < Elements.Length; i++)
                        Elements[i] = new(entry.Elements[i], prototypeDir, curveDir, typeDir, prototypeFieldDict, assetDict, assetTypeDict);

                    ListElements = new PrototypeEntryListElementJson[entry.ListElements.Length];
                    for (int i = 0; i < ListElements.Length; i++)
                        ListElements[i] = new(entry.ListElements[i], prototypeDir, curveDir, typeDir, prototypeFieldDict, assetDict, assetTypeDict);
                }

                public class PrototypeEntryElementJson
                {
                    public string Id { get; }
                    public char Type { get; }
                    public object Value { get; }

                    public PrototypeEntryElementJson(PrototypeEntryElement element, DataDirectory prototypeDir, DataDirectory curveDir, DataDirectory typeDir,
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
                                Value = new PrototypeJson((Prototype)element.Value, prototypeDir, curveDir, typeDir, prototypeFieldDict, assetDict, assetTypeDict);
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

                public class PrototypeEntryListElementJson
                {
                    public string Id { get; }
                    public char Type { get; }
                    public object[] Values { get; }

                    public PrototypeEntryListElementJson(PrototypeEntryListElement element, DataDirectory prototypeDir, DataDirectory curveDir, DataDirectory typeDir,
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
                                    Values[i] = new PrototypeJson((Prototype)element.Values[i], prototypeDir, curveDir, typeDir, prototypeFieldDict, assetDict, assetTypeDict);
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