using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class StaticRegionGeneratorPrototype : RegionGeneratorPrototype
    {
        public StaticAreaPrototype[] StaticAreas { get; protected set; }
        public AreaConnectionPrototype[] Connections { get; protected set; }

        //---

        public override PrototypeId GetStartAreaRef(Region region)
        {
            if (StaticAreas.HasValue())
                return StaticAreas[0].Area;

            return PrototypeId.Invalid;
        }

        public override void GetAreasInGenerator(HashSet<PrototypeId> areas)
        {
            if (StaticAreas.HasValue())
                foreach (var areaProto in StaticAreas)
                    if (areaProto != null && areaProto.Area != PrototypeId.Invalid)
                        areas.Add(areaProto.Area);
        }
    }

    public class AreaConnectionPrototype : Prototype
    {
        public PrototypeId AreaA { get; protected set; }
        public PrototypeId AreaB { get; protected set; }
        public bool ConnectAllShared { get; protected set; }
    }

    public class StaticAreaPrototype : Prototype
    {
        public PrototypeId Area { get; protected set; }
        public int X { get; protected set; }
        public int Y { get; protected set; }
        public int Z { get; protected set; }
    }
}
