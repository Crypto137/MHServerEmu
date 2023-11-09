using MHServerEmu.Common.Extensions;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    public class Blueprint
    {                    
        public string RuntimeBinding { get; }                           // Name of the C++ class that handles prototypes that use this blueprint
        public PrototypeId DefaultPrototypeId { get; }                        // .defaults prototype file id
        public BlueprintReference[] Parents { get; }
        public BlueprintReference[] ContributingBlueprints { get; }
        public BlueprintMember[] Members { get; }                       // Field definitions for prototypes that use this blueprint  

        public Blueprint(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                CalligraphyHeader header = reader.ReadCalligraphyHeader();  // BPT v11

                RuntimeBinding = reader.ReadFixedString16();
                DefaultPrototypeId = (PrototypeId)reader.ReadUInt64();

                Parents = new BlueprintReference[reader.ReadUInt16()];
                for (int i = 0; i < Parents.Length; i++)
                    Parents[i] = new(reader);

                ContributingBlueprints = new BlueprintReference[reader.ReadInt16()];
                for (int i = 0; i < ContributingBlueprints.Length; i++)
                    ContributingBlueprints[i] = new(reader);

                Members = new BlueprintMember[reader.ReadUInt16()];
                for (int i = 0; i < Members.Length; i++)
                    Members[i] = new(reader);
            }
        }

        public BlueprintMember GetMember(StringId id)
        {
            return Members.First(member => member.FieldId == id);
        }
    }

    public class BlueprintReference
    {
        public PrototypeId Id { get; }
        public byte ByteField { get; }

        public BlueprintReference(BinaryReader reader)
        {
            Id = (PrototypeId)reader.ReadUInt64();
            ByteField = reader.ReadByte();
        }
    }

    public class BlueprintMember
    {
        public StringId FieldId { get; }
        public string FieldName { get; }
        public CalligraphyValueType ValueType { get; }
        public CalligraphyContainerType ContainerType { get; }
        public ulong Subtype { get; }

        public BlueprintMember(BinaryReader reader)
        {
            FieldId = (StringId)reader.ReadUInt64();
            FieldName = reader.ReadFixedString16();
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
