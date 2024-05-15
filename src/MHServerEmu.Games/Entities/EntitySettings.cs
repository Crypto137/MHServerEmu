using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Generators.Population;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public class EntitySettings
    {
        public ulong Id;
        public PrototypeId EntityRef;
        public ulong RegionId;
        public Vector3 Position;
        public Orientation Orientation;
        public bool OverrideSnapToFloor;
        public bool OverrideSnapToFloorValue;
        public bool EnterGameWorld;
        public bool HotspotSkipCollide;
        public PropertyCollection Properties;
        public Cell Cell;
        public List<EntitySelectorActionPrototype> Actions;
        public PrototypeId ActionsTarget;
        public SpawnSpec SpawnSpec;
        public float LocomotorHeightOverride;
    }
}
