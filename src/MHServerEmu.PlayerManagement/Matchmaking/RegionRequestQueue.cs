using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.PlayerManagement.Matchmaking
{
    /// <summary>
    /// Represents a match queue for a particular <see cref="RegionPrototype"/>.
    /// </summary>
    public class RegionRequestQueue
    {
        private readonly List<Match> _matches = new();

        private ulong _currentMatchId = 0;

        public RegionPrototype Prototype { get; }
        public PrototypeId PrototypeDataRef { get => Prototype.DataRef; }

        public RegionRequestQueue(RegionPrototype regionProto)
        {
            Prototype = regionProto;
        }

        public void AddBypassGroup(RegionRequestGroup group)
        {
            Match match = CreateMatch(group.DifficultyTierRef, group.MetaStateRef, group.IsBypass);
            match.AddBypassGroup(group);
        }

        public void UpdateGroupBucket(RegionRequestGroup group)
        {

        }

        private Match CreateMatch(PrototypeId difficultyTierRef, PrototypeId metaStateRef, bool isBypass)
        {
            ulong matchId = ++_currentMatchId;
            Match match = new(matchId, this, difficultyTierRef, metaStateRef, isBypass);
            _matches.Add(match);
            return match;
        }
    }
}
