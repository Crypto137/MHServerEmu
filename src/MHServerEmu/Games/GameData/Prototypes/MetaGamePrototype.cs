namespace MHServerEmu.Games.GameData.Prototypes
{
    public class MetaGamePrototype : EntityPrototype
    {
        public float Duration;
        public ulong[] Teams;
        public ulong[] GameModes;
        public ulong BodysliderOverride;
        public ulong MetaGameMissionText;
        public ulong MetaGameObjectiveText;
        public ulong MapInfoAvatarDefeatedOverride;
        public bool DiscoverAvatarsForPlayers;
        public int SoftLockRegionMode;
        public MetaGameMeterType MetaGameMeter;
        public ulong MetaGameBuffList;
        public MetaGameMetricEventType MetaGameMetricEvent;
        public ulong MetaGameWidget;
        public bool AllowMissionTrackerSorting;
        public ulong InterstitialTextOverride;
        public MetaGamePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaGamePrototype), proto); }
    }
    public enum MetaGameMeterType
    {
        None = 0,
        Threat = 1,
        Entity = 2,
    }
    public enum MetaGameMetricEventType
    {
        None = 0,
        XDefense = 1,
        Holosim = 2,
    }

    public class MetaGameTeamPrototype : Prototype
    {
        public ulong Name;
        public int MinPlayers;
        public int MaxPlayers;
        public ulong Faction;
        public MetaGameTeamPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaGameTeamPrototype), proto); }
    }

    public class MatchMetaGamePrototype : MetaGamePrototype
    {
        public ulong StartRegion;
        public MatchMetaGamePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MatchMetaGamePrototype), proto); }
    }

    public class MatchQueuePrototype : Prototype
    {
        public ulong[] MatchTypes;
        public ulong Name;
        public ulong QueueMsg;
        public int BalanceMethod;
        public int RegionLevel;
        public ulong GameSystem;
        public MatchQueuePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MatchQueuePrototype), proto); }
    }

    public class MetaGameEventHandlerPrototype : Prototype
    {
        public MetaGameEventHandlerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaGameEventHandlerPrototype), proto); }
    }

    public class PvPScoreEventHandlerPrototype : MetaGameEventHandlerPrototype
    {
        public int DeathsEntry;
        public int KillsEntry;
        public int DamageTakenEntry;
        public int DamageVsMinionsEntry;
        public int DamageVsPlayersEntry;
        public int DamageVsTotalEntry;
        public int Runestones;
        public int AssistsMS;
        public int AssistsEntry;
        public EvalPrototype EvalRunestoneAssistReward;
        public int KillingSpreeEntry;
        public PvPScoreEventHandlerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PvPScoreEventHandlerPrototype), proto); }
    }

    public class PvEScoreEventHandlerPrototype : MetaGameEventHandlerPrototype
    {
        public int DeathsEntry;
        public int KillsEntry;
        public int DamageTakenEntry;
        public int DamageVsMinionsEntry;
        public int DamageVsBossEntry;
        public int DamageVsTotalEntry;
        public PvEScoreEventHandlerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PvEScoreEventHandlerPrototype), proto); }
    }

    public class PvPTeamPrototype : MetaGameTeamPrototype
    {
        public ulong Alliance;
        public ulong SpawnMarker;
        public ulong StartHealingAura;
        public ulong StartTarget;
        public ulong IconPath;
        public ulong DisplayName;
        public ulong IconPathHiRes;
        public PvPTeamPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PvPTeamPrototype), proto); }
    }

    public class PvPMiniMapIconsPrototype : Prototype
    {
        public ulong AlliedMinion;
        public ulong Ally;
        public ulong Enemy;
        public ulong EnemyMinion;
        public PvPMiniMapIconsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PvPMiniMapIconsPrototype), proto); }
    }

    public class PvPPrototype : MatchMetaGamePrototype
    {
        public int RespawnCooldown;
        public int StartingScore;
        public ulong ScoreSchemaPlayer;
        public ulong ScoreSchemaRegion;
        public ulong MiniMapFilter;
        public ulong AvatarKilledLootTable;
        public bool IsPvP;
        public EvalPrototype EvalOnPlayerAdded;
        public ulong[] RefreshVendorTypes;
        public bool RecordPlayerDeaths;
        public ulong DamageBoostForKDPct;
        public ulong DamageReductionForKDPct;
        public ulong DamageBoostForNoobs;
        public ulong DamageReductionForNoobs;
        public ulong VOEnemyTeamWiped;
        public ulong VOFirstKill;
        public ulong[] VOKillSpreeList;
        public ulong VOKillSpreeShutdown;
        public ulong VORevenge;
        public ulong VOTeammateKilled;
        public ulong DamageBoostForWinPct;
        public ulong DamageReductionForWinPct;
        public ulong DamageBoostForOmegaPct;
        public ulong DamageReductionForOmegaPct;
        public bool ScreenArrowsForNonPartyAvatars;
        public PvPPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PvPPrototype), proto); }
    }


    public class GameModePrototype : Prototype
    {
        public GameModePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(GameModePrototype), proto); }
    }

    public class PvEScaleEnemyBoostEntryPrototype : Prototype
    {
        public ulong EnemyBoost;
        public ulong UINotification;
        public PvEScaleEnemyBoostEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PvEScaleEnemyBoostEntryPrototype), proto); }
    }

    public class PvEScaleWavePopulationPrototype : Prototype
    {
        public PopulationRequiredObjectListPrototype[] Choices;
        public PvEScaleWavePopulationPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PvEScaleWavePopulationPrototype), proto); }
    }

    public class MetaGameNotificationDataPrototype : Prototype
    {
        public ulong DialogText;
        public ulong WorldEntityPrototype;
        public GameNotificationType NotificationType;
        public MetaGameNotificationDataPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaGameNotificationDataPrototype), proto); }
    }
    public enum GameNotificationType
    {
        None = 0,
        PartyInvite = 1,
        GuildInvite = 2,
        PowerPointsAwarded = 3,
        ServerMessage = 4,
        MissionUpdate = 6,
        RemoteMission = 5,
        MatchQueue = 7,
        PvPShop = 8,
        OfferingUI = 9,
        DifficultyModeUnlocked = 10,
        MetaGameInfo = 11,
        LegendaryMission = 12,
        PvPScore = 13,
        OmegaPointsAwarded = 14,
        TradeInvite = 15,
        LoginReward = 16,
        GiftReceived = 17,
        LeaderboardRewarded = 18,
        CouponReceived = 19,
        PublicEvent = 20,
    }

    public class MetaGameBannerTimeDataPrototype : Prototype
    {
        public int TimerValueMS;
        public ulong BannerText;
        public MetaGameModeTimerBannerType TimerModeType;
        public MetaGameBannerTimeDataPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaGameBannerTimeDataPrototype), proto); }
    }

    public enum MetaGameModeTimerBannerType
    {
        Interval = 0,
        Once = 1,
    }

    public class MetaGameModePrototype : Prototype
    {
        public ulong AvatarOnKilledInfoOverride;
        public ulong EventHandler;
        public ulong UINotificationOnActivate;
        public ulong UINotificationOnDeactivate;
        public bool ShowTimer;
        public ulong Name;
        public int ActiveGoalRepeatTimeMS;
        public ulong UINotificationActiveGoalRepeat;
        public bool ShowScoreboard;
        public MetaGameNotificationDataPrototype[] PlayerEnterNotifications;
        public MetaGameBannerTimeDataPrototype[] UITimedBannersOnActivate;
        public ulong PlayerEnterAudioTheme;
        public ulong[] ApplyStates;
        public ulong[] RemoveStates;
        public ulong[] RemoveGroups;
        public MetaGameModePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaGameModePrototype), proto); }
    }

    public class MetaGameModeIdlePrototype : MetaGameModePrototype
    {
        public int DurationMS;
        public int NextMode;
        public bool PlayersCanMove;
        public bool DisplayScoreInfoOnActivate;
        public bool TeleportPlayersToStartOnActivate;
        public ulong KismetSequenceOnActivate;
        public int PlayerCountToAdvance;
        public ulong DeathRegionTarget;
        public ulong PlayerLockVisualsPower;
        public MetaGameModeIdlePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaGameModeIdlePrototype), proto); }
    }

    public class MetaGameModeShutdownPrototype : MetaGameModePrototype
    {
        public ulong ShutdownTarget;
        public MetaGameModeShutdownBehaviorType Behavior;
        public MetaGameModeShutdownPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaGameModeShutdownPrototype), proto); }
    }

    public enum MetaGameModeShutdownBehaviorType
    {
        Immediate = 0,
        Delay = 1,
    }

    public class PvEScaleGameModePrototype : MetaGameModePrototype
    {
        public int WaveDurationMS;
        public int WaveDurationCriticalTimeMS;
        public int WaveDurationLowTimeMS;
        public int WaveBossDelayMS;
        public ulong[] BossPopulationObjects;
        public ulong BossUINotification;
        public ulong DeathRegionTarget;
        public ulong PopulationOverrideTheme;
        public ulong PopulationOverrideAreas;
        public ulong PowerUpMarkerType;
        public ulong PowerUpItem;
        public ulong PowerUpPowerToRemove;
        public int NextMode;
        public int FailMode;
        public ulong FailUINotification;
        public ulong SuccessUINotification;
        public int DifficultyIndex;
        public ulong BossModeNameOverride;
        public ulong PowerUpExtraText;
        public ulong WavePopulation;
        public PvEScaleEnemyBoostEntryPrototype[] WaveEnemyBoosts;
        public float WaveDifficultyPerSecond;
        public int PowerUpSpawnMS;
        public float PowerUpDifficultyReduction;
        public float MobTotalDifficultyReduction;
        public ulong PowerUpSpawnUINotification;
        public int WaveDifficultyFailureThreshold;
        public int WaveDifficultyWarningThreshold;
        public int WaveEnemyBoostsPickCount;
        public ulong WaveOnSpawnPower;
        public ulong[] BossEnemyBoosts;
        public int BossEnemyBoostsPicks;
        public ulong WaveOnDespawnPower;
        public ulong PowerUpPickupUINotification;
        public PvEScaleGameModePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PvEScaleGameModePrototype), proto); }
    }

    public class PvEWaveGameModePrototype : MetaGameModePrototype
    {
        public int WaveDurationMS;
        public int WaveDurationCriticalTimeMS;
        public int WaveDurationLowTimeMS;
        public int WaveBossDelayMS;
        public ulong BossSpawner;
        public ulong BossPopulationObject;
        public ulong BossUINotification;
        public ulong DeathRegionTarget;
        public ulong PopulationOverrideTheme;
        public ulong PopulationOverrideAreas;
        public ulong PowerUpMarkerType;
        public ulong PowerUpItem;
        public ulong PowerUpPowerToRemove;
        public int NextMode;
        public int FailMode;
        public ulong FailUINotification;
        public ulong SuccessUINotification;
        public int DifficultyIndex;
        public ulong BossModeNameOverride;
        public ulong PowerUpExtraText;
        public PvEWaveGameModePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PvEWaveGameModePrototype), proto); }
    }

    public class PvPAttackerDataPrototype : Prototype
    {
        public ulong Wave;
        public ulong WaveSpawnPosition;
        public ulong WaveSuperOnTurretDeath;
        public ulong WaveSuperMinion;
        public int WaveSiegeMinionEveryXWave;
        public ulong WaveSiegeMinion;
        public PvPAttackerDataPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PvPAttackerDataPrototype), proto); }
    }

    public class PvPDefenderDataPrototype : Prototype
    {
        public ulong Defender;
        public ulong Boost;
        public ulong UnderAttackUINotification;
        public ulong DeathUINotification;
        public ulong RespawnUINotification;
        public ulong Team;
        public ulong DeathAudioTheme;
        public ulong UnderAttackAudioTheme;
        public PvPDefenderDataPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PvPDefenderDataPrototype), proto); }
    }

    public class PvPTurretDataPrototype : Prototype
    {
        public PopulationObjectPrototype TurretPopulation;
        public ulong AVSPH;
        public int TurretGroupId;
        public ulong Team;
        public ulong DeathAudioTheme;
        public PvPTurretDataPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PvPTurretDataPrototype), proto); }
    }

    public class PvPFactionGameModePrototype : MetaGameModePrototype
    {
        public PvPDefenderDataPrototype[] Defenders;
        public PvPFactionGameModePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PvPFactionGameModePrototype), proto); }
    }

    public class MetaGameTeamStartOverridePrototype : Prototype
    {
        public ulong StartTarget;
        public ulong Team;
        public MetaGameTeamStartOverridePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaGameTeamStartOverridePrototype), proto); }
    }

    public class PvPDefenderGameModePrototype : MetaGameModePrototype
    {
        public PvPAttackerDataPrototype[] Attackers;
        public int AttackerWaveCycleMS;
        public PvPDefenderDataPrototype[] Defenders;
        public int NextMode;
        public MetaGameTeamStartOverridePrototype[] StartTargetOverrides;
        public PvPTurretDataPrototype[] Turrets;
        public MetaGameTeamStartOverridePrototype[] RespawnTargetOverrides;
        public EvalPrototype TimeToRespawn;
        public int SoftLockRegionMS;
        public ulong ChatMessagePlayerDefeatedPlayer;
        public ulong BannerMsgPlayerDefeatAttacker;
        public ulong BannerMsgPlayerDefeatDefender;
        public ulong BannerMsgPlayerDefeatOther;
        public ulong BannerMsgPlayerDefeatLock;
        public ulong BannerMsgPlayerDefeatUnlock;
        public ulong PlayerLockVisualsPower;
        public MetaGameBannerTimeDataPrototype[] UITimedBannersOnDefeatLock;
        public ulong DeathTimerText;
        public ulong DefenderInvinciblePower;
        public ulong TurretInvinciblePower;
        public int AttackerWaveInitialDelayMS;
        public ulong BannerMsgNPDefeatPlayerDefender;
        public ulong BannerMsgNPDefeatPlayerOther;
        public ulong ChatMessageNPDefeatedPlayer;
        public float DidNotParticipateDmgPerMinuteMin;
        public int DefenderVulnerabilityIntervalMS;
        public EvalPrototype DefenderVulnerabilityEval;
        public int TurretVulnerabilityIntervalMS;
        public EvalPrototype TurretVulnerabilityEval;
        public PvPDefenderGameModePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PvPDefenderGameModePrototype), proto); }
    }

    public class MetaGameStateModePrototype : MetaGameModePrototype
    {
        public ulong[] States;
        public int StatePickIntervalMS;
        public int DifficultyPerStateActivate;
        public ulong UIStateChangeBannerText;
        public ulong DeathRegionTarget;
        public ulong StatePickIntervalLabelOverride;
        public EvalPrototype EvalStateSelection;
        public EvalPrototype EvalModeEnd;
        public ulong UIStatePickIntervalWidget;
        public MetaGameStateModePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaGameStateModePrototype), proto); }
    }


    public class NexusPvPCyclePrototype : Prototype
    {
        public ulong[] Escalations;
        public float NextCycleTimeInSeconds;
        public NexusPvPCyclePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(NexusPvPCyclePrototype), proto); }
    }

    public class NexusPvPWavePrototype : Prototype
    {
        public NexusPvPCyclePrototype[] Cycles;
        public NexusPvPWavePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(NexusPvPWavePrototype), proto); }
    }

    public class NexusPvPMainModePrototype : MetaGameModePrototype
    {
        public int GameOverModeIndex;
        public PopulationEntityPrototype NexusRedPopulationEntity;
        public float TimeBetweenWavesInSeconds;
        public ulong[] Waves;
        public ulong WaveSpawnMarker;
        public NexusPvPMainModePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(NexusPvPMainModePrototype), proto); }
    }

    public class MissionMetaGamePrototype : MetaGamePrototype
    {
        public int LevelLowerBoundsOffset;
        public int LevelUpperBoundsOffset;
        public MissionMetaGamePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionMetaGamePrototype), proto); }
    }
}
