using System.Runtime.CompilerServices;
using System.Text;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Core.Extensions
{
    public static class BinaryReaderExtensions
    {
        /// <summary>
        /// Reads a fixed-length string preceded by its length as a 16-bit unsigned integer.
        /// </summary>
        public static string ReadFixedString16(this BinaryReader reader)
        {
            int size = reader.ReadUInt16();
            return reader.ReadBytesAsUtf8String(size);
        }

        /// <summary>
        /// Reads a fixed-length string preceded by its length as a 32-bit signed integer.
        /// </summary>
        public static string ReadFixedString32(this BinaryReader reader)
        {
            int size = reader.ReadInt32();
            return reader.ReadBytesAsUtf8String(size);
        }

        /// <summary>
        /// Read a null-terminated string at the current position.
        /// </summary>
        public static string ReadNullTerminatedString(this BinaryReader reader)
        {
            const int BufferSize = 65535;   // This should be enough to hold the largest locale string + more

            Span<byte> buffer = stackalloc byte[BufferSize];   

            for (int i = 0; i < BufferSize; i++)
            {
                byte b = reader.ReadByte();

                // Slice off the extra bytes and break the loop once we encounter a null.
                // If there is no null in the input for some reason, we will stop when we our buffer is filled.
                if (b == 0x00)
                {
                    buffer = buffer[..i];
                    break;
                }

                buffer[i] = b;
            }

            return Encoding.UTF8.GetString(buffer);
        }

        /// <summary>
        /// Read a null-terminated string at the specified offset.
        /// </summary>
        public static string ReadNullTerminatedString(this BinaryReader reader, long offset)
        {
            long pos = reader.BaseStream.Position;              // Remember the current position
            reader.BaseStream.Seek(offset, 0);                  // Move to the offset
            string result = reader.ReadNullTerminatedString();  // Read the string
            reader.BaseStream.Seek(pos, 0);                     // Return to the original position
            return result;
        }

        /// <summary>
        /// Reads the specified number of bytes as a UTF-8 encoded <see cref="string"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadBytesAsUtf8String(this BinaryReader reader, int numBytes)
        {
            Span<byte> bytes = stackalloc byte[numBytes];
            reader.Read(bytes);
            return Encoding.UTF8.GetString(bytes);
        }

        public static Vector2 ReadVector2(this BinaryReader reader)
        {
            return new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }

        public static Vector3 ReadVector3(this BinaryReader reader)
        {
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        public static Orientation ReadOrientation(this BinaryReader reader)
        {
            return new Orientation(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
    }
}
