using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.GameData.Gpak.FileFormats
{
    public class Blueprint
    {
        public uint Header { get; }                         // BPT + 0x0b
        public string ClassName { get; }                    // name of the C++ class that handles prototypes that use this blueprint
        public ulong PrototypeId { get; }                   // .defaults prototype file id
        public BlueprintReference[] References1 { get; }
        public BlueprintReference[] References2 { get; }
        public Dictionary<ulong, BlueprintField> FieldDict { get; }     // field definitions for prototypes that use this blueprint             

        public Blueprint(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = reader.ReadUInt32();
                ClassName = reader.ReadFixedString16();
                PrototypeId = reader.ReadUInt64();

                References1 = new BlueprintReference[reader.ReadUInt16()];
                for (int i = 0; i < References1.Length; i++)
                    References1[i] = new(reader);

                References2 = new BlueprintReference[reader.ReadInt16()];
                for (int i = 0; i < References2.Length; i++)
                    References2[i] = new(reader);

                ushort fieldCount = reader.ReadUInt16();
                FieldDict = new(fieldCount);
                for (int i = 0; i < fieldCount; i++)
                    FieldDict.Add(reader.ReadUInt64(), new(reader));
            }
        }
    }

    public class BlueprintReference
    {
        public ulong Id { get; }
        public byte Field1 { get; }

        public BlueprintReference(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Field1 = reader.ReadByte();
        }
    }

    public class BlueprintField
    {
        public string Name { get; }
        public CalligraphyValueType ValueType { get; }
        public CalligraphyContainerType ContainerType { get; }
        public ulong Subtype { get; }

        public BlueprintField(BinaryReader reader)
        {
            Name = reader.ReadFixedString16();
            ValueType = (CalligraphyValueType)reader.ReadByte();
            ContainerType = (CalligraphyContainerType)reader.ReadByte();

            switch (ValueType)
            {
                // Only these types have subtypes
                case CalligraphyValueType.A:
                case CalligraphyValueType.C:
                case CalligraphyValueType.P:
                case CalligraphyValueType.R:
                    Subtype = reader.ReadUInt64();
                    break;
            }
        }
    }
}
