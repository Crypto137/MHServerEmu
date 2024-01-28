using MHServerEmu.Games.GameData.Calligraphy.Attributes;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)None)]
    public enum MetaGameMeterType
    {
        None = 0,
        Threat = 1,
        Entity = 2,
    }

    [AssetEnum((int)None)]
    public enum MetaGameMetricEventType
    {
        None = 0,
        XDefense = 1,
        Holosim = 2,
    }

    [AssetEnum((int)None)]
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
        // Not found in client
        SynergyPoints = 21
    }

    [AssetEnum((int)Interval)]
    public enum MetaGameModeTimerBannerType
    {
        Interval = 0,
        Once = 1,
    }

    [AssetEnum((int)Immediate)]
    public enum MetaGameModeShutdownBehaviorType
    {
        Immediate = 0,
        Delay = 1,
    }

    #endregion

    public class MetaGamePrototype : EntityPrototype
    {
        public float Duration { get; protected set; }
        public PrototypeId[] Teams { get; protected set; }
        public PrototypeId[] GameModes { get; protected set; }
        public PrototypeId BodysliderOverride { get; protected set; }
        public PrototypeId MetaGameMissionText { get; protected set; }
        public PrototypeId MetaGameObjectiveText { get; protected set; }
        public PrototypeId MapInfoAvatarDefeatedOverride { get; protected set; }
        public bool DiscoverAvatarsForPlayers { get; protected set; }
        public int SoftLockRegionMode { get; protected set; }
        public MetaGameMeterType MetaGameMeter { get; protected set; }
        public PrototypeId[] MetaGameBuffList { get; protected set; }
        public MetaGameMetricEventType MetaGameMetricEvent { get; protected set; }
        public PrototypeId MetaGameWidget { get; protected set; }
        public bool AllowMissionTrackerSorting { get; protected set; }
        public LocaleStringId InterstitialTextOverride { get; protected set; }
    }

    public class MetaGameTeamPrototype : Prototype
    {
        public LocaleStringId Name { get; protected set; }
        public int MinPlayers { get; protected set; }
        public int MaxPlayers { get; protected set; }
        public PrototypeId Faction { get; protected set; }
    }

    public class MatchMetaGamePrototype : MetaGamePrototype
    {
        public PrototypeId StartRegion { get; protected set; }
    }

    public class MatchQueuePrototype : Prototype
    {
        public PrototypeId[] MatchTypes { get; protected set; }
        public LocaleStringId Name { get; protected set; }
        public LocaleStringId QueueMsg { get; protected set; }
        public int BalanceMethod { get; protected set; }
        public int RegionLevel { get; protected set; }
        public AssetId GameSystem { get; protected set; }
    }

    public class MetaGameEventHandlerPrototype : Prototype
    {
    }

    public class PvPScoreEventHandlerPrototype : MetaGameEventHandlerPrototype
    {
        public int DeathsEntry { get; protected set; }
        public int KillsEntry { get; protected set; }
        public int DamageTakenEntry { get; protected set; }
        public int DamageVsMinionsEntry { get; protected set; }
        public int DamageVsPlayersEntry { get; protected set; }
        public int DamageVsTotalEntry { get; protected set; }
        public int Runestones { get; protected set; }
        public int AssistsMS { get; protected set; }
        public int AssistsEntry { get; protected set; }
        public EvalPrototype EvalRunestoneAssistReward { get; protected set; }
        public int KillingSpreeEntry { get; protected set; }
    }

    public class PvEScoreEventHandlerPrototype : MetaGameEventHandlerPrototype
    {
        public int DeathsEntry { get; protected set; }
        public int KillsEntry { get; protected set; }
        public int DamageTakenEntry { get; protected set; }
        public int DamageVsMinionsEntry { get; protected set; }
        public int DamageVsBossEntry { get; protected set; }
        public int DamageVsTotalEntry { get; protected set; }
    }

    public class PvPTeamPrototype : MetaGameTeamPrototype
    {
        public PrototypeId Alliance { get; protected set; }
        public PrototypeId SpawnMarker { get; protected set; }
        public PrototypeId StartHealingAura { get; protected set; }
        public PrototypeId StartTarget { get; protected set; }
        public AssetId IconPath { get; protected set; }
        public LocaleStringId DisplayName { get; protected set; }
        public AssetId IconPathHiRes { get; protected set; }
    }

    public class PvPMiniMapIconsPrototype : Prototype
    {
        public PrototypeId AlliedMinion { get; protected set; }
        public PrototypeId Ally { get; protected set; }
        public PrototypeId Enemy { get; protected set; }
        public PrototypeId EnemyMinion { get; protected set; }
    }

    public class PvPPrototype : MatchMetaGamePrototype
    {
        public int RespawnCooldown { get; protected set; }
        public int StartingScore { get; protected set; }
        public PrototypeId ScoreSchemaPlayer { get; protected set; }
        public PrototypeId ScoreSchemaRegion { get; protected set; }
        public PrototypeId MiniMapFilter { get; protected set; }
        public PrototypeId AvatarKilledLootTable { get; protected set; }
        public bool IsPvP { get; protected set; }
        public EvalPrototype EvalOnPlayerAdded { get; protected set; }
        public PrototypeId[] RefreshVendorTypes { get; protected set; }
        public bool RecordPlayerDeaths { get; protected set; }
        public CurveId DamageBoostForKDPct { get; protected set; }
        public CurveId DamageReductionForKDPct { get; protected set; }
        public CurveId DamageBoostForNoobs { get; protected set; }
        public CurveId DamageReductionForNoobs { get; protected set; }
        public AssetId VOEnemyTeamWiped { get; protected set; }
        public AssetId VOFirstKill { get; protected set; }
        public AssetId[] VOKillSpreeList { get; protected set; }
        public AssetId VOKillSpreeShutdown { get; protected set; }
        public AssetId VORevenge { get; protected set; }
        public AssetId VOTeammateKilled { get; protected set; }
        public CurveId DamageBoostForWinPct { get; protected set; }
        public CurveId DamageReductionForWinPct { get; protected set; }
        public CurveId DamageBoostForOmegaPct { get; protected set; }
        public CurveId DamageReductionForOmegaPct { get; protected set; }
        public bool ScreenArrowsForNonPartyAvatars { get; protected set; }
    }

    public class GameModePrototype : Prototype
    {
    }

    public class PvEScaleEnemyBoostEntryPrototype : Prototype
    {
        public PrototypeId EnemyBoost { get; protected set; }
        public PrototypeId UINotification { get; protected set; }
    }

    public class PvEScaleWavePopulationPrototype : Prototype
    {
        public PopulationRequiredObjectListPrototype[] Choices { get; protected set; }
    }

    public class MetaGameNotificationDataPrototype : Prototype
    {
        public LocaleStringId DialogText { get; protected set; }
        public PrototypeId WorldEntityPrototype { get; protected set; }
        public GameNotificationType NotificationType { get; protected set; }
    }

    public class MetaGameBannerTimeDataPrototype : Prototype
    {
        public int TimerValueMS { get; protected set; }
        public LocaleStringId BannerText { get; protected set; }
        public MetaGameModeTimerBannerType TimerModeType { get; protected set; }
    }

    public class MetaGameModePrototype : Prototype
    {
        public PrototypeId AvatarOnKilledInfoOverride { get; protected set; }
        public PrototypeId EventHandler { get; protected set; }
        public PrototypeId UINotificationOnActivate { get; protected set; }
        public PrototypeId UINotificationOnDeactivate { get; protected set; }
        public bool ShowTimer { get; protected set; }
        public LocaleStringId Name { get; protected set; }
        public int ActiveGoalRepeatTimeMS { get; protected set; }
        public PrototypeId UINotificationActiveGoalRepeat { get; protected set; }
        public bool ShowScoreboard { get; protected set; }
        public MetaGameNotificationDataPrototype[] PlayerEnterNotifications { get; protected set; }
        public MetaGameBannerTimeDataPrototype[] UITimedBannersOnActivate { get; protected set; }
        public AssetId PlayerEnterAudioTheme { get; protected set; }
        public PrototypeId[] ApplyStates { get; protected set; }
        public PrototypeId[] RemoveStates { get; protected set; }
        public AssetId[] RemoveGroups { get; protected set; }
    }

    public class MetaGameModeIdlePrototype : MetaGameModePrototype
    {
        public int DurationMS { get; protected set; }
        public int NextMode { get; protected set; }
        public bool PlayersCanMove { get; protected set; }
        public bool DisplayScoreInfoOnActivate { get; protected set; }
        public bool TeleportPlayersToStartOnActivate { get; protected set; }
        public PrototypeId KismetSequenceOnActivate { get; protected set; }
        public int PlayerCountToAdvance { get; protected set; }
        public PrototypeId DeathRegionTarget { get; protected set; }
        public PrototypeId PlayerLockVisualsPower { get; protected set; }
    }

    public class MetaGameModeShutdownPrototype : MetaGameModePrototype
    {
        public PrototypeId ShutdownTarget { get; protected set; }
        public MetaGameModeShutdownBehaviorType Behavior { get; protected set; }
    }

    public class PvEScaleGameModePrototype : MetaGameModePrototype
    {
        public int WaveDurationMS { get; protected set; }
        public int WaveDurationCriticalTimeMS { get; protected set; }
        public int WaveDurationLowTimeMS { get; protected set; }
        public int WaveBossDelayMS { get; protected set; }
        public PrototypeId[] BossPopulationObjects { get; protected set; }
        public PrototypeId BossUINotification { get; protected set; }
        public PrototypeId DeathRegionTarget { get; protected set; }
        public PrototypeId PopulationOverrideTheme { get; protected set; }
        public PrototypeId PopulationOverrideAreas { get; protected set; }
        public PrototypeId PowerUpMarkerType { get; protected set; }
        public PrototypeId PowerUpItem { get; protected set; }
        public PrototypeId PowerUpPowerToRemove { get; protected set; }
        public int NextMode { get; protected set; }
        public int FailMode { get; protected set; }
        public PrototypeId FailUINotification { get; protected set; }
        public PrototypeId SuccessUINotification { get; protected set; }
        public int DifficultyIndex { get; protected set; }
        public LocaleStringId BossModeNameOverride { get; protected set; }
        public LocaleStringId PowerUpExtraText { get; protected set; }
        public PrototypeId WavePopulation { get; protected set; }
        public PvEScaleEnemyBoostEntryPrototype[] WaveEnemyBoosts { get; protected set; }
        public float WaveDifficultyPerSecond { get; protected set; }
        public int PowerUpSpawnMS { get; protected set; }
        public float PowerUpDifficultyReduction { get; protected set; }
        public float MobTotalDifficultyReduction { get; protected set; }
        public PrototypeId PowerUpSpawnUINotification { get; protected set; }
        public int WaveDifficultyFailureThreshold { get; protected set; }
        public int WaveDifficultyWarningThreshold { get; protected set; }
        public int WaveEnemyBoostsPickCount { get; protected set; }
        public PrototypeId WaveOnSpawnPower { get; protected set; }
        public PrototypeId[] BossEnemyBoosts { get; protected set; }
        public int BossEnemyBoostsPicks { get; protected set; }
        public PrototypeId WaveOnDespawnPower { get; protected set; }
        public PrototypeId PowerUpPickupUINotification { get; protected set; }
    }

    public class PvEWaveGameModePrototype : MetaGameModePrototype
    {
        public int WaveDurationMS { get; protected set; }
        public int WaveDurationCriticalTimeMS { get; protected set; }
        public int WaveDurationLowTimeMS { get; protected set; }
        public int WaveBossDelayMS { get; protected set; }
        public PrototypeId BossSpawner { get; protected set; }
        public PrototypeId BossPopulationObject { get; protected set; }
        public PrototypeId BossUINotification { get; protected set; }
        public PrototypeId DeathRegionTarget { get; protected set; }
        public PrototypeId PopulationOverrideTheme { get; protected set; }
        public PrototypeId PopulationOverrideAreas { get; protected set; }
        public PrototypeId PowerUpMarkerType { get; protected set; }
        public PrototypeId PowerUpItem { get; protected set; }
        public PrototypeId PowerUpPowerToRemove { get; protected set; }
        public int NextMode { get; protected set; }
        public int FailMode { get; protected set; }
        public PrototypeId FailUINotification { get; protected set; }
        public PrototypeId SuccessUINotification { get; protected set; }
        public int DifficultyIndex { get; protected set; }
        public LocaleStringId BossModeNameOverride { get; protected set; }
        public LocaleStringId PowerUpExtraText { get; protected set; }
    }

    public class PvPAttackerDataPrototype : Prototype
    {
        public PrototypeId Wave { get; protected set; }
        public PrototypeId WaveSpawnPosition { get; protected set; }
        public PrototypeId WaveSuperOnTurretDeath { get; protected set; }
        public PrototypeId WaveSuperMinion { get; protected set; }
        public int WaveSiegeMinionEveryXWave { get; protected set; }
        public PrototypeId WaveSiegeMinion { get; protected set; }
    }

    public class PvPDefenderDataPrototype : Prototype
    {
        public PrototypeId Defender { get; protected set; }
        public PrototypeId Boost { get; protected set; }
        public PrototypeId UnderAttackUINotification { get; protected set; }
        public PrototypeId DeathUINotification { get; protected set; }
        public PrototypeId RespawnUINotification { get; protected set; }
        public PrototypeId Team { get; protected set; }
        public AssetId DeathAudioTheme { get; protected set; }
        public AssetId UnderAttackAudioTheme { get; protected set; }
    }

    public class PvPTurretDataPrototype : Prototype
    {
        public PopulationObjectPrototype TurretPopulation { get; protected set; }
        public PrototypeId DeathUINotification { get; protected set; }
        public int TurretGroupId { get; protected set; }
        public PrototypeId Team { get; protected set; }
        public AssetId DeathAudioTheme { get; protected set; }
    }

    public class PvPFactionGameModePrototype : MetaGameModePrototype
    {
        public PvPDefenderDataPrototype[] Defenders { get; protected set; }
    }

    public class MetaGameTeamStartOverridePrototype : Prototype
    {
        public PrototypeId StartTarget { get; protected set; }
        public PrototypeId Team { get; protected set; }
    }

    public class PvPDefenderGameModePrototype : MetaGameModePrototype
    {
        public PvPAttackerDataPrototype[] Attackers { get; protected set; }
        public int AttackerWaveCycleMS { get; protected set; }
        public PvPDefenderDataPrototype[] Defenders { get; protected set; }
        public int NextMode { get; protected set; }
        public MetaGameTeamStartOverridePrototype[] StartTargetOverrides { get; protected set; }
        public PvPTurretDataPrototype[] Turrets { get; protected set; }
        public MetaGameTeamStartOverridePrototype[] RespawnTargetOverrides { get; protected set; }
        public EvalPrototype TimeToRespawn { get; protected set; }
        public int SoftLockRegionMS { get; protected set; }
        public LocaleStringId ChatMessagePlayerDefeatedPlayer { get; protected set; }
        public LocaleStringId BannerMsgPlayerDefeatAttacker { get; protected set; }
        public LocaleStringId BannerMsgPlayerDefeatDefender { get; protected set; }
        public LocaleStringId BannerMsgPlayerDefeatOther { get; protected set; }
        public LocaleStringId BannerMsgPlayerDefeatLock { get; protected set; }
        public LocaleStringId BannerMsgPlayerDefeatUnlock { get; protected set; }
        public PrototypeId PlayerLockVisualsPower { get; protected set; }
        public MetaGameBannerTimeDataPrototype[] UITimedBannersOnDefeatLock { get; protected set; }
        public LocaleStringId DeathTimerText { get; protected set; }
        public PrototypeId DefenderInvinciblePower { get; protected set; }
        public PrototypeId TurretInvinciblePower { get; protected set; }
        public int AttackerWaveInitialDelayMS { get; protected set; }
        public LocaleStringId BannerMsgNPDefeatPlayerDefender { get; protected set; }
        public LocaleStringId BannerMsgNPDefeatPlayerOther { get; protected set; }
        public LocaleStringId ChatMessageNPDefeatedPlayer { get; protected set; }
        public float DidNotParticipateDmgPerMinuteMin { get; protected set; }
        public int DefenderVulnerabilityIntervalMS { get; protected set; }
        public EvalPrototype DefenderVulnerabilityEval { get; protected set; }
        public int TurretVulnerabilityIntervalMS { get; protected set; }
        public EvalPrototype TurretVulnerabilityEval { get; protected set; }
    }

    public class MetaGameStateModePrototype : MetaGameModePrototype
    {
        public PrototypeId[] States { get; protected set; }
        public int StatePickIntervalMS { get; protected set; }
        public int DifficultyPerStateActivate { get; protected set; }
        public LocaleStringId UIStateChangeBannerText { get; protected set; }
        public PrototypeId DeathRegionTarget { get; protected set; }
        public LocaleStringId StatePickIntervalLabelOverride { get; protected set; }
        public EvalPrototype EvalStateSelection { get; protected set; }
        public EvalPrototype EvalModeEnd { get; protected set; }
        public PrototypeId UIStatePickIntervalWidget { get; protected set; }
    }

    public class NexusPvPCyclePrototype : Prototype
    {
        public PrototypeId[] Escalations { get; protected set; }
        public float NextCycleTimeInSeconds { get; protected set; }
    }

    public class NexusPvPWavePrototype : Prototype
    {
        public NexusPvPCyclePrototype[] Cycles { get; protected set; }
    }

    public class NexusPvPMainModePrototype : MetaGameModePrototype
    {
        public int GameOverModeIndex { get; protected set; }
        public PopulationEntityPrototype NexusRedPopulationEntity { get; protected set; }
        public float TimeBetweenWavesInSeconds { get; protected set; }
        public PrototypeId[] Waves { get; protected set; }
        public PrototypeId WaveSpawnMarker { get; protected set; }
    }

    public class MissionMetaGamePrototype : MetaGamePrototype
    {
        public int LevelLowerBoundsOffset { get; protected set; }
        public int LevelUpperBoundsOffset { get; protected set; }
    }
}
