using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    // TODO: Move Calligraphy prototype deserialization to CalligraphySerializer

    public class PrototypeFile
    {
        public CalligraphyHeader Header { get; }
        public Prototype Prototype { get; }

        public PrototypeFile(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = new(reader);
                Prototype = new(reader);
            }
        }
    }

    public readonly struct PrototypeDataHeader
    {
        public bool ReferenceExists { get; }
        public bool DataExists { get; }
        public bool PolymorphicData { get; }
        public PrototypeId ReferenceType { get; }     // Parent prototype id, invalid (0) for .defaults

        public PrototypeDataHeader(BinaryReader reader)
        {
            byte flags = reader.ReadByte();
            ReferenceExists = (flags & 0x01) > 0;
            DataExists = (flags & 0x02) > 0;
            PolymorphicData = (flags & 0x04) > 0;

            ReferenceType = ReferenceExists ? (PrototypeId)reader.ReadUInt64() : 0;
        }
    }

    public class Prototype
    {
        public PrototypeDataHeader Header { get; }
        public PrototypeFieldGroup[] FieldGroups { get; }

        public Prototype() { }

        public Prototype(BinaryReader reader)
        {
            Header = new(reader);
            if (Header.DataExists == false) return;

            FieldGroups = new PrototypeFieldGroup[reader.ReadUInt16()];
            for (int i = 0; i < FieldGroups.Length; i++)
                FieldGroups[i] = new(reader);
        }

        public PrototypeFieldGroup GetFieldGroup(PrototypeId defaultPrototypeId)
        {
            if (FieldGroups == null) return null;
            return FieldGroups.FirstOrDefault(entry => entry.DeclaringBlueprintId == defaultPrototypeId);
        }

        public PrototypeFieldGroup GetFieldGroup(DefaultPrototypeId defaultPrototypeId) => GetFieldGroup((PrototypeId)defaultPrototypeId);
    }

    public class PrototypeFieldGroup
    {
        public PrototypeId DeclaringBlueprintId { get; }
        public byte BlueprintCopyNumber { get; }
        public PrototypeSimpleField[] SimpleFields { get; }
        public PrototypeListField[] ListFields { get; }

        public PrototypeFieldGroup(BinaryReader reader)
        {
            DeclaringBlueprintId = (PrototypeId)reader.ReadUInt64();
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
        public CalligraphyValueType Type { get; }
        public object Value { get; }
        public PrototypeSimpleField(BinaryReader reader)
        {
            Id = (StringId)reader.ReadUInt64();
            Type = (CalligraphyValueType)reader.ReadByte();

            switch (Type)
            {
                case CalligraphyValueType.B:
                    Value = Convert.ToBoolean(reader.ReadUInt64());
                    break;
                case CalligraphyValueType.D:
                    Value = reader.ReadDouble();
                    break;
                case CalligraphyValueType.L:
                    Value = reader.ReadInt64();
                    break;
                case CalligraphyValueType.R:
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
        public CalligraphyValueType Type { get; }
        public object[] Values { get; }

        public PrototypeListField(BinaryReader reader)
        {
            Id = (StringId)reader.ReadUInt64();
            Type = (CalligraphyValueType)reader.ReadByte();

            Values = new object[reader.ReadUInt16()];
            for (int i = 0; i < Values.Length; i++)
            {
                switch (Type)
                {
                    case CalligraphyValueType.B:
                        Values[i] = Convert.ToBoolean(reader.ReadUInt64());
                        break;
                    case CalligraphyValueType.D:
                        Values[i] = reader.ReadDouble();
                        break;
                    case CalligraphyValueType.L:
                        Values[i] = reader.ReadInt64();
                        break;
                    case CalligraphyValueType.R:
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
