using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Localization
{
    public class Locale
    {
        public CalligraphyHeader Header { get; }
        public string Name { get; }
        public string LanguageDisplayName { get; }
        public string RegionDisplayName { get; }
        public string Directory { get; }
        public LocaleFlag[] Flags { get; }

        public Dictionary<LocaleStringId, StringMapEntry> StringMap { get; } = new();

        public Locale(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = new(reader);
                Name = reader.ReadFixedString16();
                LanguageDisplayName = reader.ReadFixedString16();
                RegionDisplayName = reader.ReadFixedString16();
                Directory = reader.ReadFixedString16();

                Flags = new LocaleFlag[reader.ReadByte()];
                for (int i = 0; i < Flags.Length; i++)
                    Flags[i] = new(reader);
            }
        }

        public void AddStringFile(StringFile stringFile)
        {
            foreach (var kvp in stringFile.StringMap)
                StringMap.Add(kvp.Key, kvp.Value);
        }
    }

    public class LocaleFlag
    {
        public ushort BitValue { get; }
        public ushort BitMask { get; }
        public string FlagText { get; }

        public LocaleFlag(BinaryReader reader)
        {
            BitValue = reader.ReadUInt16();
            BitMask = reader.ReadUInt16();
            FlagText = reader.ReadFixedString16();
        }
    }
}
