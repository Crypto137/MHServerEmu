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
        public bool Enabled { get; set; }

        public DialogButton(GameDialogResultEnum type, LocaleStringId buttonText, ButtonStyle style, bool enabled)
        {
            Type = type;
            ButtonText = new(buttonText);
            Style = style;
            Enabled = enabled;
        }

        public NetStructDialogButton ToProtobuf()
        {
            return new NetStructDialogButton.Builder()
                .SetType(Type)
                .SetFormatString(ButtonText.ToProtobuf())
                .SetStyle((uint)Style)
                .SetEnabled(Enabled).Build();
        }
    }
}
