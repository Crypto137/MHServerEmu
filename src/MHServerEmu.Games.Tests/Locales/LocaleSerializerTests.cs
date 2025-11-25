using System.Text.Json;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Locales;

namespace MHServerEmu.Games.Tests.Locales
{
    public class LocaleSerializerTests
    {
        private static readonly string TestStringMapPath = Path.Combine("TestData", "Locales", "TestStringMap.json");

        // Use sorted dictionary for consistent order.
        private readonly SortedDictionary<LocaleStringId, Dictionary<string, string>> _stringMap;

        public LocaleSerializerTests()
        {
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            if (File.Exists(TestStringMapPath) == false)
                throw new FileNotFoundException();

            using FileStream fs = File.OpenRead(TestStringMapPath);
            _stringMap = JsonSerializer.Deserialize<SortedDictionary<LocaleStringId, Dictionary<string, string>>>(fs);
        }

        [Theory]
        [InlineData("zh_tw", 0xE39C6DB2)]
        [InlineData("en_us", 0x38C969EE)]
        [InlineData("fr_fr", 0xCD8A4E41)]
        [InlineData("de_de", 0x4A342C18)]
        [InlineData("ja_jp", 0x0BBB29A2)]
        [InlineData("ko_kr", 0x2F4834C9)]
        [InlineData("pt_br", 0xC78CD127)]
        [InlineData("ru_ru", 0x6346CDCD)]
        [InlineData("es_mx", 0x8B320F99)]
        public void WriteTo_TestStringMap_MatchesCrc(string locale, uint expectedCrc)
        {
            LocaleSerializer serializer = new();

            foreach (var kvp in _stringMap)
            {
                LocaleStringId localeStringId = kvp.Key;
                string str = kvp.Value[locale];

                serializer.AddString(localeStringId, str);
            }

            using MemoryStream stream = new();
            serializer.WriteTo(stream);
            byte[] buffer = stream.ToArray();
            uint actualCrc = HashHelper.Crc32(buffer);

            Assert.Equal(expectedCrc, actualCrc);
        }
    }
}
