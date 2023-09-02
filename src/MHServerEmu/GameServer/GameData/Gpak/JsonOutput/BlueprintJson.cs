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
        public Dictionary<ulong, BlueprintFieldJson> FieldDict { get; }

        public BlueprintJson(Blueprint blueprint, DataDirectory prototypeDir, DataDirectory curveDir, DataDirectory typeDir)
        {
            Header = blueprint.Header;
            ClassName = blueprint.ClassName;
            PrototypeId = (blueprint.PrototypeId != 0) ? prototypeDir.IdDict[blueprint.PrototypeId].FilePath : "";

            References1 = new BlueprintReferenceJson[blueprint.References1.Length];
            for (int i = 0; i < References1.Length; i++)
                References1[i] = new(blueprint.References1[i], prototypeDir);

            References2 = new BlueprintReferenceJson[blueprint.References2.Length];
            for (int i = 0; i < References2.Length; i++)
                References2[i] = new(blueprint.References2[i], prototypeDir);

            FieldDict = new(blueprint.FieldDict.Count);
            foreach (var kvp in blueprint.FieldDict)
            {
                FieldDict.Add(kvp.Key, new(kvp.Value, prototypeDir, curveDir, typeDir));
            }
        }

        public class BlueprintReferenceJson
        {
            public string Id { get; }
            public byte Field1 { get; }

            public BlueprintReferenceJson(BlueprintReference reference, DataDirectory prototypeDir)
            {
                Id = (reference.Id != 0) ? prototypeDir.IdDict[reference.Id].FilePath : "";
                Field1 = reference.Field1;
            }
        }

        public class BlueprintFieldJson
        {
            public string Name { get; }
            public char ValueType { get; }
            public char ContainerType { get; }
            public string ExpectedValue { get; }

            public BlueprintFieldJson(BlueprintField field, DataDirectory prototypeDir, DataDirectory curveDir, DataDirectory typeDir)
            {
                Name = field.Name;
                ValueType = (char)field.ValueType;
                ContainerType = (char)field.ContainerType;

                switch (ValueType)
                {
                    case 'A':
                        ExpectedValue = typeDir.IdDict[field.ExpectedValue].FilePath;
                        break;

                    case 'C':
                        ExpectedValue = curveDir.IdDict[field.ExpectedValue].FilePath;
                        break;

                    case 'P':
                    case 'R':
                        ExpectedValue = (field.ExpectedValue != 0) ? prototypeDir.IdDict[field.ExpectedValue].FilePath : "";
                        break;

                    default:
                        // other types don't have expected values
                        break;
                }
            }
        }
    }
}