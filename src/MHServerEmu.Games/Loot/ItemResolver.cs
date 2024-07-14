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

        public GRandom Random { get; }
        public LootContext LootContext { get; }
        public Player Player { get; }

        public ItemResolver(GRandom random, LootContext lootContext, Player player)
        {
            Random = random;
            LootContext = lootContext;
            Player = player;
        }

        public LootRollResult PushItem(DropFilterArguments dropFilterArgs, RestrictionTestFlags restrictionTestFlags, int stackCount, IEnumerable<LootMutationPrototype> mutations)
        {
            Logger.Debug($"PushItem(): {dropFilterArgs.ItemProto}");
            return LootRollResult.NoRoll;
        }

        public LootRollResult PushCurrency(WorldEntityPrototype worldEntityProto, DropFilterArguments dropFilterArgs, RestrictionTestFlags restrictionTestFlags, LootDropChanceModifiers dropChanceModifiers, int stackCount)
        {
            Logger.Debug($"PushCurrency(): {worldEntityProto}");
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

        public void Fail()
        {

        }

        public bool Resolve(LootRollSettings settings)
        {
            return true;
        }
    }
}
