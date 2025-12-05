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

        public bool IsFull()
        {
            foreach (MatchTeam team in _teams)
            {
                if (team.IsFull() == false)
                    return false;
            }

            return true;
        }

        public bool HasGroup(RegionRequestGroup group)
        {
            foreach (MatchTeam team in _teams)
            {
                if (team.HasGroup(group))
                    return true;
            }

            return false;
        }

        public void AddBypassGroup(RegionRequestGroup group)
        {
            MatchTeam? team = GetAvailableTeam();
            if (team == null)
                return;

            if (HasGroup(group))
                return;

            team.Value.Groups.Add((group, true));
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

        private MatchTeam? GetAvailableTeam()
        {
            if (_teams.Count > 0)
            {
                _teams.Sort();
                MatchTeam team = _teams[0];

                if (team.IsFull() == false)
                    return team;
            }

            return null;
        }
    }
}
