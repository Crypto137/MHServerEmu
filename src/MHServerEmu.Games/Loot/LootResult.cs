using System.Runtime.InteropServices;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Items;

namespace MHServerEmu.Games.Loot
{
    /// <summary>
    /// A container for various types of loot.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct LootResult
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        [FieldOffset(0)]
        private readonly LootType _type = default;

        [FieldOffset(4)]
        private readonly int _amount = default;

        // Reference types (need to be at offset 8 for memory alignment reasons)
        [FieldOffset(8)]
        private readonly ItemSpec _itemSpec = default;

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

        public LootResult(LootType type, int amount)
        {
            switch (type)
            {
                case LootType.PowerPoints:
                case LootType.Credits:
                case LootType.EnduranceBonus:
                case LootType.HealthBonus:
                case LootType.Currency:
                    break;

                default:
                    Logger.Warn($"LootResult(): Unsupported LootType {type} for the amount-based constructor");
                    return;
            }

            if (amount < 0)
            {
                Logger.Warn($"LootResult(): Invalid amount {amount} for LootType {type}");
                return;
            }

            _type = type;
            _amount = amount;
        }
    }
}
