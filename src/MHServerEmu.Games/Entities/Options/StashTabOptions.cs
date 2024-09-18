using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
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

    public class StashTabOptions : ISerialize
    {
        private string _displayName = string.Empty;
        private AssetId _iconPathAssetId = AssetId.Invalid;
        private int _sortOrder = 0;
        private StashTabColor _color = StashTabColor.White;

        public string DisplayName { get => _displayName; set => _displayName = value; }
        public AssetId IconPathAssetId { get => _iconPathAssetId; set => _iconPathAssetId = value; }
        public int SortOrder { get => _sortOrder; set => _sortOrder = value; }
        public StashTabColor Color { get => _color; set => _color = value; }

        public StashTabOptions() { }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            success &= Serializer.Transfer(archive, ref _displayName);
            success &= Serializer.Transfer(archive, ref _iconPathAssetId);
            success &= Serializer.Transfer(archive, ref _sortOrder);

            int color = (int)_color;
            success &= Serializer.Transfer(archive, ref color);
            _color = (StashTabColor)color;

            return success;
        }

        public override string ToString()
        {
            return $"[{SortOrder}] displayName={DisplayName} iconPathAssetId={GameDatabase.GetAssetName(IconPathAssetId)} color={Color}";
        }
    }
}
