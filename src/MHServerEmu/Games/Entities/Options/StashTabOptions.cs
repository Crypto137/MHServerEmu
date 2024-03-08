using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Entities.Options
{
    public enum StashTabColor
    {
        White,
        Cyan,
        Blue,
        Green,
        Orange,
        Purple,
        Red,
        Yellow
    }

    public class StashTabOptions
    {
        public string DisplayName { get; set; } = string.Empty;
        public AssetId IconPathAssetId { get; set; } = AssetId.Invalid;
        public int SortOrder { get; set; } = 0;
        public StashTabColor Color { get; set; } = StashTabColor.White;

        public StashTabOptions() { }

        public StashTabOptions(CodedInputStream stream)
        {
            DisplayName = stream.ReadRawString();
            IconPathAssetId = (AssetId)stream.ReadRawVarint64();
            SortOrder = stream.ReadRawInt32();
            Color = (StashTabColor)stream.ReadRawInt32();            
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawString(DisplayName);
            stream.WriteRawVarint64((ulong)IconPathAssetId);
            stream.WriteRawInt32(SortOrder);
            stream.WriteRawInt32((int)Color);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(DisplayName)}: {DisplayName}");
            sb.AppendLine($"{nameof(IconPathAssetId)}: {GameDatabase.GetAssetName(IconPathAssetId)}");
            sb.AppendLine($"{nameof(SortOrder)}: {SortOrder}");
            sb.AppendLine($"{nameof(Color)}: {Color}");
            return sb.ToString();
        }
    }
}
