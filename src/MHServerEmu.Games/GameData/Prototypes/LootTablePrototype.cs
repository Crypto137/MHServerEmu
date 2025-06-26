using Gazillion;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.GameData.Tables;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Loot.Visitors;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class LootNodePrototype : Prototype
    {
        public short Weight { get; protected set; }
        public LootRollModifierPrototype[] Modifiers { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Weight = Math.Max((short)0, Weight);
        }

        public virtual void OnResultsEvaluation(Player player, WorldEntity dropper)
        {
        }

        public virtual void Visit<T>(ref T visitor) where T: ILootTableNodeVisitor
        {
            visitor.Visit(this);
        }

        // NOTE: Select() / Roll() / GetWeight() need to be protected internal because they are accessed from derived loot node types (Drop and Table)

        protected internal virtual LootRollResult Select(LootRollSettings settings, IItemResolver resolver)
        {
            // TODO: Secondary avatar for coop

            // Do a modified roll
            if (Modifiers.HasValue())
            {
                using LootRollSettings modifiedSettings = ObjectPoolManager.Instance.Get<LootRollSettings>();
                modifiedSettings.Set(settings);

                foreach (LootRollModifierPrototype modifier in Modifiers)
                    modifier.Apply(modifiedSettings);

                if (modifiedSettings.IsRestrictedByLootDropChanceModifier())
                    return LootRollResult.Failure;

                return Roll(modifiedSettings, resolver);
            }

            // Do a non-modified roll
            return Roll(settings, resolver);
        }

        protected internal virtual LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            return LootRollResult.NoRoll;
        }

        protected internal virtual int GetWeight()
        {
            return Weight;
        }

        protected LootRollResult PushLootNodeCallback(LootRollSettings settings, IItemResolver resolver)
        {
            LootRollResult result = resolver.PushLootNodeCallback(this);
            if (result.HasFlag(LootRollResult.Failure))
            {
                resolver.ClearPending();
                return LootRollResult.Failure;
            }

            return resolver.ProcessPending(settings) ? result : LootRollResult.Failure;
        }
    }

    public class LootDropPrototype : LootNodePrototype
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public short NumMin { get; protected set; }
        public short NumMax { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            if (NumMin < 0) NumMin = 0;
            if (NumMax < NumMin) NumMax = NumMin;
        }

        public LootRollResult RollItem(ItemPrototype itemProto, int numItems, LootRollSettings settings, IItemResolver resolver, LootMutationPrototype[] mutations)
        {
            LootRollResult result = LootRollResult.NoRoll;

            if (numItems < 1)
                return result;

            if (resolver.LootContext == LootContext.MysteryChest && (itemProto is CostumePrototype || itemProto is CharacterTokenPrototype))
                Logger.Info($"RollItem(): Item: {itemProto} Player: {resolver.Player}");

            bool isAbstract = DataDirectory.Instance.PrototypeIsAbstract(itemProto.DataRef);
            AvatarPrototype usableAvatarProto = settings.UsableAvatar;
            AgentPrototype usableTeamUpProto = settings.UsableTeamUp;

            Picker<Prototype> picker = new(resolver.Random);

            RestrictionTestFlags restrictionFlags = RestrictionTestFlags.All;
            if (settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.IgnoreCooldown) ||
                settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.PreviewOnly))
            {
                restrictionFlags &= ~RestrictionTestFlags.Cooldown;
            }

            int stackSize = 1;
            if (itemProto.StackSettings != null)
                stackSize = Math.Min(numItems, itemProto.StackSettings.MaxStacks);

            int rolled = 0;
            AvatarPrototype currentPickerAvatarProto = null;

            while (rolled < numItems)
            {
                int level = resolver.ResolveLevel(settings.Level, settings.UseLevelVerbatim);
                AvatarPrototype resolvedAvatarProto = resolver.ResolveAvatarPrototype(usableAvatarProto, settings.ForceUsable, settings.UsablePercent);
                AgentPrototype resolvedTeamUpProto = resolver.ResolveTeamUpPrototype(usableTeamUpProto, settings.UsablePercent);
                PrototypeId rollFor = resolvedAvatarProto != null ? resolvedAvatarProto.DataRef : PrototypeId.Invalid;
                PrototypeId? rarityProtoRef = resolver.ResolveRarity(settings.Rarities, level, isAbstract ? null : itemProto);

                // We must have a valid rarity ref
                if (rarityProtoRef == PrototypeId.Invalid)
                {
                    resolver.ClearPending();
                    return LootRollResult.Failure;
                }

                int stackCount = Math.Min(numItems - rolled, stackSize);
                EquipmentInvUISlot slot = itemProto.GetInventorySlotForAgent(resolvedAvatarProto);

                ItemPrototype pickedItemProto = null;
                if (isAbstract)
                {
                    // Pick a random item prototype that uses the provided abstract prototype
                    if (currentPickerAvatarProto != resolvedAvatarProto)
                    {
                        // Refill the picker if we are rolling for a different avatar now
                        picker.Clear();
                        GameDataTables.Instance.LootPickingTable.GetConcreteLootPicker(picker, itemProto.DataRef, resolvedAvatarProto);
                        currentPickerAvatarProto = resolvedAvatarProto;
                    }

                    using DropFilterArguments pickFilterArgs = ObjectPoolManager.Instance.Get<DropFilterArguments>();
                    DropFilterArguments.Initialize(pickFilterArgs, null, rollFor, level, rarityProtoRef.Value, 0, slot, resolver.LootContext);
                    pickFilterArgs.DropDistanceSq = settings.DropDistanceSq;

                    if (picker.Empty() ||
                        LootUtilities.PickValidItem(resolver, picker, resolvedTeamUpProto, pickFilterArgs, ref pickedItemProto, restrictionFlags, ref rarityProtoRef) == false)
                    {
                        resolver.ClearPending();
                        return LootRollResult.Failure;
                    }
                }
                else
                {
                    // Use the item prototype we were provided if it's non-abstract
                    pickedItemProto = itemProto;
                }

                using DropFilterArguments pushFilterArgs = ObjectPoolManager.Instance.Get<DropFilterArguments>();
                DropFilterArguments.Initialize(pushFilterArgs, pickedItemProto, rollFor, level, rarityProtoRef.Value, 0, slot, resolver.LootContext);
                pushFilterArgs.DropDistanceSq = settings.DropDistanceSq;

                if (pickedItemProto.IsCurrency)
                {
                    result |= resolver.PushCurrency(pickedItemProto, pushFilterArgs, restrictionFlags, settings.DropChanceModifiers, stackCount);
                }
                else
                {
                    // TODO: Costume rolling for costume closet (consoles / 1.53)
                    result |= resolver.PushItem(pushFilterArgs, restrictionFlags, stackCount, mutations);
                }

                // Stop rolling if something went wrong
                if (result.HasFlag(LootRollResult.Failure))
                {
                    resolver.ClearPending();
                    return LootRollResult.Failure;
                }

                rolled += stackSize;
            }

            return resolver.ProcessPending(settings) ? result : LootRollResult.Failure;
        }

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            Logger.Warn($"Roll(): Unimplemented drop type {GetType().Name}");
            return LootRollResult.NoRoll;
        }
    }

    public class LootTablePrototype : LootDropPrototype
    {
        public const int MaxLootTreeDepth = 50;
        private static readonly Logger Logger = LogManager.CreateLogger();

        public PickMethod PickMethod { get; protected set; }
        public float NoDropPercent { get; protected set; }
        public LootNodePrototype[] Choices { get; protected set; }
        public LocaleStringId MissionLogRewardsText { get; protected set; }
        public bool LiveTuningDefaultEnabled { get; protected set; }

        [DoNotCopy]
        public int LootTablePrototypeEnumValue { get; private set; }

        public override void PostProcess()
        {
            base.PostProcess();

            NoDropPercent = Math.Clamp(NoDropPercent, 0f, 1f);

            LootTablePrototypeEnumValue = GetEnumValueFromBlueprint(LiveTuningData.GetLootTableBlueprintDataRef());
        }

        public override void Visit<T>(ref T visitor)
        {
            base.Visit(ref visitor);

            if (Choices.IsNullOrEmpty())
                return;

            foreach (LootNodePrototype node in Choices)
                node.Visit(ref visitor);
        }

        public bool IsLiveTuningEnabled()
        {
            int tuningVar = (int)Math.Floor(LiveTuningManager.GetLiveLootTableTuningVar(this, LootTableTuningVar.eLTTV_Enabled));

            if (tuningVar == 0)
                return false;

            if (tuningVar == 1)
                return LiveTuningDefaultEnabled;

            return true;
        }

        public LootRollResult RollLootTable(LootRollSettings settings, IItemResolver resolver)
        {
            // NOTE: This is renamed from LootTablePrototype::Roll() to avoid confusion with the inherited LootNodePrototype::roll()
            settings.Depth = 1;
            return Select(settings, resolver);
        }

        protected internal override LootRollResult Select(LootRollSettings settings, IItemResolver resolver)
        {
            if (IsLiveTuningEnabled() == false)
                return LootRollResult.NoRoll;

            // Determine the number of rolls based on live tuning
            int numRolls = (int)Math.Floor(LiveTuningManager.GetLiveLootTableTuningVar(this, LootTableTuningVar.eLTTV_Rolls));
            numRolls = Math.Max(numRolls, 1);

            LootRollResult result = LootRollResult.NoRoll;
            for (int i = 0; i < numRolls; i++)
                result |= base.Select(settings, resolver);

            return result;
        }

        protected internal override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            if (NumMin < 1 || Choices.IsNullOrEmpty())
                return LootRollResult.NoRoll;

            if (settings.Depth > MaxLootTreeDepth)
                return Logger.WarnReturn(LootRollResult.Failure, $"Roll(): Loot Table infinite recursion check failed, max depth of {MaxLootTreeDepth} exceeded [{this}]");

            // Use prototype value if live tuning no drop percent is set to 1f, otherwise use the no drop percent from tuning
            float noDropPercent = MathF.Max(LiveTuningManager.GetLiveLootTableTuningVar(this, LootTableTuningVar.eLTTV_NoDropPercent), 0f);
            if (Segment.EpsilonTest(noDropPercent, 1f))
                noDropPercent = NoDropPercent;

            // Cancel roll if no roll percent check fails
            if (resolver.CheckDropChance(settings, noDropPercent) == false)
                return resolver.ProcessPending(settings) ? LootRollResult.Success : LootRollResult.Failure;

            settings.Depth++;

            LootRollResult result;
            switch (PickMethod)
            {
                case PickMethod.PickWeight:
                    result = PickWeight(settings, resolver);
                    break;

                case PickMethod.PickWeightTryAll:
                    result = PickWeightTryAll(settings, resolver);
                    break;

                case PickMethod.PickAll:
                    result = PickAll(settings, resolver);
                    break;

                default:
                    return Logger.WarnReturn(LootRollResult.NoRoll, $"Roll(): Unknown pick method in table [{this}]");
            }

            PickLiveTuningNodes(settings, resolver);
            settings.Depth--;

            return result;
        }

        protected internal override int GetWeight()
        {
            float weightLiveTuningVar = LiveTuningManager.GetLiveLootTableTuningVar(this, LootTableTuningVar.eLTTV_Weight);
            return (int)(base.GetWeight() * weightLiveTuningVar);
        }

        private LootRollResult PickWeight(LootRollSettings settings, IItemResolver resolver)
        {
            if (Choices.IsNullOrEmpty())
                return Logger.WarnReturn(LootRollResult.NoRoll, $"PickWeight(): LootTable with no choices!\n Loot Table: {this}");

            // Create a picker of possible nodes
            Picker<LootNodePrototype> nodePicker = new(resolver.Random);
            foreach (LootNodePrototype proto in Choices)
                nodePicker.Add(proto, proto.GetWeight());

            int numPicks = NumMin == NumMax ? NumMin : resolver.Random.Next(NumMin, NumMax + 1);

            LootRollResult result = LootRollResult.NoRoll;
            for (int i = 0; i < numPicks; i++)
            {
                if (nodePicker.Pick(out LootNodePrototype node))
                    result |= node.Select(settings, resolver);
            }

            return result;
        }

        private LootRollResult PickWeightTryAll(LootRollSettings settings, IItemResolver resolver)
        {
            // Create a picker of possible nodes.
            // NOTE: Same as the client, we use the Weight prototype field instead of the GetWeight() method.
            // Because of this, PickWeightTryAll is not affected by live tuning.
            Picker<LootNodePrototype> nodePicker = new(resolver.Random);
            foreach (LootNodePrototype proto in Choices)
                nodePicker.Add(proto, Weight);

            int numPicks = NumMin == NumMax ? NumMin : resolver.Random.Next(NumMin, NumMax + 1);

            LootRollResult result = LootRollResult.NoRoll;
            for (int i = 0; i < numPicks; i++)
            {
                Picker<LootNodePrototype> removePicker = new(nodePicker);

                LootRollResult nodeResult = LootRollResult.NoRoll;
                while (nodeResult.HasFlag(LootRollResult.Success) == false && removePicker.PickRemove(out LootNodePrototype node))
                    nodeResult |= node.Select(settings, resolver);

                result |= nodeResult;
            }

            return result;
        }

        private LootRollResult PickAll(LootRollSettings settings, IItemResolver resolver)
        {
            int numPicks = NumMin == NumMax ? NumMin : resolver.Random.Next(NumMin, NumMax + 1);

            LootRollResult result = LootRollResult.NoRoll;
            for (int i = 0; i < numPicks; i++)
            {
                foreach(LootNodePrototype node in Choices)
                    result |= node.Select(settings, resolver);
            }

            return result;
        }

        private LootRollResult PickLiveTuningNodes(LootRollSettings settings, IItemResolver resolver)
        {
            int groupNum = (int)LiveTuningManager.GetLiveLootTableTuningVar(this, LootTableTuningVar.eLTTV_GroupNum);
            if (groupNum == LiveTuningData.DefaultTuningVarValue)
                return LootRollResult.NoRoll;

            LiveTuningManager.GetLiveLootGroup(groupNum, out IReadOnlyList<WorldEntityPrototype> lootGroup);
            if (lootGroup.Count == 0)
                return LootRollResult.NoRoll;

            LootRollResult result = LootRollResult.NoRoll;
            for (int i = 0; i < lootGroup.Count; i++)
            {
                WorldEntityPrototype entityProto = lootGroup[i];
                if (entityProto == null)
                {
                    Logger.Warn("PickLiveTuningNodes(): entityProto == null");
                    continue;
                }

                // Check custom drop chance
                float noDropPercent = LiveTuningManager.GetLiveWorldEntityTuningVar(entityProto, WorldEntityTuningVar.eWETV_LootNoDropPercent);
                if (Segment.EpsilonTest(noDropPercent, LiveTuningData.DefaultTuningVarValue) == false && resolver.CheckDropChance(settings, noDropPercent) == false)
                    continue;

                // CUSTOM: Override loot context for live tuning drops to allow costumes to drop
                resolver.LootContextOverride = LootContext.CashShop;

                switch (entityProto)
                {
                    case AgentPrototype agentProto:
                        result |= LootDropAgentPrototype.RollAgent(agentProto, 1, settings, resolver);
                        break;

                    case ItemPrototype itemProto:
                        result |= RollItem(itemProto, 1, settings, resolver, null);
                        break;

                    default:
                        Logger.Warn($"PickLiveTuningNodes(): None ItemPrototype or AgentPrototype being used in a live-tuning roll!\n Prototype: {entityProto}");
                        break;
                }

                resolver.LootContextOverride = LootContext.None;
            }

            return result;
        }
    }

    public class LootTableAssignmentPrototype : Prototype
    {
        public AssetId Name { get; protected set; }
        public LootDropEventType Event { get; protected set; }
        public PrototypeId Table { get; protected set; }
    }
}
