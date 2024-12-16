using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.Events;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class ScoringEventContextPrototype : Prototype
    {
        public PrototypeId ContextAvatar { get; protected set; }
        public PrototypeId ContextItemEquipped { get; protected set; }
        public PrototypeId ContextParty { get; protected set; }
        public PrototypeId ContextPet { get; protected set; }
        public PrototypeId ContextRegion { get; protected set; }
        public bool ContextRegionIncludeChildren { get; protected set; }
        public PrototypeId ContextRegionKeyword { get; protected set; }
        public PrototypeId ContextDifficultyTierMin { get; protected set; }
        public PrototypeId ContextDifficultyTierMax { get; protected set; }
        public PrototypeId ContextTeamUp { get; protected set; }
        public PrototypeId ContextPublicEventTeam { get; protected set; }

        [DoNotCopy]
        public ScoringEventContext Context { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Context = new(this);
        }
    }

    public class ScoringEventTimerPrototype : Prototype
    {
        public PrototypeId UIWidget { get; protected set; }
    }

    public class ScoringEventPrototype : Prototype
    {
        public ScoringEventContextPrototype Context { get; protected set; }

        [DoNotCopy]
        public ScoringEventType Type { get; protected set; }
        [DoNotCopy]
        public PrototypeId Proto0 { get; protected set; }
        [DoNotCopy]
        public PrototypeId Proto1 { get; protected set; }
        [DoNotCopy]
        public PrototypeId Proto2 { get; protected set; }
        [DoNotCopy]
        public bool Proto0IncludeChildren { get; protected set; }
        [DoNotCopy]
        public bool Proto1IncludeChildren { get; protected set; }
        [DoNotCopy]
        public bool Proto2IncludeChildren { get; protected set; }
    }

    public class ScoringEventAchievementScorePrototype : ScoringEventPrototype
    {
        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AchievementScore;
        }
    }

    public class ScoringEventAreaEnterPrototype : ScoringEventPrototype
    {
        public PrototypeId Area { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AreaEnter;
            Proto0 = Area;
        }
    }

    public class ScoringEventAvatarDeathPrototype : ScoringEventPrototype
    {
        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AvatarDeath;
        }
    }

    public class ScoringEventAvatarKillPrototype : ScoringEventPrototype
    {
        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AvatarKill;
        }
    }

    public class ScoringEventAvatarKillAssistPrototype : ScoringEventPrototype
    {
        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AvatarKillAssist;
        }
    }

    public class ScoringEventAvatarLevelPrototype : ScoringEventPrototype
    {
        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AvatarLevel;
        }
    }

    public class ScoringEventAvatarLevelTotalPrototype : ScoringEventPrototype
    {
        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AvatarLevelTotal;
        }
    }

    public class ScoringEventAvatarLevelTotalAllAvatarsPrototype : ScoringEventPrototype
    {
        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AvatarLevelTotalAllAvatars;
        }
    }

    public class ScoringEventAvatarPrestigeLevelPrototype : ScoringEventPrototype
    {
        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AvatarPrestigeLevel;
        }
    }

    public class ScoringEventAvatarsAtLevelCapPrototype : ScoringEventPrototype
    {
        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AvatarsAtLevelCap;
        }
    }

    public class ScoringEventAvatarsAtPrstgLvlPrototype : ScoringEventPrototype
    {
        public PrototypeId PrestigeLevel { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AvatarsAtPrestigeLevel;
            Proto0 = PrestigeLevel;
        }
    }

    public class ScoringEventAvatarsAtPrstgLvlCapPrototype : ScoringEventPrototype
    {
        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AvatarsAtPrestigeLevelCap;
        }
    }

    public class ScoringEventAvatarsUnlockedPrototype : ScoringEventPrototype
    {
        public PrototypeId Avatar { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AvatarsUnlocked;
            Proto0 = Avatar;
        }
    }

    public class ScoringEventAvatarUsedPowerPrototype : ScoringEventPrototype
    {
        public PrototypeId Power { get; protected set; }
        public PrototypeId PowerKeyword { get; protected set; }
        public PrototypeId TargetKeyword { get; protected set; }
        public PrototypeId TargetPrototype { get; protected set; }
        public bool TargetPrototypeIncludeChildren { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();

            Type = ScoringEventType.AvatarUsedPower;
            if (PowerKeyword != PrototypeId.Invalid)
                Proto0 = PowerKeyword;
            else
                Proto0 = Power;

            if (TargetPrototype != PrototypeId.Invalid)
            {
                Proto1 = TargetPrototype;
                Proto1IncludeChildren = TargetPrototypeIncludeChildren;
            }
            else
                Proto1 = TargetKeyword;
        }
    }

    public class ScoringEventCompleteMissionPrototype : ScoringEventPrototype
    {
        public PrototypeId Mission { get; protected set; }
        public PrototypeId MissionKeyword { get; protected set; }
        public override void PostProcess()
        {
            base.PostProcess();

            Type = ScoringEventType.CompleteMission;
            if (MissionKeyword != PrototypeId.Invalid)
                Proto0 = MissionKeyword;
            else
                Proto0 = Mission;
        }
    }

    public class ScoringEventCompletionTimePrototype : ScoringEventPrototype
    {
        public PrototypeId Timer { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.CompletionTime;
            Proto0 = Timer;
        }
    }

    public class ScoringEventCurrencyCollectedPrototype : ScoringEventPrototype
    {
        public PrototypeId Currency { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.CurrencyCollected;
            Proto0 = Currency;
        }
    }

    public class ScoringEventCurrencySpentPrototype : ScoringEventPrototype
    {
        public PrototypeId Currency { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.CurrencySpent;
            Proto0 = Currency;
        }
    }

    public class ScoringEventEntityDeathPrototype : ScoringEventPrototype
    {
        public PrototypeId EntityKeyword { get; protected set; }
        public PrototypeId EntityPrototype { get; protected set; }
        public bool EntityPrototypeIncludeChildren { get; protected set; }
        public PrototypeId Rank { get; protected set; }
        public PrototypeId RankKeyword { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();

            Type = ScoringEventType.EntityDeath;
            if (EntityPrototype != PrototypeId.Invalid)
            {
                Proto0 = EntityPrototype;
                Proto0IncludeChildren = EntityPrototypeIncludeChildren;
            }
            else
                Proto0 = EntityKeyword;

            if (RankKeyword != PrototypeId.Invalid)
                Proto1 = RankKeyword;
            else
                Proto1 = Rank;
        }
    }

    public class ScoringEventEntityDeathViaPowerPrototype : ScoringEventPrototype
    {
        public PrototypeId EntityKeyword { get; protected set; }
        public PrototypeId EntityPrototype { get; protected set; }
        public bool EntityPrototypeIncludeChildren { get; protected set; }
        public PrototypeId Power { get; protected set; }
        public PrototypeId PowerKeyword { get; protected set; }
        public PrototypeId Rank { get; protected set; }
        public PrototypeId RankKeyword { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();

            Type = ScoringEventType.EntityDeathViaPower;
            if (EntityPrototype != PrototypeId.Invalid)
            {
                Proto0 = EntityPrototype;
                Proto0IncludeChildren = EntityPrototypeIncludeChildren;
            }
            else
                Proto0 = EntityKeyword;
            
            if (PowerKeyword != PrototypeId.Invalid)
                Proto1 = PowerKeyword;
            else
                Proto1 = Power;

            if (RankKeyword != PrototypeId.Invalid)
                Proto2 = RankKeyword;
            else
                Proto2 = Rank;
        }
    }

    public class ScoringEventEntityInteractPrototype : ScoringEventPrototype
    {
        public PrototypeId EntityKeyword { get; protected set; }
        public PrototypeId EntityPrototype { get; protected set; }
        public bool EntityPrototypeIncludeChildren { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();

            Type = ScoringEventType.EntityInteract;
            if (EntityPrototype != PrototypeId.Invalid)
            {
                Proto0 = EntityPrototype;
                Proto0IncludeChildren = EntityPrototypeIncludeChildren;
            }
            else
                Proto0 = EntityKeyword;
        }
    }

    public class ScoringEventFullyUpgradedLgndrysPrototype : ScoringEventPrototype
    {
        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.FullyUpgradedLegendaries;
        }
    }

    public class ScoringEventFullyUpgradedPetTechPrototype : ScoringEventPrototype
    {
        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.FullyUpgradedPetTech;
        }
    }

    public class ScoringEventHotspotEnterPrototype : ScoringEventPrototype
    {
        public PrototypeId HotspotEntity { get; protected set; }
        public bool HotspotEntityIncludeChildren { get; protected set; }
        public PrototypeId HotspotKeyword { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();

            Type = ScoringEventType.HotspotEnter;
            if (HotspotEntity != PrototypeId.Invalid)
            {
                Proto0 = HotspotEntity;
                Proto0IncludeChildren = HotspotEntityIncludeChildren;
            }
            else
                Proto0 = HotspotKeyword;
        }
    }

    public class ScoringEventHoursPlayedPrototype : ScoringEventPrototype
    {
        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.HoursPlayed;
        }
    }

    public class ScoringEventHoursPlayedByAvatarPrototype : ScoringEventPrototype
    {
        public PrototypeId Avatar { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.HoursPlayedByAvatar;
            Proto0 = Avatar;
        }
    }

    public class ScoringEventItemBoughtPrototype : ScoringEventPrototype
    {
        public PrototypeId ItemKeyword { get; protected set; }
        public PrototypeId ItemPrototype { get; protected set; }
        public bool ItemPrototypeIncludeChildren { get; protected set; }
        public PrototypeId Rarity { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();

            Type = ScoringEventType.ItemBought;
            if (ItemPrototype != PrototypeId.Invalid)
            {
                Proto0 = ItemPrototype;
                Proto0IncludeChildren = ItemPrototypeIncludeChildren;
            }
            else
                Proto0 = ItemKeyword;
            
            Proto1 = Rarity;
        }
    }

    public class ScoringEventItemCollectedPrototype : ScoringEventPrototype
    {
        public PrototypeId ItemKeyword { get; protected set; }
        public PrototypeId ItemPrototype { get; protected set; }
        public bool ItemPrototypeIncludeChildren { get; protected set; }
        public PrototypeId Rarity { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();

            Type = ScoringEventType.ItemCollected;
            if (ItemPrototype != PrototypeId.Invalid)
            {
                Proto0 = ItemPrototype;
                Proto0IncludeChildren = ItemPrototypeIncludeChildren;
            }
            else
                Proto0 = ItemKeyword;

            Proto1 = Rarity;
        }
    }

    public class ScoringEventItemCraftedPrototype : ScoringEventPrototype
    {
        public PrototypeId Rarity { get; protected set; }
        public PrototypeId RecipeKeyword { get; protected set; }
        public PrototypeId RecipePrototype { get; protected set; }
        public bool RecipePrototypeIncludeChildren { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();

            Type = ScoringEventType.ItemCrafted;
            if (RecipePrototype != PrototypeId.Invalid)
            {
                Proto0 = RecipePrototype;
                Proto0IncludeChildren = RecipePrototypeIncludeChildren;
            }
            else
                Proto0 = RecipeKeyword;

            Proto1 = Rarity;
        }
    }

    public class ScoringEventItemDonatedPrototype : ScoringEventPrototype
    {
        public PrototypeId ItemKeyword { get; protected set; }
        public PrototypeId ItemPrototype { get; protected set; }
        public bool ItemPrototypeIncludeChildren { get; protected set; }
        public PrototypeId Rarity { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();

            Type = ScoringEventType.ItemDonated;
            if (ItemPrototype != PrototypeId.Invalid)
            {
                Proto0 = ItemPrototype;
                Proto0IncludeChildren = ItemPrototypeIncludeChildren;
            }
            else
                Proto0 = ItemKeyword;

            Proto1 = Rarity;
        }
    }

    public class ScoringEventItemSpentPrototype : ScoringEventPrototype
    {
        public PrototypeId ItemKeyword { get; protected set; }
        public PrototypeId ItemPrototype { get; protected set; }
        public bool ItemPrototypeIncludeChildren { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();

            Type = ScoringEventType.ItemSpent;
            if (ItemPrototype != PrototypeId.Invalid)
            {
                Proto0 = ItemPrototype;
                Proto0IncludeChildren = ItemPrototypeIncludeChildren;
            }
            else
                Proto0 = ItemKeyword;
        }
    }

    public class ScoringEventMetaGameModeCompletePrototype : ScoringEventPrototype
    {
        public PrototypeId MetaGameMode { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.MetaGameModeComplete;
            Proto0 = MetaGameMode;
        }
    }

    public class ScoringEventMetaGameStateCompltePrototype : ScoringEventPrototype
    {
        public PrototypeId MetaGameState { get; protected set; }
        public PrototypeId ItemRarity { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.MetaGameStateComplete;
            Proto0 = MetaGameState;
            Proto1 = ItemRarity;
        }
    }

    public class ScoringEventMetaGameStateCompDifPrototype : ScoringEventPrototype
    {
        public PrototypeId ItemRarity { get; protected set; }
        public PrototypeId MetaGameState { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.MetaGameStateCompleteDifficulty;
            Proto0 = MetaGameState;
            Proto1 = ItemRarity;
        }
    }

    public class ScoringEventMetaGameStateCompAfxPrototype : ScoringEventPrototype
    {
        public PrototypeId ItemRarity { get; protected set; }
        public PrototypeId MetaGameState { get; protected set; }
        public PrototypeId RegionAffix { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.MetaGameStateCompleteAffix;
            Proto0 = MetaGameState;
            Proto1 = ItemRarity;
            Proto2 = RegionAffix;
        }
    }

    public class ScoringEventMetaGameWaveCompletePrototype : ScoringEventPrototype
    {
        public PrototypeId MetaGameMode { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.MetaGameWaveComplete;
            Proto0 = MetaGameMode;
        }
    }

    public class ScoringEventMinGearLevelPrototype : ScoringEventPrototype
    {
        public PrototypeId Avatar { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.MinGearLevel;
            Proto0 = Avatar;
        }
    }

    public class ScoringEventOrbsCollectedPrototype : ScoringEventPrototype
    {
        public PrototypeId OrbKeyword { get; protected set; }
        public PrototypeId OrbPrototype { get; protected set; }
        public bool OrbPrototypeIncludeChildren { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();

            Type = ScoringEventType.OrbsCollected;
            if (OrbPrototype != PrototypeId.Invalid)
            {
                Proto0 = OrbPrototype;
                Proto0IncludeChildren = OrbPrototypeIncludeChildren;
            }
            else
                Proto0 = OrbKeyword;
        }
    }

    public class ScoringEventPowerRankPrototype : ScoringEventPrototype
    {
        public PrototypeId Power { get; protected set; }
        public PrototypeId PowerKeyword { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();

            Type = ScoringEventType.PowerRank;
            if (PowerKeyword != PrototypeId.Invalid)
                Proto0 = PowerKeyword;
            else
                Proto0 = Power;
        }
    }

    public class ScoringEventPowerRankUltimatePrototype : ScoringEventPrototype
    {
        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.PowerRankUltimate;
        }
    }

    public class ScoringEventPvPMatchLostPrototype : ScoringEventPrototype
    {
        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.PvPMatchLost;
        }
    }

    public class ScoringEventPvPMatchWonPrototype : ScoringEventPrototype
    {
        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.PvPMatchWon;
        }
    }

    public class ScoringEventRegionEnterPrototype : ScoringEventPrototype
    {
        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.RegionEnter;
        }
    }

    public class ScoringEventVendorLevelPrototype : ScoringEventPrototype
    {
        public PrototypeId VendorType { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.VendorLevel;
            Proto0 = VendorType;
        }
    }

    public class ScoringEventWaypointUnlockedPrototype : ScoringEventPrototype
    {
        public PrototypeId Waypoint { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.WaypointUnlocked;
            Proto0 = Waypoint;
        }
    }
}
