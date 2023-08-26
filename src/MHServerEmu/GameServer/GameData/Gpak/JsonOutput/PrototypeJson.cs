using MHServerEmu.GameServer.GameData.Gpak.FileFormats;

namespace MHServerEmu.GameServer.GameData.Gpak.JsonOutput
{
    public class PrototypeJson
    {
        public uint Header { get; }
        public PrototypeDataJson Data { get; }

        public PrototypeJson(Prototype prototype)
        {
            Header = prototype.Header;
            Data = new(prototype.Data);
        }

        public class PrototypeDataJson
        {
            public byte Flags { get; }
            public ulong Id { get; }
            public PrototypeDataEntryJson[] Entries { get; }

            public PrototypeDataJson(PrototypeData data)
            {
                Flags = data.Flags;
                Id = data.Id;

                if (data.Entries != null)
                {
                    Entries = new PrototypeDataEntryJson[data.Entries.Length];
                    for (int i = 0; i < Entries.Length; i++)
                        Entries[i] = new(data.Entries[i]);
                }
            }

            public class PrototypeDataEntryJson
            {
                public ulong Id { get; }
                public byte Field1 { get; }
                public PrototypeDataEntryElementJson[] Elements { get; }
                public PrototypeDataEntryListElementJson[] ListElements { get; }

                public PrototypeDataEntryJson(PrototypeDataEntry entry)
                {
                    Id = entry.Id;
                    Field1 = entry.Field1;

                    Elements = new PrototypeDataEntryElementJson[entry.Elements.Length];
                    for (int i = 0; i < Elements.Length; i++)
                        Elements[i] = new(entry.Elements[i]);

                    ListElements = new PrototypeDataEntryListElementJson[entry.ListElements.Length];
                    for (int i = 0; i < ListElements.Length; i++)
                        ListElements[i] = new(entry.ListElements[i]);
                }

                public class PrototypeDataEntryElementJson
                {
                    public ulong Id { get; }
                    public char Type { get; }
                    public object Value { get; }

                    public PrototypeDataEntryElementJson(PrototypeDataEntryElement element)
                    {
                        Id = element.Id;
                        Type = (char)element.Type;
                        Value = element.Value;
                    }
                }

                public class PrototypeDataEntryListElementJson
                {
                    public ulong Id { get; }
                    public char Type { get; }
                    public object[] Values { get; }

                    public PrototypeDataEntryListElementJson(PrototypeDataEntryListElement listElement)
                    {
                        Id = listElement.Id;
                        Type = (char)listElement.Type;

                        Values = new object[listElement.Values.Length]; ;
                        for (int i = 0; i < Values.Length; i++)
                            Values[i] = listElement.Values[i];
                    }
                }
            }
        }
    }
}