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
        public byte Type { get; }
        public ulong Id { get; }
        public PrototypeDataEntry[] Entries { get; }

        public PrototypeData(BinaryReader reader)
        {
            Type = reader.ReadByte();
            Id = reader.ReadUInt64();

            Entries = new PrototypeDataEntry[reader.ReadUInt16()];
            for (int i = 0; i < Entries.Length; i++)
                Entries[i] = new(reader);
        }
    }

    public class PrototypeDataEntry
    {
        public ulong Id { get; }
        public byte Zero { get; }
        public PrototypeDataEntryElement[] Elements1 { get; }
        public PrototypeDataEntryElement[] Elements2 { get; }

        public PrototypeDataEntry(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Zero = reader.ReadByte();

            Elements1 = new PrototypeDataEntryElement[reader.ReadUInt16()];
            for (int i = 0; i < Elements1.Length; i++)
                Elements1[i] = new(reader);

            Elements2 = new PrototypeDataEntryElement[reader.ReadUInt16()];
            for (int i = 0; i < Elements2.Length; i++)
                Elements2[i] = new(reader, true);
        }

    }

    public class PrototypeDataEntryElement
    {
        public ulong Id { get; }
        public byte Type { get; }   //A,B,C,D,P,R,L,S
        public ulong Value { get; }
        public PrototypeData[] SubPrototypeData { get; }

        public PrototypeDataEntryElement(BinaryReader reader, bool isList = false)
        {
            Id = reader.ReadUInt64();
            Type = reader.ReadByte();

            if (Type == 0x52)   // R
            {
                if (isList)
                {
                    SubPrototypeData = new PrototypeData[reader.ReadUInt16()];
                    for (int i = 0; i < SubPrototypeData.Length; i++)
                        SubPrototypeData[i] = new(reader);
                }
                else
                {
                    SubPrototypeData = new PrototypeData[1];
                    SubPrototypeData[0] = new(reader);
                }

            }
            else
            {
                Value = reader.ReadUInt64();
            }
        }
    }
}
