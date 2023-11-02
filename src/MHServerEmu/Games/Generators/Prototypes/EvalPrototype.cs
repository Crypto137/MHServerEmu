using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class EvalPrototype : Prototype
    {
        public EvalPrototype(Prototype proto) { FillPrototype(typeof(EvalPrototype), proto); }
    }
}
