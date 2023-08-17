using System.Text;

namespace MHServerEmu.GameServer.GameData.Gpak.FileFormats
{
    public class Blueprint
    {
        public uint Header { get; }
        public string PrototypeName { get; }
        public ulong PrototypeId { get; }
        //public string PrototypeIdName { get; }
        public BlueprintReference[] References1 { get; }
        public BlueprintReference[] References2 { get; }
        public BlueprintEntry[] Entries { get; }

        public Blueprint(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = reader.ReadUInt32();
                PrototypeName = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadUInt16()));
                PrototypeId = reader.ReadUInt64();
                //PrototypeIdName = prototypeIdDict[PrototypeId];

                References1 = new BlueprintReference[reader.ReadUInt16()];
                for (int i = 0; i < References1.Length; i++)
                    References1[i] = new(reader);

                References2 = new BlueprintReference[reader.ReadInt16()];
                for (int i = 0; i < References2.Length; i++)
                    References2[i] = new(reader);

                Entries = new BlueprintEntry[reader.ReadUInt16()];
                for (int i = 0; i < Entries.Length; i++)
                    Entries[i] = new(reader);
            }
        }
    }

    public class BlueprintReference
    {
        public ulong Id { get; }
        //public string IdName { get; }
        public byte Field1 { get; }

        public BlueprintReference(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            //IdName = prototypeIdDict[Id];
            Field1 = reader.ReadByte();
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
        //public string TypeSpecificIdName { get; }

        public BlueprintEntry(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Name = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadUInt16()));
            Type1 = (BlueprintEntryType1)reader.ReadByte();
            Type2 = (BlueprintEntryType2)reader.ReadByte();

            if (Type1 == BlueprintEntryType1.P && Type2 == BlueprintEntryType2.S)
            {
                TypeSpecificId = reader.ReadUInt64();
                //TypeSpecificIdName = prototypeIdDict[TypeSpecificId];
            }
            else if (Type1 == BlueprintEntryType1.C && Type2 == BlueprintEntryType2.S)
            {
                TypeSpecificId = reader.ReadUInt64();
                //TypeSpecificIdName = "not a prototype? (CS)";

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
                //TypeSpecificIdName = "not a prototype? (AS)";
            }
            else if (Type1 == BlueprintEntryType1.D && Type2 == BlueprintEntryType2.S)
            {
                // nothing
            }
            else if (Type1 == BlueprintEntryType1.R && Type2 == BlueprintEntryType2.S)
            {
                TypeSpecificId = reader.ReadUInt64();
                //TypeSpecificIdName = "not a prototype? (RS)";
            }
            else if (Type1 == BlueprintEntryType1.P && Type2 == BlueprintEntryType2.L)
            {
                TypeSpecificId = reader.ReadUInt64();
                //TypeSpecificIdName = prototypeIdDict[TypeSpecificId];
            }
            else if (Type1 == BlueprintEntryType1.A && Type2 == BlueprintEntryType2.L)
            {
                TypeSpecificId = reader.ReadUInt64();
                //TypeSpecificIdName = "not a prototype? (AL)";
            }
            else if (Type1 == BlueprintEntryType1.R && Type2 == BlueprintEntryType2.L)
            {
                TypeSpecificId = reader.ReadUInt64();
                //TypeSpecificIdName = prototypeIdDict[TypeSpecificId];
            }
            else if (Type1 == BlueprintEntryType1.S && Type2 == BlueprintEntryType2.S)
            {
                // nothing
            }
        }
    }
}
