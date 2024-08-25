using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionAction
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public IMissionActionOwner Owner { get; private set; }
        public PrototypeId MissionRef { get => Owner.PrototypeDataRef; }
        public MissionActionPrototype Prototype { get; private set; }
        public Mission Mission { get; private set; }
        public Region Region { get => Owner.Region; }
        public Game Game { get => Mission?.Game; }
        public MissionManager MissionManager { get => Mission?.MissionManager; }
        public PrototypeId Context { get => Owner.PrototypeDataRef; }
        public EntityTracker EntityTracker { get => Owner.Region?.EntityTracker; }

        public MissionAction(IMissionActionOwner owner, MissionActionPrototype prototype)
        {
            Owner = owner;
            Prototype = prototype;
            var missionList = owner as MissionActionList;
            Mission = missionList?.Mission;
        }

        public virtual void Destroy() { }

        public static MissionAction CreateAction(IMissionActionOwner owner, MissionActionPrototype actionProto) 
        {
            if (MissionManager.Debug) Logger.Debug($"CreateAction {GameDatabase.GetFormattedPrototypeName(owner.PrototypeDataRef)} {actionProto}");
            return actionProto.AllocateAction(owner);
        }

        public IEnumerable<Player> GetDistributors(DistributionType distributionType)
        {
            return distributionType switch
            {
                DistributionType.Participants => Mission.GetParticipants(),
                DistributionType.Contributors => Mission.GetContributors(),
                DistributionType.AllInOpenMissionRegion => Mission.GetRegionPlayers(),
                _ => Enumerable.Empty<Player>(),
            };
        }

        public virtual bool Initialize() => true;
        public virtual void Run() { }
        public virtual bool RunOnStart() => false;
    }
}
