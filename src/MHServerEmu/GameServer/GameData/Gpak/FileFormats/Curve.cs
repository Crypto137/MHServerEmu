using System.Text;

namespace MHServerEmu.GameServer.GameData.Gpak.FileFormats
{
    public class Curve
    {
        public uint Header { get; }
        public double[] Entries { get; }

        public Curve(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = reader.ReadUInt32();
                int startPosition = reader.ReadInt32();
                Entries = new double[reader.ReadInt32() - startPosition + 1];

                for (int i = 0; i < Entries.Length; i++)
                    Entries[i] = reader.ReadDouble();
            }
        }
    }
}
