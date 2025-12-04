using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.PlayerManagement.Regions;

namespace MHServerEmu.PlayerManagement.Matchmaking
{
    public class Match
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly List<MatchTeam> _teams = new();

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

            int[] teamLimits = Queue.Prototype.TeamLimits;
            if (teamLimits.HasValue())
            {
                for (int i = 0; i < teamLimits.Length; i++)
                {
                    MatchTeam team = new(i, teamLimits[i]);
                    _teams.Add(team);
                }
            }
            else
            {
                MatchTeam team = new(-1, Queue.Prototype.PlayerLimit);
                _teams.Add(team);
            }
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
            PrototypeId regionProtoRef = Queue.PrototypeDataRef;

            NetStructCreateRegionParams createRegionParams = NetStructCreateRegionParams.CreateBuilder()
                .SetLevel(0)
                .SetDifficultyTierProtoId((ulong)DifficultyTierRef)
                .SetGameStateId((ulong)MetaStateRef)
                .SetMatchNumber(Id)
                .Build();

            Region = PlayerManagerService.Instance.WorldManager.CreateMatchRegion(regionProtoRef, createRegionParams);

            Logger.Debug($"CreateRegion(): {regionProtoRef.GetName()}");

        }
    }
}
