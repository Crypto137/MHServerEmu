using MHServerEmu.GameServer.GameData.Gpak.FileFormats;

namespace MHServerEmu.GameServer.GameData.Gpak.JsonOutput
{
    public class BlueprintJson
    {
        public uint Header { get; }
        public string PrototypeName { get; }
        public ulong PrototypeId { get; }
        public string PrototypeIdName { get; }
        public BlueprintReferenceJson[] References1 { get; }
        public BlueprintReferenceJson[] References2 { get; }
        public BlueprintEntryJson[] Entries { get; }

        public BlueprintJson(Blueprint blueprint, Dictionary<ulong, string> prototypeDict, Dictionary<ulong, string> curveDict)
        {
            Header = blueprint.Header;
            PrototypeName = blueprint.PrototypeName;
            PrototypeId = blueprint.PrototypeId;
            PrototypeIdName = prototypeDict[PrototypeId];

            References1 = new BlueprintReferenceJson[blueprint.References1.Length];
            for (int i = 0; i < References1.Length; i++)
                References1[i] = new(blueprint.References1[i], prototypeDict);

            References2 = new BlueprintReferenceJson[blueprint.References2.Length];
            for (int i = 0; i < References2.Length; i++)
                References2[i] = new(blueprint.References2[i], prototypeDict);

            Entries = new BlueprintEntryJson[blueprint.Entries.Length];
            for (int i = 0; i < Entries.Length; i++)
                Entries[i] = new(blueprint.Entries[i], prototypeDict, curveDict);
        }

        public class BlueprintReferenceJson
        {
            public ulong Id { get; }
            public string IdName { get; }
            public byte Field1 { get; }

            public BlueprintReferenceJson(BlueprintReference blueprintReference, Dictionary<ulong, string> prototypeDict)
            {
                Id = blueprintReference.Id;
                IdName = prototypeDict[Id];
                Field1 = blueprintReference.Field1;
            }
        }

        public class BlueprintEntryJson
        {
            public ulong Id { get; }
            public string Name { get; }
            public char Type1 { get; }
            public char Type2 { get; }
            public ulong TypeSpecificId { get; }
            public string TypeSpecificIdName { get; }

            public BlueprintEntryJson(BlueprintEntry entry, Dictionary<ulong, string> prototypeDict, Dictionary<ulong, string> curveDict)
            {
                Id = entry.Id;
                Name = entry.Name;
                Type1 = (char)entry.Type1;
                Type2 = (char)entry.Type2;

                switch (Type1)
                {
                    case 'A':
                        TypeSpecificId = entry.TypeSpecificId;

                        if (Type2 == 'L')
                            TypeSpecificIdName = "unknown id (AL)";
                        else if (Type2 == 'S')
                            TypeSpecificIdName = "unknown id (AS)";

                        break;

                    case 'C':
                        TypeSpecificId = entry.TypeSpecificId;
                        TypeSpecificIdName = curveDict[TypeSpecificId];

                        break;
                    case 'P':
                        TypeSpecificId = entry.TypeSpecificId;
                        TypeSpecificIdName = prototypeDict[TypeSpecificId];

                        break;

                    case 'R':
                        TypeSpecificId = entry.TypeSpecificId;

                        if (Type2 == 'L')
                            TypeSpecificIdName = prototypeDict[TypeSpecificId];
                        else if (Type2 == 'S')
                            TypeSpecificIdName = "unknown (RS)";

                        break;

                    default:
                        // other types don't have ids
                        break;
                }
            }
        }
    }
}