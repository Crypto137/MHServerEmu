using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class StaticRegionGeneratorPrototype : RegionGeneratorPrototype
    {
        public StaticAreaPrototype[] StaticAreas { get; protected set; }
        public AreaConnectionPrototype[] Connections { get; protected set; }

        public override PrototypeId GetStartAreaRef(Region region)
        {

            if (StaticAreas != null && StaticAreas.Length > 0)
                return StaticAreas[0].Area;

            return 0;
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
