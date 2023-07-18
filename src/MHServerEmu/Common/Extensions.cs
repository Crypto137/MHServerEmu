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
    }
}
