namespace MHServerEmu.Games.GameData.Prototypes
{
    public class ScoringEventContextPrototype : Prototype
    {
        public ulong ContextAvatar { get; set; }
        public ulong ContextItemEquipped { get; set; }
        public ulong ContextParty { get; set; }
        public ulong ContextPet { get; set; }
        public ulong ContextRegion { get; set; }
        public bool ContextRegionIncludeChildren { get; set; }
        public ulong ContextRegionKeyword { get; set; }
        public ulong ContextDifficultyTierMin { get; set; }
        public ulong ContextDifficultyTierMax { get; set; }
        public ulong ContextTeamUp { get; set; }
        public ulong ContextPublicEventTeam { get; set; }
    }

    public class ScoringEventTimerPrototype : Prototype
    {
        public ulong UIWidget { get; set; }
    }

    public class ScoringEventPrototype : Prototype
    {
        public ScoringEventContextPrototype Context { get; set; }
    }

    public class ScoringEventAchievementScorePrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventAreaEnterPrototype : ScoringEventPrototype
    {
        public ulong Area { get; set; }
    }

    public class ScoringEventAvatarDeathPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventAvatarKillPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventAvatarKillAssistPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventAvatarLevelPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventAvatarLevelTotalPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventAvatarLevelTotalAllAvatarsPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventAvatarPrestigeLevelPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventAvatarsAtLevelCapPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventAvatarsAtPrstgLvlPrototype : ScoringEventPrototype
    {
        public ulong PrestigeLevel { get; set; }
    }

    public class ScoringEventAvatarsAtPrstgLvlCapPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventAvatarsUnlockedPrototype : ScoringEventPrototype
    {
        public ulong Avatar { get; set; }
    }

    public class ScoringEventAvatarUsedPowerPrototype : ScoringEventPrototype
    {
        public ulong Power { get; set; }
        public ulong PowerKeyword { get; set; }
        public ulong TargetKeyword { get; set; }
        public ulong TargetPrototype { get; set; }
        public bool TargetPrototypeIncludeChildren { get; set; }
    }

    public class ScoringEventCompleteMissionPrototype : ScoringEventPrototype
    {
        public ulong Mission { get; set; }
        public ulong MissionKeyword { get; set; }
    }

    public class ScoringEventCompletionTimePrototype : ScoringEventPrototype
    {
        public ulong Timer { get; set; }
    }

    public class ScoringEventCurrencyCollectedPrototype : ScoringEventPrototype
    {
        public ulong Currency { get; set; }
    }

    public class ScoringEventCurrencySpentPrototype : ScoringEventPrototype
    {
        public ulong Currency { get; set; }
    }

    public class ScoringEventEntityDeathPrototype : ScoringEventPrototype
    {
        public ulong EntityKeyword { get; set; }
        public ulong EntityPrototype { get; set; }
        public bool EntityPrototypeIncludeChildren { get; set; }
        public ulong Rank { get; set; }
        public ulong RankKeyword { get; set; }
    }

    public class ScoringEventEntityDeathViaPowerPrototype : ScoringEventPrototype
    {
        public ulong EntityKeyword { get; set; }
        public ulong EntityPrototype { get; set; }
        public bool EntityPrototypeIncludeChildren { get; set; }
        public ulong Power { get; set; }
        public ulong PowerKeyword { get; set; }
        public ulong Rank { get; set; }
        public ulong RankKeyword { get; set; }
    }

    public class ScoringEventEntityInteractPrototype : ScoringEventPrototype
    {
        public ulong EntityKeyword { get; set; }
        public ulong EntityPrototype { get; set; }
        public bool EntityPrototypeIncludeChildren { get; set; }
    }

    public class ScoringEventFullyUpgradedLgndrysPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventFullyUpgradedPetTechPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventHotspotEnterPrototype : ScoringEventPrototype
    {
        public ulong HotspotEntity { get; set; }
        public bool HotspotEntityIncludeChildren { get; set; }
        public ulong HotspotKeyword { get; set; }
    }

    public class ScoringEventHoursPlayedPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventHoursPlayedByAvatarPrototype : ScoringEventPrototype
    {
        public ulong Avatar { get; set; }
    }

    public class ScoringEventItemBoughtPrototype : ScoringEventPrototype
    {
        public ulong ItemKeyword { get; set; }
        public ulong ItemPrototype { get; set; }
        public bool ItemPrototypeIncludeChildren { get; set; }
        public ulong Rarity { get; set; }
    }

    public class ScoringEventItemCollectedPrototype : ScoringEventPrototype
    {
        public ulong ItemKeyword { get; set; }
        public ulong ItemPrototype { get; set; }
        public bool ItemPrototypeIncludeChildren { get; set; }
        public ulong Rarity { get; set; }
    }

    public class ScoringEventItemCraftedPrototype : ScoringEventPrototype
    {
        public ulong Rarity { get; set; }
        public ulong RecipeKeyword { get; set; }
        public ulong RecipePrototype { get; set; }
        public bool RecipePrototypeIncludeChildren { get; set; }
    }

    public class ScoringEventItemDonatedPrototype : ScoringEventPrototype
    {
        public ulong ItemKeyword { get; set; }
        public ulong ItemPrototype { get; set; }
        public bool ItemPrototypeIncludeChildren { get; set; }
        public ulong Rarity { get; set; }
    }

    public class ScoringEventItemSpentPrototype : ScoringEventPrototype
    {
        public ulong ItemKeyword { get; set; }
        public ulong ItemPrototype { get; set; }
        public bool ItemPrototypeIncludeChildren { get; set; }
    }

    public class ScoringEventMetaGameModeCompletePrototype : ScoringEventPrototype
    {
        public ulong MetaGameMode { get; set; }
    }

    public class ScoringEventMetaGameStateCompltePrototype : ScoringEventPrototype
    {
        public ulong MetaGameState { get; set; }
        public ulong ItemRarity { get; set; }
    }

    public class ScoringEventMetaGameStateCompDifPrototype : ScoringEventPrototype
    {
        public ulong ItemRarity { get; set; }
        public ulong MetaGameState { get; set; }
    }

    public class ScoringEventMetaGameStateCompAfxPrototype : ScoringEventPrototype
    {
        public ulong ItemRarity { get; set; }
        public ulong MetaGameState { get; set; }
        public ulong RegionAffix { get; set; }
    }

    public class ScoringEventMetaGameWaveCompletePrototype : ScoringEventPrototype
    {
        public ulong MetaGameMode { get; set; }
    }

    public class ScoringEventMinGearLevelPrototype : ScoringEventPrototype
    {
        public ulong Avatar { get; set; }
    }

    public class ScoringEventOrbsCollectedPrototype : ScoringEventPrototype
    {
        public ulong OrbKeyword { get; set; }
        public ulong OrbPrototype { get; set; }
        public bool OrbPrototypeIncludeChildren { get; set; }
    }

    public class ScoringEventPowerRankPrototype : ScoringEventPrototype
    {
        public ulong Power { get; set; }
        public ulong PowerKeyword { get; set; }
    }

    public class ScoringEventPowerRankUltimatePrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventPvPMatchLostPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventPvPMatchWonPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventRegionEnterPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventVendorLevelPrototype : ScoringEventPrototype
    {
        public ulong VendorType { get; set; }
    }

    public class ScoringEventWaypointUnlockedPrototype : ScoringEventPrototype
    {
        public ulong Waypoint { get; set; }
    }
}
