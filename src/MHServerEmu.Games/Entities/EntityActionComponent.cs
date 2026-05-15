using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Behavior;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
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

        private Dictionary<EntitySelectorActionPrototype, FireActionPointer> _pendingActions;
        private readonly EventGroup _pendingEvents = new();
        public bool RequiresBrain { get; private set; }
        public HashSet<PrototypeId> PerformPowers { get; private set; }

        public EntityActionComponent(WorldEntity owner)
        {
            Owner = owner;
            ActionTable = new();
            CancelTable = new();
            PerformPowers = new();
            _pendingActions = new();
        }

        public void Destroy()
        {
            ActionTable.Clear();
            CancelTable.Clear();
            CancelAll();
        }

        public void Register(List<EntitySelectorActionPrototype> actions)
        {
            if (actions == null) return;
            foreach (var action in actions) RegisterAction(action);
        }

        private void RegisterAction(EntitySelectorActionPrototype action)
        {
            if (!Verify.IsNotNull(Owner)) return;
            if (!Verify.IsNotNull(action)) return;
            if (!Verify.IsTrue(action.EventTypes.HasValue())) return;
            
            foreach (var eventType in action.EventTypes) 
            {                
                if (ActionTable.TryGetValue(eventType, out ActionSet actionSet) == false)
                { 
                    actionSet = new();
                    ActionTable.Add(eventType, actionSet);
                }
                actionSet.Add(action);
            }

            RequiresBrain |= action.RequiresBrain;
            if (Owner.IsInWorld) InitActionBrain();

            if (HasInteractOption())
                Owner.Properties[PropertyEnum.EntSelActHasInteractOption] = true;
        }

        private void DeregisterAction(EntitySelectorActionPrototype action)
        {
            if (!Verify.IsNotNull(Owner)) return;
            if (!Verify.IsNotNull(action)) return;
            if (!Verify.IsTrue(action.EventTypes.HasValue())) return;

            foreach (var eventType in action.EventTypes)
            {
                if (ActionTable.TryGetValue(eventType, out ActionSet actionSet) == false) continue;               
                actionSet.Remove(action);
                if (actionSet.Count == 0) ActionTable.Remove(eventType);
            }

            if (HasInteractOption() == false)
                Owner.Properties[PropertyEnum.EntSelActHasInteractOption] = false;
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

            using PropertyCollection collection = ObjectPoolManager.Instance.Get<PropertyCollection>();
            collection[PropertyEnum.AICustomThinkRateMS] = 1000;
            if (Verify.IsNotNull(selectorProto))
            {
                collection[PropertyEnum.AIAggroRangeAlly] = selectorProto.DefaultAggroRangeAlly;
                collection[PropertyEnum.AIAggroRangeHostile] = selectorProto.DefaultAggroRangeHostile;
                collection[PropertyEnum.AIProximityRangeOverride] = selectorProto.DefaultProximityRangeHostile;
            }

            agent.InitAIOverride(profile, collection);
            agent.AIController.Blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AIFullOverride);

            if (Owner.IsSimulated)
                Trigger(EntitySelectorActionEventType.OnSimulated);
        }

        public bool Trigger(EntitySelectorActionEventType eventType)
        {
            if (!Verify.IsNotNull(Owner)) return false;
            if (!Verify.IsTrue(Owner.IsControlledEntity == false)) return false;

            CancelActions(eventType);
            if (ActionTable.TryGetValue(eventType, out ActionSet actionSet))
            {
                using var actionsHandle = ListPool<EntitySelectorActionPrototype>.Instance.Get(out List<EntitySelectorActionPrototype> actions);
                actions.AddRange(actionSet);

                foreach (var action in actions)
                    ScheduleAction(action, eventType);
            }
            return true;
        }

        private bool ScheduleAction(EntitySelectorActionPrototype action, EntitySelectorActionEventType eventType)
        {
            if (!Verify.IsNotNull(Owner)) return false;
            if (!Verify.IsNotNull(action)) return false;

            if (_pendingActions.ContainsKey(action)) return false;
            DeregisterAction(action);

            EventScheduler scheduler = Game.Current.GameEventScheduler;
            if (!Verify.IsNotNull(scheduler)) return false;

            CancelTableInsert(action);

            TimeSpan reactionTime = TimeSpan.FromMilliseconds(action.ReactionTimeMS);
            if (eventType == EntitySelectorActionEventType.OnSimulated) reactionTime = TimeSpan.Zero;

            FireActionPointer actionEvent = new();
            _pendingActions[action] = actionEvent;
            scheduler.ScheduleEvent(actionEvent, reactionTime, _pendingEvents);
            actionEvent.Get().Initialize(this, action, eventType);

            return true;
        }

        private bool CancelActions(EntitySelectorActionEventType eventType)
        {
            bool canceled = false;
            if (CancelTable.TryGetValue(eventType, out ActionSet actionSet))
                while (actionSet.Count > 0)
                    canceled |= CancelAction(actionSet.First());
            return canceled;
        }

        private bool CancelAction(EntitySelectorActionPrototype action)
        {
            return CancelTableRemove(action) | CancelActionEvent(action);
        }

        private bool CancelActionEvent(EntitySelectorActionPrototype action)
        {
            if (_pendingActions.TryGetValue(action, out FireActionPointer fireAction) == false) return false;

            EventScheduler scheduler = Game.Current.GameEventScheduler;
            if (!Verify.IsNotNull(scheduler)) return false;            
            scheduler.CancelEvent(fireAction);

            _pendingActions.Remove(action);
            return true;
        }

        private bool CancelTableRemove(EntitySelectorActionPrototype action)
        {
            if (!Verify.IsNotNull(action)) return false;
            bool removed = false;
            if (action.CancelOnEventTypes.HasValue())
                foreach (var eventType in action.CancelOnEventTypes)
                    if (CancelTable.TryGetValue(eventType, out ActionSet cancellableActions))
                        removed |= cancellableActions.Remove(action);
            return removed;
        }

        private void CancelTableInsert(EntitySelectorActionPrototype action)
        {
            if (!Verify.IsNotNull(action)) return;
            if (action.CancelOnEventTypes.HasValue())
                foreach (var eventType in action.CancelOnEventTypes)
                {
                    if (CancelTable.TryGetValue(eventType, out ActionSet cancellableActions) == false)
                    {
                        cancellableActions = new();
                        CancelTable[eventType] = cancellableActions;
                    }
                    cancellableActions?.Add(action);
                }
        }

        private bool HasInteractOption()
        {
            return ActionTable.ContainsKey(EntitySelectorActionEventType.OnPlayerInteract) || ActionTable.ContainsKey(EntitySelectorActionEventType.OnPlayerInteract);
        }

        public bool CanTrigger(EntitySelectorActionEventType eventType)
        {
            return ActionTable.ContainsKey(eventType) || CancelTable.ContainsKey(eventType);
        }

        public void FireAction(EntitySelectorActionPrototype action, EntitySelectorActionEventType eventType)
        {
            if (!Verify.IsNotNull(action)) return;
            if (!Verify.IsNotNull(Owner)) return;

            CancelAction(action); 
            if (Owner.ProcessEntityAction(action) == false && eventType == EntitySelectorActionEventType.OnSimulated)
                RegisterAction(action);
        }

        public void RestartPendingActions()
        {
            EventScheduler scheduler = Game.Current.GameEventScheduler;
            if (!Verify.IsNotNull(scheduler)) return;

            using var actionsHandle = ListPool<EntitySelectorActionPrototype>.Instance.Get(out List<EntitySelectorActionPrototype> actions);
            actions.AddRange(_pendingActions.Keys);         
            
            foreach (var action in actions)
            {
                CancelAction(action);
                RegisterAction(action);
            }
        }

        public void CancelAll()
        {
            EventScheduler scheduler = Game.Current.GameEventScheduler;
            if (!Verify.IsNotNull(scheduler)) return;

            foreach (var kvp in _pendingActions)
                scheduler.CancelEvent(kvp.Value);

            scheduler.CancelAllEvents(_pendingEvents);
        }
    }

    public class FireActionPointer : EventPointer<FireActionEvent> { }
    public class FireActionEvent : CallMethodEventParam2<EntityActionComponent, EntitySelectorActionPrototype, EntitySelectorActionEventType>
    {
        protected override CallbackDelegate GetCallback() => (component, action, eventType) => component.FireAction(action, eventType);
    }
}
