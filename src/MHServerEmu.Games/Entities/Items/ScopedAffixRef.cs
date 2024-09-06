using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Entities.Items
{
    /// <summary>
    /// AffixProtoRef + ScopeProtoRef combo used to ensure rolled affix uniqueness.
    /// </summary>
    public readonly struct ScopedAffixRef
    {
        public PrototypeId AffixProtoRef { get; }
        public PrototypeId ScopeProtoRef { get; }

        public ScopedAffixRef(PrototypeId affixProtoRef, PrototypeId scopeProtoRef)
        {
            AffixProtoRef = affixProtoRef;
            ScopeProtoRef = scopeProtoRef;
        }

        public override string ToString()
        {
            return $"{nameof(AffixProtoRef)}={AffixProtoRef.GetName()}, {nameof(ScopeProtoRef)}={ScopeProtoRef.GetName()}";
        }
    }
}
