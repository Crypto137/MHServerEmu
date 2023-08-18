using System.Text;
using MHServerEmu.Common;

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
        private static readonly Logger Logger = LogManager.CreateLogger();

        public enum BlueprintEntryType1 : byte
        {
            A = 0x41,   // asset?
            B = 0x42,
            C = 0x43,   // curve?
            D = 0x44,
            L = 0x4c,
            P = 0x50,   // prototype?
            R = 0x52,
            S = 0x53,
            T = 0x54
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

            switch (Type1)
            {
                case BlueprintEntryType1.A:
                    TypeSpecificId = reader.ReadUInt64();

                    /* if (Type2 == BlueprintEntryType2.L)
                        TypeSpecificIdName = "not a prototype? (AL)";
                    else if (Type2 == BlueprintEntryType2.S)
                        TypeSpecificIdName = "not a prototype? (AS)"; */

                    break;

                case BlueprintEntryType1.C:
                    TypeSpecificId = reader.ReadUInt64();
                    if (Type2 == BlueprintEntryType2.L) Logger.Warn("Found CL");    // there are no CL entries in any of our data

                    /* if (Type2 == BlueprintEntryType2.S)
                        TypeSpecificIdName = "not a prototype? (CS)"; */

                    break;

                case BlueprintEntryType1.P:
                    TypeSpecificId = reader.ReadUInt64();
                    //TypeSpecificIdName = prototypeIdDict[TypeSpecificId];

                    break;

                case BlueprintEntryType1.R:
                    TypeSpecificId = reader.ReadUInt64();

                    /* if (Type2 == BlueprintEntryType2.L)
                        TypeSpecificIdName = prototypeIdDict[TypeSpecificId]
                    else if (Type2 == BlueprintEntryType2.S)
                        TypeSpecificIdName = "not a prototype? (RS)"; */

                    break;

                default:
                    // other types don't have ids
                    break;
            }
        }
    }
}
