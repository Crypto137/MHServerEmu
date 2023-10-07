using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.GameData.Gpak.FileFormats
{
    public class PrototypeFile
    {
        public FileHeader Header { get; }
        public Prototype Prototype { get; }

        public PrototypeFile(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = reader.ReadHeader();
                Prototype = new(reader);
            }
        }
    }

    public class Prototype
    {
        public byte Flags { get; }
        public ulong ParentId { get; }  // 0 for .defaults
        public PrototypeEntry[] Entries { get; }

        public Prototype(BinaryReader reader)
        {
            Flags = reader.ReadByte();

            if ((Flags & 0x01) > 0)      // flag0 == contains parent id
            {
                ParentId = reader.ReadUInt64();

                if ((Flags & 0x02) > 0)  // flag1 == contains data
                {
                    Entries = new PrototypeEntry[reader.ReadUInt16()];
                    for (int i = 0; i < Entries.Length; i++)
                        Entries[i] = new(reader);
                }
            }

            // flag2 == ??
        }

        public PrototypeEntry GetEntry(ulong blueprintId)
        {
            if (Entries == null) return null;
            return Entries.FirstOrDefault(entry => entry.Id == blueprintId);
        }
        public PrototypeEntry GetEntry(BlueprintId blueprintId) => GetEntry((ulong)blueprintId);
    }

    public class PrototypeEntry
    {
        public ulong Id { get; }
        public byte ByteField { get; }
        public PrototypeEntryElement[] Elements { get; }
        public PrototypeEntryListElement[] ListElements { get; }

        public PrototypeEntry(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            ByteField = reader.ReadByte();

            Elements = new PrototypeEntryElement[reader.ReadUInt16()];
            for (int i = 0; i < Elements.Length; i++)
                Elements[i] = new(reader);

            ListElements = new PrototypeEntryListElement[reader.ReadUInt16()];
            for (int i = 0; i < ListElements.Length; i++)
                ListElements[i] = new(reader);
        }

        public PrototypeEntryElement GetField(ulong fieldId)
        {
            if (Elements == null) return null;
            return Elements.FirstOrDefault(field => field.Id == fieldId);
        }
        public PrototypeEntryElement GetField(FieldId fieldId) => GetField((ulong)fieldId);
    }

    public class PrototypeEntryElement
    {
        public ulong Id { get; }
        public CalligraphyValueType Type { get; }
        public object Value { get; }

        public PrototypeEntryElement(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
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

    public class PrototypeEntryListElement
    {
        public ulong Id { get; }
        public CalligraphyValueType Type { get; }
        public object[] Values { get; }

        public PrototypeEntryListElement(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
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
