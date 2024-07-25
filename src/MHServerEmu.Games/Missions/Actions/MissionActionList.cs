using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionList
    {
        public IMissionActionOwner Owner { get; private set; }
        public bool IsInitialized { get; private set; }
        public List<MissionAction> Actions { get; private set; }
        public Mission Mission { get => Owner as Mission; }
        public bool IsActive { get; private set; }

        public MissionActionList(IMissionActionOwner owner)
        {
            Owner = owner;
            Actions = new();
        }

        public bool Initialize(MissionActionPrototype[] protoList)
        {
            if (protoList.IsNullOrEmpty() || IsInitialized || IsActive) return false;
            foreach(var actionProto in protoList)
            {
                var action = MissionAction.CreateAction(Owner, actionProto);
                if (action == null) return false;
                Actions.Add(action);
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
            IsActive = true;
            return true;
        }

        public bool Deactivate()
        {
            if (IsInitialized == false) return false;
            if (IsActive == false) return true;
            IsActive = false;
            return true;
        }
    }
}
