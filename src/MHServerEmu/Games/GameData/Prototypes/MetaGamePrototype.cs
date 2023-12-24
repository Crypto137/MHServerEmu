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
        public float Duration { get; private set; }
        public ulong[] Teams { get; private set; }
        public ulong[] GameModes { get; private set; }
        public ulong BodysliderOverride { get; private set; }
        public ulong MetaGameMissionText { get; private set; }
        public ulong MetaGameObjectiveText { get; private set; }
        public ulong MapInfoAvatarDefeatedOverride { get; private set; }
        public bool DiscoverAvatarsForPlayers { get; private set; }
        public int SoftLockRegionMode { get; private set; }
        public MetaGameMeterType MetaGameMeter { get; private set; }
        public ulong MetaGameBuffList { get; private set; }
        public MetaGameMetricEventType MetaGameMetricEvent { get; private set; }
        public ulong MetaGameWidget { get; private set; }
        public bool AllowMissionTrackerSorting { get; private set; }
        public ulong InterstitialTextOverride { get; private set; }
    }

    public class MetaGameTeamPrototype : Prototype
    {
        public ulong Name { get; private set; }
        public int MinPlayers { get; private set; }
        public int MaxPlayers { get; private set; }
        public ulong Faction { get; private set; }
    }

    public class MatchMetaGamePrototype : MetaGamePrototype
    {
        public ulong StartRegion { get; private set; }
    }

    public class MatchQueuePrototype : Prototype
    {
        public ulong[] MatchTypes { get; private set; }
        public ulong Name { get; private set; }
        public ulong QueueMsg { get; private set; }
        public int BalanceMethod { get; private set; }
        public int RegionLevel { get; private set; }
        public ulong GameSystem { get; private set; }
    }

    public class MetaGameEventHandlerPrototype : Prototype
    {
    }

    public class PvPScoreEventHandlerPrototype : MetaGameEventHandlerPrototype
    {
        public int DeathsEntry { get; private set; }
        public int KillsEntry { get; private set; }
        public int DamageTakenEntry { get; private set; }
        public int DamageVsMinionsEntry { get; private set; }
        public int DamageVsPlayersEntry { get; private set; }
        public int DamageVsTotalEntry { get; private set; }
        public int Runestones { get; private set; }
        public int AssistsMS { get; private set; }
        public int AssistsEntry { get; private set; }
        public EvalPrototype EvalRunestoneAssistReward { get; private set; }
        public int KillingSpreeEntry { get; private set; }
    }

    public class PvEScoreEventHandlerPrototype : MetaGameEventHandlerPrototype
    {
        public int DeathsEntry { get; private set; }
        public int KillsEntry { get; private set; }
        public int DamageTakenEntry { get; private set; }
        public int DamageVsMinionsEntry { get; private set; }
        public int DamageVsBossEntry { get; private set; }
        public int DamageVsTotalEntry { get; private set; }
    }

    public class PvPTeamPrototype : MetaGameTeamPrototype
    {
        public ulong Alliance { get; private set; }
        public ulong SpawnMarker { get; private set; }
        public ulong StartHealingAura { get; private set; }
        public ulong StartTarget { get; private set; }
        public ulong IconPath { get; private set; }
        public ulong DisplayName { get; private set; }
        public ulong IconPathHiRes { get; private set; }
    }

    public class PvPMiniMapIconsPrototype : Prototype
    {
        public ulong AlliedMinion { get; private set; }
        public ulong Ally { get; private set; }
        public ulong Enemy { get; private set; }
        public ulong EnemyMinion { get; private set; }
    }

    public class PvPPrototype : MatchMetaGamePrototype
    {
        public int RespawnCooldown { get; private set; }
        public int StartingScore { get; private set; }
        public ulong ScoreSchemaPlayer { get; private set; }
        public ulong ScoreSchemaRegion { get; private set; }
        public ulong MiniMapFilter { get; private set; }
        public ulong AvatarKilledLootTable { get; private set; }
        public bool IsPvP { get; private set; }
        public EvalPrototype EvalOnPlayerAdded { get; private set; }
        public ulong[] RefreshVendorTypes { get; private set; }
        public bool RecordPlayerDeaths { get; private set; }
        public ulong DamageBoostForKDPct { get; private set; }
        public ulong DamageReductionForKDPct { get; private set; }
        public ulong DamageBoostForNoobs { get; private set; }
        public ulong DamageReductionForNoobs { get; private set; }
        public ulong VOEnemyTeamWiped { get; private set; }
        public ulong VOFirstKill { get; private set; }
        public ulong[] VOKillSpreeList { get; private set; }
        public ulong VOKillSpreeShutdown { get; private set; }
        public ulong VORevenge { get; private set; }
        public ulong VOTeammateKilled { get; private set; }
        public ulong DamageBoostForWinPct { get; private set; }
        public ulong DamageReductionForWinPct { get; private set; }
        public ulong DamageBoostForOmegaPct { get; private set; }
        public ulong DamageReductionForOmegaPct { get; private set; }
        public bool ScreenArrowsForNonPartyAvatars { get; private set; }
    }

    public class GameModePrototype : Prototype
    {
    }

    public class PvEScaleEnemyBoostEntryPrototype : Prototype
    {
        public ulong EnemyBoost { get; private set; }
        public ulong UINotification { get; private set; }
    }

    public class PvEScaleWavePopulationPrototype : Prototype
    {
        public PopulationRequiredObjectListPrototype[] Choices { get; private set; }
    }

    public class MetaGameNotificationDataPrototype : Prototype
    {
        public ulong DialogText { get; private set; }
        public ulong WorldEntityPrototype { get; private set; }
        public GameNotificationType NotificationType { get; private set; }
    }

    public class MetaGameBannerTimeDataPrototype : Prototype
    {
        public int TimerValueMS { get; private set; }
        public ulong BannerText { get; private set; }
        public MetaGameModeTimerBannerType TimerModeType { get; private set; }
    }

    public class MetaGameModePrototype : Prototype
    {
        public ulong AvatarOnKilledInfoOverride { get; private set; }
        public ulong EventHandler { get; private set; }
        public ulong UINotificationOnActivate { get; private set; }
        public ulong UINotificationOnDeactivate { get; private set; }
        public bool ShowTimer { get; private set; }
        public ulong Name { get; private set; }
        public int ActiveGoalRepeatTimeMS { get; private set; }
        public ulong UINotificationActiveGoalRepeat { get; private set; }
        public bool ShowScoreboard { get; private set; }
        public MetaGameNotificationDataPrototype[] PlayerEnterNotifications { get; private set; }
        public MetaGameBannerTimeDataPrototype[] UITimedBannersOnActivate { get; private set; }
        public ulong PlayerEnterAudioTheme { get; private set; }
        public ulong[] ApplyStates { get; private set; }
        public ulong[] RemoveStates { get; private set; }
        public ulong[] RemoveGroups { get; private set; }
    }

    public class MetaGameModeIdlePrototype : MetaGameModePrototype
    {
        public int DurationMS { get; private set; }
        public int NextMode { get; private set; }
        public bool PlayersCanMove { get; private set; }
        public bool DisplayScoreInfoOnActivate { get; private set; }
        public bool TeleportPlayersToStartOnActivate { get; private set; }
        public ulong KismetSequenceOnActivate { get; private set; }
        public int PlayerCountToAdvance { get; private set; }
        public ulong DeathRegionTarget { get; private set; }
        public ulong PlayerLockVisualsPower { get; private set; }
    }

    public class MetaGameModeShutdownPrototype : MetaGameModePrototype
    {
        public ulong ShutdownTarget { get; private set; }
        public MetaGameModeShutdownBehaviorType Behavior { get; private set; }
    }

    public class PvEScaleGameModePrototype : MetaGameModePrototype
    {
        public int WaveDurationMS { get; private set; }
        public int WaveDurationCriticalTimeMS { get; private set; }
        public int WaveDurationLowTimeMS { get; private set; }
        public int WaveBossDelayMS { get; private set; }
        public ulong[] BossPopulationObjects { get; private set; }
        public ulong BossUINotification { get; private set; }
        public ulong DeathRegionTarget { get; private set; }
        public ulong PopulationOverrideTheme { get; private set; }
        public ulong PopulationOverrideAreas { get; private set; }
        public ulong PowerUpMarkerType { get; private set; }
        public ulong PowerUpItem { get; private set; }
        public ulong PowerUpPowerToRemove { get; private set; }
        public int NextMode { get; private set; }
        public int FailMode { get; private set; }
        public ulong FailUINotification { get; private set; }
        public ulong SuccessUINotification { get; private set; }
        public int DifficultyIndex { get; private set; }
        public ulong BossModeNameOverride { get; private set; }
        public ulong PowerUpExtraText { get; private set; }
        public ulong WavePopulation { get; private set; }
        public PvEScaleEnemyBoostEntryPrototype[] WaveEnemyBoosts { get; private set; }
        public float WaveDifficultyPerSecond { get; private set; }
        public int PowerUpSpawnMS { get; private set; }
        public float PowerUpDifficultyReduction { get; private set; }
        public float MobTotalDifficultyReduction { get; private set; }
        public ulong PowerUpSpawnUINotification { get; private set; }
        public int WaveDifficultyFailureThreshold { get; private set; }
        public int WaveDifficultyWarningThreshold { get; private set; }
        public int WaveEnemyBoostsPickCount { get; private set; }
        public ulong WaveOnSpawnPower { get; private set; }
        public ulong[] BossEnemyBoosts { get; private set; }
        public int BossEnemyBoostsPicks { get; private set; }
        public ulong WaveOnDespawnPower { get; private set; }
        public ulong PowerUpPickupUINotification { get; private set; }
    }

    public class PvEWaveGameModePrototype : MetaGameModePrototype
    {
        public int WaveDurationMS { get; private set; }
        public int WaveDurationCriticalTimeMS { get; private set; }
        public int WaveDurationLowTimeMS { get; private set; }
        public int WaveBossDelayMS { get; private set; }
        public ulong BossSpawner { get; private set; }
        public ulong BossPopulationObject { get; private set; }
        public ulong BossUINotification { get; private set; }
        public ulong DeathRegionTarget { get; private set; }
        public ulong PopulationOverrideTheme { get; private set; }
        public ulong PopulationOverrideAreas { get; private set; }
        public ulong PowerUpMarkerType { get; private set; }
        public ulong PowerUpItem { get; private set; }
        public ulong PowerUpPowerToRemove { get; private set; }
        public int NextMode { get; private set; }
        public int FailMode { get; private set; }
        public ulong FailUINotification { get; private set; }
        public ulong SuccessUINotification { get; private set; }
        public int DifficultyIndex { get; private set; }
        public ulong BossModeNameOverride { get; private set; }
        public ulong PowerUpExtraText { get; private set; }
    }

    public class PvPAttackerDataPrototype : Prototype
    {
        public ulong Wave { get; private set; }
        public ulong WaveSpawnPosition { get; private set; }
        public ulong WaveSuperOnTurretDeath { get; private set; }
        public ulong WaveSuperMinion { get; private set; }
        public int WaveSiegeMinionEveryXWave { get; private set; }
        public ulong WaveSiegeMinion { get; private set; }
    }

    public class PvPDefenderDataPrototype : Prototype
    {
        public ulong Defender { get; private set; }
        public ulong Boost { get; private set; }
        public ulong UnderAttackUINotification { get; private set; }
        public ulong DeathUINotification { get; private set; }
        public ulong RespawnUINotification { get; private set; }
        public ulong Team { get; private set; }
        public ulong DeathAudioTheme { get; private set; }
        public ulong UnderAttackAudioTheme { get; private set; }
    }

    public class PvPTurretDataPrototype : Prototype
    {
        public PopulationObjectPrototype TurretPopulation { get; private set; }
        public ulong AVSPH { get; private set; }
        public int TurretGroupId { get; private set; }
        public ulong Team { get; private set; }
        public ulong DeathAudioTheme { get; private set; }
    }

    public class PvPFactionGameModePrototype : MetaGameModePrototype
    {
        public PvPDefenderDataPrototype[] Defenders { get; private set; }
    }

    public class MetaGameTeamStartOverridePrototype : Prototype
    {
        public ulong StartTarget { get; private set; }
        public ulong Team { get; private set; }
    }

    public class PvPDefenderGameModePrototype : MetaGameModePrototype
    {
        public PvPAttackerDataPrototype[] Attackers { get; private set; }
        public int AttackerWaveCycleMS { get; private set; }
        public PvPDefenderDataPrototype[] Defenders { get; private set; }
        public int NextMode { get; private set; }
        public MetaGameTeamStartOverridePrototype[] StartTargetOverrides { get; private set; }
        public PvPTurretDataPrototype[] Turrets { get; private set; }
        public MetaGameTeamStartOverridePrototype[] RespawnTargetOverrides { get; private set; }
        public EvalPrototype TimeToRespawn { get; private set; }
        public int SoftLockRegionMS { get; private set; }
        public ulong ChatMessagePlayerDefeatedPlayer { get; private set; }
        public ulong BannerMsgPlayerDefeatAttacker { get; private set; }
        public ulong BannerMsgPlayerDefeatDefender { get; private set; }
        public ulong BannerMsgPlayerDefeatOther { get; private set; }
        public ulong BannerMsgPlayerDefeatLock { get; private set; }
        public ulong BannerMsgPlayerDefeatUnlock { get; private set; }
        public ulong PlayerLockVisualsPower { get; private set; }
        public MetaGameBannerTimeDataPrototype[] UITimedBannersOnDefeatLock { get; private set; }
        public ulong DeathTimerText { get; private set; }
        public ulong DefenderInvinciblePower { get; private set; }
        public ulong TurretInvinciblePower { get; private set; }
        public int AttackerWaveInitialDelayMS { get; private set; }
        public ulong BannerMsgNPDefeatPlayerDefender { get; private set; }
        public ulong BannerMsgNPDefeatPlayerOther { get; private set; }
        public ulong ChatMessageNPDefeatedPlayer { get; private set; }
        public float DidNotParticipateDmgPerMinuteMin { get; private set; }
        public int DefenderVulnerabilityIntervalMS { get; private set; }
        public EvalPrototype DefenderVulnerabilityEval { get; private set; }
        public int TurretVulnerabilityIntervalMS { get; private set; }
        public EvalPrototype TurretVulnerabilityEval { get; private set; }
    }

    public class MetaGameStateModePrototype : MetaGameModePrototype
    {
        public ulong[] States { get; private set; }
        public int StatePickIntervalMS { get; private set; }
        public int DifficultyPerStateActivate { get; private set; }
        public ulong UIStateChangeBannerText { get; private set; }
        public ulong DeathRegionTarget { get; private set; }
        public ulong StatePickIntervalLabelOverride { get; private set; }
        public EvalPrototype EvalStateSelection { get; private set; }
        public EvalPrototype EvalModeEnd { get; private set; }
        public ulong UIStatePickIntervalWidget { get; private set; }
    }

    public class NexusPvPCyclePrototype : Prototype
    {
        public ulong[] Escalations { get; private set; }
        public float NextCycleTimeInSeconds { get; private set; }
    }

    public class NexusPvPWavePrototype : Prototype
    {
        public NexusPvPCyclePrototype[] Cycles { get; private set; }
    }

    public class NexusPvPMainModePrototype : MetaGameModePrototype
    {
        public int GameOverModeIndex { get; private set; }
        public PopulationEntityPrototype NexusRedPopulationEntity { get; private set; }
        public float TimeBetweenWavesInSeconds { get; private set; }
        public ulong[] Waves { get; private set; }
        public ulong WaveSpawnMarker { get; private set; }
    }

    public class MissionMetaGamePrototype : MetaGamePrototype
    {
        public int LevelLowerBoundsOffset { get; private set; }
        public int LevelUpperBoundsOffset { get; private set; }
    }
}
