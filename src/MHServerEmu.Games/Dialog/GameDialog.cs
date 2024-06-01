using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Dialog
{
    public class DialogDataCollection
    {
        public List<DialogData> DialogDataVector = new();

        public void Add(DialogData dialogData)
        {
            DialogDataVector.Add(dialogData);
        }
    }

    public enum DialogDataType
    {
        None,
        Vendor,
        Stash,
        Mission,
        MTXStore
    }

    public class DialogData
    {
        public DialogDataType DialogDataType;
        public LocaleStringId DialogText;
        public DialogStyle DialogStyle;
        public VOCategory VoCategory;
        public ulong InteractorId;

        public static LocaleStringId PickDialog(Game game, WorldEntityPrototype dialogSourceProto)
        {
            if (game == null || dialogSourceProto == null)
                return LocaleStringId.Blank;
            return PickDialog(game, dialogSourceProto.DialogText, dialogSourceProto.DialogTextList);
        }

        public static LocaleStringId PickDialog(Game game, MissionConditionEntityInteractPrototype dialogSourceProto)
        {
            if (game == null || dialogSourceProto == null)
                return LocaleStringId.Blank;
            return PickDialog(game, dialogSourceProto.DialogText, dialogSourceProto.DialogTextList);
        }

        private static LocaleStringId PickDialog(Game game, LocaleStringId dialogText, WeightedTextEntryPrototype[] dialogTextList)
        {
            LocaleStringId resultDialog = LocaleStringId.Blank;
            if (game == null) return resultDialog;

            if (dialogTextList.HasValue())
            {
                Picker<LocaleStringId> textPicker = new (game.Random);
                foreach (var textEntry in dialogTextList)
                    if (textEntry != null && textEntry.Text != LocaleStringId.Blank)
                        textPicker.Add(textEntry.Text, (int)textEntry.Weight);
                textPicker.Pick(out resultDialog);
            }
            else
                resultDialog = dialogText;

            return resultDialog;
        }
    }

    public class MissionDialogData : DialogData
    {
        public MissionDialogData() // client only in AttachDialogDataFromMission
        {
            DialogDataType = DialogDataType.Mission;
        }
    }

    public class MTXStoreDialogData : DialogData
    {
        public MTXStoreDialogData()
        {
            DialogDataType = DialogDataType.MTXStore;
        }

        public string StoreName { get; internal set; }
        public int StoreId { get; internal set; }
    }

    public class StashDialogData : DialogData
    {
        public StashDialogData()
        {
            DialogDataType = DialogDataType.Stash;
        }
    }

    public class VendorDialogData : DialogData
    {
        public VendorDialogData()
        {
            DialogDataType = DialogDataType.Vendor;
        }

        public bool AllowActionDonate { get; internal set; }
        public bool AllowActionRefresh { get; internal set; }
        public bool IsCrafter { get; internal set; }
        public bool IsRaidVendor { get; internal set; }
        public bool IsGlobalEvent { get; internal set; }
    }
}
