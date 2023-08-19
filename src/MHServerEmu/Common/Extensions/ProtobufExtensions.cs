using Google.ProtocolBuffers;

namespace MHServerEmu.Common.Extensions
{
    public static class ProtobufExtensions
    {
        #region CodedInputStream

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

        #endregion
    }
}
