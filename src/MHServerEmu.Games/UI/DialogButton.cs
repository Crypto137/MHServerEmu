using Gazillion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.UI
{
    public class DialogButton
    {
        public GameDialogResultEnum Type { get; }
        public LocaleStringMessageHandler ButtonText { get; }
        public ButtonStyle Style { get; }
        public bool Hold { get; }
        public bool Enabled { get; set; }

        public DialogButton(GameDialogResultEnum type, LocaleStringId buttonText, ButtonStyle style, bool hold, bool enabled)
        {
            Type = type;
            ButtonText = new(buttonText);
            Style = style;
            Hold = hold;
            Enabled = enabled;
        }

        public NetStructDialogButton ToProtobuf()
        {
            return new NetStructDialogButton.Builder()
                .SetType(Type)
                .SetFormatString(ButtonText.ToProtobuf())
                .SetStyle((uint)Style)
#if GAME_VERSION_1_53
                .SetHold(Hold)
#endif
                .SetEnabled(Enabled)
                .Build();
        }
    }
}
