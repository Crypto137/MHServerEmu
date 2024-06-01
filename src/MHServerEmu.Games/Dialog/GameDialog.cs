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
    }

    public class MissionDialogData : DialogData
    {
        public MissionDialogData()
        {
            DialogDataType = DialogDataType.Mission;
        }
    }

    public class MTXStoreDialogData : DialogData
    {
        public MTXStoreDialogData()
        {
            DialogDataType = DialogDataType. MTXStore;
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
