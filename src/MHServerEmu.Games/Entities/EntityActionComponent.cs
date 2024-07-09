using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.Behavior;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Entities
{
    public class ActionSet : HashSet<EntitySelectorActionPrototype> { }
    public class EntityActionComponent
    {
        public WorldEntity Owner { get; private set; }
        public Dictionary<EntitySelectorActionEventType, ActionSet> ActionTable { get; private set; }
        public Dictionary<EntitySelectorActionEventType, ActionSet> CancelTable { get; private set; }
        public bool RequiresBrain { get; private set; }

        public EntityActionComponent(WorldEntity owner)
        {
            Owner = owner;
            ActionTable = new();
            CancelTable = new();
        }

        public void Register(List<EntitySelectorActionPrototype> actions)
        {
            if (actions == null) return;
            foreach (var action in actions) RegisterAction(action);
        }

        private void RegisterAction(EntitySelectorActionPrototype action)
        {
            if (Owner == null || action == null || action.EventTypes.IsNullOrEmpty()) return;
            foreach (var eventType in action.EventTypes) 
            {                
                if (ActionTable.TryGetValue(eventType, out ActionSet actionSet) == false)
                { 
                    actionSet = new();
                    ActionTable[eventType] = actionSet;
                }
                actionSet.Add(action);
            }

            RequiresBrain |= action.RequiresBrain;
            // if (Owner.IsInWorld) InitActionBrain();

            if (HasInteractOption())
                Owner.Properties[PropertyEnum.EntSelActHasInteractOption] = true;
        }

        public void InitActionBrain()
        {
            if (RequiresBrain == false) return;
            if (Owner is not Agent agent || agent is Avatar || agent.IsControlledEntity) return;
            AIController controller = agent.AIController;
            if (controller != null) return;

            var selectorProto = agent.SpawnSpec?.EntitySelectorProto;
            PrototypeId brainRef;
            if (selectorProto != null)
                brainRef = selectorProto.DefaultBrainOnSimulated;
            else
                brainRef = GameDatabase.AIGlobalsPrototype.DefaultSimpleNpcBrain;
            var brain = GameDatabase.GetPrototype<BrainPrototype>(brainRef);
            if (brain is not ProceduralAIProfilePrototype profile) return;

            PropertyCollection collection = new ();
            collection[PropertyEnum.AICustomThinkRateMS] = 1000;
            if (selectorProto != null) {
                collection[PropertyEnum.AIAggroRangeAlly] = selectorProto.DefaultAggroRangeAlly;
                collection[PropertyEnum.AIAggroRangeHostile] = selectorProto.DefaultAggroRangeHostile;
                collection[PropertyEnum.AIProximityRangeOverride] = selectorProto.DefaultProximityRangeHostile;
            }

            agent.InitAIOverride(profile, collection);

            if (Owner.IsSimulated)
                Trigger(EntitySelectorActionEventType.OnSimulated);
        }

        public void Trigger(EntitySelectorActionEventType eventType)
        {
            throw new NotImplementedException();
        }

        private bool HasInteractOption()
        {
            return ActionTable.ContainsKey(EntitySelectorActionEventType.OnPlayerInteract);
        }
    }
}
