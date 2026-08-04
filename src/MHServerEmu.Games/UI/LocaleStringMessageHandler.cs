using Gazillion;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.UI
{
    public class LocaleStringMessageHandler
    {
        public LocaleStringId LocaleString { get; set; }

        public bool HasString { get => LocaleString != LocaleStringId.Blank; }

        public LocaleStringMessageHandler(LocaleStringId localeString = LocaleStringId.Blank)
        {
            LocaleString = localeString;
        }

        public NetStructFormatString ToProtobuf()
        {
            return NetStructFormatString.CreateBuilder()
                .SetFormatStringId((ulong)LocaleString)
                .Build();
        }
    }
}
