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

        public static bool CreateActionList(MissionActionList actions, MissionActionPrototype[] protoList, IMissionActionOwner owner)
        {
            if (actions == null && protoList.HasValue())
            {
                actions = new MissionActionList(owner);
                if (actions == null || actions.Initialize(protoList) == false) return false;
            }
            actions?.Run();
            return true;
        }

        public void Run()
        {
            if (IsInitialized == false) return;
            Mission mission = Mission;
            if (mission != null && mission.IsSuspended) return;
            if (IsActive == false) Activate();
            foreach(var action in Actions) 
            {
                action.Run();
            }
        }

        public void Activate()
        {
            if (IsInitialized == false || IsActive) return;
            IsActive = true;
        }

        public void Deactivate()
        {
            if (IsInitialized == false || IsActive == false) return;
            IsActive = false;
        }
    }
}
