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
        public RegionRequestQueueParams QueueParams { get; }

        public RegionHandle Region { get; private set; }

        public Match(ulong id, RegionRequestQueue queue, in RegionRequestQueueParams queueParams)
        {
            Id = id;
            Queue = queue;
            QueueParams = queueParams;

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

        public bool IsEmpty()
        {
            foreach (MatchTeam team in _teams)
            {
                if (team.Groups.Count > 0)
                    return false;
            }

            return true;
        }

        public int GetAvailableCount()
        {
            int count = 0;

            foreach (MatchTeam team in _teams)
                count += team.GetAvailableCount();

            return count;
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

        public MatchTeam? GetTeamForGroup(RegionRequestGroup group)
        {
            foreach (MatchTeam team in _teams)
            {
                foreach ((RegionRequestGroup itGroup, _) in team.Groups)
                {
                    if (itGroup == group)
                        return team;
                }
            }

            return null;
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

            MatchTeam? nullableTeam = GetTeamForGroup(group);
            if (nullableTeam == null)
            {
                Logger.Warn("OnGroupUpdate(): nullableTeam == null");
                return;
            }

            MatchTeam team = nullableTeam.Value;

            // Remove groups that have become empty
            if (memberCountChanged && group.Count == 0)
            {
                var groups = team.Groups;
                for (int i = 0; i < groups.Count; i++)
                {
                    (RegionRequestGroup itGroup, _) = groups[i];
                    if (itGroup == group)
                    {
                        Logger.Debug($"Removed group {group} from team {team}");
                        groups.RemoveAt(i);
                        break;
                    }
                }
            }

            // Try filling the team with waitlisted players from existing groups.
            if (team.IsFull() == false)
            {
                foreach ((RegionRequestGroup itGroup, _) in team.Groups)
                {
                    if (team.GetAvailableCount() <= 0)
                        break;

                    itGroup.OnMatchRegionAccessChange(Region);
                }
            }

            // TODO: Check matchmaking state transition for the group

            Queue.UpdateMatchSortOrder(this);
        }

        public void OnRegionAccessChanged(RegionHandle region)
        {
            if (region != Region)
            {
                Logger.Warn("OnRegionAccessChanged(): region != Region");
                return;
            }

            foreach (MatchTeam team in _teams)
            {
                foreach ((RegionRequestGroup group, _) in team.Groups)
                    group?.OnMatchRegionAccessChange(region);
            }
        }

        public void CreateRegion()
        {
            PrototypeId regionProtoRef = Queue.PrototypeDataRef;

            NetStructCreateRegionParams createRegionParams = NetStructCreateRegionParams.CreateBuilder()
                .SetLevel(0)
                .SetDifficultyTierProtoId((ulong)QueueParams.DifficultyTierRef)
                .SetGameStateId((ulong)QueueParams.MetaStateRef)
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
