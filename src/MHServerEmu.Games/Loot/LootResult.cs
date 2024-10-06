using System.Runtime.InteropServices;
using MHServerEmu.Games.Entities.Items;

namespace MHServerEmu.Games.Loot
{
    /// <summary>
    /// A container for various types of loot.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct LootResult
    {
        [FieldOffset(0)]
        private readonly LootType _type;

        //[FieldOffset(4)]
        // NOTE: Reference types need to be at offset 8 for memory alignment reasons

        // Reference types
        [FieldOffset(8)]
        private readonly ItemSpec _itemSpec;

        // Value types
        //[FieldOffset(16)]
        // todo

        public LootType Type { get => _type; }
        public ItemSpec ItemSpec { get => _type.HasFlag(LootType.Item) ? _itemSpec : null; }

        public LootResult(ItemSpec itemSpec)
        {
            _type = LootType.Item;
            _itemSpec = itemSpec;
        }
    }
}
