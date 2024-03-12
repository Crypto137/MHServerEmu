namespace MHServerEmu.Core.Extensions
{
    public static class BinaryWriterExtensions
    {
        public static void WriteUInt24(this BinaryWriter writer, int value)
        {
            if (value < 0 || value > 16777215) throw new("UInt24 overflow.");
            byte[] bytes = BitConverter.GetBytes(value);
            writer.Write(bytes, 0, 3);
        }
    }
}
