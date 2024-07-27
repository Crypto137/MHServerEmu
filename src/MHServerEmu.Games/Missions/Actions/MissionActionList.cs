using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionList
    {
        public IMissionActionOwner Owner { get; private set; }
        public bool IsInitialized { get; private set; }
        public List<MissionAction> Actions { get; private set; }
        public List<MissionActionEntityTarget> EntityActions { get; private set; }
        public Mission Mission { get => Owner as Mission; }
        public Region Region { get => Mission.Region; }
        public PrototypeId Context { get => Owner.PrototypeDataRef; }
        public bool IsActive { get; private set; }
        public Action<EntitySetSimulatedGameEvent> EntitySetSimulatedAction { get; private set; }
        public Action<EntityLeaveDormantGameEvent> EntityLeaveDormantAction { get; private set; }

        public MissionActionList(IMissionActionOwner owner)
        {
            Owner = owner;
            Actions = new();
            EntityActions = new();
            EntitySetSimulatedAction = OnEntitySetSimulated;
            EntityLeaveDormantAction = OnEntityLeaveDormant;
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
                    if (action is MissionActionEntityTarget entityAction && entityAction.RunOnStart())
                        EntityActions.Add(entityAction);
                }
            }
            IsInitialized = true;
            return true;
        }

        public static bool CreateActionList(ref MissionActionList actions, MissionActionPrototype[] protoList, 
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
                if (runOnStart || action.RunOnStart())
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
                    region.EntitySetSimulatedEvent.AddActionBack(EntitySetSimulatedAction);
                    region.EntityLeaveDormantEvent.AddActionBack(EntityLeaveDormantAction);
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
                    region.EntitySetSimulatedEvent.RemoveAction(EntitySetSimulatedAction);
                    region.EntityLeaveDormantEvent.RemoveAction(EntityLeaveDormantAction);
                }
            }
            IsActive = false;
            return true;
        }

        private void OnEntitySetSimulated(EntitySetSimulatedGameEvent evt)
        {
            RunEntityActions(evt.Entity);
        }

        private void OnEntityLeaveDormant(EntityLeaveDormantGameEvent evt)
        {
            RunEntityActions(evt.Entity);
        }

        private void RunEntityActions(WorldEntity entity)
        {
            if (entity == null || entity.IsDormant) return;
            if (IsInitialized == false || IsActive == false) return;
            if (entity.IsTrackedByContext(Context) == false) return;

            foreach (var action in EntityActions)
                action?.EvaluateAndRunEntity(entity);
        }
    }
}
