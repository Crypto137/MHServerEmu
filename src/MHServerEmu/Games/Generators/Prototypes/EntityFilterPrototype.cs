using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class EntityFilterPrototype : Prototype
    {
        public EntityFilterPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityFilterPrototype), proto); }

    }
}
