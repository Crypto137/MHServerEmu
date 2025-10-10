using Gazillion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.UI
{
    [Flags]
    public enum DialogOptionEnum
    {
        None            = 0,
        MouseCenter     = 1 << 0,
        ScreenBottom    = 1 << 1,
        Modal           = 1 << 2,
        WorldClick      = 1 << 3,
        InputEnabled    = 1 << 4,
        FrontEndDisplay = 1 << 5,
    }

    public class GameDialogInstance
    {
        public GameDialogManager GameDialogManager { get; }
        public ulong ServerId { get; }
        public ulong PlayerGuid { get; }
        public DialogOptionEnum Options { get; set; }
        public LocaleStringMessageHandler Message { get; }
        public LocaleStringMessageHandler Checkbox { get; }
        public List<DialogButton> Buttons { get; }
        public ulong TargetId { get; set; }
        public ulong InteractorId { get; set; }
        public Action<ulong, DialogResponse> OnResponse { get; set; }

        public GameDialogInstance(GameDialogManager gameDialogManager, ulong serverId, ulong playerGuid)
        {
            GameDialogManager = gameDialogManager;
            ServerId = serverId;
            PlayerGuid = playerGuid;
            Message = new();
            Checkbox = new();
            Buttons = new();
        }

        public void AddButton(GameDialogResultEnum option, LocaleStringId text, ButtonStyle style, bool enabled = true)
        {
            var button = new DialogButton(option, text, style, enabled);
            Buttons.Add(button);
        }

        public NetStructDialog ToProtobuf()
        {
            var builder = NetStructDialog.CreateBuilder();

            builder.SetMessageString(Message.ToProtobuf());

            foreach (var button in Buttons)
                builder.AddButtonStrings(button.ToProtobuf());

            if (Checkbox.HasString)
                builder.SetCheckboxString(Checkbox.ToProtobuf());

            builder.SetTargetId(TargetId);
            builder.SetInteractorId(InteractorId);
            builder.SetOptions((uint)Options);

            return builder.Build();
        }
    }
}
