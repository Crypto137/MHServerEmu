using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Powers;

namespace MHServerEmu.Games.Entities
{
    public class Agent : WorldEntity
    {
        public AgentPrototype AgentPrototype { get => EntityPrototype as AgentPrototype; }

        // New
        public Agent(Game game) : base(game) { }

        public override void Initialize(EntitySettings settings)
        {
            base.Initialize(settings);
        }

        public override void OnEnteredWorld(EntitySettings settings)
        {
            base.OnEnteredWorld(settings);
            RegionLocation.Cell.EnemySpawn(); // Calc Enemy
        }

        public override void AppendStartAction(PrototypeId actionsTarget) // TODO rewrite this
        {
            bool startAction = false;

            if (EntityActionComponent != null && EntityActionComponent.ActionTable.TryGetValue(EntitySelectorActionEventType.OnSimulated, out var actionSet))
                startAction = AppendSelectorActions(actionSet);
            if (startAction == false && actionsTarget != PrototypeId.Invalid)
                AppendOnStartActions(actionsTarget);
        }

        private bool AppendStartPower(PrototypeId startPowerRef)
        {
            if (startPowerRef == PrototypeId.Invalid) return false;
            //Console.WriteLine($"[{Id}]{GameDatabase.GetPrototypeName(startPowerRef)}");
            Condition condition = new()
            {
                SerializationFlags = ConditionSerializationFlags.NoCreatorId
                | ConditionSerializationFlags.NoUltimateCreatorId
                | ConditionSerializationFlags.NoConditionPrototypeId
                | ConditionSerializationFlags.HasIndex
                | ConditionSerializationFlags.HasAssetDataRef,
                Id = 1,
                CreatorPowerPrototypeId = startPowerRef
            };
            ConditionCollection.Add(condition);
            PowerCollectionRecord powerCollection = new()
            {
                Flags = PowerCollectionRecordFlags.PowerRefCountIsOne
                | PowerCollectionRecordFlags.PowerRankIsZero
                | PowerCollectionRecordFlags.CombatLevelIsSameAsCharacterLevel
                | PowerCollectionRecordFlags.ItemLevelIsOne
                | PowerCollectionRecordFlags.ItemVariationIsOne,
                PowerPrototypeId = startPowerRef,
                PowerRefCount = 1
            };
            PowerCollection.Add(powerCollection);
            return true;
        }

        public bool AppendOnStartActions(PrototypeId targetRef)
        {
            if (GameDatabase.InteractionManager.GetStartAction(BaseData.PrototypeId, targetRef, out MissionActionEntityPerformPowerPrototype action))
                return AppendStartPower(action.PowerPrototype);
            return false;
        }

        public bool AppendSelectorActions(HashSet<EntitySelectorActionPrototype> actions)
        {
            var action = actions.First();
            if (action.AIOverrides.HasValue())
            {
                int index = Game.Random.Next(0, action.AIOverrides.Length);
                var actionAIOverrideRef = action.AIOverrides[index];
                if (actionAIOverrideRef == PrototypeId.Invalid) return false;
                var actionAIOverride = actionAIOverrideRef.As<EntityActionAIOverridePrototype>();
                if (actionAIOverride != null) return AppendStartPower(actionAIOverride.Power);
            }
            return false;
        }

        // Old
        public Agent(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData) { }

        public Agent(EntityBaseData baseData) : base(baseData) { }
    }
}
