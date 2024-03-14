using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    /// <summary>
    /// A <see cref="List{T}"/> of <see cref="PrototypeMixinListItem"/>.
    /// </summary>
    public class PrototypeMixinList : List<PrototypeMixinListItem> { }

    /// <summary>
    /// Contains an item and its blueprint information for a list of mixin prototypes.
    /// </summary>
    public class PrototypeMixinListItem
    {
        public Prototype Prototype { get; set; }
        public BlueprintId BlueprintId { get; set; }
        public byte BlueprintCopyNum { get; set; }
    }
}
