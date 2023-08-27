using MHServerEmu.Common;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.GameData.Gpak.FileFormats
{
    public class Prototype
    {
        public uint Header { get; }
        public PrototypeData Data { get; }

        public Prototype(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = reader.ReadUInt32();
                Data = new(reader);
            }
        }
    }

    public class PrototypeData
    {
        public byte Flags { get; }
        public ulong Id { get; }
        public PrototypeDataEntry[] Entries { get; }

        public PrototypeData(BinaryReader reader)
        {
            Flags = reader.ReadByte();

            if ((Flags & 0x01) > 0)      // flag0 == contains id
            {
                Id = reader.ReadUInt64();

                if ((Flags & 0x02) > 0)  // flag1 == contains data
                {
                    Entries = new PrototypeDataEntry[reader.ReadUInt16()];
                    for (int i = 0; i < Entries.Length; i++)
                        Entries[i] = new(reader);
                }
            }

            // flag2 == ??
        }
    }

    public class PrototypeDataEntry
    {
        public ulong Id { get; }
        public byte Field1 { get; }
        public PrototypeDataEntryElement[] Elements { get; }
        public PrototypeDataEntryListElement[] ListElements { get; }

        public PrototypeDataEntry(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Field1 = reader.ReadByte();

            Elements = new PrototypeDataEntryElement[reader.ReadUInt16()];
            for (int i = 0; i < Elements.Length; i++)
                Elements[i] = new(reader);

            ListElements = new PrototypeDataEntryListElement[reader.ReadUInt16()];
            for (int i = 0; i < ListElements.Length; i++)
                ListElements[i] = new(reader);
        }
    }

    public class PrototypeDataEntryElement
    {
        public ulong Id { get; }
        public CalligraphyValueType Type { get; }
        public object Value { get; }

        public PrototypeDataEntryElement(BinaryReader reader)
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
                    Value = new PrototypeData(reader);
                    break;
                default:
                    Value = reader.ReadUInt64();
                    break;
            }
        }
    }

    public class PrototypeDataEntryListElement
    {
        public ulong Id { get; }
        public CalligraphyValueType Type { get; }
        public object[] Values { get; }

        public PrototypeDataEntryListElement(BinaryReader reader)
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
                        Values[i] = new PrototypeData(reader);
                        break;
                    default:
                        Values[i] = reader.ReadUInt64();
                        break;
                }
            }
        }
    }
}
