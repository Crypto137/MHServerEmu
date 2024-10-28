
using Gazillion;

namespace MHServerEmu.Games.UI
{
    public class DialogResponse
    {
        public GameDialogResultEnum ButtonIndex { get; }
        public bool CheckboxClicked { get; }

        public DialogResponse(GameDialogResultEnum buttonIndex, bool checkboxClicked)
        {
            ButtonIndex = buttonIndex;
            CheckboxClicked = checkboxClicked;
        }
    }
}
