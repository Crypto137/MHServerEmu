using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class IPoint2Prototype : Prototype
    {
        public int X;
        public int Y;

        public IPoint2Prototype(Prototype proto) { FillPrototype(typeof(IPoint2Prototype), proto); }

    }

}
