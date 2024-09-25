using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MHServerEmu.Core.Extensions
{
    public static class BinaryWriterExtensions
    {
        /// <summary>
        /// Writes the provided <see cref="int"/> as an unsigned 24-bit integer.
        /// </summary>
        public static void WriteUInt24(this BinaryWriter writer, int value)
        {
            // NOTE: This implementation assumes little-endian byte order.
            if (value < 0 || value > 16777215) throw new($"UInt24 overflow for value {value}.");

            // Same as BitConverter.TryWriteBytes(), but without the size check
            Span<byte> bytes = stackalloc byte[sizeof(int)];
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(bytes), value);
            
            // The last byte is omitted by slicing the span
            writer.Write(bytes[..3]);
        }
    }
}
