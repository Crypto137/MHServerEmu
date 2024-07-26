using MHServerEmu.Core.Collisions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Regions
{
    public class RegionSettings
    {
        public ulong InstanceAddress { get; set; }  // region id
        public PrototypeId RegionDataRef { get; set; }
        public Aabb Bounds { get; set; }
        public int Level { get; set; }
        public PrototypeId DifficultyTierRef { get; set; }
        public int EndlessLevel { get; set; }
        public ulong MatchNumber { get; set; }
        public int Seed { get; set; }
        public List<PrototypeId> Affixes { get; set; }
        public int PlayerDeaths { get; set; }
        public PropertyCollection Properties { get; set; }

        public bool DebugLevel { get; set; }
        public bool GenerateLog { get; set; }
        public bool GenerateEntities { get; set; }
        public bool GenerateAreas { get; set; }
    }
}
