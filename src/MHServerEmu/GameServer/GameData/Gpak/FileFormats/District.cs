using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.GameData.Gpak.FileFormats
{
    public class District
    {
        public uint Header { get; }
        public uint[] Types { get; }
        public DistrictCell[] Cells { get; }

        public District(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = reader.ReadUInt32();

                Types = new uint[reader.ReadUInt32()];
                for (int i = 0; i < Types.Length; i++)
                    Types[i] = reader.ReadUInt32();

                Cells = new DistrictCell[reader.ReadUInt32()];
                for (int i = 0; i < Cells.Length; i++)
                    Cells[i] = new(reader);
            }
        }
    }

    public class DistrictCell
    {
        public uint Type { get; }
        public string Name { get; }
        public uint[] UnknownZeroes { get; } = new uint[6];

        public DistrictCell(BinaryReader reader)
        {
            Type = reader.ReadUInt32();
            Name = reader.ReadFixedString32();

            for (int i = 0; i < UnknownZeroes.Length; i++)
                UnknownZeroes[i] = reader.ReadUInt32();
        }
    }
}
