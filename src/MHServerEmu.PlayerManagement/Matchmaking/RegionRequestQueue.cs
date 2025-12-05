using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.PlayerManagement.Matchmaking
{
    /// <summary>
    /// Represents a match queue for a particular <see cref="RegionPrototype"/>.
    /// </summary>
    public class RegionRequestQueue
    {
        private readonly Dictionary<ulong, Match> _matches = new();

        private ulong _currentMatchNumber = 0;

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

        public Match GetMatch(ulong matchNumber)
        {
            if (_matches.TryGetValue(matchNumber, out Match match) == false)
                return null;

            return match;
        }

        private Match CreateMatch(PrototypeId difficultyTierRef, PrototypeId metaStateRef, bool isBypass)
        {
            ulong matchNumber = ++_currentMatchNumber;
            Match match = new(matchNumber, this, difficultyTierRef, metaStateRef, isBypass);
            _matches.Add(matchNumber, match);
            return match;
        }
    }
}
