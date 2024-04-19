using Google.ProtocolBuffers;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Entities
{
    public class Missile : Agent
    {
        public override Bounds EntityCollideBounds { get; set; }
        // new
        public Missile(Game game) : base(game) { }

        public override bool CanCollideWith(WorldEntity other)
        {
            if (base.CanCollideWith(other) == false) return false;
            if (other.Properties[PropertyEnum.NoMissileCollide] == true) return false;
            return true;
        }

        // old
        public Missile(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData) { }

        public Missile(EntityBaseData baseData) : base(baseData) { }
    }
}
