using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Loot
{
    /// <summary>
    /// A basic implementation of <see cref="IItemResolver"/>.
    /// </summary>
    public class ItemResolver : IItemResolver
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly List<PrototypeId> _pendingItemList = new();
        private readonly List<PrototypeId> _processedItemList = new();

        public GRandom Random { get; }
        public LootContext LootContext { get; }
        public Player Player { get; }

        public IEnumerable<PrototypeId> ProcessedItems { get => _processedItemList; }
        public int ProcessedItemCount { get => _processedItemList.Count; }

        public ItemResolver(GRandom random, LootContext lootContext, Player player)
        {
            Random = random;
            LootContext = lootContext;
            Player = player;
        }

        public LootRollResult PushItem(in DropFilterArguments dropFilterArgs, RestrictionTestFlags restrictionTestFlags, int stackCount, IEnumerable<LootMutationPrototype> mutations)
        {
            _pendingItemList.Add(dropFilterArgs.ItemProto.DataRef);
            return LootRollResult.Success;
        }

        public LootRollResult PushCurrency(WorldEntityPrototype worldEntityProto, in DropFilterArguments dropFilterArgs, RestrictionTestFlags restrictionTestFlags, LootDropChanceModifiers dropChanceModifiers, int stackCount)
        {
            //Logger.Debug($"PushCurrency(): {worldEntityProto}");
            return LootRollResult.NoRoll;
        }

        public void PushLootNodeCallback()
        {
            Logger.Debug($"PushLootNodeCallback()");
        }

        public void PushCraftingCallback()
        {
            Logger.Debug($"PushCraftingCallback()");
        }

        public int ResolveLevel(int level, bool useLevelVerbatim)
        {
            if (useLevelVerbatim)
                return level;

            return 1;
        }

        public AvatarPrototype ResolveAvatarPrototype(AvatarPrototype usableAvatarProto, bool hasUsableOverride, float usableOverrideValue)
        {
            return usableAvatarProto;
        }

        public AgentPrototype ResolveTeamUpPrototype(AgentPrototype usableTeamUpProto, float usableOverrideValue)
        {
            return usableTeamUpProto;
        }

        public PrototypeId ResolveRarity(HashSet<PrototypeId> rarities, int level, ItemPrototype itemProto)
        {
            return GameDatabase.LootGlobalsPrototype.RarityDefault;
        }

        public bool CheckDropPercent(LootRollSettings settings, float noDropPercent)
        {
            return Random.NextFloat() < 1f - noDropPercent;
        }

        public bool CheckItem(in DropFilterArguments dropFilterArgs, RestrictionTestFlags restrictionTestFlags, bool arg2)
        {
            ItemPrototype itemProto = dropFilterArgs.ItemProto as ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(false, $"CheckItem(): itemProto == null");

            if (itemProto.ApprovedForUse() == false)
                return false;

            if (itemProto.IsDroppableForRestrictions(dropFilterArgs, restrictionTestFlags) == false)
                return false;

            return true;
        }

        public void ClearPending()
        {
            _pendingItemList.Clear();
        }

        public bool ProcessPending(LootRollSettings settings)
        {
            foreach (PrototypeId itemProtoRef in _pendingItemList)
            {
                Logger.Debug($"ProcessPending(): {itemProtoRef.GetName()}");
                _processedItemList.Add(itemProtoRef);
            }

            _pendingItemList.Clear();
            return true;
        }
    }
}
