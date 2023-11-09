using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Common.Extensions
{
    public static class ProtobufExtensions
    {
        #region CodedInputStream

        public static ushort ReadRawUInt16(this CodedInputStream stream)
        {
            return BitConverter.ToUInt16(stream.ReadRawBytes(2));
        }

        public static int ReadRawUInt24(this CodedInputStream stream)
        {
            // C# doesn't have native support for UInt24, so we add an extra byte to make it an Int32.
            // We're using int instead of uint because this is used only for packet sizes, and we can avoid casting it to int later.
            byte[] bytes = new byte[4];   
            bytes[0] = stream.ReadRawByte();
            bytes[1] = stream.ReadRawByte();
            bytes[2] = stream.ReadRawByte();
            bytes[3] = 0;

            return BitConverter.ToInt32(bytes);
        }

        public static int ReadRawInt32(this CodedInputStream stream)
        {
            return CodedInputStream.DecodeZigZag32(stream.ReadRawVarint32());
        }

        public static long ReadRawInt64(this CodedInputStream stream)
        {
            return CodedInputStream.DecodeZigZag64(stream.ReadRawVarint64());
        }

        public static uint ReadRawUInt32(this CodedInputStream stream)
        {
            return BitConverter.ToUInt32(stream.ReadRawBytes(4));
        }

        public static float ReadRawFloat(this CodedInputStream stream)
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(stream.ReadRawVarint32()));
        }

        public static float ReadRawZigZagFloat(this CodedInputStream stream, int precision)
        {
            int intValue = CodedInputStream.DecodeZigZag32(stream.ReadRawVarint32());
            return (float)intValue / (1 << precision);
        }

        public static string ReadRawString(this CodedInputStream stream)
        {
            int length = (int)stream.ReadRawVarint32();
            return Encoding.UTF8.GetString(stream.ReadRawBytes(length));
        }

        /// <summary>
        /// Reads a prototype enum value from the stream and converts it to a data ref.
        /// </summary>
        public static PrototypeId ReadPrototypeEnum(this CodedInputStream stream, PrototypeEnumType enumType)
        {
            return GameDatabase.DataDirectory.GetPrototypeFromEnumValue(stream.ReadRawVarint64(), enumType);
        }

        #endregion

        #region CodedOutputStream

        public static void WriteRawInt32(this CodedOutputStream stream, int value)
        {
            stream.WriteRawVarint32(CodedOutputStream.EncodeZigZag32(value));
        }

        public static void WriteRawInt64(this CodedOutputStream stream, long value)
        {
            stream.WriteRawVarint64(CodedOutputStream.EncodeZigZag64(value));
        }

        public static void WriteRawUInt32(this CodedOutputStream stream, uint value)
        {
            stream.WriteRawBytes(BitConverter.GetBytes(value));
        }

        public static void WriteRawFloat(this CodedOutputStream stream, float value)
        {
            stream.WriteRawVarint32(BitConverter.ToUInt32(BitConverter.GetBytes(value)));
        }

        public static void WriteRawZigZagFloat(this CodedOutputStream stream, float value, int precision)
        {
            int intValue = (int)(value * (1 << precision));
            stream.WriteRawVarint32(CodedOutputStream.EncodeZigZag32(intValue));
        }

        public static void WriteRawString(this CodedOutputStream stream, string value)
        {
            byte[] rawBytes = Encoding.UTF8.GetBytes(value);
            stream.WriteRawVarint64((ulong)rawBytes.Length);
            stream.WriteRawBytes(rawBytes);
        }

        /// <summary>
        /// Converts a prototype data ref to an enum value and writes it to the stream.
        /// </summary>
        public static void WritePrototypeEnum(this CodedOutputStream stream, PrototypeId prototypeId, PrototypeEnumType enumType)
        {
            stream.WriteRawVarint64(GameDatabase.DataDirectory.GetPrototypeEnumValue(prototypeId, enumType));
        }

        #endregion
    }
}
