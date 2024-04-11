using Google.ProtocolBuffers;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    // Hotspot doesn't contain any data of its own, but probably contains behavior
    public class Hotspot : WorldEntity
    {
        // new
        public Hotspot(Game game) : base(game) { }

        public override void Initialize(EntitySettings settings)
        {
            base.Initialize(settings);
            _flags |= EntityFlags.IsHotspot;            
        }

        // old
        public Hotspot(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData) { }

        public Hotspot(EntityBaseData baseData) : base(baseData) { }
    }
}
