using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Items;
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
        public LootResolverFlags Flags { get; }

        public LootContext LootContext { get; }
        public LootContext LootContextOverride { get; set; }
        public Player Player { get; }
        public Region Region { get; }

        #region Push Functions

        /// <summary>
        /// Pushes the result of rolling a <see cref="LootDropItemPrototype"/> or a <see cref="LootDropItemFilterPrototype"/> to this <see cref="IItemResolver"/>.
        /// </summary>
        public LootRollResult PushItem(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags, int stackCount, LootMutationPrototype[] mutations);

        /// <summary>
        /// Pushes the result of rolling a <see cref="LootDropClonePrototype"/> to this <see cref="IItemResolver"/>.
        /// </summary>
        public LootRollResult PushClone(LootCloneRecord cloneRecord);

        /// <summary>
        /// Pushes the result of rolling a <see cref="LootDropAgentPrototype"/> to this <see cref="IItemResolver"/>.
        /// </summary>
        public LootRollResult PushAgent(PrototypeId agentProtoRef, int level, RestrictionTestFlags restrictionFlags);

        /// <summary>
        /// Pushes the result of rolling a <see cref="LootDropCreditsPrototype"/> to this <see cref="IItemResolver"/>.
        /// </summary>
        public LootRollResult PushCredits(int amount);

        /// <summary>
        /// Pushes the result of rolling a <see cref="LootDropXPPrototype"/> to this <see cref="IItemResolver"/>.
        /// </summary>
        public LootRollResult PushXP(CurveId curveRef, int amount);

        /// <summary>
        /// Pushes the result of rolling a <see cref="LootDropPowerPointsPrototype"/> to this <see cref="IItemResolver"/>.
        /// </summary>
        public LootRollResult PushPowerPoints(int amount);

        /// <summary>
        /// Pushes the result of rolling a <see cref="LootDropHealthBonusPrototype"/> to this <see cref="IItemResolver"/>.
        /// </summary>
        public LootRollResult PushHealthBonus(int amount);

        /// <summary>
        /// Pushes the result of rolling a <see cref="LootDropEnduranceBonusPrototype"/> to this <see cref="IItemResolver"/>.
        /// </summary>
        public LootRollResult PushEnduranceBonus(int amount);

        /// <summary>
        /// Pushes the result of rolling a <see cref="LootDropRealMoneyPrototype"/> to this <see cref="IItemResolver"/>.
        /// </summary>
        /// <remarks>
        /// This loot drop type appears to had been used only for the Vibranium Ticket promotion during the game's second anniversary.
        /// </remarks>
        public LootRollResult PushRealMoney(LootDropRealMoneyPrototype lootDropRealMoneyProto);

        /// <summary>
        /// Pushes the result of rolling a callback loot node to this <see cref="IItemResolver"/>.
        /// </summary>
        /// <remarks>
        /// Callback nodes are <see cref="LootDropBannerMessagePrototype"/>, <see cref="LootDropUsePowerPrototype"/>,
        /// <see cref="LootDropPlayVisualEffectPrototype"/>, and <see cref="LootDropChatMessagePrototype"/>.
        /// </remarks>
        public LootRollResult PushLootNodeCallback(LootNodePrototype lootNodeProto);

        /// <summary>
        /// Pushes the result of a loot mutation (crafting) to this <see cref="IItemResolver"/>.
        /// </summary>
        public LootRollResult PushCraftingCallback(LootMutationPrototype lootMutationProto);

        /// <summary>
        /// Pushes the result of rolling a <see cref="LootDropVanityTitlePrototype"/> to this <see cref="IItemResolver"/>.
        /// </summary>
        public LootRollResult PushVanityTitle(PrototypeId vanityTitleProtoRef);

        /// <summary>
        /// Pushes the result of rollign a <see cref="LootDropVendorXPPrototype"/> to this <see cref="IItemResolver"/>.
        /// </summary>
        public LootRollResult PushVendorXP(PrototypeId vendorProtoRef, int xpAmount);

        /// <summary>
        /// Pushes the result of rolling a <see cref="LootDropItemPrototype"/> or <see cref="LootDropAgentPrototype"/> representing a currency to this <see cref="IItemResolver"/>.
        /// </summary>
        public LootRollResult PushCurrency(WorldEntityPrototype worldEntityProto, DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags, LootDropChanceModifiers dropChanceModifiers, int stackCount);

        #endregion

        #region Resolving

        /// <summary>
        /// Determines the level of a drop.
        /// </summary>
        public int ResolveLevel(int level, bool useLevelVerbatim);

        /// <summary>
        /// Determines the <see cref="AvatarPrototype"/> a drop should be usable by.
        /// </summary>
        public AvatarPrototype ResolveAvatarPrototype(AvatarPrototype usableAvatarProto, bool forceUsable, float usablePercent);

        /// <summary>
        /// Determines the team-up <see cref="AgentPrototype"/> a drop should be usable by.
        /// </summary>
        public AgentPrototype ResolveTeamUpPrototype(AgentPrototype usableTeamUpProto, float usablePercent);

        /// <summary>
        /// Determines the rarity of a drop.
        /// </summary>
        public PrototypeId ResolveRarity(HashSet<PrototypeId> rarities, int level, ItemPrototype itemProto);

        /// <summary>
        /// Returns <see langword="true"/> if something should drop.
        /// </summary>
        public bool CheckDropChance(LootRollSettings settings, float noDropPercent);

        /// <summary>
        /// Returns <see langword="true"/> if the specified drop is on cooldown.
        /// </summary>
        public bool CheckDropCooldown(PrototypeId dropProtoRef, int count);

        /// <summary>
        /// Returns <see langword="true"/> if an item is allowed to drop given the specified filters.
        /// </summary>
        public bool CheckItem(DropFilterArguments filterArgs, RestrictionTestFlags restrictionFlags, bool arg2 = false, int amount = 1);

        /// <summary>
        /// Returns <see langword="true"/> if an agent is allowed to drop given the specified filters.
        /// </summary>
        public bool CheckAgent(PrototypeId agentProtoRef, RestrictionTestFlags restrictionFlags);

        #endregion

        #region Clone Source Management

        /// <summary>
        /// Initializes the provided <see cref="LootCloneRecord"/> using the clone source with the specified index.
        /// Returns <see langword="true"/> if successful.
        /// </summary>
        public bool InitializeCloneRecordFromSource(int index, LootCloneRecord lootCloneRecord);

        /// <summary>
        /// Sets the clone source for the specified index.
        /// </summary>
        public void SetCloneSource(int index, ItemSpec itemSpec);

        #endregion

        #region Pending Item Processing

        /// <summary>
        /// Clears pending data pushed to this <see cref="IItemResolver"/>.
        /// </summary>
        public void ClearPending();

        /// <summary>
        /// Processes pending data pushed to this <see cref="IItemResolver"/>.
        /// </summary>
        public bool ProcessPending(LootRollSettings settings);

        #endregion
    }
}
