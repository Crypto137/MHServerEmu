using System.Text;

namespace MHServerEmu.Games.Entities.PowerCollections
{
    public class PowerIndexProperties
    {
        public uint PowerRank { get; set; }
        public uint CharacterLevel { get; set; }
        public uint CombatLevel { get; set; }
        public uint ItemLevel { get; set; }
        public float ItemVariation { get; set; }

        public PowerIndexProperties()
        {
            PowerRank = 0;
            CharacterLevel = 1;
            CombatLevel = 1;
            ItemLevel = 1;
            ItemVariation = 1.0f;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"PowerRank: {PowerRank}");
            sb.AppendLine($"CharacterLevel: {CharacterLevel}");
            sb.AppendLine($"CombatLevel: {CombatLevel}");
            sb.AppendLine($"ItemLevel: {ItemLevel}");
            sb.AppendLine($"ItemVariation: {ItemVariation}");
            return sb.ToString();
        }
    }
}
