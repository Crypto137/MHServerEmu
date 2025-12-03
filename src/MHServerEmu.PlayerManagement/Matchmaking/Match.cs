using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.PlayerManagement.Regions;

namespace MHServerEmu.PlayerManagement.Matchmaking
{
    public class Match
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public ulong Id { get; }
        public RegionRequestQueue Queue { get; }
        public PrototypeId DifficultyTierRef { get; }
        public PrototypeId MetaStateRef { get; }
        public bool IsBypass { get; }

        public RegionHandle Region { get; private set; }

        public Match(ulong id, RegionRequestQueue queue, PrototypeId difficultyTierRef, PrototypeId metaStateRef, bool isBypass)
        {
            Id = id;
            Queue = queue;

            DifficultyTierRef = difficultyTierRef;
            MetaStateRef = metaStateRef;
            IsBypass = isBypass;
        }

        public void AddBypassGroup(RegionRequestGroup group)
        {
            // TODO: other stuff
            group.SetMatch(this);
        }

        public void OnGroupUpdate(RegionRequestGroup group, bool memberCountChanged)
        {
            Logger.Debug($"OnGroupUpdate(): group={group}, memberCountChanged={memberCountChanged}");
        }

        public void CreateRegion()
        {
            Logger.Debug("CreateRegion()");
        }
    }
}
