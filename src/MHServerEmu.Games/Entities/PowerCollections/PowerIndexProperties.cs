namespace MHServerEmu.Games.Entities.PowerCollections
{
    public class PowerIndexProperties
    {
        // TODO: Maybe this can be a readonly struct?

        public int PowerRank { get; set; } = 0;
        public int CharacterLevel { get; set; } = 1;
        public int CombatLevel { get; set; } = 1;
        public int ItemLevel { get; set; } = 1;
        public float ItemVariation { get; set; } = 1.0f;

        public PowerIndexProperties() { }

        public override string ToString()
        {
            return $"{nameof(PowerRank)}={PowerRank}, {nameof(CharacterLevel)}={CharacterLevel}, {nameof(CombatLevel)}={CombatLevel}, {nameof(ItemLevel)}={ItemLevel}, {nameof(ItemVariation)}={ItemVariation:0.0}f";
        }
    }
}
