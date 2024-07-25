using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionAction
    {
        public IMissionActionOwner Owner { get; private set; }
        public PrototypeId MissionRef { get => Owner.PrototypeDataRef; }
        public MissionActionPrototype Prototype { get; private set; }
        public virtual bool RunOnStart { get => false; }

        public MissionAction(IMissionActionOwner owner, MissionActionPrototype prototype)
        {
            Owner = owner;
            Prototype = prototype;
        }

        public static MissionAction CreateAction(IMissionActionOwner owner, MissionActionPrototype actionProto) 
        {
            return actionProto.AllocateAction(owner);
        }

        public virtual bool Initialize() => true;
        public virtual void Run() { }
    }
}
