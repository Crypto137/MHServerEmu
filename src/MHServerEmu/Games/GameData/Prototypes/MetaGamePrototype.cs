using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum MetaGameMeterType
    {
        None = 0,
        Threat = 1,
        Entity = 2,
    }

    [AssetEnum]
    public enum MetaGameMetricEventType
    {
        None = 0,
        XDefense = 1,
        Holosim = 2,
    }

    [AssetEnum]
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

    [AssetEnum]
    public enum MetaGameModeTimerBannerType
    {
        Interval = 0,
        Once = 1,
    }

    [AssetEnum]
    public enum MetaGameModeShutdownBehaviorType
    {
        Immediate = 0,
        Delay = 1,
    }

    #endregion

    public class MetaGamePrototype : EntityPrototype
    {
        public float Duration { get; protected set; }
        public ulong[] Teams { get; protected set; }
        public ulong[] GameModes { get; protected set; }
        public ulong BodysliderOverride { get; protected set; }
        public ulong MetaGameMissionText { get; protected set; }
        public ulong MetaGameObjectiveText { get; protected set; }
        public ulong MapInfoAvatarDefeatedOverride { get; protected set; }
        public bool DiscoverAvatarsForPlayers { get; protected set; }
        public int SoftLockRegionMode { get; protected set; }
        public MetaGameMeterType MetaGameMeter { get; protected set; }
        public ulong MetaGameBuffList { get; protected set; }
        public MetaGameMetricEventType MetaGameMetricEvent { get; protected set; }
        public ulong MetaGameWidget { get; protected set; }
        public bool AllowMissionTrackerSorting { get; protected set; }
        public ulong InterstitialTextOverride { get; protected set; }
    }

    public class MetaGameTeamPrototype : Prototype
    {
        public ulong Name { get; protected set; }
        public int MinPlayers { get; protected set; }
        public int MaxPlayers { get; protected set; }
        public ulong Faction { get; protected set; }
    }

    public class MatchMetaGamePrototype : MetaGamePrototype
    {
        public ulong StartRegion { get; protected set; }
    }

    public class MatchQueuePrototype : Prototype
    {
        public ulong[] MatchTypes { get; protected set; }
        public ulong Name { get; protected set; }
        public ulong QueueMsg { get; protected set; }
        public int BalanceMethod { get; protected set; }
        public int RegionLevel { get; protected set; }
        public ulong GameSystem { get; protected set; }
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
        public ulong Alliance { get; protected set; }
        public ulong SpawnMarker { get; protected set; }
        public ulong StartHealingAura { get; protected set; }
        public ulong StartTarget { get; protected set; }
        public ulong IconPath { get; protected set; }
        public ulong DisplayName { get; protected set; }
        public ulong IconPathHiRes { get; protected set; }
    }

    public class PvPMiniMapIconsPrototype : Prototype
    {
        public ulong AlliedMinion { get; protected set; }
        public ulong Ally { get; protected set; }
        public ulong Enemy { get; protected set; }
        public ulong EnemyMinion { get; protected set; }
    }

    public class PvPPrototype : MatchMetaGamePrototype
    {
        public int RespawnCooldown { get; protected set; }
        public int StartingScore { get; protected set; }
        public ulong ScoreSchemaPlayer { get; protected set; }
        public ulong ScoreSchemaRegion { get; protected set; }
        public ulong MiniMapFilter { get; protected set; }
        public ulong AvatarKilledLootTable { get; protected set; }
        public bool IsPvP { get; protected set; }
        public EvalPrototype EvalOnPlayerAdded { get; protected set; }
        public ulong[] RefreshVendorTypes { get; protected set; }
        public bool RecordPlayerDeaths { get; protected set; }
        public ulong DamageBoostForKDPct { get; protected set; }
        public ulong DamageReductionForKDPct { get; protected set; }
        public ulong DamageBoostForNoobs { get; protected set; }
        public ulong DamageReductionForNoobs { get; protected set; }
        public ulong VOEnemyTeamWiped { get; protected set; }
        public ulong VOFirstKill { get; protected set; }
        public ulong[] VOKillSpreeList { get; protected set; }
        public ulong VOKillSpreeShutdown { get; protected set; }
        public ulong VORevenge { get; protected set; }
        public ulong VOTeammateKilled { get; protected set; }
        public ulong DamageBoostForWinPct { get; protected set; }
        public ulong DamageReductionForWinPct { get; protected set; }
        public ulong DamageBoostForOmegaPct { get; protected set; }
        public ulong DamageReductionForOmegaPct { get; protected set; }
        public bool ScreenArrowsForNonPartyAvatars { get; protected set; }
    }

    public class GameModePrototype : Prototype
    {
    }

    public class PvEScaleEnemyBoostEntryPrototype : Prototype
    {
        public ulong EnemyBoost { get; protected set; }
        public ulong UINotification { get; protected set; }
    }

    public class PvEScaleWavePopulationPrototype : Prototype
    {
        public PopulationRequiredObjectListPrototype[] Choices { get; protected set; }
    }

    public class MetaGameNotificationDataPrototype : Prototype
    {
        public ulong DialogText { get; protected set; }
        public ulong WorldEntityPrototype { get; protected set; }
        public GameNotificationType NotificationType { get; protected set; }
    }

    public class MetaGameBannerTimeDataPrototype : Prototype
    {
        public int TimerValueMS { get; protected set; }
        public ulong BannerText { get; protected set; }
        public MetaGameModeTimerBannerType TimerModeType { get; protected set; }
    }

    public class MetaGameModePrototype : Prototype
    {
        public ulong AvatarOnKilledInfoOverride { get; protected set; }
        public ulong EventHandler { get; protected set; }
        public ulong UINotificationOnActivate { get; protected set; }
        public ulong UINotificationOnDeactivate { get; protected set; }
        public bool ShowTimer { get; protected set; }
        public ulong Name { get; protected set; }
        public int ActiveGoalRepeatTimeMS { get; protected set; }
        public ulong UINotificationActiveGoalRepeat { get; protected set; }
        public bool ShowScoreboard { get; protected set; }
        public MetaGameNotificationDataPrototype[] PlayerEnterNotifications { get; protected set; }
        public MetaGameBannerTimeDataPrototype[] UITimedBannersOnActivate { get; protected set; }
        public ulong PlayerEnterAudioTheme { get; protected set; }
        public ulong[] ApplyStates { get; protected set; }
        public ulong[] RemoveStates { get; protected set; }
        public ulong[] RemoveGroups { get; protected set; }
    }

    public class MetaGameModeIdlePrototype : MetaGameModePrototype
    {
        public int DurationMS { get; protected set; }
        public int NextMode { get; protected set; }
        public bool PlayersCanMove { get; protected set; }
        public bool DisplayScoreInfoOnActivate { get; protected set; }
        public bool TeleportPlayersToStartOnActivate { get; protected set; }
        public ulong KismetSequenceOnActivate { get; protected set; }
        public int PlayerCountToAdvance { get; protected set; }
        public ulong DeathRegionTarget { get; protected set; }
        public ulong PlayerLockVisualsPower { get; protected set; }
    }

    public class MetaGameModeShutdownPrototype : MetaGameModePrototype
    {
        public ulong ShutdownTarget { get; protected set; }
        public MetaGameModeShutdownBehaviorType Behavior { get; protected set; }
    }

    public class PvEScaleGameModePrototype : MetaGameModePrototype
    {
        public int WaveDurationMS { get; protected set; }
        public int WaveDurationCriticalTimeMS { get; protected set; }
        public int WaveDurationLowTimeMS { get; protected set; }
        public int WaveBossDelayMS { get; protected set; }
        public ulong[] BossPopulationObjects { get; protected set; }
        public ulong BossUINotification { get; protected set; }
        public ulong DeathRegionTarget { get; protected set; }
        public ulong PopulationOverrideTheme { get; protected set; }
        public ulong PopulationOverrideAreas { get; protected set; }
        public ulong PowerUpMarkerType { get; protected set; }
        public ulong PowerUpItem { get; protected set; }
        public ulong PowerUpPowerToRemove { get; protected set; }
        public int NextMode { get; protected set; }
        public int FailMode { get; protected set; }
        public ulong FailUINotification { get; protected set; }
        public ulong SuccessUINotification { get; protected set; }
        public int DifficultyIndex { get; protected set; }
        public ulong BossModeNameOverride { get; protected set; }
        public ulong PowerUpExtraText { get; protected set; }
        public ulong WavePopulation { get; protected set; }
        public PvEScaleEnemyBoostEntryPrototype[] WaveEnemyBoosts { get; protected set; }
        public float WaveDifficultyPerSecond { get; protected set; }
        public int PowerUpSpawnMS { get; protected set; }
        public float PowerUpDifficultyReduction { get; protected set; }
        public float MobTotalDifficultyReduction { get; protected set; }
        public ulong PowerUpSpawnUINotification { get; protected set; }
        public int WaveDifficultyFailureThreshold { get; protected set; }
        public int WaveDifficultyWarningThreshold { get; protected set; }
        public int WaveEnemyBoostsPickCount { get; protected set; }
        public ulong WaveOnSpawnPower { get; protected set; }
        public ulong[] BossEnemyBoosts { get; protected set; }
        public int BossEnemyBoostsPicks { get; protected set; }
        public ulong WaveOnDespawnPower { get; protected set; }
        public ulong PowerUpPickupUINotification { get; protected set; }
    }

    public class PvEWaveGameModePrototype : MetaGameModePrototype
    {
        public int WaveDurationMS { get; protected set; }
        public int WaveDurationCriticalTimeMS { get; protected set; }
        public int WaveDurationLowTimeMS { get; protected set; }
        public int WaveBossDelayMS { get; protected set; }
        public ulong BossSpawner { get; protected set; }
        public ulong BossPopulationObject { get; protected set; }
        public ulong BossUINotification { get; protected set; }
        public ulong DeathRegionTarget { get; protected set; }
        public ulong PopulationOverrideTheme { get; protected set; }
        public ulong PopulationOverrideAreas { get; protected set; }
        public ulong PowerUpMarkerType { get; protected set; }
        public ulong PowerUpItem { get; protected set; }
        public ulong PowerUpPowerToRemove { get; protected set; }
        public int NextMode { get; protected set; }
        public int FailMode { get; protected set; }
        public ulong FailUINotification { get; protected set; }
        public ulong SuccessUINotification { get; protected set; }
        public int DifficultyIndex { get; protected set; }
        public ulong BossModeNameOverride { get; protected set; }
        public ulong PowerUpExtraText { get; protected set; }
    }

    public class PvPAttackerDataPrototype : Prototype
    {
        public ulong Wave { get; protected set; }
        public ulong WaveSpawnPosition { get; protected set; }
        public ulong WaveSuperOnTurretDeath { get; protected set; }
        public ulong WaveSuperMinion { get; protected set; }
        public int WaveSiegeMinionEveryXWave { get; protected set; }
        public ulong WaveSiegeMinion { get; protected set; }
    }

    public class PvPDefenderDataPrototype : Prototype
    {
        public ulong Defender { get; protected set; }
        public ulong Boost { get; protected set; }
        public ulong UnderAttackUINotification { get; protected set; }
        public ulong DeathUINotification { get; protected set; }
        public ulong RespawnUINotification { get; protected set; }
        public ulong Team { get; protected set; }
        public ulong DeathAudioTheme { get; protected set; }
        public ulong UnderAttackAudioTheme { get; protected set; }
    }

    public class PvPTurretDataPrototype : Prototype
    {
        public PopulationObjectPrototype TurretPopulation { get; protected set; }
        public ulong AVSPH { get; protected set; }
        public int TurretGroupId { get; protected set; }
        public ulong Team { get; protected set; }
        public ulong DeathAudioTheme { get; protected set; }
    }

    public class PvPFactionGameModePrototype : MetaGameModePrototype
    {
        public PvPDefenderDataPrototype[] Defenders { get; protected set; }
    }

    public class MetaGameTeamStartOverridePrototype : Prototype
    {
        public ulong StartTarget { get; protected set; }
        public ulong Team { get; protected set; }
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
        public ulong ChatMessagePlayerDefeatedPlayer { get; protected set; }
        public ulong BannerMsgPlayerDefeatAttacker { get; protected set; }
        public ulong BannerMsgPlayerDefeatDefender { get; protected set; }
        public ulong BannerMsgPlayerDefeatOther { get; protected set; }
        public ulong BannerMsgPlayerDefeatLock { get; protected set; }
        public ulong BannerMsgPlayerDefeatUnlock { get; protected set; }
        public ulong PlayerLockVisualsPower { get; protected set; }
        public MetaGameBannerTimeDataPrototype[] UITimedBannersOnDefeatLock { get; protected set; }
        public ulong DeathTimerText { get; protected set; }
        public ulong DefenderInvinciblePower { get; protected set; }
        public ulong TurretInvinciblePower { get; protected set; }
        public int AttackerWaveInitialDelayMS { get; protected set; }
        public ulong BannerMsgNPDefeatPlayerDefender { get; protected set; }
        public ulong BannerMsgNPDefeatPlayerOther { get; protected set; }
        public ulong ChatMessageNPDefeatedPlayer { get; protected set; }
        public float DidNotParticipateDmgPerMinuteMin { get; protected set; }
        public int DefenderVulnerabilityIntervalMS { get; protected set; }
        public EvalPrototype DefenderVulnerabilityEval { get; protected set; }
        public int TurretVulnerabilityIntervalMS { get; protected set; }
        public EvalPrototype TurretVulnerabilityEval { get; protected set; }
    }

    public class MetaGameStateModePrototype : MetaGameModePrototype
    {
        public ulong[] States { get; protected set; }
        public int StatePickIntervalMS { get; protected set; }
        public int DifficultyPerStateActivate { get; protected set; }
        public ulong UIStateChangeBannerText { get; protected set; }
        public ulong DeathRegionTarget { get; protected set; }
        public ulong StatePickIntervalLabelOverride { get; protected set; }
        public EvalPrototype EvalStateSelection { get; protected set; }
        public EvalPrototype EvalModeEnd { get; protected set; }
        public ulong UIStatePickIntervalWidget { get; protected set; }
    }

    public class NexusPvPCyclePrototype : Prototype
    {
        public ulong[] Escalations { get; protected set; }
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
        public ulong[] Waves { get; protected set; }
        public ulong WaveSpawnMarker { get; protected set; }
    }

    public class MissionMetaGamePrototype : MetaGamePrototype
    {
        public int LevelLowerBoundsOffset { get; protected set; }
        public int LevelUpperBoundsOffset { get; protected set; }
    }
}
