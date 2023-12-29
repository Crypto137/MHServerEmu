using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    public class Blueprint
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private PrototypeId[] _enumValueToPrototypeLookup = Array.Empty<PrototypeId>();
        private Dictionary<PrototypeId, int> _prototypeToEnumValueDict;

        private PrototypeId _propertyDataRef = PrototypeId.Invalid;

        public BlueprintId Id { get; }
        public BlueprintGuid Guid { get; }

        public HashSet<BlueprintId> FileIdHashSet { get; } = new();                 // Contains ids of all blueprints related to this one in the hierarchy
        public List<PrototypeDataRefRecord> PrototypeRecordList { get; } = new();   // A list of all prototype records that use this blueprint for iteration

        public Type RuntimeBindingClassType { get; }                                // Type of the class that handles prototypes that use this blueprint
        public PrototypeId DefaultPrototypeId { get; }                              // .defaults prototype file id
        public BlueprintReference[] Parents { get; }
        public BlueprintReference[] ContributingBlueprints { get; }
        public BlueprintMember[] Members { get; }                                   // Field definitions for prototypes that use this blueprint  

        public int PrototypeMaxEnumValue { get => _enumValueToPrototypeLookup.Length - 1; }

        public Blueprint(Stream stream, BlueprintId id, BlueprintGuid guid)
        {
            Id = id;
            Guid = guid;

            // Deserialize
            using (BinaryReader reader = new(stream))
            {
                CalligraphyHeader header = new(reader);

                // Read runtime binding name and get a matching prototype class type from the prototype class manager
                string runtimeBinding = reader.ReadFixedString16();
                RuntimeBindingClassType = GameDatabase.PrototypeClassManager.GetPrototypeClassTypeByName(runtimeBinding);
                
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
            // Begin building a new hash set if ours is empty
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

            // Add this blueprint's hash set if it's a parent of the caller
            if (callerFileIdHashSet != FileIdHashSet)
            {
                foreach (BlueprintId id in FileIdHashSet)
                    callerFileIdHashSet.Add(id);
            }
        }

        public void GenerateEnumLookups()
        {
            // Note: this method is not present in the original game where this is done
            // within DataDirectory::initializeHierarchyCache() instead.

            if (_enumValueToPrototypeLookup.Length > 0)
            {
                Logger.Warn($"Failed to generate enum lookups for blueprint {GameDatabase.GetBlueprintName(Id)}: already generated");
                return;
            }

            // EnumValue -> PrototypeId
            _enumValueToPrototypeLookup = new PrototypeId[PrototypeRecordList.Count + 1];
            _enumValueToPrototypeLookup[0] = PrototypeId.Invalid;
            for (int i = 0; i < PrototypeRecordList.Count; i++)
                _enumValueToPrototypeLookup[i + 1] = PrototypeRecordList[i].PrototypeId;

            // PrototypeId -> EnumValue
            _prototypeToEnumValueDict = new(_enumValueToPrototypeLookup.Length);
            for (int i = 0; i < _enumValueToPrototypeLookup.Length; i++)
                _prototypeToEnumValueDict.Add(_enumValueToPrototypeLookup[i], i);
        }

        public void SetPropertyPrototypeDataRef(PrototypeId propertyDataRef)
        {
            if (_propertyDataRef != PrototypeId.Invalid)
                Logger.Warn(string.Format("Trying to bind blueprint {0} to property {1}, but this blueprint is already bound to {2}",
                            GameDatabase.GetBlueprint(Id), GameDatabase.GetPrototypeName(propertyDataRef), GameDatabase.GetPrototypeName(_propertyDataRef)));

            _propertyDataRef = propertyDataRef;
        }

        public PrototypeId GetPrototypeFromEnumValue(int enumValue)
        {
            if (enumValue < 0 || enumValue >= _enumValueToPrototypeLookup.Length)
                return Logger.WarnReturn(PrototypeId.Invalid, $"Failed to get prototype for enumValue {enumValue} for blueprint {GameDatabase.GetBlueprintName(Id)}");

            return _enumValueToPrototypeLookup[enumValue];
        }

        public int GetPrototypeEnumValue(PrototypeId prototypeId)
        {
            if (_prototypeToEnumValueDict.TryGetValue(prototypeId, out int enumValue) == false)
                return Logger.WarnReturn(0, $"Failed to get enum value for prototype {GameDatabase.GetPrototypeName(prototypeId)} for blueprint {GameDatabase.GetBlueprintName(Id)}");

            return enumValue;
        }

        public PrototypeId GetPropertyPrototypeRef() => _propertyDataRef;

        /// <summary>
        /// Checks if this blueprint belongs to the specified blueprint in the hierarchy.
        /// </summary>
        public bool IsA(BlueprintId blueprintId)
        {
            return FileIdHashSet.Contains(blueprintId);
        }

        public bool IsProperty()
        {
            return _propertyDataRef != PrototypeId.Invalid;
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
