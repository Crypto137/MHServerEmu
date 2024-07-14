using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Loot
{
    public class LootRollSettings
    {
        public const int MaxLootTreeDepth = 50;

        public int Depth { get; set; }
        public LootDropChanceModifiers DropChanceModifiers { get; }

        public AvatarPrototype UsableAvatar { get; }            // LootRollSetAvatarPrototype
        public AgentPrototype UsableTeamUp { get; }             // Team-ups are the only agents other than avatars that have equipment

        public bool HasUsableOverride { get; set; }             // LootRollSetAvatarPrototype
        public float UsableOverrideValue { get; set; }          // LootRollSetUsablePrototype

        public int Level { get; set; } = 1;                     // LootRollOffsetLevelPrototype
        public bool UseLevelVerbatim { get; set; } = false;     // LootRollUseLevelVerbatimPrototype

        public HashSet<PrototypeId> Rarities { get; } = new();  // LootRollSetRarityPrototype

        public float DropDistanceThresholdSq { get; set; }      // DistanceRestrictionPrototype::Allow()
    }
}
