using MHServerEmu.Common;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class StringHashKey
    {
        public uint Hash;
        public StringHashKey(string str)
        {
            Hash = str.Hash();
        }
    }

    public static class StringExtensions
    {
        public static uint Hash(this string str)
        {
            return HashHelper.Djb2(str);
        }
    }
}

