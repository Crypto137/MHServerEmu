namespace MHServerEmu.Games.GameData.Prototypes
{

    public class ScoringEventContextPrototype : Prototype
    {
        public ulong ContextAvatar;
        public ulong ContextItemEquipped;
        public ulong ContextParty;
        public ulong ContextPet;
        public ulong ContextRegion;
        public bool ContextRegionIncludeChildren;
        public ulong ContextRegionKeyword;
        public ulong ContextDifficultyTierMin;
        public ulong ContextDifficultyTierMax;
        public ulong ContextTeamUp;
        public ulong ContextPublicEventTeam;
        public ScoringEventContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventContextPrototype), proto); }
    }

    public class ScoringEventTimerPrototype : Prototype
    {
        public ulong UIWidget;
        public ScoringEventTimerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventTimerPrototype), proto); }
    }

    public class ScoringEventPrototype : Prototype
    {
        public ScoringEventContextPrototype Context;
        public ScoringEventPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventPrototype), proto); }
    }

    public class ScoringEventAchievementScorePrototype : ScoringEventPrototype
    {
        public ScoringEventAchievementScorePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventAchievementScorePrototype), proto); }
    }

    public class ScoringEventAreaEnterPrototype : ScoringEventPrototype
    {
        public ulong Area;
        public ScoringEventAreaEnterPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventAreaEnterPrototype), proto); }
    }

    public class ScoringEventAvatarDeathPrototype : ScoringEventPrototype
    {
        public ScoringEventAvatarDeathPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventAvatarDeathPrototype), proto); }
    }

    public class ScoringEventAvatarKillPrototype : ScoringEventPrototype
    {
        public ScoringEventAvatarKillPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventAvatarKillPrototype), proto); }
    }

    public class ScoringEventAvatarKillAssistPrototype : ScoringEventPrototype
    {
        public ScoringEventAvatarKillAssistPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventAvatarKillAssistPrototype), proto); }
    }

    public class ScoringEventAvatarLevelPrototype : ScoringEventPrototype
    {
        public ScoringEventAvatarLevelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventAvatarLevelPrototype), proto); }
    }

    public class ScoringEventAvatarLevelTotalPrototype : ScoringEventPrototype
    {
        public ScoringEventAvatarLevelTotalPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventAvatarLevelTotalPrototype), proto); }
    }

    public class ScoringEventAvatarLevelTotalAllAvatarsPrototype : ScoringEventPrototype
    {
        public ScoringEventAvatarLevelTotalAllAvatarsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventAvatarLevelTotalAllAvatarsPrototype), proto); }
    }

    public class ScoringEventAvatarPrestigeLevelPrototype : ScoringEventPrototype
    {
        public ScoringEventAvatarPrestigeLevelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventAvatarPrestigeLevelPrototype), proto); }
    }

    public class ScoringEventAvatarsAtLevelCapPrototype : ScoringEventPrototype
    {
        public ScoringEventAvatarsAtLevelCapPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventAvatarsAtLevelCapPrototype), proto); }
    }

    public class ScoringEventAvatarsAtPrstgLvlPrototype : ScoringEventPrototype
    {
        public ulong PrestigeLevel;
        public ScoringEventAvatarsAtPrstgLvlPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventAvatarsAtPrstgLvlPrototype), proto); }
    }

    public class ScoringEventAvatarsAtPrstgLvlCapPrototype : ScoringEventPrototype
    {
        public ScoringEventAvatarsAtPrstgLvlCapPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventAvatarsAtPrstgLvlCapPrototype), proto); }
    }

    public class ScoringEventAvatarsUnlockedPrototype : ScoringEventPrototype
    {
        public ulong Avatar;
        public ScoringEventAvatarsUnlockedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventAvatarsUnlockedPrototype), proto); }
    }

    public class ScoringEventAvatarUsedPowerPrototype : ScoringEventPrototype
    {
        public ulong Power;
        public ulong PowerKeyword;
        public ulong TargetKeyword;
        public ulong TargetPrototype;
        public bool TargetPrototypeIncludeChildren;
        public ScoringEventAvatarUsedPowerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventAvatarUsedPowerPrototype), proto); }
    }

    public class ScoringEventCompleteMissionPrototype : ScoringEventPrototype
    {
        public ulong Mission;
        public ulong MissionKeyword;
        public ScoringEventCompleteMissionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventCompleteMissionPrototype), proto); }
    }

    public class ScoringEventCompletionTimePrototype : ScoringEventPrototype
    {
        public ulong Timer;
        public ScoringEventCompletionTimePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventCompletionTimePrototype), proto); }
    }

    public class ScoringEventCurrencyCollectedPrototype : ScoringEventPrototype
    {
        public ulong Currency;
        public ScoringEventCurrencyCollectedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventCurrencyCollectedPrototype), proto); }
    }

    public class ScoringEventCurrencySpentPrototype : ScoringEventPrototype
    {
        public ulong Currency;
        public ScoringEventCurrencySpentPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventCurrencySpentPrototype), proto); }
    }

    public class ScoringEventEntityDeathPrototype : ScoringEventPrototype
    {
        public ulong EntityKeyword;
        public ulong EntityPrototype;
        public bool EntityPrototypeIncludeChildren;
        public ulong Rank;
        public ulong RankKeyword;
        public ScoringEventEntityDeathPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventEntityDeathPrototype), proto); }
    }

    public class ScoringEventEntityDeathViaPowerPrototype : ScoringEventPrototype
    {
        public ulong EntityKeyword;
        public ulong EntityPrototype;
        public bool EntityPrototypeIncludeChildren;
        public ulong Power;
        public ulong PowerKeyword;
        public ulong Rank;
        public ulong RankKeyword;
        public ScoringEventEntityDeathViaPowerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventEntityDeathViaPowerPrototype), proto); }
    }

    public class ScoringEventEntityInteractPrototype : ScoringEventPrototype
    {
        public ulong EntityKeyword;
        public ulong EntityPrototype;
        public bool EntityPrototypeIncludeChildren;
        public ScoringEventEntityInteractPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventEntityInteractPrototype), proto); }
    }

    public class ScoringEventFullyUpgradedLgndrysPrototype : ScoringEventPrototype
    {
        public ScoringEventFullyUpgradedLgndrysPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventFullyUpgradedLgndrysPrototype), proto); }
    }

    public class ScoringEventFullyUpgradedPetTechPrototype : ScoringEventPrototype
    {
        public ScoringEventFullyUpgradedPetTechPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventFullyUpgradedPetTechPrototype), proto); }
    }

    public class ScoringEventHotspotEnterPrototype : ScoringEventPrototype
    {
        public ulong HotspotEntity;
        public bool HotspotEntityIncludeChildren;
        public ulong HotspotKeyword;
        public ScoringEventHotspotEnterPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventHotspotEnterPrototype), proto); }
    }

    public class ScoringEventHoursPlayedPrototype : ScoringEventPrototype
    {
        public ScoringEventHoursPlayedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventHoursPlayedPrototype), proto); }
    }

    public class ScoringEventHoursPlayedByAvatarPrototype : ScoringEventPrototype
    {
        public ulong Avatar;
        public ScoringEventHoursPlayedByAvatarPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventHoursPlayedByAvatarPrototype), proto); }
    }

    public class ScoringEventItemBoughtPrototype : ScoringEventPrototype
    {
        public ulong ItemKeyword;
        public ulong ItemPrototype;
        public bool ItemPrototypeIncludeChildren;
        public ulong Rarity;
        public ScoringEventItemBoughtPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventItemBoughtPrototype), proto); }
    }

    public class ScoringEventItemCollectedPrototype : ScoringEventPrototype
    {
        public ulong ItemKeyword;
        public ulong ItemPrototype;
        public bool ItemPrototypeIncludeChildren;
        public ulong Rarity;
        public ScoringEventItemCollectedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventItemCollectedPrototype), proto); }
    }

    public class ScoringEventItemCraftedPrototype : ScoringEventPrototype
    {
        public ulong Rarity;
        public ulong RecipeKeyword;
        public ulong RecipePrototype;
        public bool RecipePrototypeIncludeChildren;
        public ScoringEventItemCraftedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventItemCraftedPrototype), proto); }
    }

    public class ScoringEventItemDonatedPrototype : ScoringEventPrototype
    {
        public ulong ItemKeyword;
        public ulong ItemPrototype;
        public bool ItemPrototypeIncludeChildren;
        public ulong Rarity;
        public ScoringEventItemDonatedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventItemDonatedPrototype), proto); }
    }

    public class ScoringEventItemSpentPrototype : ScoringEventPrototype
    {
        public ulong ItemKeyword;
        public ulong ItemPrototype;
        public bool ItemPrototypeIncludeChildren;
        public ScoringEventItemSpentPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventItemSpentPrototype), proto); }
    }

    public class ScoringEventMetaGameModeCompletePrototype : ScoringEventPrototype
    {
        public ulong MetaGameMode;
        public ScoringEventMetaGameModeCompletePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventMetaGameModeCompletePrototype), proto); }
    }

    public class ScoringEventMetaGameStateCompltePrototype : ScoringEventPrototype
    {
        public ulong MetaGameState;
        public ulong ItemRarity;
        public ScoringEventMetaGameStateCompltePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventMetaGameStateCompltePrototype), proto); }
    }

    public class ScoringEventMetaGameStateCompDifPrototype : ScoringEventPrototype
    {
        public ulong ItemRarity;
        public ulong MetaGameState;
        public ScoringEventMetaGameStateCompDifPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventMetaGameStateCompDifPrototype), proto); }
    }

    public class ScoringEventMetaGameStateCompAfxPrototype : ScoringEventPrototype
    {
        public ulong ItemRarity;
        public ulong MetaGameState;
        public ulong RegionAffix;
        public ScoringEventMetaGameStateCompAfxPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventMetaGameStateCompAfxPrototype), proto); }
    }

    public class ScoringEventMetaGameWaveCompletePrototype : ScoringEventPrototype
    {
        public ulong MetaGameMode;
        public ScoringEventMetaGameWaveCompletePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventMetaGameWaveCompletePrototype), proto); }
    }

    public class ScoringEventMinGearLevelPrototype : ScoringEventPrototype
    {
        public ulong Avatar;
        public ScoringEventMinGearLevelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventMinGearLevelPrototype), proto); }
    }

    public class ScoringEventOrbsCollectedPrototype : ScoringEventPrototype
    {
        public ulong OrbKeyword;
        public ulong OrbPrototype;
        public bool OrbPrototypeIncludeChildren;
        public ScoringEventOrbsCollectedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventOrbsCollectedPrototype), proto); }
    }

    public class ScoringEventPowerRankPrototype : ScoringEventPrototype
    {
        public ulong Power;
        public ulong PowerKeyword;
        public ScoringEventPowerRankPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventPowerRankPrototype), proto); }
    }

    public class ScoringEventPowerRankUltimatePrototype : ScoringEventPrototype
    {
        public ScoringEventPowerRankUltimatePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventPowerRankUltimatePrototype), proto); }
    }

    public class ScoringEventPvPMatchLostPrototype : ScoringEventPrototype
    {
        public ScoringEventPvPMatchLostPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventPvPMatchLostPrototype), proto); }
    }

    public class ScoringEventPvPMatchWonPrototype : ScoringEventPrototype
    {
        public ScoringEventPvPMatchWonPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventPvPMatchWonPrototype), proto); }
    }

    public class ScoringEventRegionEnterPrototype : ScoringEventPrototype
    {
        public ScoringEventRegionEnterPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventRegionEnterPrototype), proto); }
    }

    public class ScoringEventVendorLevelPrototype : ScoringEventPrototype
    {
        public ulong VendorType;
        public ScoringEventVendorLevelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventVendorLevelPrototype), proto); }
    }

    public class ScoringEventWaypointUnlockedPrototype : ScoringEventPrototype
    {
        public ulong Waypoint;
        public ScoringEventWaypointUnlockedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ScoringEventWaypointUnlockedPrototype), proto); }
    }
}
