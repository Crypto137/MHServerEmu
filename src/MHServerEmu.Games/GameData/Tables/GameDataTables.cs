namespace MHServerEmu.Games.GameData.Tables
{
    public class GameDataTables
    {
        public static GameDataTables Instance { get; } = new();

        public AllianceTable AllianceTable { get; } = new();
        public EquipmentSlotTable EquipmentSlotTable { get; } = new();
        public InfinityGemBonusTable InfinityGemBonusTable { get; } = new();
        public InfinityGemBonusPostreqsTable InfinityGetBonusPostreqsTable { get; } = new();
        public LootPickingTable LootPickingTable { get; } = new();
        public PowerOwnerTable PowerOwnerTable { get; } = new();
        public OmegaBonusSetTable OmegaBonusSetTable { get; } = new();
        public OmegaBonusPostreqsTable OmegaBonusPostreqsTable { get; } = new();
        public LootCooldownTable LootCooldownTable { get; } = new();

        private GameDataTables() { }
    }
}
