using MHServerEmu.Core.Collisions;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Regions
{
    public class RegionSettings
    {
        public int EndlessLevel { get; set; }
        public int Seed { get; set; }
        public bool GenerateAreas { get; set; }
        public PrototypeId DifficultyTierRef { get; set; }
        public ulong InstanceAddress { get; set; }  // region id
        public Aabb Bound { get; set; }

        public List<PrototypeId> Affixes { get; set; }
        public int Level { get; set; }
        public bool DebugLevel { get; set; }
        public PrototypeId RegionDataRef { get; set; }
        public ulong MatchNumber { get; set; }

        public bool GenerateEntities { get; set; }
        public bool GenerateLog { get; set; }
    }
}
