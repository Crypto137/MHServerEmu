using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Loot
{
    /// <summary>
    /// An interface for a class that does the rolling ("resolves") on loot tables.
    /// </summary>
    public interface IItemResolver
    {
        public GRandom Random { get; }
        public LootContext LootContext { get; }
        public Player Player { get; }
        public Region Region { get; }

        public LootRollResult PushItem(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags,
            int stackCount, IEnumerable<LootMutationPrototype> mutations);
        public LootRollResult PushCurrency(WorldEntityPrototype worldEntityProto, DropFilterArguments filterArgs,
            RestrictionTestFlags restrictionFlags, LootDropChanceModifiers dropChanceModifiers, int stackCount);
        public LootRollResult PushAgent(PrototypeId agentProtoRef, int level, RestrictionTestFlags restrictionFlags);
        public LootRollResult PushCredits(int amount);

        public void PushLootNodeCallback();
        public void PushCraftingCallback();

        public int ResolveLevel(int level, bool useLevelVerbatim);
        public AvatarPrototype ResolveAvatarPrototype(AvatarPrototype usableAvatarProto, bool forceUsable, float usablePercent);
        public AgentPrototype ResolveTeamUpPrototype(AgentPrototype usableTeamUpProto, float usablePercent);
        public PrototypeId ResolveRarity(HashSet<PrototypeId> rarities, int level, ItemPrototype itemProto);
        public bool CheckDropPercent(LootRollSettings settings, float noDropPercent);
        public bool CheckItem(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags, bool arg2);

        public void ClearPending();
        public bool ProcessPending(LootRollSettings settings);
    }
}
