using MHServerEmu.Core.Memory;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Loot
{
    public class DropFilterArguments : IPoolable, IDisposable
    {
        public LootContext LootContext { get; private set; }

        public Prototype ItemProto { get; set; }
        public PrototypeId RollFor { get; set; }
        public int Level { get; set; }
        public PrototypeId Rarity { get; set; }
        public int Rank { get; set; }
        public EquipmentInvUISlot Slot { get; set; }
        public float DropDistanceSq { get; set; }

        public bool IsInPool { get; set; }

        public DropFilterArguments() { }    // Use pooling instead of calling this directly

        public static void Initialize(DropFilterArguments args, LootContext lootContext)
        {
            // NOTE: This is a replacement for a constructor to work with pooling
            args.LootContext = lootContext;

            args.ItemProto = default;
            args.RollFor = default;
            args.Level = default;
            args.Rarity = default;
            args.Rank = default;
            args.Slot = default;

            args.DropDistanceSq = default;
        }

        public static void Initialize(DropFilterArguments args, Prototype itemProto, PrototypeId rollFor, int level,
            PrototypeId rarityProtoRef, int rank, EquipmentInvUISlot slot, LootContext lootContext)
        {
            // NOTE: This is a replacement for a constructor to work with pooling
            args.LootContext = lootContext;

            args.ItemProto = itemProto;
            args.RollFor = rollFor;
            args.Level = level;
            args.Rarity = rarityProtoRef;
            args.Rank = rank;
            args.Slot = slot;

            args.DropDistanceSq = default;
        }

        public static void Initialize(DropFilterArguments args, DropFilterArguments other)
        {
            // NOTE: This is a replacement for a constructor to work with pooling
            args.LootContext = other.LootContext;

            args.ItemProto = other.ItemProto;
            args.RollFor = other.RollFor;
            args.Level = other.Level;
            args.Rarity = other.Rarity;
            args.Rank = other.Rank;
            args.Slot = other.Slot;

            args.DropDistanceSq = other.DropDistanceSq;
        }

        public override string ToString()
        {
            return string.Format("Item: {0} Context: {1} Level: {2} Rarity: {3} Rank: {4} Slot: {5} RollFor: {6}",
                ItemProto,
                LootContext,
                Level,
                GameDatabase.GetFormattedPrototypeName(Rarity),
                Rank,
                Slot,
                RollFor.GetName());
        }

        public virtual void ResetForPool()
        {
            LootContext = default;

            ItemProto = default;
            RollFor = default;
            Level = default;
            Rarity = default;
            Rank = default;
            Slot = default;

            DropDistanceSq = default;
        }

        public virtual void Dispose()
        {
            ObjectPoolManager.Instance.Return(this);
        }
    }
}
