using System.Text;
using Google.ProtocolBuffers;

namespace MHServerEmu.Common
{
    public static class Extensions
    {
        public static string ToHexString(this byte[] byteArray)
        {
            return byteArray.Aggregate("", (current, b) => current + b.ToString("X2"));
        }

        public static byte[] ToUInt24ByteArray(this int number)
        {
            byte[] byteArray = BitConverter.GetBytes((uint)number);
            return BitConverter.IsLittleEndian
                ? new byte[] { byteArray[0], byteArray[1], byteArray[2] }
                : new byte[] { byteArray[3], byteArray[2], byteArray[1] };
        }

        /*
        public static float ZigZagDecode32(this uint number, int precision)
        {
            return (float)((number >> 1) ^ -(number & 1)) / (1 << precision);
        }

        public static uint ZigZagEncode32(this float number, int precision)
        {
            int n = (int)(number * (1 << precision));
            return (uint)((2 * n) ^ (n >> 31));
        }
        */

        public static float ReadRawFloat(this CodedInputStream stream, int precision)
        {
            uint number = stream.ReadRawVarint32();
            return (float)((number >> 1) ^ -(number & 1)) / (1 << precision);
        }

        public static uint ReadRawUInt32(this CodedInputStream stream)
        {
            return BitConverter.ToUInt32(stream.ReadRawBytes(4));
        }

        public static int ReadRawInt32(this CodedInputStream stream)
        {
            return (int)(stream.ReadRawVarint64() >> 1);
        }

        public static string ReadRawString(this CodedInputStream stream)
        {
            int length = (int)stream.ReadRawVarint32();
            return Encoding.UTF8.GetString(stream.ReadRawBytes(length));
        }

        public static void WriteRawFloat(this CodedOutputStream stream, float number, int precision)
        {
            int n = (int)(number * (1 << precision));
            stream.WriteRawVarint32((uint)((2 * n) ^ (n >> 31)));
        }

        public static void WriteRawUInt32(this CodedOutputStream stream, uint value)
        {
            stream.WriteRawBytes(BitConverter.GetBytes(value));
        }

        public static void WriteRawInt32(this CodedOutputStream stream, int value)
        {
            stream.WriteRawVarint64((ulong)(value << 1));
        }

        public static void WriteRawString(this CodedOutputStream stream, string value)
        {
            byte[] rawBytes = Encoding.UTF8.GetBytes(value);
            stream.WriteRawVarint64((ulong)rawBytes.Length);
            stream.WriteRawBytes(rawBytes);
        }
    }
}
