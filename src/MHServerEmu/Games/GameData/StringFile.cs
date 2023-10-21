using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData
{
    public class StringFile
    {
        public CalligraphyHeader Header { get; }
        public StringMapEntry[] StringMap { get; }

        public StringFile(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = reader.ReadCalligraphyHeader();

                StringMap = new StringMapEntry[reader.ReadUInt16()];
                for (int i = 0; i < StringMap.Length; i++)
                    StringMap[i] = new(reader);
            }
        }
    }

    public class StringMapEntry
    {
        public ulong LocaleStringId { get; set; }
        public StringVariation[] Variants { get; set; }
        public ushort FlagsProduced { get; set; }
        public string String { get; set; }

        public StringMapEntry(BinaryReader reader)
        {
            LocaleStringId = reader.ReadUInt64();

            ushort variantNum = reader.ReadUInt16();
            Variants = variantNum > 0
                ? new StringVariation[variantNum - 1]
                : Array.Empty<StringVariation>();

            FlagsProduced = reader.ReadUInt16();
            String = reader.ReadNullTerminatedString(reader.ReadUInt32());

            for (int i = 0; i < Variants.Length; i++)
                Variants[i] = new(reader);
        }
    }

    public class StringVariation
    {
        public ulong FlagsConsumed { get; set; }
        public ushort FlagsProduced { get; set; }
        public string String { get; set; }

        public StringVariation(BinaryReader reader)
        {
            FlagsConsumed = reader.ReadUInt64();
            FlagsProduced = reader.ReadUInt16();
            String = reader.ReadNullTerminatedString(reader.ReadUInt32());
        }
    }
}
