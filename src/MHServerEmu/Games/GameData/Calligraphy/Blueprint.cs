using MHServerEmu.Common.Extensions;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    public class Blueprint
    {
        public BlueprintId Id { get; }
        public BlueprintGuid Guid { get; }

        public HashSet<BlueprintId> FileIdHashSet { get; } = new();     // Contains ids of all blueprints related to this one in the hierarchy

        public string RuntimeBinding { get; }                           // Name of the C++ class that handles prototypes that use this blueprint
        public PrototypeId DefaultPrototypeId { get; }                  // .defaults prototype file id
        public BlueprintReference[] Parents { get; }
        public BlueprintReference[] ContributingBlueprints { get; }
        public BlueprintMember[] Members { get; }                       // Field definitions for prototypes that use this blueprint  

        public Blueprint(byte[] data, BlueprintId id, BlueprintGuid guid)
        {
            Id = id;
            Guid = guid;

            // Deserialize
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

        public void OnAllDirectoriesLoaded()
        {
            // Data ref fixups happen here in the client - we don't really need those right now

            PopulateFileIds(FileIdHashSet);
        }

        public void PopulateFileIds(HashSet<BlueprintId> callerFileIdHashSet)
        {
            // Begin building a new list if ours is empty
            if (FileIdHashSet.Count == 0)
            {
                FileIdHashSet.Add(Id);     // add this blueprint's id

                // Add parent ids
                foreach (BlueprintReference parentRef in Parents)
                {
                    var parent = GameDatabase.GetBlueprint(parentRef.BlueprintId);
                    parent.PopulateFileIds(FileIdHashSet);
                }
            }

            // Add this blueprint's list if it's a parent of the caller
            if (callerFileIdHashSet != FileIdHashSet)
            {
                foreach (BlueprintId id in FileIdHashSet)
                    callerFileIdHashSet.Add(id);
            }
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
