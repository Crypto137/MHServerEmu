using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.GameData.Resources;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PropSetPrototype : Prototype, IBinaryResource
    {
        public PropSetTypeListPrototype[] PropShapeLists { get; private set; }
        public string PropSetPackage { get; private set; }

        public void Deserialize(BinaryReader reader)
        {
            PropShapeLists = new PropSetTypeListPrototype[reader.ReadUInt32()];
            for (int i = 0; i < PropShapeLists.Length; i++)
                PropShapeLists[i] = new(reader);

            PropSetPackage = reader.ReadFixedString32();
        }
    }

    public class PropSetTypeListPrototype : Prototype
    {
        public PropSetTypeEntryPrototype[] PropShapeEntries { get; }
        public PrototypeGuid PropType { get; }

        public PropSetTypeListPrototype(BinaryReader reader)
        {
            var protoNameHash = (ResourcePrototypeHash)reader.ReadUInt32();

            PropShapeEntries = new PropSetTypeEntryPrototype[reader.ReadUInt32()];
            for (int i = 0; i < PropShapeEntries.Length; i++)
                PropShapeEntries[i] = new(reader);

            PropType = (PrototypeGuid)reader.ReadUInt64();
        }
    }

    public class PropSetTypeEntryPrototype : Prototype
    {
        public string NameId { get; }
        public string ResourcePackage { get; }

        public PropSetTypeEntryPrototype(BinaryReader reader)
        {
            var protoNameHash = (ResourcePrototypeHash)reader.ReadUInt32();
            
            NameId = reader.ReadFixedString32();
            ResourcePackage = reader.ReadFixedString32();
        }
    }
}
