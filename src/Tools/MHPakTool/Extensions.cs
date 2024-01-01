using System.Text;

namespace MHPakTool
{
    public static class Extensions
    {
        /// <summary>
        /// Reads a fixed-length string preceded by its length as a 32-bit signed integer.
        /// </summary>
        public static string ReadFixedString32(this BinaryReader reader)
        {
            return Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
        }

        /// <summary>
        /// Writes a fixed-length string preceded by its length as a 32-bit signed integer.
        /// </summary>
        public static void WriteFixedString32(this BinaryWriter writer, string @string)
        {
            writer.Write(@string.Length);
            writer.Write(Encoding.UTF8.GetBytes(@string));
        }
    }
}
