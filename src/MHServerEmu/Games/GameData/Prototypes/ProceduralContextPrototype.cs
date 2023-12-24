using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum PathMethod  // AI/Misc/Types/MoveToPathMethodType.type
    {
        Invalid = 0,
        Forward = 1,
        ForwardLoop = 5,
        ForwardBackAndForth = 3,
        Reverse = 2,
        ReverseLoop = 6,
        ReverseBackAndForth = 4,
    }

    #endregion

    public class ProceduralContextPrototype : Prototype
    {
    }

    public class ProceduralUsePowerContextSwitchTargetPrototype : Prototype
    {
        public SelectEntityContextPrototype SelectTarget { get; private set; }
        public bool SwitchPermanently { get; private set; }
        public bool UsePowerOnCurTargetIfSwitchFails { get; private set; }
    }

    public class ProceduralUsePowerContextPrototype : ProceduralContextPrototype
    {
        public int InitialCooldownMinMS { get; private set; }
        public int MaxCooldownMS { get; private set; }
        public int MinCooldownMS { get; private set; }
        public UsePowerContextPrototype PowerContext { get; private set; }
        public int PickWeight { get; private set; }
        public ProceduralUsePowerContextSwitchTargetPrototype TargetSwitch { get; private set; }
        public int InitialCooldownMaxMS { get; private set; }
        public ulong RestrictToDifficultyMin { get; private set; }
        public ulong RestrictToDifficultyMax { get; private set; }
    }

    public class ProceduralUseAffixPowerContextPrototype : ProceduralContextPrototype
    {
        public UseAffixPowerContextPrototype AffixContext { get; private set; }
        public int PickWeight { get; private set; }
    }

    public class ProceduralFlankContextPrototype : ProceduralContextPrototype
    {
        public int MaxFlankCooldownMS { get; private set; }
        public int MinFlankCooldownMS { get; private set; }
        public FlankContextPrototype FlankContext { get; private set; }
    }

    public class ProceduralInteractContextPrototype : ProceduralContextPrototype
    {
        public InteractContextPrototype InteractContext { get; private set; }
    }

    public class ProceduralFleeContextPrototype : ProceduralContextPrototype
    {
        public int MaxFleeCooldownMS { get; private set; }
        public int MinFleeCooldownMS { get; private set; }
        public FleeContextPrototype FleeContext { get; private set; }
    }

    public class ProceduralSyncAttackContextPrototype : Prototype
    {
        public ulong TargetEntity { get; private set; }
        public ulong TargetEntityPower { get; private set; }
        public ProceduralUsePowerContextPrototype LeaderPower { get; private set; }
    }

    public class ProceduralThresholdPowerContextPrototype : ProceduralUsePowerContextPrototype
    {
        public float HealthThreshold { get; private set; }
    }

    public class ProceduralPowerWithSpecificTargetsPrototype : Prototype
    {
        public float HealthThreshold { get; private set; }
        public ulong PowerToUse { get; private set; }
        public AgentPrototype Targets { get; private set; }
    }

    public class ProceduralAIProfilePrototype : BrainPrototype
    {
    }

    public class ProceduralProfileWithTargetPrototype : ProceduralAIProfilePrototype
    {
        public SelectEntityContextPrototype SelectTarget { get; private set; }
        public ulong NoTargetOverrideProfile { get; private set; }
    }

    public class ProceduralProfileWithAttackPrototype : ProceduralProfileWithTargetPrototype
    {
        public int AttackRateMaxMS { get; private set; }
        public int AttackRateMinMS { get; private set; }
        public ProceduralUsePowerContextPrototype[] GenericProceduralPowers { get; private set; }
        public ProceduralUseAffixPowerContextPrototype AffixSettings { get; private set; }
    }

    public class ProceduralProfileEnticerPrototype : ProceduralAIProfilePrototype
    {
        public int CooldownMinMS { get; private set; }
        public int CooldownMaxMS { get; private set; }
        public int EnticeeEnticerCooldownMaxMS { get; private set; }
        public int EnticeeEnticerCooldownMinMS { get; private set; }
        public int EnticeeGlobalEnticerCDMaxMS { get; private set; }
        public int EnticeeGlobalEnticerCDMinMS { get; private set; }
        public int MaxSubscriptions { get; private set; }
        public int MaxSubscriptionsPerActivation { get; private set; }
        public float Radius { get; private set; }
        public AIEntityAttributePrototype[] EnticeeAttributes { get; private set; }
        public ulong EnticedBehavior { get; private set; }
    }

    public class ProceduralProfileEnticedBehaviorPrototype : ProceduralAIProfilePrototype
    {
        public FlankContextPrototype FlankToEnticer { get; private set; }
        public MoveToContextPrototype MoveToEnticer { get; private set; }
        public ulong DynamicBehavior { get; private set; }
        public bool OrientToEnticerOrientation { get; private set; }
    }

    public class ProceduralProfileSenseOnlyPrototype : ProceduralAIProfilePrototype
    {
        public AIEntityAttributePrototype[] AttributeList { get; private set; }
        public ulong AllianceOverride { get; private set; }
    }

    public class ProceduralProfileInteractEnticerOverridePrototype : ProceduralAIProfilePrototype
    {
        public InteractContextPrototype Interact { get; private set; }
    }

    public class ProceduralProfileUsePowerEnticerOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public UsePowerContextPrototype Power { get; private set; }
        public new SelectEntityContextPrototype SelectTarget { get; private set; }
    }

    public class ProceduralProfileFearOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public FleeContextPrototype FleeFromTarget { get; private set; }
        public WanderContextPrototype WanderIfNoTarget { get; private set; }
    }

    public class ProceduralProfileLeashOverridePrototype : ProceduralAIProfilePrototype
    {
        public ulong LeashReturnHeal { get; private set; }
        public ulong LeashReturnImmunity { get; private set; }
        public MoveToContextPrototype MoveToSpawn { get; private set; }
        public TeleportContextPrototype TeleportToSpawn { get; private set; }
        public ulong LeashReturnTeleport { get; private set; }
        public ulong LeashReturnInvulnerability { get; private set; }
    }

    public class ProceduralProfileRunToExitAndDespawnOverridePrototype : ProceduralAIProfilePrototype
    {
        public MoveToContextPrototype RunToExit { get; private set; }
        public int NumberOfWandersBeforeDestroy { get; private set; }
        public DelayContextPrototype DelayBeforeRunToExit { get; private set; }
        public SelectEntityContextPrototype SelectPortalToExitFrom { get; private set; }
        public DelayContextPrototype DelayBeforeDestroyOnMoveExitFail { get; private set; }
        public bool VanishesIfMoveToExitFails { get; private set; }
    }

    public class ProceduralProfileRunToTargetAndDespawnOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public ulong Invulnerability { get; private set; }
        public int NumberOfWandersBeforeDestroy { get; private set; }
        public MoveToContextPrototype RunToTarget { get; private set; }
        public WanderContextPrototype WanderIfMoveFails { get; private set; }
    }

    public class ProceduralProfileDefaultActiveOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public DelayContextPrototype DelayAfterWander { get; private set; }
        public WanderContextPrototype Wander { get; private set; }
        public WanderContextPrototype WanderInPlace { get; private set; }
    }

    public class ProceduralProfileFleeOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public FleeContextPrototype FleeFromTarget { get; private set; }
    }

    public class ProceduralProfileOrbPrototype : ProceduralProfileWithTargetPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public int InitialMoveToDelayMS { get; private set; }
        public StateChangePrototype InvalidTargetState { get; private set; }
        public float OrbRadius { get; private set; }
        public ulong EffectPower { get; private set; }
        public bool AcceptsAggroRangeBonus { get; private set; }
        public int ShrinkageDelayMS { get; private set; }
        public int ShrinkageDurationMS { get; private set; }
        public float ShrinkageMinScale { get; private set; }
        public bool DestroyOrbOnUnSimOrTargetLoss { get; private set; }
    }

    public class ProceduralProfileStationaryTurretPrototype : ProceduralProfileWithAttackPrototype
    {
    }

    public class ProceduralProfileRotatingTurretPrototype : ProceduralAIProfilePrototype
    {
        public UsePowerContextPrototype Power { get; private set; }
        public RotateContextPrototype Rotate { get; private set; }
    }

    public class ProceduralProfileRotatingTurretWithTargetPrototype : ProceduralProfileWithAttackPrototype
    {
        public RotateContextPrototype Rotate { get; private set; }
    }

    public class ProceduralProfileBasicMeleePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public ProceduralUsePowerContextPrototype PrimaryPower { get; private set; }
    }

    public class ProceduralProfileBasicMelee2PowerPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public ProceduralUsePowerContextPrototype Power1 { get; private set; }
        public ProceduralUsePowerContextPrototype Power2 { get; private set; }
    }

    public class ProceduralProfileBasicRangePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
    }

    public class ProceduralProfileAlternateRange2Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public ProceduralFlankContextPrototype FlankTarget { get; private set; }
        public ProceduralUsePowerContextPrototype Power1 { get; private set; }
        public ProceduralUsePowerContextPrototype Power2 { get; private set; }
        public ProceduralUsePowerContextPrototype PowerSwap { get; private set; }
    }

    public class ProceduralProfileMultishotRangePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public ProceduralUsePowerContextPrototype MultishotPower { get; private set; }
        public int NumShots { get; private set; }
        public bool RetargetPerShot { get; private set; }
    }

    public class ProceduralProfileMultishotFlankerPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralFlankContextPrototype FlankTarget { get; private set; }
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public ProceduralUsePowerContextPrototype MultishotPower { get; private set; }
        public int NumShots { get; private set; }
        public bool RetargetPerShot { get; private set; }
    }

    public class ProceduralProfileMultishotHiderPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralUsePowerContextPrototype HidePower { get; private set; }
        public ProceduralUsePowerContextPrototype MultishotPower { get; private set; }
        public int NumShots { get; private set; }
        public bool RetargetPerShot { get; private set; }
    }

    public class ProceduralProfileMeleeSpeedByDistancePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public ProceduralUsePowerContextPrototype PrimaryPower { get; private set; }
        public UsePowerContextPrototype ExtraSpeedPower { get; private set; }
        public UsePowerContextPrototype SpeedRemovalPower { get; private set; }
        public float DistanceFromTargetForSpeedBonus { get; private set; }
    }

    public class ProceduralProfileRangeFlankerPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralFlankContextPrototype FlankTarget { get; private set; }
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public ProceduralUsePowerContextPrototype PrimaryPower { get; private set; }
    }

    public class ProceduralProfileSkirmisherPrototype : ProceduralProfileWithAttackPrototype
    {
        public WanderContextPrototype SkirmishMovement { get; private set; }
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public ProceduralUsePowerContextPrototype PrimaryPower { get; private set; }
        public float MoveToSpeedBonus { get; private set; }
    }

    public class ProceduralProfileRangedWithMeleePriority2PowerPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public ProceduralUsePowerContextPrototype MeleePower { get; private set; }
        public ProceduralUsePowerContextPrototype RangedPower { get; private set; }
        public float MaxDistToMoveIntoMelee { get; private set; }
        public MoveToContextPrototype MoveIntoMeleeRange { get; private set; }
    }

    public class ProfMeleePwrSpecialAtHealthPctPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public float SpecialAtHealthChunkPct { get; private set; }
        public UsePowerContextPrototype SpecialPowerAtHealthChunkPct { get; private set; }
    }

    public class ProceduralProfileShockerPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public MoveToContextPrototype MoveIntoMeleeRange { get; private set; }
        public ProceduralUsePowerContextPrototype MeleePower { get; private set; }
        public ProceduralUsePowerContextPrototype SpecialPower { get; private set; }
        public ulong SpecialSummonPower { get; private set; }
        public float MaxDistToMoveIntoMelee { get; private set; }
        public int SpecialPowerNumSummons { get; private set; }
        public float SpecialPowerMaxRadius { get; private set; }
        public float SpecialPowerMinRadius { get; private set; }
    }

    public class ProceduralProfileLadyDeathstrikePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public ProceduralUsePowerContextPrototype HealingPower { get; private set; }
        public ProceduralUsePowerContextPrototype SpecialPower { get; private set; }
        public SelectEntityContextPrototype SpecialPowerSelectTarget { get; private set; }
        public int SpecialPowerChangeTgtIntervalMS { get; private set; }
    }

    public class ProceduralProfileFastballSpecialWolverinePrototype : ProceduralProfileWithTargetPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public WanderContextPrototype MoveToNoTarget { get; private set; }
        public UsePowerContextPrototype Power { get; private set; }
        public int PowerChangeTargetIntervalMS { get; private set; }
    }

    public class ProceduralProfileSeekingMissilePrototype : ProceduralProfileWithTargetPrototype
    {
        public SelectEntityContextPrototype SecondaryTargetSelection { get; private set; }
        public int SeekDelayMS { get; private set; }
        public float SeekDelaySpeed { get; private set; }
    }

    public class ProceduralProfileSeekingMissileUniqueTargetPrototype : ProceduralProfileWithTargetPrototype
    {
    }

    public class ProceduralProfileNoMoveDefaultSensoryPrototype : ProceduralProfileWithAttackPrototype
    {
    }

    public class ProceduralProfileNoMoveSimplifiedSensoryPrototype : ProceduralProfileWithAttackPrototype
    {
    }

    public class ProceduralProfileNoMoveSimplifiedAllySensoryPrototype : ProceduralProfileWithAttackPrototype
    {
    }

    public class ProfKillSelfAfterOnePowerNoMovePrototype : ProceduralProfileWithAttackPrototype
    {
    }

    public class ProceduralProfileNoMoveNoSensePrototype : ProceduralProfileWithAttackPrototype
    {
    }

    public class ProceduralProfileMoveToUniqueTargetNoPowerPrototype : ProceduralProfileWithTargetPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
    }

    public class ProceduralProfileWanderNoPowerPrototype : ProceduralAIProfilePrototype
    {
        public WanderContextPrototype WanderMovement { get; private set; }
    }

    public class ProceduralProfileBasicWanderPrototype : ProceduralProfileWithAttackPrototype
    {
        public WanderContextPrototype WanderMovement { get; private set; }
    }

    public class ProceduralProfilePvPMeleePrototype : ProceduralProfileWithAttackPrototype
    {
        public float AggroRadius { get; private set; }
        public float AggroDropRadius { get; private set; }
        public float AggroDropByLOSChance { get; private set; }
        public long AttentionSpanMS { get; private set; }
        public ulong PrimaryPower { get; private set; }
        public int PathGroup { get; private set; }
        public PathMethod PathMethod { get; private set; }
        public float PathThreshold { get; private set; }
    }

    public class ProceduralProfilePvPTowerPrototype : ProceduralProfileWithAttackPrototype
    {
        public SelectEntityContextPrototype SelectTarget2 { get; private set; }
        public SelectEntityContextPrototype SelectTarget3 { get; private set; }
        public SelectEntityContextPrototype SelectTarget4 { get; private set; }
    }

    public class ProceduralProfileMeleeDropWeaponPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public ProceduralUsePowerContextPrototype PowerMeleeWithWeapon { get; private set; }
        public ProceduralUsePowerContextPrototype PowerMeleeNoWeapon { get; private set; }
        public ProceduralUsePowerContextPrototype PowerDropWeapon { get; private set; }
        public ProceduralUsePowerContextPrototype PowerPickupWeapon { get; private set; }
        public SelectEntityContextPrototype SelectWeaponAsTarget { get; private set; }
        public int DropPickupTimeMax { get; private set; }
        public int DropPickupTimeMin { get; private set; }
    }

    public class ProceduralProfileMeleeAllyDeathFleePrototype : ProceduralProfileWithAttackPrototype
    {
        public FleeContextPrototype FleeFromTargetOnAllyDeath { get; private set; }
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
    }

    public class ProceduralProfileRangedFlankerAllyDeathFleePrototype : ProceduralProfileWithAttackPrototype
    {
        public FleeContextPrototype FleeFromTargetOnAllyDeath { get; private set; }
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public ProceduralFlankContextPrototype FlankTarget { get; private set; }
    }

    public class ProceduralProfileMagnetoPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public ProceduralFlankContextPrototype FlankTarget { get; private set; }
        public ProceduralUsePowerContextPrototype ChasePower { get; private set; }
    }

    public class ProcProfMrSinisterPrototype : ProceduralProfileWithAttackPrototype
    {
        public float CloneCylHealthPctThreshWave1 { get; private set; }
        public float CloneCylHealthPctThreshWave2 { get; private set; }
        public float CloneCylHealthPctThreshWave3 { get; private set; }
        public UsePowerContextPrototype CloneCylSummonFXPower { get; private set; }
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public TriggerSpawnersContextPrototype TriggerCylinderSpawnerAction { get; private set; }
    }

    public class ProcProfMrSinisterCloneCylinderPrototype : ProceduralProfileWithAttackPrototype
    {
        public UsePowerContextPrototype CylinderOpenPower { get; private set; }
        public DespawnContextPrototype DespawnAction { get; private set; }
        public int PreOpenDelayMS { get; private set; }
        public int PostOpenDelayMS { get; private set; }
    }

    public class ProceduralProfileBlobPrototype : ProceduralProfileBasicRangePrototype
    {
        public ProceduralUsePowerContextPrototype SummonToadPower { get; private set; }
        public ulong ToadPrototype { get; private set; }
    }

    public class ProceduralProfileRangedHotspotDropperPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public ProceduralUsePowerContextPrototype RangedPower { get; private set; }
        public ProceduralUsePowerContextPrototype HotspotPower { get; private set; }
        public WanderContextPrototype HotspotDroppingMovement { get; private set; }
    }

    public class ProceduralProfileTeamUpPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public ProceduralFlankContextPrototype FlankTarget { get; private set; }
        public bool IsRanged { get; private set; }
        public MoveToContextPrototype MoveToMaster { get; private set; }
        public TeleportContextPrototype TeleportToMasterIfTooFarAway { get; private set; }
        public int MaxDistToMasterBeforeTeleport { get; private set; }
        public ProceduralUsePowerContextPrototype[] TeamUpPowerProgressionPowers { get; private set; }
    }

    public class ProceduralProfilePetPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype PetFollow { get; private set; }
        public TeleportContextPrototype TeleportToMasterIfTooFarAway { get; private set; }
        public int MaxDistToMasterBeforeTeleport { get; private set; }
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public ProceduralFlankContextPrototype FlankTarget { get; private set; }
        public bool IsRanged { get; private set; }
    }

    public class ProceduralProfilePetFidgetPrototype : ProceduralProfilePetPrototype
    {
        public ProceduralUsePowerContextPrototype Fidget { get; private set; }
    }

    public class ProceduralProfileSquirrelGirlSquirrelPrototype : ProceduralProfilePetPrototype
    {
        public ProceduralFlankContextPrototype FlankMaster { get; private set; }
        public float DeadzoneAroundFlankTarget { get; private set; }
    }

    public class ProceduralProfileRollingGrenadesPrototype : ProceduralAIProfilePrototype
    {
        public int MaxSpeedDegreeUpdateIntervalMS { get; private set; }
        public int MinSpeedDegreeUpdateIntervalMS { get; private set; }
        public int MovementSpeedVariance { get; private set; }
        public int RandomDegreeFromForward { get; private set; }
    }

    public class ProceduralProfileSquirrelTriplePrototype : ProceduralAIProfilePrototype
    {
        public int JumpDistanceMax { get; private set; }
        public int JumpDistanceMin { get; private set; }
        public DelayContextPrototype PauseSettings { get; private set; }
        public int RandomDirChangeDegrees { get; private set; }
    }

    public class ProceduralProfileFrozenOrbPrototype : ProceduralAIProfilePrototype
    {
        public int ShardBurstsPerSecond { get; private set; }
        public int ShardsPerBurst { get; private set; }
        public int ShardRotationSpeed { get; private set; }
        public ulong ShardPower { get; private set; }
    }

    public class ProceduralProfileDrDoomPhase1Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public ulong DeathStun { get; private set; }
        public ProceduralUsePowerContextPrototype SummonTurretPowerAnimOnly { get; private set; }
        public UsePowerContextPrototype SummonDoombotBlockades { get; private set; }
        public UsePowerContextPrototype SummonDoombotInfernos { get; private set; }
        public UsePowerContextPrototype SummonDoombotFlyers { get; private set; }
        public ProceduralUsePowerContextPrototype SummonDoombotAnimOnly { get; private set; }
        public ulong SummonDoombotBlockadesCurve { get; private set; }
        public ulong SummonDoombotInfernosCurve { get; private set; }
        public ulong SummonDoombotFlyersCurve { get; private set; }
        public int SummonDoombotWaveIntervalMS { get; private set; }
        public ProceduralUsePowerContextPrototype SummonOrbSpawners { get; private set; }
        public TriggerSpawnersContextPrototype SpawnTurrets { get; private set; }
        public TriggerSpawnersContextPrototype SpawnDrDoomPhase2 { get; private set; }
        public TriggerSpawnersContextPrototype DestroyTurretsOnDeath { get; private set; }
    }

    public class ProceduralProfileDrDoomPhase2Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public ulong DeathStun { get; private set; }
        public ulong StarryExpanseAnimOnly { get; private set; }
        public TriggerSpawnersContextPrototype SpawnDrDoomPhase3 { get; private set; }
        public TriggerSpawnersContextPrototype SpawnStarryExpanseAreas { get; private set; }
    }

    public class ProceduralProfileDrDoomPhase3Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public RotateContextPrototype RapidFireRotate { get; private set; }
        public ProceduralUsePowerContextPrototype RapidFirePower { get; private set; }
        public ulong StarryExpanseAnimOnly { get; private set; }
        public ProceduralUsePowerContextPrototype CosmicSummonsAnimOnly { get; private set; }
        public UsePowerContextPrototype CosmicSummonsPower { get; private set; }
        public ulong[] CosmicSummonEntities { get; private set; }
        public ulong CosmicSummonsNumEntities { get; private set; }
        public TriggerSpawnersContextPrototype SpawnStarryExpanseAreas { get; private set; }
    }

    public class ProceduralProfileDrDoomPhase1OrbSpawnerPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveTo { get; private set; }
        public ProceduralFlankContextPrototype Flank { get; private set; }
    }

    public class ProceduralProfileMODOKPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralUsePowerContextPrototype TeleportToEntityPower { get; private set; }
        public SelectEntityContextPrototype SelectTeleportTarget { get; private set; }
        public ProceduralUsePowerContextPrototype[] SummonProceduralPowers { get; private set; }
    }

    public class ProceduralProfileSauronPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public ProceduralUsePowerContextPrototype MovementPower { get; private set; }
        public ProceduralUsePowerContextPrototype LowHealthPower { get; private set; }
        public float LowHealthPowerThresholdPct { get; private set; }
    }

    public class ProceduralProfileMandarinPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public ProceduralFlankContextPrototype FlankTarget { get; private set; }
        public SelectEntityContextPrototype SequencePowerSelectTarget { get; private set; }
        public ProceduralUsePowerContextPrototype[] SequencePowers { get; private set; }
    }

    public class ProceduralProfileSabretoothPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public ProceduralUsePowerContextPrototype MovementPower { get; private set; }
        public SelectEntityContextPrototype MovementPowerSelectTarget { get; private set; }
        public ProceduralUsePowerContextPrototype LowHealthPower { get; private set; }
        public float LowHealthPowerThresholdPct { get; private set; }
    }

    public class ProceduralProfileMeleePowerOnHitPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public ProceduralUsePowerContextPrototype PowerOnHit { get; private set; }
    }

    public class ProceduralProfileGrimReaperPrototype : ProfMeleePwrSpecialAtHealthPctPrototype
    {
        public ProceduralUsePowerContextPrototype TripleShotPower { get; private set; }
        public ProceduralUsePowerContextPrototype SpecialPower { get; private set; }
        public SelectEntityContextPrototype SpecialSelectTarget { get; private set; }
        public int SpecialPowerChangeTgtIntervalMS { get; private set; }
    }

    public class ProceduralProfileMoleManPrototype : ProceduralProfileBasicRangePrototype
    {
        public TriggerSpawnersContextPrototype[] GigantoSpawners { get; private set; }
        public ProceduralUsePowerContextPrototype MoloidInvasionPower { get; private set; }
        public TriggerSpawnersContextPrototype MoloidInvasionSpawner { get; private set; }
        public ProceduralUsePowerContextPrototype SummonGigantoAnimPower { get; private set; }
    }

    public class ProceduralProfileVenomPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralFlankContextPrototype FlankTarget { get; private set; }
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public UsePowerContextPrototype VenomMad { get; private set; }
        public float VenomMadThreshold1 { get; private set; }
        public float VenomMadThreshold2 { get; private set; }
    }

    public class ProceduralProfileVanityPetPrototype : ProceduralProfileWithTargetPrototype
    {
        public MoveToContextPrototype PetFollow { get; private set; }
        public TeleportContextPrototype TeleportToMasterIfTooFarAway { get; private set; }
        public int MinTimerWhileNotMovingFidgetMS { get; private set; }
        public int MaxTimerWhileNotMovingFidgetMS { get; private set; }
        public float MaxDistToMasterBeforeTeleport { get; private set; }
    }

    public class ProceduralProfileDoopPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralFleeContextPrototype Flee { get; private set; }
        public ProceduralUsePowerContextPrototype DisappearPower { get; private set; }
        public int LifeTimeMinMS { get; private set; }
        public int LifeTimeMaxMS { get; private set; }
    }

    public class ProceduralProfileGorgonPrototype : ProceduralProfileWithAttackPrototype
    {
        public RotateContextPrototype RotateInStoneGaze { get; private set; }
        public ProceduralUsePowerContextPrototype StoneGaze { get; private set; }
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
    }

    public class ProceduralProfileBotAIPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public SelectEntityContextPrototype SelectTargetItem { get; private set; }
        public WanderContextPrototype WanderMovement { get; private set; }
        public ProceduralUsePowerContextPrototype[] SlottedAbilities { get; private set; }
    }

    public class ProceduralProfileBullseyePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public ProceduralFlankContextPrototype FlankTarget { get; private set; }
        public ProceduralUsePowerContextPrototype MarkForDeath { get; private set; }
    }

    public class ProceduralProfileRhinoPrototype : ProceduralProfileBasicMeleePrototype
    {
    }

    public class ProceduralProfileBlackCatPrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralUsePowerContextPrototype TumblePower { get; private set; }
        public ProceduralUsePowerContextPrototype TumbleComboPower { get; private set; }
        public SelectEntityContextPrototype SelectEntityForTumbleCombo { get; private set; }
    }

    public class ProceduralProfileControlledMobOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public MoveToContextPrototype ControlFollow { get; private set; }
        public TeleportContextPrototype TeleportToMasterIfTooFarAway { get; private set; }
        public float MaxDistToMasterBeforeTeleport { get; private set; }
        public int MaxDistToMasterBeforeFollow { get; private set; }
    }

    public class ProceduralProfileLivingLaserPrototype : ProceduralProfileBasicRangePrototype
    {
        public ProceduralUsePowerContextPrototype SweepingBeamPowerClock { get; private set; }
        public ProceduralUsePowerContextPrototype SweepingBeamPowerCounterClock { get; private set; }
        public RotateContextPrototype SweepingBeamClock { get; private set; }
        public RotateContextPrototype SweepingBeamCounterClock { get; private set; }
    }

    public class ProceduralProfileMeleeFlockerPrototype : ProceduralProfileWithAttackPrototype
    {
        public FlockContextPrototype FlockContext { get; private set; }
        public ulong FleeOnAllyDeathOverride { get; private set; }
    }

    public class ProceduralProfileLizardBossPrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralUsePowerContextPrototype LizardSwarmPower { get; private set; }
    }

    public class ProceduralProfilePetDirectedPrototype : ProceduralProfilePetPrototype
    {
        public ProceduralUsePowerContextPrototype[] DirectedPowers { get; private set; }
    }

    public class ProceduralProfileLokiPhase1Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public ProceduralFlankContextPrototype FlankTarget { get; private set; }
    }

    public class ProceduralProfileLokiPhase2Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public ProceduralUsePowerContextPrototype InverseRings { get; private set; }
        public ulong InverseRingsTargetedVFXOnly { get; private set; }
        public ulong LokiBossSafeZoneKeyword { get; private set; }
        public ulong InverseRingsVFXRemoval { get; private set; }
    }

    public class ProceduralProfileDrStrangeProjectionPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralUsePowerContextPrototype[] ProjectionPowers { get; private set; }
        public ProceduralFlankContextPrototype FlankMaster { get; private set; }
        public float DeadzoneAroundFlankTarget { get; private set; }
        public int FlankToMasterDelayMS { get; private set; }
        public TeleportContextPrototype TeleportToMasterIfTooFarAway { get; private set; }
        public int MaxDistToMasterBeforeTeleport { get; private set; }
    }

    public class ProceduralProfileEyeOfAgamottoPrototype : ProceduralProfileStationaryTurretPrototype
    {
        public RotateContextPrototype IdleRotation { get; private set; }
    }

    public class MistressOfMagmaTeleportDestPrototype : Prototype
    {
        public SelectEntityContextPrototype DestinationSelector { get; private set; }
        public ulong ImmunityBoost { get; private set; }
    }

    public class ProceduralProfileSpikedBallPrototype : ProceduralProfileWithTargetPrototype
    {
        public float MoveToSummonerDistance { get; private set; }
        public float IdleDistanceFromSummoner { get; private set; }
        public RotateContextPrototype Rotate { get; private set; }
        public int SeekDelayMS { get; private set; }
        public float Acceleration { get; private set; }
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public WanderContextPrototype Wander { get; private set; }
        public TeleportContextPrototype TeleportToMasterIfTooFarAway { get; private set; }
        public int MaxDistToMasterBeforeTeleport { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
    }

    public class ProceduralProfileWithEnragePrototype : ProceduralProfileWithAttackPrototype
    {
        public int EnrageTimerInMinutes { get; private set; }
        public ProceduralUsePowerContextPrototype EnragePower { get; private set; }
        public float EnrageTimerAvatarSearchRadius { get; private set; }
        public ProceduralUsePowerContextPrototype[] PostEnragePowers { get; private set; }
    }

    public class ProceduralProfileSyncAttackPrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralSyncAttackContextPrototype[] SyncAttacks { get; private set; }
    }

    public class ProceduralProfileBrimstonePrototype : ProceduralProfileWithEnragePrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public MoveToContextPrototype MoveIntoMeleeRange { get; private set; }
        public ProceduralUsePowerContextPrototype MeleePower { get; private set; }
        public ulong HellfireProtoRef { get; private set; }
    }

    public class ProceduralProfileSlagPrototype : ProceduralProfileWithEnragePrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public ProceduralUsePowerContextPrototype PrimaryPower { get; private set; }
    }

    public class ProceduralProfileMonolithPrototype : ProceduralProfileWithEnragePrototype
    {
        public ulong ObeliskKeyword { get; private set; }
        public ulong[] ObeliskDamageMonolithPowers { get; private set; }
        public ulong DisableShield { get; private set; }
    }

    public class ProceduralProfileHellfirePrototype : ProceduralProfileWithEnragePrototype
    {
        public ProceduralFlankContextPrototype FlankTarget { get; private set; }
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public ProceduralUsePowerContextPrototype PrimaryPower { get; private set; }
        public ulong BrimstoneProtoRef { get; private set; }
        public ProceduralUsePowerContextPrototype SpecialPower { get; private set; }
        public ulong SpecialSummonPower { get; private set; }
        public int SpecialPowerNumSummons { get; private set; }
        public float SpecialPowerMaxRadius { get; private set; }
        public float SpecialPowerMinRadius { get; private set; }
    }

    public class ProceduralProfileMistressOfMagmaPrototype : ProceduralProfileWithEnragePrototype
    {
        public ProceduralFlankContextPrototype FlankTarget { get; private set; }
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public ProceduralUsePowerContextPrototype BombDancePower { get; private set; }
    }

    public class ProceduralProfileSurturPrototype : ProceduralProfileWithEnragePrototype
    {
        public ulong FirePillarPower { get; private set; }
        public int FirePillarMinCooldownMS { get; private set; }
        public int FirePillarMaxCooldownMS { get; private set; }
        public int FirePillarPowerMaxTargets { get; private set; }
        public ulong PowerUnlockBrimstone { get; private set; }
        public ulong PowerUnlockHellfire { get; private set; }
        public ulong PowerUnlockMistress { get; private set; }
        public ulong PowerUnlockMonolith { get; private set; }
        public ulong PowerUnlockSlag { get; private set; }
        public ulong MiniBossBrimstone { get; private set; }
        public ulong MiniBossHellfire { get; private set; }
        public ulong MiniBossMistress { get; private set; }
        public ulong MiniBossMonolith { get; private set; }
        public ulong MiniBossSlag { get; private set; }
    }

    public class ProceduralProfileSurturPortalPrototype : ProceduralProfileNoMoveNoSensePrototype
    {
    }

    public class ProceduralProfileObeliskHealerPrototype : ProceduralProfileBasicMeleePrototype
    {
        public ulong[] ObeliskTargets { get; private set; }
    }

    public class ProceduralProfileObeliskPrototype : ProceduralProfileNoMoveDefaultSensoryPrototype
    {
        public ulong DeadEntityForDetonateIslandPower { get; private set; }
        public ulong DetonateIslandPower { get; private set; }
        public ulong FullyHealedPower { get; private set; }
    }

    public class ProceduralProfileFireGiantChaserPrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralUsePowerContextPrototype MarkTargetPower { get; private set; }
        public ulong MarkTargetVFXRemoval { get; private set; }
    }

    public class ProceduralProfileMissionAllyPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public ProceduralFlankContextPrototype FlankTarget { get; private set; }
        public MoveToContextPrototype MoveToAvatarAlly { get; private set; }
        public TeleportContextPrototype TeleportToAvatarAllyIfTooFarAway { get; private set; }
        public int MaxDistToAvatarAllyBeforeTele { get; private set; }
        public bool IsRanged { get; private set; }
        public float AvatarAllySearchRadius { get; private set; }
    }

    public class ProceduralProfileLOSRangedPrototype : ProceduralProfileBasicRangePrototype
    {
        public ProceduralUsePowerContextPrototype LOSChannelPower { get; private set; }
    }

    public class ProceduralProfileRedSkullOneShotPrototype : ProceduralProfileWithAttackPrototype
    {
        public ulong[] HulkBustersToActivate { get; private set; }
        public ProceduralUsePowerContextPrototype ActivateHulkBusterAnimOnly { get; private set; }
        public float HulkBusterHealthThreshold1 { get; private set; }
        public float HulkBusterHealthThreshold2 { get; private set; }
        public float HulkBusterHealthThreshold3 { get; private set; }
        public float HulkBusterHealthThreshold4 { get; private set; }
        public ulong WeaponsCrate { get; private set; }
        public ProceduralUsePowerContextPrototype[] WeaponsCratesAnimOnlyPowers { get; private set; }
        public MoveToContextPrototype MoveToWeaponsCrate { get; private set; }
        public ulong WeaponCrate1UnlockPower { get; private set; }
        public ulong WeaponCrate2UnlockPower { get; private set; }
        public ulong WeaponCrate3UnlockPower { get; private set; }
        public ulong WeaponCrate4UnlockPower { get; private set; }
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public ProceduralFlankContextPrototype FlankTarget { get; private set; }
    }

    public class ProceduralProfileHulkBusterOSPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public ulong RedSkullAxis { get; private set; }
        public ProceduralUsePowerContextPrototype ShieldRedSkull { get; private set; }
        public ProceduralUsePowerContextPrototype DeactivatedAnimOnly { get; private set; }
        public ProceduralUsePowerContextPrototype ActivatingAnimOnly { get; private set; }
    }

    public class ProceduralProfileSymbioteDrainPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralUsePowerContextPrototype SymbiotePower1 { get; private set; }
        public ProceduralUsePowerContextPrototype SymbiotePower2 { get; private set; }
        public ProceduralUsePowerContextPrototype SymbiotePower3 { get; private set; }
    }

    public class ProceduralProfileOnslaughtPrototype : ProceduralProfileWithEnragePrototype
    {
        public ulong PlatformMarkerLeft { get; private set; }
        public ulong PlatformMarkerCenter { get; private set; }
        public ulong PlatformMarkerRight { get; private set; }
        public ProceduralUsePowerContextPrototype PsionicBlastLeft { get; private set; }
        public ProceduralUsePowerContextPrototype PsionicBlastCenter { get; private set; }
        public ProceduralUsePowerContextPrototype PsionicBlastRight { get; private set; }
        public ProceduralUsePowerContextPrototype SpikeDanceVFXOnly { get; private set; }
        public ProceduralUsePowerContextPrototype PrisonBeamPowerCenter { get; private set; }
        public ProceduralUsePowerContextPrototype PrisonPowerCenter { get; private set; }
        public ProceduralUsePowerContextPrototype SpikeDanceSingleVFXOnly { get; private set; }
        public ulong CallSentinelPower { get; private set; }
        public ulong CallSentinelPowerVFXOnly { get; private set; }
        public float SummonPowerThreshold1 { get; private set; }
        public float SummonPowerThreshold2 { get; private set; }
        public ProceduralUsePowerContextPrototype PrisonBeamPowerLeft { get; private set; }
        public ProceduralUsePowerContextPrototype PrisonBeamPowerRight { get; private set; }
        public ProceduralUsePowerContextPrototype PrisonPowerLeft { get; private set; }
        public ProceduralUsePowerContextPrototype PrisonPowerRight { get; private set; }
        public ulong PrisonKeyword { get; private set; }
        public ulong CenterPlatformKeyword { get; private set; }
        public ulong RightPlatformKeyword { get; private set; }
        public ulong LeftPlatformKeyword { get; private set; }
    }

    public class ProcProfileSpikeDanceControllerPrototype : ProceduralAIProfilePrototype
    {
        public ulong Onslaught { get; private set; }
        public ulong SpikeDanceMob { get; private set; }
        public int MaxSpikeDanceActivations { get; private set; }
        public float SpikeDanceMobSearchRadius { get; private set; }
    }

    public class ProceduralProfileSpikeDanceMobPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralUsePowerContextPrototype SpikeDanceMissile { get; private set; }
    }

    public class ProceduralProfileNullifierPrototype : ProceduralProfileNoMoveNoSensePrototype
    {
        public ulong ShieldEngineerKeyword { get; private set; }
        public ProceduralUsePowerContextPrototype BeamPower { get; private set; }
        public ulong NullifierAntiShield { get; private set; }
    }

    public class ProceduralProfileStrangeCauldronPrototype : ProceduralProfileNoMoveNoSensePrototype
    {
        public ulong KaeciliusPrototype { get; private set; }
    }

    public class ProceduralProfileShieldEngineerPrototype : ProceduralProfileMissionAllyPrototype
    {
        public AgentPrototype PsychicNullifierTargets { get; private set; }
        public ProceduralUsePowerContextPrototype ChargeNullifierPower { get; private set; }
        public float NullifierSearchRadius { get; private set; }
        public ulong NullifierAntiShield { get; private set; }
    }

    public class ProcProfileNullifierAntiShieldPrototype : ProceduralProfileWithEnragePrototype
    {
        public AgentPrototype Nullifiers { get; private set; }
        public ulong ShieldDamagePower { get; private set; }
        public ulong ShieldEngineerSpawner { get; private set; }
        public float SpawnerSearchRadius { get; private set; }
    }

    public class ProceduralProfileMadameHydraPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public ProceduralFlankContextPrototype FlankTarget { get; private set; }
        public ulong SummonHydraPower { get; private set; }
        public ulong InvulnerablePower { get; private set; }
        public ProceduralUsePowerContextPrototype TeleportPower { get; private set; }
        public int SummonHydraMinCooldownMS { get; private set; }
        public int SummonHydraMaxCooldownMS { get; private set; }
    }

    public class ProceduralProfileStarktechSentinelPrototype : ProceduralProfileWithEnragePrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public ProceduralUsePowerContextPrototype SummonSentinels { get; private set; }
        public float SummonPowerThreshold1 { get; private set; }
        public float SummonPowerThreshold2 { get; private set; }
    }

    public class ProceduralProfileKingpinPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public ProceduralUsePowerContextPrototype SummonElektra { get; private set; }
        public ProceduralUsePowerContextPrototype SummonBullseye { get; private set; }
        public float SummonElektraThreshold { get; private set; }
        public float SummonBullseyeThreshold { get; private set; }
    }

    public class ProceduralProfilePowerRestrictedPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public ProceduralFlankContextPrototype FlankTarget { get; private set; }
        public bool IsRanged { get; private set; }
        public ProceduralUsePowerContextPrototype RestrictedModeStartPower { get; private set; }
        public ProceduralUsePowerContextPrototype RestrictedModeEndPower { get; private set; }
        public ProceduralUsePowerContextPrototype[] RestrictedModeProceduralPowers { get; private set; }
        public int RestrictedModeMinCooldownMS { get; private set; }
        public int RestrictedModeMaxCooldownMS { get; private set; }
        public int RestrictedModeTimerMS { get; private set; }
        public bool NoMoveInRestrictedMode { get; private set; }
    }

    public class ProceduralProfileUltronEMPPrototype : ProceduralProfileNoMoveNoSensePrototype
    {
        public ProceduralUsePowerContextPrototype EMPPower { get; private set; }
    }

    public class ProcProfileQuicksilverTeamUpPrototype : ProceduralProfileTeamUpPrototype
    {
        public ProceduralUsePowerContextPrototype SpecialMovementPower { get; private set; }
    }

    public class ProceduralProfileSkrullNickFuryPrototype : ProceduralProfileRangeFlankerPrototype
    {
        public ProceduralUsePowerContextPrototype OpenRocketCratePower { get; private set; }
        public ProceduralUsePowerContextPrototype OpenMinigunCratePower { get; private set; }
        public ProceduralUsePowerContextPrototype UseRocketPower { get; private set; }
        public ProceduralUsePowerContextPrototype UseMinigunPower { get; private set; }
        public MoveToContextPrototype MoveToCrate { get; private set; }
        public ProceduralUsePowerContextPrototype CommandTurretPower { get; private set; }
        public int CratePowerUseCount { get; private set; }
        public ProceduralUsePowerContextPrototype DiscardWeaponPower { get; private set; }
        public ulong CrateUsedState { get; private set; }
    }

    public class ProceduralProfileNickFuryTurretPrototype : ProceduralProfileRotatingTurretWithTargetPrototype
    {
        public ProceduralUsePowerContextPrototype SpecialCommandPower { get; private set; }
        public ulong SkrullNickFuryRef { get; private set; }
    }

    public class ProceduralProfileKaeciliusPrototype : ProceduralProfileBasicRangePrototype
    {
        public ProceduralPowerWithSpecificTargetsPrototype[] HotspotSpawners { get; private set; }
        public ProceduralThresholdPowerContextPrototype FalseDeathPower { get; private set; }
        public ProceduralUsePowerContextPrototype HealFinalFormPower { get; private set; }
        public ProceduralUsePowerContextPrototype DeathPreventerPower { get; private set; }
        public ulong Cauldron { get; private set; }
    }

    public class ProceduralProfileMeleeRevengePrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralUsePowerContextPrototype RevengePower { get; private set; }
        public ulong RevengeSupport { get; private set; }
    }

    public class ProceduralProfileRangedRevengePrototype : ProceduralProfileBasicRangePrototype
    {
        public ProceduralUsePowerContextPrototype RevengePower { get; private set; }
        public ulong RevengeSupport { get; private set; }
    }

    public class ProceduralProfileTaserTrapPrototype : ProceduralProfileWithTargetPrototype
    {
        public ulong TaserHotspot { get; private set; }
    }

    public class ProceduralProfileVulturePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; private set; }
        public OrbitContextPrototype OrbitTarget { get; private set; }
        public ProceduralUsePowerContextPrototype LungePower { get; private set; }
        public int MaxLungeActivations { get; private set; }
    }
}
