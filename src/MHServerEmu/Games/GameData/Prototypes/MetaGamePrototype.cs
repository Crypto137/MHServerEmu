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
        public float Duration { get; set; }
        public ulong[] Teams { get; set; }
        public ulong[] GameModes { get; set; }
        public ulong BodysliderOverride { get; set; }
        public ulong MetaGameMissionText { get; set; }
        public ulong MetaGameObjectiveText { get; set; }
        public ulong MapInfoAvatarDefeatedOverride { get; set; }
        public bool DiscoverAvatarsForPlayers { get; set; }
        public int SoftLockRegionMode { get; set; }
        public MetaGameMeterType MetaGameMeter { get; set; }
        public ulong MetaGameBuffList { get; set; }
        public MetaGameMetricEventType MetaGameMetricEvent { get; set; }
        public ulong MetaGameWidget { get; set; }
        public bool AllowMissionTrackerSorting { get; set; }
        public ulong InterstitialTextOverride { get; set; }
    }

    public class MetaGameTeamPrototype : Prototype
    {
        public ulong Name { get; set; }
        public int MinPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public ulong Faction { get; set; }
    }

    public class MatchMetaGamePrototype : MetaGamePrototype
    {
        public ulong StartRegion { get; set; }
    }

    public class MatchQueuePrototype : Prototype
    {
        public ulong[] MatchTypes { get; set; }
        public ulong Name { get; set; }
        public ulong QueueMsg { get; set; }
        public int BalanceMethod { get; set; }
        public int RegionLevel { get; set; }
        public ulong GameSystem { get; set; }
    }

    public class MetaGameEventHandlerPrototype : Prototype
    {
    }

    public class PvPScoreEventHandlerPrototype : MetaGameEventHandlerPrototype
    {
        public int DeathsEntry { get; set; }
        public int KillsEntry { get; set; }
        public int DamageTakenEntry { get; set; }
        public int DamageVsMinionsEntry { get; set; }
        public int DamageVsPlayersEntry { get; set; }
        public int DamageVsTotalEntry { get; set; }
        public int Runestones { get; set; }
        public int AssistsMS { get; set; }
        public int AssistsEntry { get; set; }
        public EvalPrototype EvalRunestoneAssistReward { get; set; }
        public int KillingSpreeEntry { get; set; }
    }

    public class PvEScoreEventHandlerPrototype : MetaGameEventHandlerPrototype
    {
        public int DeathsEntry { get; set; }
        public int KillsEntry { get; set; }
        public int DamageTakenEntry { get; set; }
        public int DamageVsMinionsEntry { get; set; }
        public int DamageVsBossEntry { get; set; }
        public int DamageVsTotalEntry { get; set; }
    }

    public class PvPTeamPrototype : MetaGameTeamPrototype
    {
        public ulong Alliance { get; set; }
        public ulong SpawnMarker { get; set; }
        public ulong StartHealingAura { get; set; }
        public ulong StartTarget { get; set; }
        public ulong IconPath { get; set; }
        public ulong DisplayName { get; set; }
        public ulong IconPathHiRes { get; set; }
    }

    public class PvPMiniMapIconsPrototype : Prototype
    {
        public ulong AlliedMinion { get; set; }
        public ulong Ally { get; set; }
        public ulong Enemy { get; set; }
        public ulong EnemyMinion { get; set; }
    }

    public class PvPPrototype : MatchMetaGamePrototype
    {
        public int RespawnCooldown { get; set; }
        public int StartingScore { get; set; }
        public ulong ScoreSchemaPlayer { get; set; }
        public ulong ScoreSchemaRegion { get; set; }
        public ulong MiniMapFilter { get; set; }
        public ulong AvatarKilledLootTable { get; set; }
        public bool IsPvP { get; set; }
        public EvalPrototype EvalOnPlayerAdded { get; set; }
        public ulong[] RefreshVendorTypes { get; set; }
        public bool RecordPlayerDeaths { get; set; }
        public ulong DamageBoostForKDPct { get; set; }
        public ulong DamageReductionForKDPct { get; set; }
        public ulong DamageBoostForNoobs { get; set; }
        public ulong DamageReductionForNoobs { get; set; }
        public ulong VOEnemyTeamWiped { get; set; }
        public ulong VOFirstKill { get; set; }
        public ulong[] VOKillSpreeList { get; set; }
        public ulong VOKillSpreeShutdown { get; set; }
        public ulong VORevenge { get; set; }
        public ulong VOTeammateKilled { get; set; }
        public ulong DamageBoostForWinPct { get; set; }
        public ulong DamageReductionForWinPct { get; set; }
        public ulong DamageBoostForOmegaPct { get; set; }
        public ulong DamageReductionForOmegaPct { get; set; }
        public bool ScreenArrowsForNonPartyAvatars { get; set; }
    }

    public class GameModePrototype : Prototype
    {
    }

    public class PvEScaleEnemyBoostEntryPrototype : Prototype
    {
        public ulong EnemyBoost { get; set; }
        public ulong UINotification { get; set; }
    }

    public class PvEScaleWavePopulationPrototype : Prototype
    {
        public PopulationRequiredObjectListPrototype[] Choices { get; set; }
    }

    public class MetaGameNotificationDataPrototype : Prototype
    {
        public ulong DialogText { get; set; }
        public ulong WorldEntityPrototype { get; set; }
        public GameNotificationType NotificationType { get; set; }
    }

    public class MetaGameBannerTimeDataPrototype : Prototype
    {
        public int TimerValueMS { get; set; }
        public ulong BannerText { get; set; }
        public MetaGameModeTimerBannerType TimerModeType { get; set; }
    }

    public class MetaGameModePrototype : Prototype
    {
        public ulong AvatarOnKilledInfoOverride { get; set; }
        public ulong EventHandler { get; set; }
        public ulong UINotificationOnActivate { get; set; }
        public ulong UINotificationOnDeactivate { get; set; }
        public bool ShowTimer { get; set; }
        public ulong Name { get; set; }
        public int ActiveGoalRepeatTimeMS { get; set; }
        public ulong UINotificationActiveGoalRepeat { get; set; }
        public bool ShowScoreboard { get; set; }
        public MetaGameNotificationDataPrototype[] PlayerEnterNotifications { get; set; }
        public MetaGameBannerTimeDataPrototype[] UITimedBannersOnActivate { get; set; }
        public ulong PlayerEnterAudioTheme { get; set; }
        public ulong[] ApplyStates { get; set; }
        public ulong[] RemoveStates { get; set; }
        public ulong[] RemoveGroups { get; set; }
    }

    public class MetaGameModeIdlePrototype : MetaGameModePrototype
    {
        public int DurationMS { get; set; }
        public int NextMode { get; set; }
        public bool PlayersCanMove { get; set; }
        public bool DisplayScoreInfoOnActivate { get; set; }
        public bool TeleportPlayersToStartOnActivate { get; set; }
        public ulong KismetSequenceOnActivate { get; set; }
        public int PlayerCountToAdvance { get; set; }
        public ulong DeathRegionTarget { get; set; }
        public ulong PlayerLockVisualsPower { get; set; }
    }

    public class MetaGameModeShutdownPrototype : MetaGameModePrototype
    {
        public ulong ShutdownTarget { get; set; }
        public MetaGameModeShutdownBehaviorType Behavior { get; set; }
    }

    public class PvEScaleGameModePrototype : MetaGameModePrototype
    {
        public int WaveDurationMS { get; set; }
        public int WaveDurationCriticalTimeMS { get; set; }
        public int WaveDurationLowTimeMS { get; set; }
        public int WaveBossDelayMS { get; set; }
        public ulong[] BossPopulationObjects { get; set; }
        public ulong BossUINotification { get; set; }
        public ulong DeathRegionTarget { get; set; }
        public ulong PopulationOverrideTheme { get; set; }
        public ulong PopulationOverrideAreas { get; set; }
        public ulong PowerUpMarkerType { get; set; }
        public ulong PowerUpItem { get; set; }
        public ulong PowerUpPowerToRemove { get; set; }
        public int NextMode { get; set; }
        public int FailMode { get; set; }
        public ulong FailUINotification { get; set; }
        public ulong SuccessUINotification { get; set; }
        public int DifficultyIndex { get; set; }
        public ulong BossModeNameOverride { get; set; }
        public ulong PowerUpExtraText { get; set; }
        public ulong WavePopulation { get; set; }
        public PvEScaleEnemyBoostEntryPrototype[] WaveEnemyBoosts { get; set; }
        public float WaveDifficultyPerSecond { get; set; }
        public int PowerUpSpawnMS { get; set; }
        public float PowerUpDifficultyReduction { get; set; }
        public float MobTotalDifficultyReduction { get; set; }
        public ulong PowerUpSpawnUINotification { get; set; }
        public int WaveDifficultyFailureThreshold { get; set; }
        public int WaveDifficultyWarningThreshold { get; set; }
        public int WaveEnemyBoostsPickCount { get; set; }
        public ulong WaveOnSpawnPower { get; set; }
        public ulong[] BossEnemyBoosts { get; set; }
        public int BossEnemyBoostsPicks { get; set; }
        public ulong WaveOnDespawnPower { get; set; }
        public ulong PowerUpPickupUINotification { get; set; }
    }

    public class PvEWaveGameModePrototype : MetaGameModePrototype
    {
        public int WaveDurationMS { get; set; }
        public int WaveDurationCriticalTimeMS { get; set; }
        public int WaveDurationLowTimeMS { get; set; }
        public int WaveBossDelayMS { get; set; }
        public ulong BossSpawner { get; set; }
        public ulong BossPopulationObject { get; set; }
        public ulong BossUINotification { get; set; }
        public ulong DeathRegionTarget { get; set; }
        public ulong PopulationOverrideTheme { get; set; }
        public ulong PopulationOverrideAreas { get; set; }
        public ulong PowerUpMarkerType { get; set; }
        public ulong PowerUpItem { get; set; }
        public ulong PowerUpPowerToRemove { get; set; }
        public int NextMode { get; set; }
        public int FailMode { get; set; }
        public ulong FailUINotification { get; set; }
        public ulong SuccessUINotification { get; set; }
        public int DifficultyIndex { get; set; }
        public ulong BossModeNameOverride { get; set; }
        public ulong PowerUpExtraText { get; set; }
    }

    public class PvPAttackerDataPrototype : Prototype
    {
        public ulong Wave { get; set; }
        public ulong WaveSpawnPosition { get; set; }
        public ulong WaveSuperOnTurretDeath { get; set; }
        public ulong WaveSuperMinion { get; set; }
        public int WaveSiegeMinionEveryXWave { get; set; }
        public ulong WaveSiegeMinion { get; set; }
    }

    public class PvPDefenderDataPrototype : Prototype
    {
        public ulong Defender { get; set; }
        public ulong Boost { get; set; }
        public ulong UnderAttackUINotification { get; set; }
        public ulong DeathUINotification { get; set; }
        public ulong RespawnUINotification { get; set; }
        public ulong Team { get; set; }
        public ulong DeathAudioTheme { get; set; }
        public ulong UnderAttackAudioTheme { get; set; }
    }

    public class PvPTurretDataPrototype : Prototype
    {
        public PopulationObjectPrototype TurretPopulation { get; set; }
        public ulong AVSPH { get; set; }
        public int TurretGroupId { get; set; }
        public ulong Team { get; set; }
        public ulong DeathAudioTheme { get; set; }
    }

    public class PvPFactionGameModePrototype : MetaGameModePrototype
    {
        public PvPDefenderDataPrototype[] Defenders { get; set; }
    }

    public class MetaGameTeamStartOverridePrototype : Prototype
    {
        public ulong StartTarget { get; set; }
        public ulong Team { get; set; }
    }

    public class PvPDefenderGameModePrototype : MetaGameModePrototype
    {
        public PvPAttackerDataPrototype[] Attackers { get; set; }
        public int AttackerWaveCycleMS { get; set; }
        public PvPDefenderDataPrototype[] Defenders { get; set; }
        public int NextMode { get; set; }
        public MetaGameTeamStartOverridePrototype[] StartTargetOverrides { get; set; }
        public PvPTurretDataPrototype[] Turrets { get; set; }
        public MetaGameTeamStartOverridePrototype[] RespawnTargetOverrides { get; set; }
        public EvalPrototype TimeToRespawn { get; set; }
        public int SoftLockRegionMS { get; set; }
        public ulong ChatMessagePlayerDefeatedPlayer { get; set; }
        public ulong BannerMsgPlayerDefeatAttacker { get; set; }
        public ulong BannerMsgPlayerDefeatDefender { get; set; }
        public ulong BannerMsgPlayerDefeatOther { get; set; }
        public ulong BannerMsgPlayerDefeatLock { get; set; }
        public ulong BannerMsgPlayerDefeatUnlock { get; set; }
        public ulong PlayerLockVisualsPower { get; set; }
        public MetaGameBannerTimeDataPrototype[] UITimedBannersOnDefeatLock { get; set; }
        public ulong DeathTimerText { get; set; }
        public ulong DefenderInvinciblePower { get; set; }
        public ulong TurretInvinciblePower { get; set; }
        public int AttackerWaveInitialDelayMS { get; set; }
        public ulong BannerMsgNPDefeatPlayerDefender { get; set; }
        public ulong BannerMsgNPDefeatPlayerOther { get; set; }
        public ulong ChatMessageNPDefeatedPlayer { get; set; }
        public float DidNotParticipateDmgPerMinuteMin { get; set; }
        public int DefenderVulnerabilityIntervalMS { get; set; }
        public EvalPrototype DefenderVulnerabilityEval { get; set; }
        public int TurretVulnerabilityIntervalMS { get; set; }
        public EvalPrototype TurretVulnerabilityEval { get; set; }
    }

    public class MetaGameStateModePrototype : MetaGameModePrototype
    {
        public ulong[] States { get; set; }
        public int StatePickIntervalMS { get; set; }
        public int DifficultyPerStateActivate { get; set; }
        public ulong UIStateChangeBannerText { get; set; }
        public ulong DeathRegionTarget { get; set; }
        public ulong StatePickIntervalLabelOverride { get; set; }
        public EvalPrototype EvalStateSelection { get; set; }
        public EvalPrototype EvalModeEnd { get; set; }
        public ulong UIStatePickIntervalWidget { get; set; }
    }

    public class NexusPvPCyclePrototype : Prototype
    {
        public ulong[] Escalations { get; set; }
        public float NextCycleTimeInSeconds { get; set; }
    }

    public class NexusPvPWavePrototype : Prototype
    {
        public NexusPvPCyclePrototype[] Cycles { get; set; }
    }

    public class NexusPvPMainModePrototype : MetaGameModePrototype
    {
        public int GameOverModeIndex { get; set; }
        public PopulationEntityPrototype NexusRedPopulationEntity { get; set; }
        public float TimeBetweenWavesInSeconds { get; set; }
        public ulong[] Waves { get; set; }
        public ulong WaveSpawnMarker { get; set; }
    }

    public class MissionMetaGamePrototype : MetaGamePrototype
    {
        public int LevelLowerBoundsOffset { get; set; }
        public int LevelUpperBoundsOffset { get; set; }
    }
}
