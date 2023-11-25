namespace MHServerEmu.Games.GameData.Prototypes
{
    public enum PathMethod
    {
        Invalid = 0,
        Forward = 1,
        ForwardLoop = 5,
        ForwardBackAndForth = 3,
        Reverse = 2,
        ReverseLoop = 6,
        ReverseBackAndForth = 4,
    }

    public class ProceduralContextPrototype : Prototype
    {
    }

    public class ProceduralUsePowerContextSwitchTargetPrototype : Prototype
    {
        public SelectEntityContextPrototype SelectTarget { get; set; }
        public bool SwitchPermanently { get; set; }
        public bool UsePowerOnCurTargetIfSwitchFails { get; set; }
    }

    public class ProceduralUsePowerContextPrototype : ProceduralContextPrototype
    {
        public int InitialCooldownMinMS { get; set; }
        public int MaxCooldownMS { get; set; }
        public int MinCooldownMS { get; set; }
        public UsePowerContextPrototype PowerContext { get; set; }
        public int PickWeight { get; set; }
        public ProceduralUsePowerContextSwitchTargetPrototype TargetSwitch { get; set; }
        public int InitialCooldownMaxMS { get; set; }
        public ulong RestrictToDifficultyMin { get; set; }
        public ulong RestrictToDifficultyMax { get; set; }
    }

    public class ProceduralUseAffixPowerContextPrototype : ProceduralContextPrototype
    {
        public UseAffixPowerContextPrototype AffixContext { get; set; }
        public int PickWeight { get; set; }
    }

    public class ProceduralFlankContextPrototype : ProceduralContextPrototype
    {
        public int MaxFlankCooldownMS { get; set; }
        public int MinFlankCooldownMS { get; set; }
        public FlankContextPrototype FlankContext { get; set; }
    }

    public class ProceduralInteractContextPrototype : ProceduralContextPrototype
    {
        public InteractContextPrototype InteractContext { get; set; }
    }

    public class ProceduralFleeContextPrototype : ProceduralContextPrototype
    {
        public int MaxFleeCooldownMS { get; set; }
        public int MinFleeCooldownMS { get; set; }
        public FleeContextPrototype FleeContext { get; set; }
    }

    public class ProceduralSyncAttackContextPrototype : Prototype
    {
        public ulong TargetEntity { get; set; }
        public ulong TargetEntityPower { get; set; }
        public ProceduralUsePowerContextPrototype LeaderPower { get; set; }
    }

    public class ProceduralThresholdPowerContextPrototype : ProceduralUsePowerContextPrototype
    {
        public float HealthThreshold { get; set; }
    }

    public class ProceduralPowerWithSpecificTargetsPrototype : Prototype
    {
        public float HealthThreshold { get; set; }
        public ulong PowerToUse { get; set; }
        public AgentPrototype Targets { get; set; }
    }

    public class ProceduralAIProfilePrototype : BrainPrototype
    {
    }

    public class ProceduralProfileWithTargetPrototype : ProceduralAIProfilePrototype
    {
        public SelectEntityContextPrototype SelectTarget { get; set; }
        public ulong NoTargetOverrideProfile { get; set; }
    }

    public class ProceduralProfileWithAttackPrototype : ProceduralProfileWithTargetPrototype
    {
        public int AttackRateMaxMS { get; set; }
        public int AttackRateMinMS { get; set; }
        public ProceduralUsePowerContextPrototype[] GenericProceduralPowers { get; set; }
        public ProceduralUseAffixPowerContextPrototype AffixSettings { get; set; }
    }

    public class ProceduralProfileEnticerPrototype : ProceduralAIProfilePrototype
    {
        public int CooldownMinMS { get; set; }
        public int CooldownMaxMS { get; set; }
        public int EnticeeEnticerCooldownMaxMS { get; set; }
        public int EnticeeEnticerCooldownMinMS { get; set; }
        public int EnticeeGlobalEnticerCDMaxMS { get; set; }
        public int EnticeeGlobalEnticerCDMinMS { get; set; }
        public int MaxSubscriptions { get; set; }
        public int MaxSubscriptionsPerActivation { get; set; }
        public float Radius { get; set; }
        public AIEntityAttributePrototype[] EnticeeAttributes { get; set; }
        public ulong EnticedBehavior { get; set; }
    }

    public class ProceduralProfileEnticedBehaviorPrototype : ProceduralAIProfilePrototype
    {
        public FlankContextPrototype FlankToEnticer { get; set; }
        public MoveToContextPrototype MoveToEnticer { get; set; }
        public ulong DynamicBehavior { get; set; }
        public bool OrientToEnticerOrientation { get; set; }
    }

    public class ProceduralProfileSenseOnlyPrototype : ProceduralAIProfilePrototype
    {
        public AIEntityAttributePrototype[] AttributeList { get; set; }
        public ulong AllianceOverride { get; set; }
    }

    public class ProceduralProfileInteractEnticerOverridePrototype : ProceduralAIProfilePrototype
    {
        public InteractContextPrototype Interact { get; set; }
    }

    public class ProceduralProfileUsePowerEnticerOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public UsePowerContextPrototype Power { get; set; }
        public new SelectEntityContextPrototype SelectTarget { get; set; }
    }

    public class ProceduralProfileFearOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public FleeContextPrototype FleeFromTarget { get; set; }
        public WanderContextPrototype WanderIfNoTarget { get; set; }
    }

    public class ProceduralProfileLeashOverridePrototype : ProceduralAIProfilePrototype
    {
        public ulong LeashReturnHeal { get; set; }
        public ulong LeashReturnImmunity { get; set; }
        public MoveToContextPrototype MoveToSpawn { get; set; }
        public TeleportContextPrototype TeleportToSpawn { get; set; }
        public ulong LeashReturnTeleport { get; set; }
        public ulong LeashReturnInvulnerability { get; set; }
    }

    public class ProceduralProfileRunToExitAndDespawnOverridePrototype : ProceduralAIProfilePrototype
    {
        public MoveToContextPrototype RunToExit { get; set; }
        public int NumberOfWandersBeforeDestroy { get; set; }
        public DelayContextPrototype DelayBeforeRunToExit { get; set; }
        public SelectEntityContextPrototype SelectPortalToExitFrom { get; set; }
        public DelayContextPrototype DelayBeforeDestroyOnMoveExitFail { get; set; }
        public bool VanishesIfMoveToExitFails { get; set; }
    }

    public class ProceduralProfileRunToTargetAndDespawnOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public ulong Invulnerability { get; set; }
        public int NumberOfWandersBeforeDestroy { get; set; }
        public MoveToContextPrototype RunToTarget { get; set; }
        public WanderContextPrototype WanderIfMoveFails { get; set; }
    }

    public class ProceduralProfileDefaultActiveOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public DelayContextPrototype DelayAfterWander { get; set; }
        public WanderContextPrototype Wander { get; set; }
        public WanderContextPrototype WanderInPlace { get; set; }
    }

    public class ProceduralProfileFleeOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public FleeContextPrototype FleeFromTarget { get; set; }
    }

    public class ProceduralProfileOrbPrototype : ProceduralProfileWithTargetPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public int InitialMoveToDelayMS { get; set; }
        public StateChangePrototype InvalidTargetState { get; set; }
        public float OrbRadius { get; set; }
        public ulong EffectPower { get; set; }
        public bool AcceptsAggroRangeBonus { get; set; }
        public int ShrinkageDelayMS { get; set; }
        public int ShrinkageDurationMS { get; set; }
        public float ShrinkageMinScale { get; set; }
        public bool DestroyOrbOnUnSimOrTargetLoss { get; set; }
    }

    public class ProceduralProfileStationaryTurretPrototype : ProceduralProfileWithAttackPrototype
    {
    }

    public class ProceduralProfileRotatingTurretPrototype : ProceduralAIProfilePrototype
    {
        public UsePowerContextPrototype Power { get; set; }
        public RotateContextPrototype Rotate { get; set; }
    }

    public class ProceduralProfileRotatingTurretWithTargetPrototype : ProceduralProfileWithAttackPrototype
    {
        public RotateContextPrototype Rotate { get; set; }
    }

    public class ProceduralProfileBasicMeleePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public ProceduralUsePowerContextPrototype PrimaryPower { get; set; }
    }

    public class ProceduralProfileBasicMelee2PowerPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public ProceduralUsePowerContextPrototype Power1 { get; set; }
        public ProceduralUsePowerContextPrototype Power2 { get; set; }
    }

    public class ProceduralProfileBasicRangePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
    }

    public class ProceduralProfileAlternateRange2Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public ProceduralFlankContextPrototype FlankTarget { get; set; }
        public ProceduralUsePowerContextPrototype Power1 { get; set; }
        public ProceduralUsePowerContextPrototype Power2 { get; set; }
        public ProceduralUsePowerContextPrototype PowerSwap { get; set; }
    }

    public class ProceduralProfileMultishotRangePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public ProceduralUsePowerContextPrototype MultishotPower { get; set; }
        public int NumShots { get; set; }
        public bool RetargetPerShot { get; set; }
    }

    public class ProceduralProfileMultishotFlankerPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralFlankContextPrototype FlankTarget { get; set; }
        public MoveToContextPrototype MoveToTarget { get; set; }
        public ProceduralUsePowerContextPrototype MultishotPower { get; set; }
        public int NumShots { get; set; }
        public bool RetargetPerShot { get; set; }
    }

    public class ProceduralProfileMultishotHiderPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralUsePowerContextPrototype HidePower { get; set; }
        public ProceduralUsePowerContextPrototype MultishotPower { get; set; }
        public int NumShots { get; set; }
        public bool RetargetPerShot { get; set; }
    }

    public class ProceduralProfileMeleeSpeedByDistancePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public ProceduralUsePowerContextPrototype PrimaryPower { get; set; }
        public UsePowerContextPrototype ExtraSpeedPower { get; set; }
        public UsePowerContextPrototype SpeedRemovalPower { get; set; }
        public float DistanceFromTargetForSpeedBonus { get; set; }
    }

    public class ProceduralProfileRangeFlankerPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralFlankContextPrototype FlankTarget { get; set; }
        public MoveToContextPrototype MoveToTarget { get; set; }
        public ProceduralUsePowerContextPrototype PrimaryPower { get; set; }
    }

    public class ProceduralProfileSkirmisherPrototype : ProceduralProfileWithAttackPrototype
    {
        public WanderContextPrototype SkirmishMovement { get; set; }
        public MoveToContextPrototype MoveToTarget { get; set; }
        public ProceduralUsePowerContextPrototype PrimaryPower { get; set; }
        public float MoveToSpeedBonus { get; set; }
    }

    public class ProceduralProfileRangedWithMeleePriority2PowerPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public ProceduralUsePowerContextPrototype MeleePower { get; set; }
        public ProceduralUsePowerContextPrototype RangedPower { get; set; }
        public float MaxDistToMoveIntoMelee { get; set; }
        public MoveToContextPrototype MoveIntoMeleeRange { get; set; }
    }

    public class ProfMeleePwrSpecialAtHealthPctPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public float SpecialAtHealthChunkPct { get; set; }
        public UsePowerContextPrototype SpecialPowerAtHealthChunkPct { get; set; }
    }

    public class ProceduralProfileShockerPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public MoveToContextPrototype MoveIntoMeleeRange { get; set; }
        public ProceduralUsePowerContextPrototype MeleePower { get; set; }
        public ProceduralUsePowerContextPrototype SpecialPower { get; set; }
        public ulong SpecialSummonPower { get; set; }
        public float MaxDistToMoveIntoMelee { get; set; }
        public int SpecialPowerNumSummons { get; set; }
        public float SpecialPowerMaxRadius { get; set; }
        public float SpecialPowerMinRadius { get; set; }
    }

    public class ProceduralProfileLadyDeathstrikePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public ProceduralUsePowerContextPrototype HealingPower { get; set; }
        public ProceduralUsePowerContextPrototype SpecialPower { get; set; }
        public SelectEntityContextPrototype SpecialPowerSelectTarget { get; set; }
        public int SpecialPowerChangeTgtIntervalMS { get; set; }
    }

    public class ProceduralProfileFastballSpecialWolverinePrototype : ProceduralProfileWithTargetPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public WanderContextPrototype MoveToNoTarget { get; set; }
        public UsePowerContextPrototype Power { get; set; }
        public int PowerChangeTargetIntervalMS { get; set; }
    }

    public class ProceduralProfileSeekingMissilePrototype : ProceduralProfileWithTargetPrototype
    {
        public SelectEntityContextPrototype SecondaryTargetSelection { get; set; }
        public int SeekDelayMS { get; set; }
        public float SeekDelaySpeed { get; set; }
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
        public MoveToContextPrototype MoveToTarget { get; set; }
    }

    public class ProceduralProfileWanderNoPowerPrototype : ProceduralAIProfilePrototype
    {
        public WanderContextPrototype WanderMovement { get; set; }
    }

    public class ProceduralProfileBasicWanderPrototype : ProceduralProfileWithAttackPrototype
    {
        public WanderContextPrototype WanderMovement { get; set; }
    }

    public class ProceduralProfilePvPMeleePrototype : ProceduralProfileWithAttackPrototype
    {
        public float AggroRadius { get; set; }
        public float AggroDropRadius { get; set; }
        public float AggroDropByLOSChance { get; set; }
        public long AttentionSpanMS { get; set; }
        public ulong PrimaryPower { get; set; }
        public int PathGroup { get; set; }
        public PathMethod PathMethod { get; set; }
        public float PathThreshold { get; set; }
    }

    public class ProceduralProfilePvPTowerPrototype : ProceduralProfileWithAttackPrototype
    {
        public SelectEntityContextPrototype SelectTarget2 { get; set; }
        public SelectEntityContextPrototype SelectTarget3 { get; set; }
        public SelectEntityContextPrototype SelectTarget4 { get; set; }
    }

    public class ProceduralProfileMeleeDropWeaponPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public ProceduralUsePowerContextPrototype PowerMeleeWithWeapon { get; set; }
        public ProceduralUsePowerContextPrototype PowerMeleeNoWeapon { get; set; }
        public ProceduralUsePowerContextPrototype PowerDropWeapon { get; set; }
        public ProceduralUsePowerContextPrototype PowerPickupWeapon { get; set; }
        public SelectEntityContextPrototype SelectWeaponAsTarget { get; set; }
        public int DropPickupTimeMax { get; set; }
        public int DropPickupTimeMin { get; set; }
    }

    public class ProceduralProfileMeleeAllyDeathFleePrototype : ProceduralProfileWithAttackPrototype
    {
        public FleeContextPrototype FleeFromTargetOnAllyDeath { get; set; }
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
    }

    public class ProceduralProfileRangedFlankerAllyDeathFleePrototype : ProceduralProfileWithAttackPrototype
    {
        public FleeContextPrototype FleeFromTargetOnAllyDeath { get; set; }
        public MoveToContextPrototype MoveToTarget { get; set; }
        public ProceduralFlankContextPrototype FlankTarget { get; set; }
    }

    public class ProceduralProfileMagnetoPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public ProceduralFlankContextPrototype FlankTarget { get; set; }
        public ProceduralUsePowerContextPrototype ChasePower { get; set; }
    }

    public class ProcProfMrSinisterPrototype : ProceduralProfileWithAttackPrototype
    {
        public float CloneCylHealthPctThreshWave1 { get; set; }
        public float CloneCylHealthPctThreshWave2 { get; set; }
        public float CloneCylHealthPctThreshWave3 { get; set; }
        public UsePowerContextPrototype CloneCylSummonFXPower { get; set; }
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public TriggerSpawnersContextPrototype TriggerCylinderSpawnerAction { get; set; }
    }

    public class ProcProfMrSinisterCloneCylinderPrototype : ProceduralProfileWithAttackPrototype
    {
        public UsePowerContextPrototype CylinderOpenPower { get; set; }
        public DespawnContextPrototype DespawnAction { get; set; }
        public int PreOpenDelayMS { get; set; }
        public int PostOpenDelayMS { get; set; }
    }

    public class ProceduralProfileBlobPrototype : ProceduralProfileBasicRangePrototype
    {
        public ProceduralUsePowerContextPrototype SummonToadPower { get; set; }
        public ulong ToadPrototype { get; set; }
    }

    public class ProceduralProfileRangedHotspotDropperPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public ProceduralUsePowerContextPrototype RangedPower { get; set; }
        public ProceduralUsePowerContextPrototype HotspotPower { get; set; }
        public WanderContextPrototype HotspotDroppingMovement { get; set; }
    }

    public class ProceduralProfileTeamUpPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public ProceduralFlankContextPrototype FlankTarget { get; set; }
        public bool IsRanged { get; set; }
        public MoveToContextPrototype MoveToMaster { get; set; }
        public TeleportContextPrototype TeleportToMasterIfTooFarAway { get; set; }
        public int MaxDistToMasterBeforeTeleport { get; set; }
        public ProceduralUsePowerContextPrototype[] TeamUpPowerProgressionPowers { get; set; }
    }

    public class ProceduralProfilePetPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype PetFollow { get; set; }
        public TeleportContextPrototype TeleportToMasterIfTooFarAway { get; set; }
        public int MaxDistToMasterBeforeTeleport { get; set; }
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public ProceduralFlankContextPrototype FlankTarget { get; set; }
        public bool IsRanged { get; set; }
    }

    public class ProceduralProfilePetFidgetPrototype : ProceduralProfilePetPrototype
    {
        public ProceduralUsePowerContextPrototype Fidget { get; set; }
    }

    public class ProceduralProfileSquirrelGirlSquirrelPrototype : ProceduralProfilePetPrototype
    {
        public ProceduralFlankContextPrototype FlankMaster { get; set; }
        public float DeadzoneAroundFlankTarget { get; set; }
    }

    public class ProceduralProfileRollingGrenadesPrototype : ProceduralAIProfilePrototype
    {
        public int MaxSpeedDegreeUpdateIntervalMS { get; set; }
        public int MinSpeedDegreeUpdateIntervalMS { get; set; }
        public int MovementSpeedVariance { get; set; }
        public int RandomDegreeFromForward { get; set; }
    }

    public class ProceduralProfileSquirrelTriplePrototype : ProceduralAIProfilePrototype
    {
        public int JumpDistanceMax { get; set; }
        public int JumpDistanceMin { get; set; }
        public DelayContextPrototype PauseSettings { get; set; }
        public int RandomDirChangeDegrees { get; set; }
    }

    public class ProceduralProfileFrozenOrbPrototype : ProceduralAIProfilePrototype
    {
        public int ShardBurstsPerSecond { get; set; }
        public int ShardsPerBurst { get; set; }
        public int ShardRotationSpeed { get; set; }
        public ulong ShardPower { get; set; }
    }

    public class ProceduralProfileDrDoomPhase1Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public ulong DeathStun { get; set; }
        public ProceduralUsePowerContextPrototype SummonTurretPowerAnimOnly { get; set; }
        public UsePowerContextPrototype SummonDoombotBlockades { get; set; }
        public UsePowerContextPrototype SummonDoombotInfernos { get; set; }
        public UsePowerContextPrototype SummonDoombotFlyers { get; set; }
        public ProceduralUsePowerContextPrototype SummonDoombotAnimOnly { get; set; }
        public ulong SummonDoombotBlockadesCurve { get; set; }
        public ulong SummonDoombotInfernosCurve { get; set; }
        public ulong SummonDoombotFlyersCurve { get; set; }
        public int SummonDoombotWaveIntervalMS { get; set; }
        public ProceduralUsePowerContextPrototype SummonOrbSpawners { get; set; }
        public TriggerSpawnersContextPrototype SpawnTurrets { get; set; }
        public TriggerSpawnersContextPrototype SpawnDrDoomPhase2 { get; set; }
        public TriggerSpawnersContextPrototype DestroyTurretsOnDeath { get; set; }
    }

    public class ProceduralProfileDrDoomPhase2Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public ulong DeathStun { get; set; }
        public ulong StarryExpanseAnimOnly { get; set; }
        public TriggerSpawnersContextPrototype SpawnDrDoomPhase3 { get; set; }
        public TriggerSpawnersContextPrototype SpawnStarryExpanseAreas { get; set; }
    }

    public class ProceduralProfileDrDoomPhase3Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public RotateContextPrototype RapidFireRotate { get; set; }
        public ProceduralUsePowerContextPrototype RapidFirePower { get; set; }
        public ulong StarryExpanseAnimOnly { get; set; }
        public ProceduralUsePowerContextPrototype CosmicSummonsAnimOnly { get; set; }
        public UsePowerContextPrototype CosmicSummonsPower { get; set; }
        public ulong[] CosmicSummonEntities { get; set; }
        public ulong CosmicSummonsNumEntities { get; set; }
        public TriggerSpawnersContextPrototype SpawnStarryExpanseAreas { get; set; }
    }

    public class ProceduralProfileDrDoomPhase1OrbSpawnerPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveTo { get; set; }
        public ProceduralFlankContextPrototype Flank { get; set; }
    }

    public class ProceduralProfileMODOKPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralUsePowerContextPrototype TeleportToEntityPower { get; set; }
        public SelectEntityContextPrototype SelectTeleportTarget { get; set; }
        public ProceduralUsePowerContextPrototype[] SummonProceduralPowers { get; set; }
    }

    public class ProceduralProfileSauronPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public ProceduralUsePowerContextPrototype MovementPower { get; set; }
        public ProceduralUsePowerContextPrototype LowHealthPower { get; set; }
        public float LowHealthPowerThresholdPct { get; set; }
    }

    public class ProceduralProfileMandarinPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public ProceduralFlankContextPrototype FlankTarget { get; set; }
        public SelectEntityContextPrototype SequencePowerSelectTarget { get; set; }
        public ProceduralUsePowerContextPrototype[] SequencePowers { get; set; }
    }

    public class ProceduralProfileSabretoothPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public ProceduralUsePowerContextPrototype MovementPower { get; set; }
        public SelectEntityContextPrototype MovementPowerSelectTarget { get; set; }
        public ProceduralUsePowerContextPrototype LowHealthPower { get; set; }
        public float LowHealthPowerThresholdPct { get; set; }
    }

    public class ProceduralProfileMeleePowerOnHitPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public ProceduralUsePowerContextPrototype PowerOnHit { get; set; }
    }

    public class ProceduralProfileGrimReaperPrototype : ProfMeleePwrSpecialAtHealthPctPrototype
    {
        public ProceduralUsePowerContextPrototype TripleShotPower { get; set; }
        public ProceduralUsePowerContextPrototype SpecialPower { get; set; }
        public SelectEntityContextPrototype SpecialSelectTarget { get; set; }
        public int SpecialPowerChangeTgtIntervalMS { get; set; }
    }

    public class ProceduralProfileMoleManPrototype : ProceduralProfileBasicRangePrototype
    {
        public TriggerSpawnersContextPrototype[] GigantoSpawners { get; set; }
        public ProceduralUsePowerContextPrototype MoloidInvasionPower { get; set; }
        public TriggerSpawnersContextPrototype MoloidInvasionSpawner { get; set; }
        public ProceduralUsePowerContextPrototype SummonGigantoAnimPower { get; set; }
    }

    public class ProceduralProfileVenomPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralFlankContextPrototype FlankTarget { get; set; }
        public MoveToContextPrototype MoveToTarget { get; set; }
        public UsePowerContextPrototype VenomMad { get; set; }
        public float VenomMadThreshold1 { get; set; }
        public float VenomMadThreshold2 { get; set; }
    }

    public class ProceduralProfileVanityPetPrototype : ProceduralProfileWithTargetPrototype
    {
        public MoveToContextPrototype PetFollow { get; set; }
        public TeleportContextPrototype TeleportToMasterIfTooFarAway { get; set; }
        public int MinTimerWhileNotMovingFidgetMS { get; set; }
        public int MaxTimerWhileNotMovingFidgetMS { get; set; }
        public float MaxDistToMasterBeforeTeleport { get; set; }
    }

    public class ProceduralProfileDoopPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralFleeContextPrototype Flee { get; set; }
        public ProceduralUsePowerContextPrototype DisappearPower { get; set; }
        public int LifeTimeMinMS { get; set; }
        public int LifeTimeMaxMS { get; set; }
    }

    public class ProceduralProfileGorgonPrototype : ProceduralProfileWithAttackPrototype
    {
        public RotateContextPrototype RotateInStoneGaze { get; set; }
        public ProceduralUsePowerContextPrototype StoneGaze { get; set; }
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
    }

    public class ProceduralProfileBotAIPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public SelectEntityContextPrototype SelectTargetItem { get; set; }
        public WanderContextPrototype WanderMovement { get; set; }
        public ProceduralUsePowerContextPrototype[] SlottedAbilities { get; set; }
    }

    public class ProceduralProfileBullseyePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public ProceduralFlankContextPrototype FlankTarget { get; set; }
        public ProceduralUsePowerContextPrototype MarkForDeath { get; set; }
    }

    public class ProceduralProfileRhinoPrototype : ProceduralProfileBasicMeleePrototype
    {
    }

    public class ProceduralProfileBlackCatPrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralUsePowerContextPrototype TumblePower { get; set; }
        public ProceduralUsePowerContextPrototype TumbleComboPower { get; set; }
        public SelectEntityContextPrototype SelectEntityForTumbleCombo { get; set; }
    }

    public class ProceduralProfileControlledMobOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public MoveToContextPrototype ControlFollow { get; set; }
        public TeleportContextPrototype TeleportToMasterIfTooFarAway { get; set; }
        public float MaxDistToMasterBeforeTeleport { get; set; }
        public int MaxDistToMasterBeforeFollow { get; set; }
    }

    public class ProceduralProfileLivingLaserPrototype : ProceduralProfileBasicRangePrototype
    {
        public ProceduralUsePowerContextPrototype SweepingBeamPowerClock { get; set; }
        public ProceduralUsePowerContextPrototype SweepingBeamPowerCounterClock { get; set; }
        public RotateContextPrototype SweepingBeamClock { get; set; }
        public RotateContextPrototype SweepingBeamCounterClock { get; set; }
    }

    public class ProceduralProfileMeleeFlockerPrototype : ProceduralProfileWithAttackPrototype
    {
        public FlockContextPrototype FlockContext { get; set; }
        public ulong FleeOnAllyDeathOverride { get; set; }
    }

    public class ProceduralProfileLizardBossPrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralUsePowerContextPrototype LizardSwarmPower { get; set; }
    }

    public class ProceduralProfilePetDirectedPrototype : ProceduralProfilePetPrototype
    {
        public ProceduralUsePowerContextPrototype[] DirectedPowers { get; set; }
    }

    public class ProceduralProfileLokiPhase1Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public ProceduralFlankContextPrototype FlankTarget { get; set; }
    }

    public class ProceduralProfileLokiPhase2Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public ProceduralUsePowerContextPrototype InverseRings { get; set; }
        public ulong InverseRingsTargetedVFXOnly { get; set; }
        public ulong LokiBossSafeZoneKeyword { get; set; }
        public ulong InverseRingsVFXRemoval { get; set; }
    }

    public class ProceduralProfileDrStrangeProjectionPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralUsePowerContextPrototype[] ProjectionPowers { get; set; }
        public ProceduralFlankContextPrototype FlankMaster { get; set; }
        public float DeadzoneAroundFlankTarget { get; set; }
        public int FlankToMasterDelayMS { get; set; }
        public TeleportContextPrototype TeleportToMasterIfTooFarAway { get; set; }
        public int MaxDistToMasterBeforeTeleport { get; set; }
    }

    public class ProceduralProfileEyeOfAgamottoPrototype : ProceduralProfileStationaryTurretPrototype
    {
        public RotateContextPrototype IdleRotation { get; set; }
    }

    public class MistressOfMagmaTeleportDestPrototype : Prototype
    {
        public SelectEntityContextPrototype DestinationSelector { get; set; }
        public ulong ImmunityBoost { get; set; }
    }

    public class ProceduralProfileSpikedBallPrototype : ProceduralProfileWithTargetPrototype
    {
        public float MoveToSummonerDistance { get; set; }
        public float IdleDistanceFromSummoner { get; set; }
        public RotateContextPrototype Rotate { get; set; }
        public int SeekDelayMS { get; set; }
        public float Acceleration { get; set; }
        public MoveToContextPrototype MoveToTarget { get; set; }
        public WanderContextPrototype Wander { get; set; }
        public TeleportContextPrototype TeleportToMasterIfTooFarAway { get; set; }
        public int MaxDistToMasterBeforeTeleport { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
    }

    public class ProceduralProfileWithEnragePrototype : ProceduralProfileWithAttackPrototype
    {
        public int EnrageTimerInMinutes { get; set; }
        public ProceduralUsePowerContextPrototype EnragePower { get; set; }
        public float EnrageTimerAvatarSearchRadius { get; set; }
        public ProceduralUsePowerContextPrototype[] PostEnragePowers { get; set; }
    }

    public class ProceduralProfileSyncAttackPrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralSyncAttackContextPrototype[] SyncAttacks { get; set; }
    }

    public class ProceduralProfileBrimstonePrototype : ProceduralProfileWithEnragePrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public MoveToContextPrototype MoveIntoMeleeRange { get; set; }
        public ProceduralUsePowerContextPrototype MeleePower { get; set; }
        public ulong HellfireProtoRef { get; set; }
    }

    public class ProceduralProfileSlagPrototype : ProceduralProfileWithEnragePrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public ProceduralUsePowerContextPrototype PrimaryPower { get; set; }
    }

    public class ProceduralProfileMonolithPrototype : ProceduralProfileWithEnragePrototype
    {
        public ulong ObeliskKeyword { get; set; }
        public ulong[] ObeliskDamageMonolithPowers { get; set; }
        public ulong DisableShield { get; set; }
    }

    public class ProceduralProfileHellfirePrototype : ProceduralProfileWithEnragePrototype
    {
        public ProceduralFlankContextPrototype FlankTarget { get; set; }
        public MoveToContextPrototype MoveToTarget { get; set; }
        public ProceduralUsePowerContextPrototype PrimaryPower { get; set; }
        public ulong BrimstoneProtoRef { get; set; }
        public ProceduralUsePowerContextPrototype SpecialPower { get; set; }
        public ulong SpecialSummonPower { get; set; }
        public int SpecialPowerNumSummons { get; set; }
        public float SpecialPowerMaxRadius { get; set; }
        public float SpecialPowerMinRadius { get; set; }
    }

    public class ProceduralProfileMistressOfMagmaPrototype : ProceduralProfileWithEnragePrototype
    {
        public ProceduralFlankContextPrototype FlankTarget { get; set; }
        public MoveToContextPrototype MoveToTarget { get; set; }
        public ProceduralUsePowerContextPrototype BombDancePower { get; set; }
    }

    public class ProceduralProfileSurturPrototype : ProceduralProfileWithEnragePrototype
    {
        public ulong FirePillarPower { get; set; }
        public int FirePillarMinCooldownMS { get; set; }
        public int FirePillarMaxCooldownMS { get; set; }
        public int FirePillarPowerMaxTargets { get; set; }
        public ulong PowerUnlockBrimstone { get; set; }
        public ulong PowerUnlockHellfire { get; set; }
        public ulong PowerUnlockMistress { get; set; }
        public ulong PowerUnlockMonolith { get; set; }
        public ulong PowerUnlockSlag { get; set; }
        public ulong MiniBossBrimstone { get; set; }
        public ulong MiniBossHellfire { get; set; }
        public ulong MiniBossMistress { get; set; }
        public ulong MiniBossMonolith { get; set; }
        public ulong MiniBossSlag { get; set; }
    }

    public class ProceduralProfileSurturPortalPrototype : ProceduralProfileNoMoveNoSensePrototype
    {
    }

    public class ProceduralProfileObeliskHealerPrototype : ProceduralProfileBasicMeleePrototype
    {
        public ulong[] ObeliskTargets { get; set; }
    }

    public class ProceduralProfileObeliskPrototype : ProceduralProfileNoMoveDefaultSensoryPrototype
    {
        public ulong DeadEntityForDetonateIslandPower { get; set; }
        public ulong DetonateIslandPower { get; set; }
        public ulong FullyHealedPower { get; set; }
    }

    public class ProceduralProfileFireGiantChaserPrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralUsePowerContextPrototype MarkTargetPower { get; set; }
        public ulong MarkTargetVFXRemoval { get; set; }
    }

    public class ProceduralProfileMissionAllyPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public ProceduralFlankContextPrototype FlankTarget { get; set; }
        public MoveToContextPrototype MoveToAvatarAlly { get; set; }
        public TeleportContextPrototype TeleportToAvatarAllyIfTooFarAway { get; set; }
        public int MaxDistToAvatarAllyBeforeTele { get; set; }
        public bool IsRanged { get; set; }
        public float AvatarAllySearchRadius { get; set; }
    }

    public class ProceduralProfileLOSRangedPrototype : ProceduralProfileBasicRangePrototype
    {
        public ProceduralUsePowerContextPrototype LOSChannelPower { get; set; }
    }

    public class ProceduralProfileRedSkullOneShotPrototype : ProceduralProfileWithAttackPrototype
    {
        public ulong[] HulkBustersToActivate { get; set; }
        public ProceduralUsePowerContextPrototype ActivateHulkBusterAnimOnly { get; set; }
        public float HulkBusterHealthThreshold1 { get; set; }
        public float HulkBusterHealthThreshold2 { get; set; }
        public float HulkBusterHealthThreshold3 { get; set; }
        public float HulkBusterHealthThreshold4 { get; set; }
        public ulong WeaponsCrate { get; set; }
        public ProceduralUsePowerContextPrototype[] WeaponsCratesAnimOnlyPowers { get; set; }
        public MoveToContextPrototype MoveToWeaponsCrate { get; set; }
        public ulong WeaponCrate1UnlockPower { get; set; }
        public ulong WeaponCrate2UnlockPower { get; set; }
        public ulong WeaponCrate3UnlockPower { get; set; }
        public ulong WeaponCrate4UnlockPower { get; set; }
        public MoveToContextPrototype MoveToTarget { get; set; }
        public ProceduralFlankContextPrototype FlankTarget { get; set; }
    }

    public class ProceduralProfileHulkBusterOSPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public ulong RedSkullAxis { get; set; }
        public ProceduralUsePowerContextPrototype ShieldRedSkull { get; set; }
        public ProceduralUsePowerContextPrototype DeactivatedAnimOnly { get; set; }
        public ProceduralUsePowerContextPrototype ActivatingAnimOnly { get; set; }
    }

    public class ProceduralProfileSymbioteDrainPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralUsePowerContextPrototype SymbiotePower1 { get; set; }
        public ProceduralUsePowerContextPrototype SymbiotePower2 { get; set; }
        public ProceduralUsePowerContextPrototype SymbiotePower3 { get; set; }
    }

    public class ProceduralProfileOnslaughtPrototype : ProceduralProfileWithEnragePrototype
    {
        public ulong PlatformMarkerLeft { get; set; }
        public ulong PlatformMarkerCenter { get; set; }
        public ulong PlatformMarkerRight { get; set; }
        public ProceduralUsePowerContextPrototype PsionicBlastLeft { get; set; }
        public ProceduralUsePowerContextPrototype PsionicBlastCenter { get; set; }
        public ProceduralUsePowerContextPrototype PsionicBlastRight { get; set; }
        public ProceduralUsePowerContextPrototype SpikeDanceVFXOnly { get; set; }
        public ProceduralUsePowerContextPrototype PrisonBeamPowerCenter { get; set; }
        public ProceduralUsePowerContextPrototype PrisonPowerCenter { get; set; }
        public ProceduralUsePowerContextPrototype SpikeDanceSingleVFXOnly { get; set; }
        public ulong CallSentinelPower { get; set; }
        public ulong CallSentinelPowerVFXOnly { get; set; }
        public float SummonPowerThreshold1 { get; set; }
        public float SummonPowerThreshold2 { get; set; }
        public ProceduralUsePowerContextPrototype PrisonBeamPowerLeft { get; set; }
        public ProceduralUsePowerContextPrototype PrisonBeamPowerRight { get; set; }
        public ProceduralUsePowerContextPrototype PrisonPowerLeft { get; set; }
        public ProceduralUsePowerContextPrototype PrisonPowerRight { get; set; }
        public ulong PrisonKeyword { get; set; }
        public ulong CenterPlatformKeyword { get; set; }
        public ulong RightPlatformKeyword { get; set; }
        public ulong LeftPlatformKeyword { get; set; }
    }

    public class ProcProfileSpikeDanceControllerPrototype : ProceduralAIProfilePrototype
    {
        public ulong Onslaught { get; set; }
        public ulong SpikeDanceMob { get; set; }
        public int MaxSpikeDanceActivations { get; set; }
        public float SpikeDanceMobSearchRadius { get; set; }
    }

    public class ProceduralProfileSpikeDanceMobPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralUsePowerContextPrototype SpikeDanceMissile { get; set; }
    }

    public class ProceduralProfileNullifierPrototype : ProceduralProfileNoMoveNoSensePrototype
    {
        public ulong ShieldEngineerKeyword { get; set; }
        public ProceduralUsePowerContextPrototype BeamPower { get; set; }
        public ulong NullifierAntiShield { get; set; }
    }

    public class ProceduralProfileStrangeCauldronPrototype : ProceduralProfileNoMoveNoSensePrototype
    {
        public ulong KaeciliusPrototype { get; set; }
    }

    public class ProceduralProfileShieldEngineerPrototype : ProceduralProfileMissionAllyPrototype
    {
        public AgentPrototype PsychicNullifierTargets { get; set; }
        public ProceduralUsePowerContextPrototype ChargeNullifierPower { get; set; }
        public float NullifierSearchRadius { get; set; }
        public ulong NullifierAntiShield { get; set; }
    }

    public class ProcProfileNullifierAntiShieldPrototype : ProceduralProfileWithEnragePrototype
    {
        public AgentPrototype Nullifiers { get; set; }
        public ulong ShieldDamagePower { get; set; }
        public ulong ShieldEngineerSpawner { get; set; }
        public float SpawnerSearchRadius { get; set; }
    }

    public class ProceduralProfileMadameHydraPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public ProceduralFlankContextPrototype FlankTarget { get; set; }
        public ulong SummonHydraPower { get; set; }
        public ulong InvulnerablePower { get; set; }
        public ProceduralUsePowerContextPrototype TeleportPower { get; set; }
        public int SummonHydraMinCooldownMS { get; set; }
        public int SummonHydraMaxCooldownMS { get; set; }
    }

    public class ProceduralProfileStarktechSentinelPrototype : ProceduralProfileWithEnragePrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public ProceduralUsePowerContextPrototype SummonSentinels { get; set; }
        public float SummonPowerThreshold1 { get; set; }
        public float SummonPowerThreshold2 { get; set; }
    }

    public class ProceduralProfileKingpinPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public ProceduralUsePowerContextPrototype SummonElektra { get; set; }
        public ProceduralUsePowerContextPrototype SummonBullseye { get; set; }
        public float SummonElektraThreshold { get; set; }
        public float SummonBullseyeThreshold { get; set; }
    }

    public class ProceduralProfilePowerRestrictedPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public ProceduralFlankContextPrototype FlankTarget { get; set; }
        public bool IsRanged { get; set; }
        public ProceduralUsePowerContextPrototype RestrictedModeStartPower { get; set; }
        public ProceduralUsePowerContextPrototype RestrictedModeEndPower { get; set; }
        public ProceduralUsePowerContextPrototype[] RestrictedModeProceduralPowers { get; set; }
        public int RestrictedModeMinCooldownMS { get; set; }
        public int RestrictedModeMaxCooldownMS { get; set; }
        public int RestrictedModeTimerMS { get; set; }
        public bool NoMoveInRestrictedMode { get; set; }
    }

    public class ProceduralProfileUltronEMPPrototype : ProceduralProfileNoMoveNoSensePrototype
    {
        public ProceduralUsePowerContextPrototype EMPPower { get; set; }
    }

    public class ProcProfileQuicksilverTeamUpPrototype : ProceduralProfileTeamUpPrototype
    {
        public ProceduralUsePowerContextPrototype SpecialMovementPower { get; set; }
    }

    public class ProceduralProfileSkrullNickFuryPrototype : ProceduralProfileRangeFlankerPrototype
    {
        public ProceduralUsePowerContextPrototype OpenRocketCratePower { get; set; }
        public ProceduralUsePowerContextPrototype OpenMinigunCratePower { get; set; }
        public ProceduralUsePowerContextPrototype UseRocketPower { get; set; }
        public ProceduralUsePowerContextPrototype UseMinigunPower { get; set; }
        public MoveToContextPrototype MoveToCrate { get; set; }
        public ProceduralUsePowerContextPrototype CommandTurretPower { get; set; }
        public int CratePowerUseCount { get; set; }
        public ProceduralUsePowerContextPrototype DiscardWeaponPower { get; set; }
        public ulong CrateUsedState { get; set; }
    }

    public class ProceduralProfileNickFuryTurretPrototype : ProceduralProfileRotatingTurretWithTargetPrototype
    {
        public ProceduralUsePowerContextPrototype SpecialCommandPower { get; set; }
        public ulong SkrullNickFuryRef { get; set; }
    }

    public class ProceduralProfileKaeciliusPrototype : ProceduralProfileBasicRangePrototype
    {
        public ProceduralPowerWithSpecificTargetsPrototype[] HotspotSpawners { get; set; }
        public ProceduralThresholdPowerContextPrototype FalseDeathPower { get; set; }
        public ProceduralUsePowerContextPrototype HealFinalFormPower { get; set; }
        public ProceduralUsePowerContextPrototype DeathPreventerPower { get; set; }
        public ulong Cauldron { get; set; }
    }

    public class ProceduralProfileMeleeRevengePrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralUsePowerContextPrototype RevengePower { get; set; }
        public ulong RevengeSupport { get; set; }
    }

    public class ProceduralProfileRangedRevengePrototype : ProceduralProfileBasicRangePrototype
    {
        public ProceduralUsePowerContextPrototype RevengePower { get; set; }
        public ulong RevengeSupport { get; set; }
    }

    public class ProceduralProfileTaserTrapPrototype : ProceduralProfileWithTargetPrototype
    {
        public ulong TaserHotspot { get; set; }
    }

    public class ProceduralProfileVulturePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget { get; set; }
        public OrbitContextPrototype OrbitTarget { get; set; }
        public ProceduralUsePowerContextPrototype LungePower { get; set; }
        public int MaxLungeActivations { get; set; }
    }
}
