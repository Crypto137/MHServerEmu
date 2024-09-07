using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Loot
{
    /// <summary>
    /// AffixProtoRef + ScopeProtoRef combo used to ensure rolled affix uniqueness.
    /// </summary>
    /// <remarks>
    /// The client uses an std::pair for this, but we define an explicit struct for readability.
    /// </remarks>
    public readonly struct ScopedAffixRef : IEquatable<ScopedAffixRef>
    {
        public PrototypeId AffixProtoRef { get; }
        public PrototypeId ScopeProtoRef { get; }

        public ScopedAffixRef(PrototypeId affixProtoRef, PrototypeId scopeProtoRef)
        {
            AffixProtoRef = affixProtoRef;
            ScopeProtoRef = scopeProtoRef;
        }

        public override bool Equals(object obj)
        {
            return obj is ScopedAffixRef other && Equals(other);
        }

        public bool Equals(ScopedAffixRef other)
        {
            return AffixProtoRef == other.AffixProtoRef && ScopeProtoRef == other.ScopeProtoRef;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(AffixProtoRef, ScopeProtoRef);
        }

        public override string ToString()
        {
            return $"{nameof(AffixProtoRef)}={AffixProtoRef.GetName()}, {nameof(ScopeProtoRef)}={ScopeProtoRef.GetName()}";
        }

        public static bool operator ==(ScopedAffixRef left, ScopedAffixRef right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ScopedAffixRef left, ScopedAffixRef right)
        {
            return !(left == right);
        }
    }
}
