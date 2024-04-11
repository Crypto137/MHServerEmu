using Google.ProtocolBuffers;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public class Agent : WorldEntity
    {
        // New
        public Agent(Game game) : base(game) { }

        public override void Initialize(EntitySettings settings)
        {
            base.Initialize(settings);
        }

        public override void EnterWorld(Region region, Vector3 position, Orientation orientation)
        {
            base.EnterWorld(region, position, orientation);
            Location.Cell.EnemySpawn(); // Calc Enemy
        }

        // Old
        public Agent(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData) { }

        public Agent(EntityBaseData baseData) : base(baseData) { }
    }
}
