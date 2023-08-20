using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu.Common.Extensions
{
    public static class ArrayExtensions
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

        #region uint <-> bool[] conversion
        /* uint mask cheat sheet for getting bools (1 << i)
        0 == 0x1, 1 == 0x2, 2 == 0x4, 3 == 0x8, 4 == 0x10, 5 == 0x20, 6 == 0x40, 7 == 0x80,
        8 == 0x100, 9 == 0x200, 10 == 0x400, 11 == 0x800, 12 == 0x1000, 13 == 0x2000, 14 == 0x4000, 15 == 0x8000
        16 == 0x10000, 17 == 0x20000, 18 == 0x40000, 19 = 0x80000, 20 == 0x100000
        */
        public static bool[] ToBoolArray(this uint value, int arraySize = 32)
        {
            if (arraySize > 32) throw new("Cannot decode more than 32 bools from a uint.");

            bool[] output = new bool[arraySize];

            for (int i = 0; i < output.Length; i++)
                output[i] = (value & (1 << i)) > 0;

            return output;
        }

        public static uint ToUInt32(this bool[] boolArray)
        {
            if (boolArray.Length > 32) throw new("Cannot encode more than 32 bools in a uint.");

            uint output = 0;

            for (int i = 0; i < boolArray.Length; i++)
                if (boolArray[i]) output |= (uint)(1 << i);

            return output;
        }
        #endregion
    }
}
