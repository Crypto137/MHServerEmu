using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Localization
{
    public class StringFile
    {
        public CalligraphyHeader Header { get; }
        public Dictionary<LocaleStringId, StringMapEntry> StringMap { get; } = new();

        public StringFile(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = new(reader);

                ushort entryCount = reader.ReadUInt16();
                for (int i = 0; i < entryCount; i++)
                {
                    var id = (LocaleStringId)reader.ReadUInt64();
                    StringMap.Add(id, new(reader));
                }
            }
        }
    }

    public class StringMapEntry
    {
        public LocaleStringId LocaleStringId { get; set; }
        public StringVariation[] Variants { get; set; }
        public ushort FlagsProduced { get; set; }
        public string String { get; set; }

        public StringMapEntry(BinaryReader reader)
        {
            LocaleStringId = (LocaleStringId)reader.ReadUInt64();

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
