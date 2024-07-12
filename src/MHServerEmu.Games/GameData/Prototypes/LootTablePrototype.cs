using Gazillion;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.LiveTuning;
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

        public virtual void OnResultsEvaluation(Player player, WorldEntity worldEntity)
        {

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
        public short NumMin { get; protected set; }
        public short NumMax { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            if (NumMin < 0) NumMin = 0;
            if (NumMax < NumMin) NumMax = NumMin;
        }

        public LootRollResult RollItem(ItemPrototype itemProto, LootRollSettings settings, IItemResolver resolver, IEnumerable<LootMutationPrototype> mutations)
        {
            return LootRollResult.NoRoll;
        }

        protected override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            return LootRollResult.NoRoll;
        }
    }

    public class LootTablePrototype : LootDropPrototype
    {
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

        public LootRollResult RollLootTable(LootRollResult settings, IItemResolver resolver)
        {
            // NOTE: This is renamed from LootTablePrototype::Roll() to avoid confusion with the inherited LootNodePrototype::roll()
            return LootRollResult.NoRoll;
        }

        protected override LootRollResult Select(LootRollSettings settings, IItemResolver resolver)
        {
            return base.Select(settings, resolver);
        }

        protected override LootRollResult Roll(LootRollSettings settings, IItemResolver resolver)
        {
            return base.Roll(settings, resolver);
        }

        protected override int GetWeight()
        {
            return base.GetWeight();
        }

        private LootRollResult PickWeight()
        {
            return LootRollResult.NoRoll;
        }

        private LootRollResult PickWeightTryAll()
        {
            return LootRollResult.NoRoll;
        }

        private LootRollResult PickAll()
        {
            return LootRollResult.NoRoll;
        }

        private LootRollResult PickLiveTuningNodes()
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
