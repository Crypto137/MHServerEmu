using MHServerEmu.Core.Extensions;
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
            if (HasInteractOption())
                Owner.Properties[PropertyEnum.EntSelActHasInteractOption] = true;
        }

        private bool HasInteractOption()
        {
            return ActionTable.ContainsKey(EntitySelectorActionEventType.OnPlayerInteract);
        }
    }
}
