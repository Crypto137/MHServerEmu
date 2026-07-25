namespace MHServerEmu.Games.GameData.Tables
{
    public class GameDataTables
    {
        public static GameDataTables Instance { get; } = new();

        public AllianceTable AllianceTable { get; } = new();
        public EquipmentSlotTable EquipmentSlotTable { get; } = new();
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public InfinityGemBonusTable InfinityGemBonusTable { get; } = new();
        public InfinityGemBonusPostreqsTable InfinityGetBonusPostreqsTable { get; } = new();
#endif
        public LootPickingTable LootPickingTable { get; } = new();
        public PowerOwnerTable PowerOwnerTable { get; } = new();
#if !GAME_VERSION_1_53
        public OmegaBonusSetTable OmegaBonusSetTable { get; } = new();
        public OmegaBonusPostreqsTable OmegaBonusPostreqsTable { get; } = new();
#endif
        public LootCooldownTable LootCooldownTable { get; } = new();

        private GameDataTables() { }
    }
}
