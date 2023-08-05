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

        public static float ZigZagDecode32(this uint number, int precision)
        {
            return (float)((number >> 1) ^ -(number & 1)) / (1 << precision);
        }

        public static uint ZigZagEncode32(this float number, int precision)
        {
            int n = (int)(number * (1 << precision));
            return (uint)((2 * n) ^ (n >> 31));
        }
    }
}
