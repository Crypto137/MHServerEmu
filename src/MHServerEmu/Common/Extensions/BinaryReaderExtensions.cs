using MHServerEmu.GameServer.Common;

namespace MHServerEmu.Common.Extensions
{
    public static class BinaryReaderExtensions
    {
        public static string ReadFixedString16(this BinaryReader reader)
        {
            return System.Text.Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadUInt16()));
        }

        public static string ReadFixedString32(this BinaryReader reader)
        {
            return System.Text.Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
        }

        public static Vector3 ReadVector3(this BinaryReader reader)
        {
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
    }
}
