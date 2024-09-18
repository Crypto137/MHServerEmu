namespace MHServerEmu.Games.Entities.PowerCollections
{
    public readonly struct PowerIndexProperties
    {
        public int PowerRank { get; } = 0;
        public int CharacterLevel { get; } = 1;
        public int CombatLevel { get; } = 1;
        public int ItemLevel { get; } = 1;
        public float ItemVariation { get; } = 1.0f;

        public PowerIndexProperties(int powerRank = 0, int characterLevel = 1, int combatLevel = 1, int itemLevel = 1, float itemVariation = 1.0f)
        {
            PowerRank = powerRank;
            CharacterLevel = characterLevel;
            CombatLevel = combatLevel;
            ItemLevel = itemLevel;
            ItemVariation = itemVariation;
        }

        public override string ToString()
        {
            return $"{nameof(PowerRank)}={PowerRank}, {nameof(CharacterLevel)}={CharacterLevel}, {nameof(CombatLevel)}={CombatLevel}, {nameof(ItemLevel)}={ItemLevel}, {nameof(ItemVariation)}={ItemVariation:0.0}f";
        }
    }
}
