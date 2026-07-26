using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Events;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class ScoringEventContextPrototype : Prototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public AvatarPrototype ContextAvatar { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public ItemPrototype ContextItemEquipped { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public PartyFilterPrototype ContextParty { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public ItemPrototype ContextPet { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public RegionPrototype ContextRegion { get; protected set; }
        public bool ContextRegionIncludeChildren { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public RegionKeywordPrototype ContextRegionKeyword { get; protected set; }
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public DifficultyTierPrototype ContextDifficultyTierMin { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public DifficultyTierPrototype ContextDifficultyTierMax { get; protected set; }
#endif
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public AgentTeamUpPrototype ContextTeamUp { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public PublicEventTeamPrototype ContextPublicEventTeam { get; protected set; }

        //---

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

        //---

        [DoNotCopy]
        public ScoringEventType Type { get; protected set; }
        [DoNotCopy]
        public Prototype Proto0 { get; protected set; }
        [DoNotCopy]
        public Prototype Proto1 { get; protected set; }
        [DoNotCopy]
        public Prototype Proto2 { get; protected set; }
        [DoNotCopy]
        public bool Proto0IncludeChildren { get; protected set; }
        [DoNotCopy]
        public bool Proto1IncludeChildren { get; protected set; }
        [DoNotCopy]
        public bool Proto2IncludeChildren { get; protected set; }
    }

    public class ScoringEventAchievementScorePrototype : ScoringEventPrototype
    {
        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AchievementScore;
        }
    }

    public class ScoringEventAreaEnterPrototype : ScoringEventPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public AreaPrototype Area { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AreaEnter;
            Proto0 = Area;
        }
    }

    public class ScoringEventAvatarDeathPrototype : ScoringEventPrototype
    {
        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AvatarDeath;
        }
    }

    public class ScoringEventAvatarKillPrototype : ScoringEventPrototype
    {
        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AvatarKill;
        }
    }

    public class ScoringEventAvatarKillAssistPrototype : ScoringEventPrototype
    {
        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AvatarKillAssist;
        }
    }

    public class ScoringEventAvatarLevelPrototype : ScoringEventPrototype
    {
        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AvatarLevel;
        }
    }

    public class ScoringEventAvatarLevelTotalPrototype : ScoringEventPrototype
    {
        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AvatarLevelTotal;
        }
    }

    public class ScoringEventAvatarLevelTotalAllAvatarsPrototype : ScoringEventPrototype
    {
        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AvatarLevelTotalAllAvatars;
        }
    }

    public class ScoringEventAvatarPrestigeLevelPrototype : ScoringEventPrototype
    {
        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AvatarPrestigeLevel;
        }
    }

    public class ScoringEventAvatarsAtLevelCapPrototype : ScoringEventPrototype
    {
        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AvatarsAtLevelCap;
        }
    }

    public class ScoringEventAvatarsAtPrstgLvlPrototype : ScoringEventPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public PrestigeLevelPrototype PrestigeLevel { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AvatarsAtPrestigeLevel;
            Proto0 = PrestigeLevel;
        }
    }

    public class ScoringEventAvatarsAtPrstgLvlCapPrototype : ScoringEventPrototype
    {
        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AvatarsAtPrestigeLevelCap;
        }
    }

    public class ScoringEventAvatarsUnlockedPrototype : ScoringEventPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public AvatarPrototype Avatar { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AvatarsUnlocked;
            Proto0 = Avatar;
        }
    }

    public class ScoringEventAvatarUsedPowerPrototype : ScoringEventPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public PowerPrototype Power { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public PowerKeywordPrototype PowerKeyword { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public MobKeywordPrototype TargetKeyword { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public WorldEntityPrototype TargetPrototype { get; protected set; }
        public bool TargetPrototypeIncludeChildren { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();

            Type = ScoringEventType.AvatarUsedPower;
            if (PowerKeyword != null)
                Proto0 = PowerKeyword;
            else
                Proto0 = Power;

            if (TargetPrototype != null)
            {
                Proto1 = TargetPrototype;
                Proto1IncludeChildren = TargetPrototypeIncludeChildren;
            }
            else
            {
                Proto1 = TargetKeyword;
            }
        }
    }

    public class ScoringEventCompleteMissionPrototype : ScoringEventPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public MissionPrototype Mission { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public MissionKeywordPrototype MissionKeyword { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();

            Type = ScoringEventType.CompleteMission;
            if (MissionKeyword != null)
                Proto0 = MissionKeyword;
            else
                Proto0 = Mission;
        }
    }

    public class ScoringEventCompletionTimePrototype : ScoringEventPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public ScoringEventTimerPrototype Timer { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.CompletionTime;
            Proto0 = Timer;
        }
    }

    public class ScoringEventCurrencyCollectedPrototype : ScoringEventPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public CurrencyPrototype Currency { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.CurrencyCollected;
            Proto0 = Currency;
        }
    }

    public class ScoringEventCurrencySpentPrototype : ScoringEventPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public CurrencyPrototype Currency { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.CurrencySpent;
            Proto0 = Currency;
        }
    }

    public class ScoringEventEntityDeathPrototype : ScoringEventPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public MobKeywordPrototype EntityKeyword { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public WorldEntityPrototype EntityPrototype { get; protected set; }
        public bool EntityPrototypeIncludeChildren { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public RankPrototype Rank { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public RankKeywordPrototype RankKeyword { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();

            Type = ScoringEventType.EntityDeath;
            if (EntityPrototype != null)
            {
                Proto0 = EntityPrototype;
                Proto0IncludeChildren = EntityPrototypeIncludeChildren;
            }
            else
            {
                Proto0 = EntityKeyword;
            }

            if (RankKeyword != null)
                Proto1 = RankKeyword;
            else
                Proto1 = Rank;
        }
    }

    public class ScoringEventEntityDeathViaPowerPrototype : ScoringEventPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public MobKeywordPrototype EntityKeyword { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public WorldEntityPrototype EntityPrototype { get; protected set; }
        public bool EntityPrototypeIncludeChildren { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public PowerPrototype Power { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public PowerKeywordPrototype PowerKeyword { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public RankPrototype Rank { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public RankKeywordPrototype RankKeyword { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();

            Type = ScoringEventType.EntityDeathViaPower;
            if (EntityPrototype != null)
            {
                Proto0 = EntityPrototype;
                Proto0IncludeChildren = EntityPrototypeIncludeChildren;
            }
            else
            {
                Proto0 = EntityKeyword;
            }
            
            if (PowerKeyword != null)
                Proto1 = PowerKeyword;
            else
                Proto1 = Power;

            if (RankKeyword != null)
                Proto2 = RankKeyword;
            else
                Proto2 = Rank;
        }
    }

    public class ScoringEventEntityInteractPrototype : ScoringEventPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public MobKeywordPrototype EntityKeyword { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public WorldEntityPrototype EntityPrototype { get; protected set; }
        public bool EntityPrototypeIncludeChildren { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();

            Type = ScoringEventType.EntityInteract;
            if (EntityPrototype != null)
            {
                Proto0 = EntityPrototype;
                Proto0IncludeChildren = EntityPrototypeIncludeChildren;
            }
            else
            {
                Proto0 = EntityKeyword;
            }
        }
    }

    public class ScoringEventFullyUpgradedLgndrysPrototype : ScoringEventPrototype
    {
        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.FullyUpgradedLegendaries;
        }
    }

    public class ScoringEventFullyUpgradedPetTechPrototype : ScoringEventPrototype
    {
        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.FullyUpgradedPetTech;
        }
    }

    public class ScoringEventHotspotEnterPrototype : ScoringEventPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public HotspotPrototype HotspotEntity { get; protected set; }
        public bool HotspotEntityIncludeChildren { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public EntityKeywordPrototype HotspotKeyword { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();

            Type = ScoringEventType.HotspotEnter;
            if (HotspotEntity != null)
            {
                Proto0 = HotspotEntity;
                Proto0IncludeChildren = HotspotEntityIncludeChildren;
            }
            else
            {
                Proto0 = HotspotKeyword;
            }
        }
    }

    public class ScoringEventHoursPlayedPrototype : ScoringEventPrototype
    {
        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.HoursPlayed;
        }
    }

    public class ScoringEventHoursPlayedByAvatarPrototype : ScoringEventPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public AvatarPrototype Avatar { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.HoursPlayedByAvatar;
            Proto0 = Avatar;
        }
    }

    public class ScoringEventItemBoughtPrototype : ScoringEventPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public EntityKeywordPrototype ItemKeyword { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public ItemPrototype ItemPrototype { get; protected set; }
        public bool ItemPrototypeIncludeChildren { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public RarityPrototype Rarity { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();

            Type = ScoringEventType.ItemBought;
            if (ItemPrototype != null)
            {
                Proto0 = ItemPrototype;
                Proto0IncludeChildren = ItemPrototypeIncludeChildren;
            }
            else
            {
                Proto0 = ItemKeyword;
            }
            
            Proto1 = Rarity;
        }
    }

    public class ScoringEventItemCollectedPrototype : ScoringEventPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public EntityKeywordPrototype ItemKeyword { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public ItemPrototype ItemPrototype { get; protected set; }
        public bool ItemPrototypeIncludeChildren { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public RarityPrototype Rarity { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();

            Type = ScoringEventType.ItemCollected;
            if (ItemPrototype != null)
            {
                Proto0 = ItemPrototype;
                Proto0IncludeChildren = ItemPrototypeIncludeChildren;
            }
            else
            {
                Proto0 = ItemKeyword;
            }

            Proto1 = Rarity;
        }
    }

    public class ScoringEventItemCraftedPrototype : ScoringEventPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public RarityPrototype Rarity { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public EntityKeywordPrototype RecipeKeyword { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public CraftingRecipePrototype RecipePrototype { get; protected set; }
        public bool RecipePrototypeIncludeChildren { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();

            Type = ScoringEventType.ItemCrafted;
            if (RecipePrototype != null)
            {
                Proto0 = RecipePrototype;
                Proto0IncludeChildren = RecipePrototypeIncludeChildren;
            }
            else
            {
                Proto0 = RecipeKeyword;
            }

            Proto1 = Rarity;
        }
    }

    public class ScoringEventItemDonatedPrototype : ScoringEventPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public EntityKeywordPrototype ItemKeyword { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public ItemPrototype ItemPrototype { get; protected set; }
        public bool ItemPrototypeIncludeChildren { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public RarityPrototype Rarity { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();

            Type = ScoringEventType.ItemDonated;
            if (ItemPrototype != null)
            {
                Proto0 = ItemPrototype;
                Proto0IncludeChildren = ItemPrototypeIncludeChildren;
            }
            else
            {
                Proto0 = ItemKeyword;
            }

            Proto1 = Rarity;
        }
    }

    public class ScoringEventItemSpentPrototype : ScoringEventPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public EntityKeywordPrototype ItemKeyword { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public ItemPrototype ItemPrototype { get; protected set; }
        public bool ItemPrototypeIncludeChildren { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();

            Type = ScoringEventType.ItemSpent;
            if (ItemPrototype != null)
            {
                Proto0 = ItemPrototype;
                Proto0IncludeChildren = ItemPrototypeIncludeChildren;
            }
            else
            {
                Proto0 = ItemKeyword;
            }
        }
    }

    public class ScoringEventMetaGameModeCompletePrototype : ScoringEventPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public MetaGameModePrototype MetaGameMode { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.MetaGameModeComplete;
            Proto0 = MetaGameMode;
        }
    }

    public class ScoringEventMetaGameStateCompltePrototype : ScoringEventPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public MetaStateMissionActivatePrototype MetaGameState { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public RarityPrototype ItemRarity { get; protected set; }

        //---

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
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public RarityPrototype ItemRarity { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public MetaStateMissionActivatePrototype MetaGameState { get; protected set; }

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
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public RarityPrototype ItemRarity { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public MetaStateMissionActivatePrototype MetaGameState { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public RegionAffixPrototype RegionAffix { get; protected set; }

        //---

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
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public MetaGameModePrototype MetaGameMode { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.MetaGameWaveComplete;
            Proto0 = MetaGameMode;
        }
    }

    public class ScoringEventMinGearLevelPrototype : ScoringEventPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public AvatarPrototype Avatar { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.MinGearLevel;
            Proto0 = Avatar;
        }
    }

    public class ScoringEventOrbsCollectedPrototype : ScoringEventPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public EntityKeywordPrototype OrbKeyword { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public OrbPrototype OrbPrototype { get; protected set; }
        public bool OrbPrototypeIncludeChildren { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();

            Type = ScoringEventType.OrbsCollected;
            if (OrbPrototype != null)
            {
                Proto0 = OrbPrototype;
                Proto0IncludeChildren = OrbPrototypeIncludeChildren;
            }
            else
            {
                Proto0 = OrbKeyword;
            }
        }
    }

    public class ScoringEventPowerRankPrototype : ScoringEventPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public PowerPrototype Power { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public PowerKeywordPrototype PowerKeyword { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();

            Type = ScoringEventType.PowerRank;
            if (PowerKeyword != null)
                Proto0 = PowerKeyword;
            else
                Proto0 = Power;
        }
    }

    public class ScoringEventPowerRankUltimatePrototype : ScoringEventPrototype
    {
        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.PowerRankUltimate;
        }
    }

    public class ScoringEventPvPMatchLostPrototype : ScoringEventPrototype
    {
        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.PvPMatchLost;
        }
    }

    public class ScoringEventPvPMatchWonPrototype : ScoringEventPrototype
    {
        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.PvPMatchWon;
        }
    }

    public class ScoringEventRegionEnterPrototype : ScoringEventPrototype
    {
        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.RegionEnter;
        }
    }

    public class ScoringEventVendorLevelPrototype : ScoringEventPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public VendorTypePrototype VendorType { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.VendorLevel;
            Proto0 = VendorType;
        }
    }

    public class ScoringEventWaypointUnlockedPrototype : ScoringEventPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public WaypointPrototype Waypoint { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.WaypointUnlocked;
            Proto0 = Waypoint;
        }
    }

#if GAME_VERSION_1_53
    public class ScoringEventAvatarOmegaPrestigeLevelPrototype : ScoringEventPrototype
    {
        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AvatarOmegaPrestigeLevel;
        }
    }
#endif

#if GAME_VERSION_1_53
    public class ScoringEventAvatarsAtOmegaPrestigeLevelPrototype : ScoringEventPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public OmegaPrestigeLevelPrototype OmegaPrestigeLevel { get; protected set; }

        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AvatarsAtOmegaPrestigeLevel;
            Proto0 = OmegaPrestigeLevel;
        }
    }
#endif

#if GAME_VERSION_1_53
    public class ScoringEventAvatarsAtOmegaPrestigeLevelCapPrototype : ScoringEventPrototype
    {
        //---

        public override void PostProcess()
        {
            base.PostProcess();
            Type = ScoringEventType.AvatarsAtOmegaPrestigeLevelCap;
        }
    }
#endif
}
