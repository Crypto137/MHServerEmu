using MHServerEmu.Common.Extensions;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    public class Blueprint
    {                    
        public string RuntimeBinding { get; }                           // Name of the C++ class that handles prototypes that use this blueprint
        public PrototypeId DefaultPrototypeId { get; }                  // .defaults prototype file id
        public BlueprintReference[] Parents { get; }
        public BlueprintReference[] ContributingBlueprints { get; }
        public BlueprintMember[] Members { get; }                       // Field definitions for prototypes that use this blueprint  

        public Blueprint(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                CalligraphyHeader header = new(reader);

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

    public readonly struct BlueprintReference
    {
        public BlueprintId BlueprintId { get; }
        public byte NumOfCopies { get; }

        public BlueprintReference(BinaryReader reader)
        {
            BlueprintId = (BlueprintId)reader.ReadUInt64();
            NumOfCopies = reader.ReadByte();
        }
    }

    public class BlueprintMember
    {
        public StringId FieldId { get; }
        public string FieldName { get; }
        public CalligraphyBaseType BaseType { get; }
        public CalligraphyStructureType StructureType { get; }
        public ulong Subtype { get; }

        public BlueprintMember(BinaryReader reader)
        {
            FieldId = (StringId)reader.ReadUInt64();
            FieldName = reader.ReadFixedString16();
            BaseType = (CalligraphyBaseType)reader.ReadByte();
            StructureType = (CalligraphyStructureType)reader.ReadByte();

            switch (BaseType)
            {
                // Only these base types have subtypes
                case CalligraphyBaseType.Asset:
                case CalligraphyBaseType.Curve:
                case CalligraphyBaseType.Prototype:
                case CalligraphyBaseType.RHStruct:
                    Subtype = reader.ReadUInt64();
                    break;
            }
        }
    }
}
