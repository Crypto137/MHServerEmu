using MHServerEmu.Common.Logging;
using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    // TODO: Move Calligraphy prototype deserialization to CalligraphySerializer

    public readonly struct PrototypeDataHeader
    {
        // CalligraphyReader::ReadPrototypeHeader

        [Flags]
        private enum PrototypeDataDesc : byte
        {
            None            = 0,
            ReferenceExists = 1 << 0,
            DataExists      = 1 << 1,
            PolymorphicData = 1 << 2
        }

        public bool ReferenceExists { get; }
        public bool DataExists { get; }
        public bool PolymorphicData { get; }
        public PrototypeId ReferenceType { get; }     // Parent prototype id, invalid (0) for .defaults

        public PrototypeDataHeader(BinaryReader reader)
        {
            var flags = (PrototypeDataDesc)reader.ReadByte();
            ReferenceExists = flags.HasFlag(PrototypeDataDesc.ReferenceExists);
            DataExists = flags.HasFlag(PrototypeDataDesc.DataExists);
            PolymorphicData = flags.HasFlag(PrototypeDataDesc.PolymorphicData);

            ReferenceType = ReferenceExists ? (PrototypeId)reader.ReadUInt64() : 0;
        }
    }

    public class Prototype
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        // Child prototypes need to have separate mixin lists from their parents so that when we modify a child we don't change its parent.
        // To ensure this each prototype keeps track of mixins that belong to it in this field. This is accurate to how the client does this.
        // The client uses a sorted vector here, but we use a HashSet to ensure that the same field data doesn't get added twice for some reason.
        private HashSet<object> _ownedDynamicFields;

        public PrototypeId DataRef { get; set; }
        public PrototypeId ParentDataRef { get; set; }
        public PrototypeDataRefRecord DataRefRecord { get; set; }

        public PrototypeDataHeader Header { get; private set; }
        public PrototypeFieldGroup[] FieldGroups { get; private set; }

        public Prototype() { }

        public Prototype(BinaryReader reader)
        {
            DeserializeCalligraphy(reader);
        }

        public void DeserializeCalligraphy(BinaryReader reader)
        {
            // temp method for compatibility
            Header = new(reader);
            if (Header.DataExists == false) return;

            FieldGroups = new PrototypeFieldGroup[reader.ReadUInt16()];
            for (int i = 0; i < FieldGroups.Length; i++)
                FieldGroups[i] = new(reader);
        }

        public PrototypeFieldGroup GetFieldGroup(BlueprintId blueprintId)
        {
            if (FieldGroups == null) return null;
            return FieldGroups.FirstOrDefault(entry => entry.DeclaringBlueprintId == blueprintId);
        }

        public PrototypeFieldGroup GetFieldGroup(HardcodedBlueprintId blueprintId) => GetFieldGroup((BlueprintId)blueprintId);

        /// <summary>
        /// Returns <see langword="false"/> if this is a prototype in development.
        /// </summary>
        public virtual bool ApprovedForUse()
        {
            return true;
        }

        // These dynamic field management methods are part of the PrototypeClassManager in the client, but it doesn't really make sense so we moved them here.
        // They work only with reference types, but we use them only for list mixins, so it's fine.

        /// <summary>
        /// Assigns this prototype as the owner of field data of type <typeparamref name="T"/>. Field data must be a reference type.
        /// </summary>
        public void SetDynamicFieldOwner<T>(T fieldData) where T: class
        {
            // Create the owned field collection if we don't have one
            if (_ownedDynamicFields == null)
                _ownedDynamicFields = new();

            _ownedDynamicFields.Add(fieldData);
        }

        /// <summary>
        /// Removes this prototype as the owner of field data of type <typeparamref name="T"/>. Field data must be a reference type.
        /// </summary>
        public bool RemoveDynamicFieldOwner<T>(T fieldData) where T: class
        {
            // Check if we have any dynamic fields at all
            if (_ownedDynamicFields == null)
                Logger.WarnReturn(false, $"Failed to remove {GameDatabase.GetPrototypeName(DataRef)} as the owner of dynamic field data: this prototype has no owned fields");

            if (_ownedDynamicFields.Remove(fieldData) == false)
                Logger.WarnReturn(false, $"Failed to remove {GameDatabase.GetPrototypeName(DataRef)} as the owner of dynamic field data: field data not found");

            return true;
        }

        /// <summary>
        /// Checks if this prototype is the owner of field data of type <typeparamref name="T"/>. Field data must be a reference type.
        /// </summary>
        public bool IsDynamicFieldOwnedBy<T>(T fieldData) where T: class
        {
            // Make sure this prototype has any dynamic fields assigned to it at all
            if (_ownedDynamicFields == null)
                return false;

            return _ownedDynamicFields.Contains(fieldData);
        }

    }

    public class PrototypeFieldGroup
    {
        public BlueprintId DeclaringBlueprintId { get; }
        public byte BlueprintCopyNumber { get; }
        public PrototypeSimpleField[] SimpleFields { get; }
        public PrototypeListField[] ListFields { get; }

        public PrototypeFieldGroup(BinaryReader reader)
        {
            DeclaringBlueprintId = (BlueprintId)reader.ReadUInt64();
            BlueprintCopyNumber = reader.ReadByte();

            SimpleFields = new PrototypeSimpleField[reader.ReadUInt16()];
            for (int i = 0; i < SimpleFields.Length; i++)
                SimpleFields[i] = new(reader);

            ListFields = new PrototypeListField[reader.ReadUInt16()];
            for (int i = 0; i < ListFields.Length; i++)
                ListFields[i] = new(reader);
        }

        public PrototypeSimpleField GetField(StringId fieldId)
        {
            if (SimpleFields == null) return null;
            return SimpleFields.FirstOrDefault(field => field.Id == fieldId);
        }
        public PrototypeSimpleField GetField(FieldId fieldId) => GetField((StringId)fieldId);

        public ulong GetFieldDef(FieldId fieldId)
        {
            PrototypeSimpleField field = GetField((StringId)fieldId);
            if (field == null) return 0;
            return (ulong)field.Value;
        }

        public PrototypeListField GetListField(StringId fieldId)
        {
            if (ListFields == null) return null;
            return ListFields.FirstOrDefault(field => field.Id == fieldId);
        }

        public PrototypeListField GetListField(FieldId fieldId) => GetListField((StringId)fieldId);
    }

    public class PrototypeSimpleField
    {
        public StringId Id { get; }
        public CalligraphyBaseType Type { get; }
        public object Value { get; }
        public PrototypeSimpleField(BinaryReader reader)
        {
            Id = (StringId)reader.ReadUInt64();
            Type = (CalligraphyBaseType)reader.ReadByte();

            switch (Type)
            {
                case CalligraphyBaseType.Boolean:
                    Value = Convert.ToBoolean(reader.ReadUInt64());
                    break;
                case CalligraphyBaseType.Double:
                    Value = reader.ReadDouble();
                    break;
                case CalligraphyBaseType.Long:
                    Value = reader.ReadInt64();
                    break;
                case CalligraphyBaseType.RHStruct:
                    Value = new Prototype(reader);
                    break;
                default:
                    Value = reader.ReadUInt64();
                    break;
            }
        }
    }

    public class PrototypeListField
    {
        public StringId Id { get; }
        public CalligraphyBaseType Type { get; }
        public object[] Values { get; }

        public PrototypeListField(BinaryReader reader)
        {
            Id = (StringId)reader.ReadUInt64();
            Type = (CalligraphyBaseType)reader.ReadByte();

            Values = new object[reader.ReadUInt16()];
            for (int i = 0; i < Values.Length; i++)
            {
                switch (Type)
                {
                    case CalligraphyBaseType.Boolean:
                        Values[i] = Convert.ToBoolean(reader.ReadUInt64());
                        break;
                    case CalligraphyBaseType.Double:
                        Values[i] = reader.ReadDouble();
                        break;
                    case CalligraphyBaseType.Long:
                        Values[i] = reader.ReadInt64();
                        break;
                    case CalligraphyBaseType.RHStruct:
                        Values[i] = new Prototype(reader);
                        break;
                    default:
                        Values[i] = reader.ReadUInt64();
                        break;
                }
            }
        }
    }
}
