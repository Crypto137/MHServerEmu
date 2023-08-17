using System.Text;

namespace MHServerEmu.GameServer.GameData.Gpak.FileFormats
{
    public class Blueprint
    {
        public uint Header { get; }
        public string PrototypeName { get; }
        public ulong PrototypeId { get; }

        public byte ProtoSub1_1 { get; }
        public byte ProtoSub1_2 { get; }
        public ulong ProtoSub1_Data2 { get; }
        public BlueprintIduid[] Iduids1 { get; }
        public byte ProtoSub2_1 { get; }
        public byte ProtoSub2_2 { get; }
        public byte ProtoSub2_3 { get; }
        public ulong ProtoSub2_Data2 { get; }
        public ulong Uid { get; }
        public BlueprintIduid[] Iduids2 { get; }
        public byte ProtoSub3_1 { get; }
        public ushort ProtoSub3_2 { get; }

        public BlueprintEntry[] Entries { get; }

        public Blueprint(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = reader.ReadUInt32();
                PrototypeName = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadUInt16()));
                PrototypeId = reader.ReadUInt64();

                #region black magic
                ProtoSub1_1 = reader.ReadByte();
                ProtoSub1_2 = reader.ReadByte();

                if (ProtoSub1_1 == 0x00 && ProtoSub1_2 == 0x00)
                {
                }
                else if (ProtoSub1_1 == 0x01)
                {
                    ProtoSub1_Data2 = reader.ReadUInt64();
                }
                else if (ProtoSub1_1 > 0x01)
                {
                    ProtoSub1_Data2 = reader.ReadUInt64();
                    Iduids1 = new BlueprintIduid[ProtoSub1_1 - 1];
                    for (int i = 0; i < Iduids1.Length; i++)
                        Iduids1[i] = new(reader);
                }

                ProtoSub2_1 = reader.ReadByte();
                ProtoSub2_2 = reader.ReadByte();
                ProtoSub3_2 = 0;

                if (ProtoSub2_1 == 0x00 && ProtoSub2_2 == 0x00)
                {
                    ProtoSub3_2 = reader.ReadUInt16();          // elements number
                }
                else if (ProtoSub2_1 >= 0x01)
                {
                    if (ProtoSub2_2 == 0x00)
                    {
                        ProtoSub3_1 = reader.ReadByte();        // 0x00
                        ProtoSub3_2 = reader.ReadUInt16();      // elements number
                    }
                    else if (ProtoSub2_2 == 0x01)
                    {
                        ProtoSub2_3 = reader.ReadByte();           // 0x00
                        ProtoSub2_Data2 = reader.ReadUInt64();
                        ProtoSub3_1 = reader.ReadByte();                // 0x01
                        if (ProtoSub3_1 == 0x01)
                        {
                            ProtoSub3_2 = reader.ReadUInt16();          // elements number
                        }
                    }
                    else if (ProtoSub2_2 > 0x01)
                    {
                        ProtoSub2_3 = reader.ReadByte();
                        Uid = reader.ReadUInt64();
                        Iduids2 = new BlueprintIduid[((ProtoSub2_3 << 8) | ProtoSub2_2) - 1];
                        for (int i = 0; i < Iduids2.Length; i++)
                            Iduids2[i] = new(reader);

                        ProtoSub3_1 = reader.ReadByte();
                        ProtoSub3_2 = reader.ReadUInt16();       // elements number
                    }
                    // proto_sub_2_2
                }
                //proto_sub_2_1
                #endregion

                Entries = new BlueprintEntry[ProtoSub3_2];
                for (int i = 0; i < Entries.Length; i++)
                    Entries[i] = new(reader);
            }
        }
    }

    public class BlueprintIduid
    {
        public byte Id { get; }
        public ulong Uid { get; }

        public BlueprintIduid(BinaryReader reader)
        {
            Id = reader.ReadByte();
            Uid = reader.ReadUInt64();
        }
    }

    public class BlueprintEntry
    {
        public enum BlueprintEntryType1 : byte
        {
            C = 0x43,
            P = 0x50,
            B = 0x42,
            R = 0x52,
            A = 0x41,
            D = 0x44,
            L = 0x4c,
            S = 0x53
        }

        public enum BlueprintEntryType2 : byte
        {
            L = 0x4c,
            S = 0x53
        }

        public ulong Id { get; }
        public string Name { get; }
        public BlueprintEntryType1 Type1 { get; }
        public BlueprintEntryType2 Type2 { get; }
        public ulong TypeSpecificId { get; }

        public BlueprintEntry(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Name = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadUInt16()));
            Type1 = (BlueprintEntryType1)reader.ReadByte();
            Type2 = (BlueprintEntryType2)reader.ReadByte();

            if (Type1 == BlueprintEntryType1.P && Type2 == BlueprintEntryType2.S)
            {
                TypeSpecificId = reader.ReadUInt64();
            }
            else if (Type1 == BlueprintEntryType1.C && Type2 == BlueprintEntryType2.S)
            {
                TypeSpecificId = reader.ReadUInt64();
            }
            else if (Type1 == BlueprintEntryType1.B && Type2 == BlueprintEntryType2.S)
            {
                // nothing
            }
            else if (Type1 == BlueprintEntryType1.L && Type2 == BlueprintEntryType2.S)
            {
                // nothing
            }
            else if (Type1 == BlueprintEntryType1.A && Type2 == BlueprintEntryType2.S)
            {
                TypeSpecificId = reader.ReadUInt64();
            }
            else if (Type1 == BlueprintEntryType1.D && Type2 == BlueprintEntryType2.S)
            {
                // nothing
            }
            else if (Type1 == BlueprintEntryType1.R && Type2 == BlueprintEntryType2.S)
            {
                TypeSpecificId = reader.ReadUInt64();
            }
            else if (Type1 == BlueprintEntryType1.P && Type2 == BlueprintEntryType2.L)
            {
                TypeSpecificId = reader.ReadUInt64();
            }
            else if (Type1 == BlueprintEntryType1.A && Type2 == BlueprintEntryType2.L)
            {
                TypeSpecificId = reader.ReadUInt64();
            }
            else if (Type1 == BlueprintEntryType1.R && Type2 == BlueprintEntryType2.L)
            {
                TypeSpecificId = reader.ReadUInt64();
            }
            else if (Type1 == BlueprintEntryType1.S && Type2 == BlueprintEntryType2.S)
            {
                // nothing
            }
        }
    }

}
