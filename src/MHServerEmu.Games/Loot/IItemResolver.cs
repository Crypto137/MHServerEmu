using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Loot
{
    /// <summary>
    /// An interface for a class that provides context for rolling loot tables and stores intermediary results.
    /// </summary>
    /// <remarks>
    /// Although it is called an "item" resolver, it actually does the rolling for all loot types.
    /// </remarks>
    public interface IItemResolver
    {
        public GRandom Random { get; }
        public LootContext LootContext { get; }
        public Player Player { get; }
        public Region Region { get; }

        public LootRollResult PushItem(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags, int stackCount, IEnumerable<LootMutationPrototype> mutations);
        public LootRollResult PushCurrency(WorldEntityPrototype worldEntityProto, DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags, LootDropChanceModifiers dropChanceModifiers, int stackCount);
        public LootRollResult PushAgent(PrototypeId agentProtoRef, int level, RestrictionTestFlags restrictionFlags);
        public LootRollResult PushCredits(int amount);
        public LootRollResult PushPowerPoints(int amount);
        public LootRollResult PushHealthBonus(int amount);
        public LootRollResult PushEnduranceBonus(int amount);
        public LootRollResult PushXP(CurveId curveRef, int amount);
        public LootRollResult PushLootNodeCallback();
        public LootRollResult PushCraftingCallback();

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
