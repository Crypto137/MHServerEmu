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

        public AffixRecord(PrototypeId affixProtoRef, PrototypeId scopeProtoRef, int seed)
        {
            AffixProtoRef = affixProtoRef;
            ScopeProtoRef = scopeProtoRef;
            Seed = seed;
        }

        public AffixRecord(AffixSpec affixSpec)
        {
            AffixProtoRef = affixSpec.AffixProto.DataRef;
            ScopeProtoRef = affixSpec.ScopeProtoRef;
            Seed = affixSpec.Seed;
        }

        public override string ToString()
        {
            string scopeSuffix = ScopeProtoRef != PrototypeId.Invalid ? $"[{ScopeProtoRef.GetNameFormatted()}]" : string.Empty;
            return $"{AffixProtoRef.GetNameFormatted()}{scopeSuffix} (seed={Seed})";
        }

        /// <summary>
        /// Returns a new <see cref="AffixRecord"/> with the specified seed while retaining <see cref="AffixProtoRef"/> and <see cref="ScopeProtoRef"/>.
        /// </summary>
        public AffixRecord SetSeed(int seed)
        {
            return new(AffixProtoRef, ScopeProtoRef, seed);
        }
    }
}
