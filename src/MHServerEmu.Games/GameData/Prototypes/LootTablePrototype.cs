using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.Loot;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums



    #endregion

    public class LootNodePrototype : Prototype
    {
        public short Weight { get; protected set; }
        public LootRollModifierPrototype[] Modifiers { get; protected set; }
    }

    public class LootDropPrototype : LootNodePrototype
    {
        public short NumMin { get; protected set; }
        public short NumMax { get; protected set; }
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
    }

    public class LootTableAssignmentPrototype : Prototype
    {
        public AssetId Name { get; protected set; }
        public LootDropEventType Event { get; protected set; }
        public PrototypeId Table { get; protected set; }
    }
}
