using MHServerEmu.GameServer.GameData.Gpak.FileFormats;

namespace MHServerEmu.GameServer.GameData.Gpak.JsonOutput
{
    public class BlueprintJson
    {
        public uint Header { get; }
        public string ClassName { get; }
        public string PrototypeId { get; }
        public BlueprintReferenceJson[] References1 { get; }
        public BlueprintReferenceJson[] References2 { get; }
        public BlueprintFieldJson[] Fields { get; }

        public BlueprintJson(Blueprint blueprint, Dictionary<ulong, string> prototypeDict, Dictionary<ulong, string> curveDict)
        {
            Header = blueprint.Header;
            ClassName = blueprint.ClassName;
            PrototypeId = prototypeDict[blueprint.PrototypeId];

            References1 = new BlueprintReferenceJson[blueprint.References1.Length];
            for (int i = 0; i < References1.Length; i++)
                References1[i] = new(blueprint.References1[i], prototypeDict);

            References2 = new BlueprintReferenceJson[blueprint.References2.Length];
            for (int i = 0; i < References2.Length; i++)
                References2[i] = new(blueprint.References2[i], prototypeDict);

            Fields = new BlueprintFieldJson[blueprint.Fields.Length];
            for (int i = 0; i < Fields.Length; i++)
                Fields[i] = new(blueprint.Fields[i], prototypeDict, curveDict);
        }

        public class BlueprintReferenceJson
        {
            public string Id { get; }
            public byte Field1 { get; }

            public BlueprintReferenceJson(BlueprintReference reference, Dictionary<ulong, string> prototypeDict)
            {
                Id = prototypeDict[reference.Id];
                Field1 = reference.Field1;
            }
        }

        public class BlueprintFieldJson
        {
            public ulong Id { get; }
            public string Name { get; }
            public char ValueType { get; }
            public char ContainerType { get; }
            public string TypeSpecificId { get; }

            public BlueprintFieldJson(BlueprintField field, Dictionary<ulong, string> prototypeDict, Dictionary<ulong, string> curveDict)
            {
                Id = field.Id;
                Name = field.Name;
                ValueType = (char)field.ValueType;
                ContainerType = (char)field.ContainerType;

                switch (ValueType)
                {
                    case 'A':
                        if (ContainerType == 'L')
                            TypeSpecificId = $"unknown id {field.TypeSpecificId} (AL)";
                        else if (ContainerType == 'S')
                            TypeSpecificId = $"unknown id {field.TypeSpecificId} (AS)";

                        break;

                    case 'C':
                        TypeSpecificId = curveDict[field.TypeSpecificId];
                        break;

                    case 'P':
                        TypeSpecificId = prototypeDict[field.TypeSpecificId];
                        break;

                    case 'R':
                        if (ContainerType == 'L')
                            TypeSpecificId = prototypeDict[field.TypeSpecificId];
                        else if (ContainerType == 'S')
                            TypeSpecificId = $"unknown id {field.TypeSpecificId} (RS)";
                        break;

                    default:
                        // other types don't have ids
                        break;
                }
            }
        }
    }
}