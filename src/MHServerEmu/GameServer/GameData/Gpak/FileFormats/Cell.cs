using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.GameData.Gpak.FileFormats
{
    public class Cell
    {
        public uint Header { get; }
        public uint Type { get; }
        public uint Field2 { get; }
        public Vector3 Max { get; }
        public Vector3 Min { get; }
        public uint Field5 { get; }
        public uint Field6 { get; }     // _1ff
        public uint Field7 { get; }
        public uint Field8 { get; }
        public string PrototypeName { get; }
        public CellAgent[] Transitions { get; }
        public CellAgent[] Npcs { get; }


        public Cell(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = reader.ReadUInt32();
                Type = reader.ReadUInt32();
                Field2 = reader.ReadUInt32();
                Max = reader.ReadVector3();
                Min = reader.ReadVector3();
                Field5 = reader.ReadUInt32();
                Field6 = reader.ReadUInt32();
                Field7 = reader.ReadUInt32();
                Field8 = reader.ReadUInt32();
                PrototypeName = reader.ReadFixedString32();

                /*
                Transitions = new CellAgent[reader.ReadInt32()];
                for (int i = 0; i < Transitions.Length; i++)
                    Transitions[i] = new(reader);

                Npcs = new CellAgent[reader.ReadInt32()];
                for (int i = 0; i < Npcs.Length; i++)
                    Npcs[i] = new(reader);
                */
            }
        }
    }

    public class CellAgent
    {
        public uint Field0 { get; }
        public uint Field1 { get; }
        public uint Field2 { get; }
        public string PrototypeName { get; }
        public byte[] Zeroes { get; }
        public Vector3 Position { get; }
        public Vector3 Orientation { get; }

        public CellAgent(BinaryReader reader)
        {
            Field0 = reader.ReadUInt32();
            Field1 = reader.ReadUInt32();
            Field2 = reader.ReadUInt32();
            PrototypeName = reader.ReadFixedString32();
            Zeroes = reader.ReadBytes(42);
            Position = reader.ReadVector3();
            Orientation = reader.ReadVector3();
        }
    }
}
