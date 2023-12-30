namespace MHServerEmu.Games.GameData.Prototypes
{
    public class ScoringEventContextPrototype : Prototype
    {
        public ulong ContextAvatar { get; protected set; }
        public ulong ContextItemEquipped { get; protected set; }
        public ulong ContextParty { get; protected set; }
        public ulong ContextPet { get; protected set; }
        public ulong ContextRegion { get; protected set; }
        public bool ContextRegionIncludeChildren { get; protected set; }
        public ulong ContextRegionKeyword { get; protected set; }
        public ulong ContextDifficultyTierMin { get; protected set; }
        public ulong ContextDifficultyTierMax { get; protected set; }
        public ulong ContextTeamUp { get; protected set; }
        public ulong ContextPublicEventTeam { get; protected set; }
    }

    public class ScoringEventTimerPrototype : Prototype
    {
        public ulong UIWidget { get; protected set; }
    }

    public class ScoringEventPrototype : Prototype
    {
        public ScoringEventContextPrototype Context { get; protected set; }
    }

    public class ScoringEventAchievementScorePrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventAreaEnterPrototype : ScoringEventPrototype
    {
        public ulong Area { get; protected set; }
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
        public ulong PrestigeLevel { get; protected set; }
    }

    public class ScoringEventAvatarsAtPrstgLvlCapPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventAvatarsUnlockedPrototype : ScoringEventPrototype
    {
        public ulong Avatar { get; protected set; }
    }

    public class ScoringEventAvatarUsedPowerPrototype : ScoringEventPrototype
    {
        public ulong Power { get; protected set; }
        public ulong PowerKeyword { get; protected set; }
        public ulong TargetKeyword { get; protected set; }
        public ulong TargetPrototype { get; protected set; }
        public bool TargetPrototypeIncludeChildren { get; protected set; }
    }

    public class ScoringEventCompleteMissionPrototype : ScoringEventPrototype
    {
        public ulong Mission { get; protected set; }
        public ulong MissionKeyword { get; protected set; }
    }

    public class ScoringEventCompletionTimePrototype : ScoringEventPrototype
    {
        public ulong Timer { get; protected set; }
    }

    public class ScoringEventCurrencyCollectedPrototype : ScoringEventPrototype
    {
        public ulong Currency { get; protected set; }
    }

    public class ScoringEventCurrencySpentPrototype : ScoringEventPrototype
    {
        public ulong Currency { get; protected set; }
    }

    public class ScoringEventEntityDeathPrototype : ScoringEventPrototype
    {
        public ulong EntityKeyword { get; protected set; }
        public ulong EntityPrototype { get; protected set; }
        public bool EntityPrototypeIncludeChildren { get; protected set; }
        public ulong Rank { get; protected set; }
        public ulong RankKeyword { get; protected set; }
    }

    public class ScoringEventEntityDeathViaPowerPrototype : ScoringEventPrototype
    {
        public ulong EntityKeyword { get; protected set; }
        public ulong EntityPrototype { get; protected set; }
        public bool EntityPrototypeIncludeChildren { get; protected set; }
        public ulong Power { get; protected set; }
        public ulong PowerKeyword { get; protected set; }
        public ulong Rank { get; protected set; }
        public ulong RankKeyword { get; protected set; }
    }

    public class ScoringEventEntityInteractPrototype : ScoringEventPrototype
    {
        public ulong EntityKeyword { get; protected set; }
        public ulong EntityPrototype { get; protected set; }
        public bool EntityPrototypeIncludeChildren { get; protected set; }
    }

    public class ScoringEventFullyUpgradedLgndrysPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventFullyUpgradedPetTechPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventHotspotEnterPrototype : ScoringEventPrototype
    {
        public ulong HotspotEntity { get; protected set; }
        public bool HotspotEntityIncludeChildren { get; protected set; }
        public ulong HotspotKeyword { get; protected set; }
    }

    public class ScoringEventHoursPlayedPrototype : ScoringEventPrototype
    {
    }

    public class ScoringEventHoursPlayedByAvatarPrototype : ScoringEventPrototype
    {
        public ulong Avatar { get; protected set; }
    }

    public class ScoringEventItemBoughtPrototype : ScoringEventPrototype
    {
        public ulong ItemKeyword { get; protected set; }
        public ulong ItemPrototype { get; protected set; }
        public bool ItemPrototypeIncludeChildren { get; protected set; }
        public ulong Rarity { get; protected set; }
    }

    public class ScoringEventItemCollectedPrototype : ScoringEventPrototype
    {
        public ulong ItemKeyword { get; protected set; }
        public ulong ItemPrototype { get; protected set; }
        public bool ItemPrototypeIncludeChildren { get; protected set; }
        public ulong Rarity { get; protected set; }
    }

    public class ScoringEventItemCraftedPrototype : ScoringEventPrototype
    {
        public ulong Rarity { get; protected set; }
        public ulong RecipeKeyword { get; protected set; }
        public ulong RecipePrototype { get; protected set; }
        public bool RecipePrototypeIncludeChildren { get; protected set; }
    }

    public class ScoringEventItemDonatedPrototype : ScoringEventPrototype
    {
        public ulong ItemKeyword { get; protected set; }
        public ulong ItemPrototype { get; protected set; }
        public bool ItemPrototypeIncludeChildren { get; protected set; }
        public ulong Rarity { get; protected set; }
    }

    public class ScoringEventItemSpentPrototype : ScoringEventPrototype
    {
        public ulong ItemKeyword { get; protected set; }
        public ulong ItemPrototype { get; protected set; }
        public bool ItemPrototypeIncludeChildren { get; protected set; }
    }

    public class ScoringEventMetaGameModeCompletePrototype : ScoringEventPrototype
    {
        public ulong MetaGameMode { get; protected set; }
    }

    public class ScoringEventMetaGameStateCompltePrototype : ScoringEventPrototype
    {
        public ulong MetaGameState { get; protected set; }
        public ulong ItemRarity { get; protected set; }
    }

    public class ScoringEventMetaGameStateCompDifPrototype : ScoringEventPrototype
    {
        public ulong ItemRarity { get; protected set; }
        public ulong MetaGameState { get; protected set; }
    }

    public class ScoringEventMetaGameStateCompAfxPrototype : ScoringEventPrototype
    {
        public ulong ItemRarity { get; protected set; }
        public ulong MetaGameState { get; protected set; }
        public ulong RegionAffix { get; protected set; }
    }

    public class ScoringEventMetaGameWaveCompletePrototype : ScoringEventPrototype
    {
        public ulong MetaGameMode { get; protected set; }
    }

    public class ScoringEventMinGearLevelPrototype : ScoringEventPrototype
    {
        public ulong Avatar { get; protected set; }
    }

    public class ScoringEventOrbsCollectedPrototype : ScoringEventPrototype
    {
        public ulong OrbKeyword { get; protected set; }
        public ulong OrbPrototype { get; protected set; }
        public bool OrbPrototypeIncludeChildren { get; protected set; }
    }

    public class ScoringEventPowerRankPrototype : ScoringEventPrototype
    {
        public ulong Power { get; protected set; }
        public ulong PowerKeyword { get; protected set; }
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
        public ulong VendorType { get; protected set; }
    }

    public class ScoringEventWaypointUnlockedPrototype : ScoringEventPrototype
    {
        public ulong Waypoint { get; protected set; }
    }
}
