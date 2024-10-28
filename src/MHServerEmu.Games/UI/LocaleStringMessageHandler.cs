
using Gazillion;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.UI
{
    public class LocaleStringMessageHandler
    {
        public LocaleStringId LocaleString { get; set; }

        public LocaleStringMessageHandler(LocaleStringId localeString = LocaleStringId.Blank)
        {
            LocaleString = localeString;
        }

        public bool HasString => LocaleString != LocaleStringId.Blank;

        public NetStructFormatString ToProtobuf()
        {
            return new NetStructFormatString.Builder().SetFormatStringId((ulong)LocaleString).Build();
        }
    }
}
