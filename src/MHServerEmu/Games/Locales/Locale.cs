using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.Locales
{
    public class Locale
    {
        private readonly Dictionary<LocaleStringId, StringMapEntry> _stringMap = new();

        public string Name { get; }
        public string LanguageDisplayName { get; }
        public string RegionDisplayName { get; }
        public string Directory { get; }
        public LocaleFlag[] Flags { get; }

        public Locale() { }

        public Locale(Stream stream)
        {
            using (BinaryReader reader = new(stream))
            {
                CalligraphyHeader header = new(reader);

                Name = reader.ReadFixedString16();
                LanguageDisplayName = reader.ReadFixedString16();
                RegionDisplayName = reader.ReadFixedString16();
                Directory = reader.ReadFixedString16();

                Flags = new LocaleFlag[reader.ReadByte()];
                for (int i = 0; i < Flags.Length; i++)
                    Flags[i] = new(reader);
            }
        }

        public bool ImportStringStream(string streamName, Stream stream)
        {
            StringFile stringFile = new(stream);

            foreach (var kvp in stringFile.StringMap)
                _stringMap.Add(kvp.Key, kvp.Value);

            return true;
        }

        public string GetLocaleString(LocaleStringId stringId)
        {
            if (_stringMap.TryGetValue(stringId, out StringMapEntry entry) == false)
                return string.Empty;

            return entry.String;
        }

        private bool LoadStringFile(string filePath)
        {
            if (File.Exists(filePath) == false)
                return false;

            using (FileStream fs = File.OpenRead(filePath))
                return ImportStringStream(filePath, fs);
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
