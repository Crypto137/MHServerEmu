using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Loot
{
    /// <summary>
    /// Represents a summary of an <see cref="AffixSpec"/>.
    /// </summary>
    public readonly struct AffixRecord
    {
        public PrototypeId AffixProtoRef { get; }
        public PrototypeId ScopeProtoRef { get; }
        public int Seed { get; }

        public AffixRecord(AffixSpec affixSpec)
        {
            AffixProtoRef = affixSpec.AffixProto.DataRef;
            ScopeProtoRef = affixSpec.ScopeProtoRef;
            Seed = affixSpec.Seed;
        }
    }
}
