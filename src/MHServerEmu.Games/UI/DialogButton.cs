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

        public DialogButton(GameDialogResultEnum type, LocaleStringId buttonText, ButtonStyle style)
        {
            Type = type;
            ButtonText = new(buttonText);
            Style = style;
            Enabled = true;
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
