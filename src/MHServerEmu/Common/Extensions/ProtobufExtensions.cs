using Google.ProtocolBuffers;
using MHServerEmu.GameServer.GameData;

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

        public static uint ReadRawUInt32(this CodedInputStream stream)
        {
            return BitConverter.ToUInt32(stream.ReadRawBytes(4));
        }

        public static float ReadRawFloat(this CodedInputStream stream, int precision)
        {
            int intValue = CodedInputStream.DecodeZigZag32(stream.ReadRawVarint32());
            return (float)intValue / (1 << precision);
        }

        public static string ReadRawString(this CodedInputStream stream)
        {
            int length = (int)stream.ReadRawVarint32();
            return System.Text.Encoding.UTF8.GetString(stream.ReadRawBytes(length));
        }

        public static ulong ReadPrototypeId(this CodedInputStream stream, PrototypeEnumType enumType)
        {
            return GameDatabase.PrototypeEnumManager.GetPrototypeId(stream.ReadRawVarint64(), enumType);
        }

        #endregion

        #region CodedOutputStream

        public static void WriteRawInt32(this CodedOutputStream stream, int value)
        {
            stream.WriteRawVarint32(CodedOutputStream.EncodeZigZag32(value));
        }

        public static void WriteRawUInt32(this CodedOutputStream stream, uint value)
        {
            stream.WriteRawBytes(BitConverter.GetBytes(value));
        }

        public static void WriteRawFloat(this CodedOutputStream stream, float value, int precision)
        {
            int intValue = (int)(value * (1 << precision));
            stream.WriteRawVarint32(CodedOutputStream.EncodeZigZag32(intValue));
        }

        public static void WriteRawString(this CodedOutputStream stream, string value)
        {
            byte[] rawBytes = System.Text.Encoding.UTF8.GetBytes(value);
            stream.WriteRawVarint64((ulong)rawBytes.Length);
            stream.WriteRawBytes(rawBytes);
        }

        public static void WritePrototypeId(this CodedOutputStream stream, ulong prototypeId, PrototypeEnumType enumType)
        {
            stream.WriteRawVarint64(GameDatabase.PrototypeEnumManager.GetEnumValue(prototypeId, enumType));
        }

        #endregion
    }
}
