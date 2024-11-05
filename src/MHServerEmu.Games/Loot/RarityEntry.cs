using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Loot
{
    /// <summary>
    /// Wraps <see cref="RarityPrototype"/> with comparison support for magic find scaling.
    /// </summary>
    public readonly struct RarityEntry : IComparable<RarityEntry>
    {
        public RarityPrototype Prototype { get; }
        public float Weight { get; }

        public RarityEntry(RarityPrototype rarityProto, int level)
        {
            Prototype = rarityProto;
            Weight = rarityProto.GetWeight(level);
        }

        public int CompareTo(RarityEntry other)
        {
            return Prototype.Tier.CompareTo(other.Prototype.Tier);
        }
    }
}
