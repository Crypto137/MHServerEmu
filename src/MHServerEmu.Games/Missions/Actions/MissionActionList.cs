using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionList
    {
        public IMissionActionOwner Owner { get; private set; }
        public bool IsInitialized { get; private set; }
        public List<MissionAction> Actions { get; private set; }
        public List<MissionAction> EntityActions { get; private set; }
        public Mission Mission { get => Owner as Mission; }
        public Region Region { get => Mission.Region; }
        public bool IsActive { get; private set; }
        public Action<EntitySetSimulatedGameEvent> EntitySetSimulatedEvent { get; private set; }
        public Action<EntityLeaveDormantGameEvent> EntityLeaveDormantEvent { get; private set; }

        public MissionActionList(IMissionActionOwner owner)
        {
            Owner = owner;
            Actions = new();
            EntityActions = new();
            EntitySetSimulatedEvent = OnEntitySetSimulatedEvent;
            EntityLeaveDormantEvent = OnEntityLeaveDormantEvent;
        }

        public bool Initialize(MissionActionPrototype[] protoList)
        {
            if (protoList.IsNullOrEmpty() || IsInitialized || IsActive) return false;
            foreach(var actionProto in protoList)
            {
                var action = MissionAction.CreateAction(Owner, actionProto);
                if (action == null) return false;
                if (action.Initialize())
                {
                    Actions.Add(action);
                    if (action is MissionActionEntityTarget entityAction && entityAction.RunOnStart)
                        EntityActions.Add(action);
                }
            }
            IsInitialized = true;
            return true;
        }

        public static bool CreateActionList(MissionActionList actions, MissionActionPrototype[] protoList, 
            IMissionActionOwner owner, bool runOnStart = true)
        {
            if (actions == null && protoList.HasValue())
            {
                actions = new MissionActionList(owner);
                if (actions == null || actions.Initialize(protoList) == false) return false;
            }
            actions?.Run(runOnStart);
            return true;
        }

        public void Run(bool runOnStart)
        {
            if (IsInitialized == false) return;
            Mission mission = Mission;
            if (mission != null && mission.IsSuspended) return;
            if (IsActive == false) Activate();
            foreach(var action in Actions) 
            {
                if (action == null) continue;
                if (runOnStart || action.RunOnStart)
                    action.Run();
            }
        }

        public bool Activate()
        {
            if (IsInitialized == false) return false;
            if (IsActive) return true;
            if (EntityActions.Count > 0)
            {
                var region = Region;
                if (region != null)
                {
                    region.EntitySetSimulatedEvent.AddActionBack(EntitySetSimulatedEvent);
                    region.EntityLeaveDormantEvent.AddActionBack(EntityLeaveDormantEvent);
                }
            }
            IsActive = true;
            return true;
        }

        public bool Deactivate()
        {
            if (IsInitialized == false) return false;
            if (IsActive == false) return true; 
            if (EntityActions.Count > 0)
            {
                var region = Region;
                if (region != null)
                {
                    region.EntitySetSimulatedEvent.RemoveAction(EntitySetSimulatedEvent);
                    region.EntityLeaveDormantEvent.RemoveAction(EntityLeaveDormantEvent);
                }
            }
            IsActive = false;
            return true;
        }

        private void OnEntitySetSimulatedEvent(EntitySetSimulatedGameEvent evt)
        {
            throw new NotImplementedException();
        }

        private void OnEntityLeaveDormantEvent(EntityLeaveDormantGameEvent evt)
        {
            throw new NotImplementedException();
        }
    }
}
