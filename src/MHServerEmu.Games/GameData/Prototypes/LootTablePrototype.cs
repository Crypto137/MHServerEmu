using Gazillion;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.GameData.Tables;
using MHServerEmu.Games.Loot;

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

        public virtual bool OnResultsEvaluation(Player player, WorldEntity worldEntity)
        {
            return true;
        }

        public virtual void Visit(LootTableNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        protected virtual LootRollResult Select(LootRollSettings settings, IItemResolver resolver)
        {
            return LootRollResult.NoRoll;
        }

        protected virtual LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            return LootRollResult.NoRoll;
        }

        protected virtual int GetWeight()
        {
            return Weight;
        }

        protected LootRollResult PushLootNodeCallback(LootRollSettings settings, IItemResolver resolver)
        {
            return LootRollResult.NoRoll;
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

        public LootRollResult RollItem(ItemPrototype itemProto, int numItems, LootRollSettings settings, IItemResolver resolver, IEnumerable<LootMutationPrototype> mutations)
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

            RestrictionTestFlags restrictionTestFlags = RestrictionTestFlags.All;
            if (settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.IgnoreCooldown) || settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.PreviewOnly))
                restrictionTestFlags &= ~RestrictionTestFlags.Cooldown;

            int stackSize = 1;
            if (itemProto.StackSettings != null)
                stackSize = Math.Min(numItems, itemProto.StackSettings.MaxStacks);

            int rolled = 0;
            AvatarPrototype currentPickerAvatarProto = null;

            while (rolled < numItems)
            {
                int level = resolver.ResolveLevel(settings.Level, settings.UseLevelVerbatim);
                AvatarPrototype resolvedAvatarProto = resolver.ResolveAvatarPrototype(usableAvatarProto, settings.HasUsableOverride, settings.UsableOverrideValue);
                AgentPrototype resolvedTeamUpProto = resolver.ResolveTeamUpPrototype(usableTeamUpProto, settings.UsableOverrideValue);
                PrototypeId rollFor = resolvedAvatarProto != null ? resolvedAvatarProto.DataRef : PrototypeId.Invalid;
                PrototypeId rarity = resolver.ResolveRarity(settings.Rarities, level, isAbstract ? null : itemProto);

                // We must have a valid rarity ref
                if (rarity == PrototypeId.Invalid)
                {
                    resolver.Fail();
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

                    DropFilterArguments pickDropFilterArgs = new(null, rollFor, level, rarity, 0, slot, resolver.LootContext);
                    pickDropFilterArgs.DropDistanceThresholdSq = settings.DropDistanceThresholdSq;

                    if (picker.Empty() ||
                        LootUtilities.PickValidItem(resolver, picker, resolvedTeamUpProto, in pickDropFilterArgs, ref pickedItemProto, restrictionTestFlags, rarity) == false)
                    {
                        resolver.Fail();
                        return LootRollResult.Failure;
                    }
                }
                else
                {
                    // Use the item prototype we were provided if it's non-abstract
                    pickedItemProto = itemProto;
                }

                DropFilterArguments pushDropFilterArgs = new(pickedItemProto, rollFor, level, rarity, 0, slot, resolver.LootContext);
                pushDropFilterArgs.DropDistanceThresholdSq = settings.DropDistanceThresholdSq;

                if (pickedItemProto.IsCurrency)
                {
                    result |= resolver.PushCurrency(pickedItemProto, pushDropFilterArgs, restrictionTestFlags, settings.DropChanceModifiers, stackCount);
                }
                else
                {
                    // TODO: Costume rolling for costume closet (consoles / 1.53)
                    result |= resolver.PushItem(pushDropFilterArgs, restrictionTestFlags, stackCount, mutations);
                }

                // Stop rolling if something went wrong
                if (result.HasFlag(LootRollResult.Failure))
                {
                    resolver.Fail();
                    return LootRollResult.Failure;
                }

                rolled += stackSize;
            }

            return resolver.Resolve(settings) ? result : LootRollResult.Failure;
        }

        protected override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
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

        public bool IsLiveTuningEnabled()
        {
            int tuningVar = (int)Math.Floor(LiveTuningManager.GetLiveLootTableTuningVar(this, LootTableTuningVar.eLTTV_Enabled));

            return tuningVar switch
            {
                0 => false,
                1 => LiveTuningDefaultEnabled,
                2 => true,
                _ => true,
            };
        }

        public LootRollResult RollLootTable(LootRollSettings settings, IItemResolver resolver)
        {
            // NOTE: This is renamed from LootTablePrototype::Roll() to avoid confusion with the inherited LootNodePrototype::roll()
            settings.Depth = 1;
            return Select(settings, resolver);
        }

        protected override LootRollResult Select(LootRollSettings settings, IItemResolver resolver)
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

        protected override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
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
            if (resolver.CheckNoDropPercent(settings, noDropPercent) == false)
                return resolver.Resolve(settings) ? LootRollResult.Success : LootRollResult.Failure;

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

        protected override int GetWeight()
        {
            float weightLiveTuningVar = LiveTuningManager.GetLiveLootTableTuningVar(this, LootTableTuningVar.eLTTV_Weight);
            return (int)(GetWeight() * weightLiveTuningVar);
        }

        private LootRollResult PickWeight(LootRollSettings settings, IItemResolver resolver)
        {
            return LootRollResult.NoRoll;
        }

        private LootRollResult PickWeightTryAll(LootRollSettings settings, IItemResolver resolver)
        {
            return LootRollResult.NoRoll;
        }

        private LootRollResult PickAll(LootRollSettings settings, IItemResolver resolver)
        {
            return LootRollResult.NoRoll;
        }

        private LootRollResult PickLiveTuningNodes(LootRollSettings settings, IItemResolver resolver)
        {
            return LootRollResult.NoRoll;
        }
    }

    public class LootTableAssignmentPrototype : Prototype
    {
        public AssetId Name { get; protected set; }
        public LootDropEventType Event { get; protected set; }
        public PrototypeId Table { get; protected set; }
    }
}
