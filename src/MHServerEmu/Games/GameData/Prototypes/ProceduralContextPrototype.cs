using static MHServerEmu.Games.Generators.Navi.PathCache;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class ProceduralContextPrototype : Prototype
    {
        public ProceduralContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralContextPrototype), proto); }
    }

    public class ProceduralUsePowerContextSwitchTargetPrototype : Prototype
    {
        public SelectEntityContextPrototype SelectTarget;
        public bool SwitchPermanently;
        public bool UsePowerOnCurTargetIfSwitchFails;
        public ProceduralUsePowerContextSwitchTargetPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralUsePowerContextSwitchTargetPrototype), proto); }
    }

    public class ProceduralUsePowerContextPrototype : ProceduralContextPrototype
    {
        public int InitialCooldownMinMS;
        public int MaxCooldownMS;
        public int MinCooldownMS;
        public UsePowerContextPrototype PowerContext;
        public int PickWeight;
        public ProceduralUsePowerContextSwitchTargetPrototype TargetSwitch;
        public int InitialCooldownMaxMS;
        public ulong RestrictToDifficultyMin;
        public ulong RestrictToDifficultyMax;
        public ProceduralUsePowerContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralUsePowerContextPrototype), proto); }
    }

    public class ProceduralUseAffixPowerContextPrototype : ProceduralContextPrototype
    {
        public UseAffixPowerContextPrototype AffixContext;
        public int PickWeight;
        public ProceduralUseAffixPowerContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralUseAffixPowerContextPrototype), proto); }
    }

    public class ProceduralFlankContextPrototype : ProceduralContextPrototype
    {
        public int MaxFlankCooldownMS;
        public int MinFlankCooldownMS;
        public FlankContextPrototype FlankContext;
        public ProceduralFlankContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralFlankContextPrototype), proto); }
    }

    public class ProceduralInteractContextPrototype : ProceduralContextPrototype
    {
        public InteractContextPrototype InteractContext;
        public ProceduralInteractContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralInteractContextPrototype), proto); }
    }

    public class ProceduralFleeContextPrototype : ProceduralContextPrototype
    {
        public int MaxFleeCooldownMS;
        public int MinFleeCooldownMS;
        public FleeContextPrototype FleeContext;
        public ProceduralFleeContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralFleeContextPrototype), proto); }
    }

    public class ProceduralSyncAttackContextPrototype : Prototype
    {
        public ulong TargetEntity;
        public ulong TargetEntityPower;
        public ProceduralUsePowerContextPrototype LeaderPower;
        public ProceduralSyncAttackContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralSyncAttackContextPrototype), proto); }
    }

    public class ProceduralThresholdPowerContextPrototype : ProceduralUsePowerContextPrototype
    {
        public float HealthThreshold;
        public ProceduralThresholdPowerContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralThresholdPowerContextPrototype), proto); }
    }

    public class ProceduralPowerWithSpecificTargetsPrototype : Prototype
    {
        public float HealthThreshold;
        public ulong PowerToUse;
        public AgentPrototype Targets;
        public ProceduralPowerWithSpecificTargetsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralPowerWithSpecificTargetsPrototype), proto); }
    }

    public class ProceduralAIProfilePrototype : BrainPrototype
    {
        public ProceduralAIProfilePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralAIProfilePrototype), proto); }
    }

    public class ProceduralProfileWithTargetPrototype : ProceduralAIProfilePrototype
    {
        public SelectEntityContextPrototype SelectTarget;
        public ulong NoTargetOverrideProfile;
        public ProceduralProfileWithTargetPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileWithTargetPrototype), proto); }
    }

    public class ProceduralProfileWithAttackPrototype : ProceduralProfileWithTargetPrototype
    {
        public int AttackRateMaxMS;
        public int AttackRateMinMS;
        public ProceduralUsePowerContextPrototype[] GenericProceduralPowers;
        public ProceduralUseAffixPowerContextPrototype AffixSettings;
        public ProceduralProfileWithAttackPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileWithAttackPrototype), proto); }
    }

    public class ProceduralProfileEnticerPrototype : ProceduralAIProfilePrototype
    {
        public int CooldownMinMS;
        public int CooldownMaxMS;
        public int EnticeeEnticerCooldownMaxMS;
        public int EnticeeEnticerCooldownMinMS;
        public int EnticeeGlobalEnticerCDMaxMS;
        public int EnticeeGlobalEnticerCDMinMS;
        public int MaxSubscriptions;
        public int MaxSubscriptionsPerActivation;
        public float Radius;
        public AIEntityAttributePrototype[] EnticeeAttributes;
        public ulong EnticedBehavior;
        public ProceduralProfileEnticerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileEnticerPrototype), proto); }
    }

    public class ProceduralProfileEnticedBehaviorPrototype : ProceduralAIProfilePrototype
    {
        public FlankContextPrototype FlankToEnticer;
        public MoveToContextPrototype MoveToEnticer;
        public ulong DynamicBehavior;
        public bool OrientToEnticerOrientation;
        public ProceduralProfileEnticedBehaviorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileEnticedBehaviorPrototype), proto); }
    }

    public class ProceduralProfileSenseOnlyPrototype : ProceduralAIProfilePrototype
    {
        public AIEntityAttributePrototype[] AttributeList;
        public ulong AllianceOverride;
        public ProceduralProfileSenseOnlyPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileSenseOnlyPrototype), proto); }
    }

    public class ProceduralProfileInteractEnticerOverridePrototype : ProceduralAIProfilePrototype
    {
        public InteractContextPrototype Interact;
        public ProceduralProfileInteractEnticerOverridePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileInteractEnticerOverridePrototype), proto); }
    }

    public class ProceduralProfileUsePowerEnticerOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public UsePowerContextPrototype Power;
        public new SelectEntityContextPrototype SelectTarget;
        public ProceduralProfileUsePowerEnticerOverridePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileUsePowerEnticerOverridePrototype), proto); }
    }

    public class ProceduralProfileFearOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public FleeContextPrototype FleeFromTarget;
        public WanderContextPrototype WanderIfNoTarget;
        public ProceduralProfileFearOverridePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileFearOverridePrototype), proto); }
    }

    public class ProceduralProfileLeashOverridePrototype : ProceduralAIProfilePrototype
    {
        public ulong LeashReturnHeal;
        public ulong LeashReturnImmunity;
        public MoveToContextPrototype MoveToSpawn;
        public TeleportContextPrototype TeleportToSpawn;
        public ulong LeashReturnTeleport;
        public ulong LeashReturnInvulnerability;
        public ProceduralProfileLeashOverridePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileLeashOverridePrototype), proto); }
    }

    public class ProceduralProfileRunToExitAndDespawnOverridePrototype : ProceduralAIProfilePrototype
    {
        public MoveToContextPrototype RunToExit;
        public int NumberOfWandersBeforeDestroy;
        public DelayContextPrototype DelayBeforeRunToExit;
        public SelectEntityContextPrototype SelectPortalToExitFrom;
        public DelayContextPrototype DelayBeforeDestroyOnMoveExitFail;
        public bool VanishesIfMoveToExitFails;
        public ProceduralProfileRunToExitAndDespawnOverridePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileRunToExitAndDespawnOverridePrototype), proto); }
    }

    public class ProceduralProfileRunToTargetAndDespawnOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public ulong Invulnerability;
        public int NumberOfWandersBeforeDestroy;
        public MoveToContextPrototype RunToTarget;
        public WanderContextPrototype WanderIfMoveFails;
        public ProceduralProfileRunToTargetAndDespawnOverridePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileRunToTargetAndDespawnOverridePrototype), proto); }
    }

    public class ProceduralProfileDefaultActiveOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public DelayContextPrototype DelayAfterWander;
        public WanderContextPrototype Wander;
        public WanderContextPrototype WanderInPlace;
        public ProceduralProfileDefaultActiveOverridePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileDefaultActiveOverridePrototype), proto); }
    }

    public class ProceduralProfileFleeOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public FleeContextPrototype FleeFromTarget;
        public ProceduralProfileFleeOverridePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileFleeOverridePrototype), proto); }
    }

    public class ProceduralProfileOrbPrototype : ProceduralProfileWithTargetPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public int InitialMoveToDelayMS;
        public StateChangePrototype InvalidTargetState;
        public float OrbRadius;
        public ulong EffectPower;
        public bool AcceptsAggroRangeBonus;
        public int ShrinkageDelayMS;
        public int ShrinkageDurationMS;
        public float ShrinkageMinScale;
        public bool DestroyOrbOnUnSimOrTargetLoss;
        public ProceduralProfileOrbPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileOrbPrototype), proto); }
    }

    public class ProceduralProfileStationaryTurretPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralProfileStationaryTurretPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileStationaryTurretPrototype), proto); }
    }

    public class ProceduralProfileRotatingTurretPrototype : ProceduralAIProfilePrototype
    {
        public UsePowerContextPrototype Power;
        public RotateContextPrototype Rotate;
        public ProceduralProfileRotatingTurretPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileRotatingTurretPrototype), proto); }
    }

    public class ProceduralProfileRotatingTurretWithTargetPrototype : ProceduralProfileWithAttackPrototype
    {
        public RotateContextPrototype Rotate;
        public ProceduralProfileRotatingTurretWithTargetPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileRotatingTurretWithTargetPrototype), proto); }
    }

    public class ProceduralProfileBasicMeleePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public ProceduralUsePowerContextPrototype PrimaryPower;
        public ProceduralProfileBasicMeleePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileBasicMeleePrototype), proto); }
    }

    public class ProceduralProfileBasicMelee2PowerPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public ProceduralUsePowerContextPrototype Power1;
        public ProceduralUsePowerContextPrototype Power2;
        public ProceduralProfileBasicMelee2PowerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileBasicMelee2PowerPrototype), proto); }
    }

    public class ProceduralProfileBasicRangePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public ProceduralProfileBasicRangePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileBasicRangePrototype), proto); }
    }

    public class ProceduralProfileAlternateRange2Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public ProceduralFlankContextPrototype FlankTarget;
        public ProceduralUsePowerContextPrototype Power1;
        public ProceduralUsePowerContextPrototype Power2;
        public ProceduralUsePowerContextPrototype PowerSwap;
        public ProceduralProfileAlternateRange2Prototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileAlternateRange2Prototype), proto); }
    }

    public class ProceduralProfileMultishotRangePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public ProceduralUsePowerContextPrototype MultishotPower;
        public int NumShots;
        public bool RetargetPerShot;
        public ProceduralProfileMultishotRangePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileMultishotRangePrototype), proto); }
    }

    public class ProceduralProfileMultishotFlankerPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralFlankContextPrototype FlankTarget;
        public MoveToContextPrototype MoveToTarget;
        public ProceduralUsePowerContextPrototype MultishotPower;
        public int NumShots;
        public bool RetargetPerShot;
        public ProceduralProfileMultishotFlankerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileMultishotFlankerPrototype), proto); }
    }

    public class ProceduralProfileMultishotHiderPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralUsePowerContextPrototype HidePower;
        public ProceduralUsePowerContextPrototype MultishotPower;
        public int NumShots;
        public bool RetargetPerShot;
        public ProceduralProfileMultishotHiderPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileMultishotHiderPrototype), proto); }
    }

    public class ProceduralProfileMeleeSpeedByDistancePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public ProceduralUsePowerContextPrototype PrimaryPower;
        public UsePowerContextPrototype ExtraSpeedPower;
        public UsePowerContextPrototype SpeedRemovalPower;
        public float DistanceFromTargetForSpeedBonus;
        public ProceduralProfileMeleeSpeedByDistancePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileMeleeSpeedByDistancePrototype), proto); }
    }

    public class ProceduralProfileRangeFlankerPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralFlankContextPrototype FlankTarget;
        public MoveToContextPrototype MoveToTarget;
        public ProceduralUsePowerContextPrototype PrimaryPower;
        public ProceduralProfileRangeFlankerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileRangeFlankerPrototype), proto); }
    }

    public class ProceduralProfileSkirmisherPrototype : ProceduralProfileWithAttackPrototype
    {
        public WanderContextPrototype SkirmishMovement;
        public MoveToContextPrototype MoveToTarget;
        public ProceduralUsePowerContextPrototype PrimaryPower;
        public float MoveToSpeedBonus;
        public ProceduralProfileSkirmisherPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileSkirmisherPrototype), proto); }
    }

    public class ProceduralProfileRangedWithMeleePriority2PowerPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public ProceduralUsePowerContextPrototype MeleePower;
        public ProceduralUsePowerContextPrototype RangedPower;
        public float MaxDistToMoveIntoMelee;
        public MoveToContextPrototype MoveIntoMeleeRange;
        public ProceduralProfileRangedWithMeleePriority2PowerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileRangedWithMeleePriority2PowerPrototype), proto); }
    }

    public class ProfMeleePwrSpecialAtHealthPctPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public float SpecialAtHealthChunkPct;
        public UsePowerContextPrototype SpecialPowerAtHealthChunkPct;
        public ProfMeleePwrSpecialAtHealthPctPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProfMeleePwrSpecialAtHealthPctPrototype), proto); }
    }

    public class ProceduralProfileShockerPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public MoveToContextPrototype MoveIntoMeleeRange;
        public ProceduralUsePowerContextPrototype MeleePower;
        public ProceduralUsePowerContextPrototype SpecialPower;
        public ulong SpecialSummonPower;
        public float MaxDistToMoveIntoMelee;
        public int SpecialPowerNumSummons;
        public float SpecialPowerMaxRadius;
        public float SpecialPowerMinRadius;
        public ProceduralProfileShockerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileShockerPrototype), proto); }
    }

    public class ProceduralProfileLadyDeathstrikePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public ProceduralUsePowerContextPrototype HealingPower;
        public ProceduralUsePowerContextPrototype SpecialPower;
        public SelectEntityContextPrototype SpecialPowerSelectTarget;
        public int SpecialPowerChangeTgtIntervalMS;
        public ProceduralProfileLadyDeathstrikePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileLadyDeathstrikePrototype), proto); }
    }

    public class ProceduralProfileFastballSpecialWolverinePrototype : ProceduralProfileWithTargetPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public WanderContextPrototype MoveToNoTarget;
        public UsePowerContextPrototype Power;
        public int PowerChangeTargetIntervalMS;
        public ProceduralProfileFastballSpecialWolverinePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileFastballSpecialWolverinePrototype), proto); }
    }

    public class ProceduralProfileSeekingMissilePrototype : ProceduralProfileWithTargetPrototype
    {
        public SelectEntityContextPrototype SecondaryTargetSelection;
        public int SeekDelayMS;
        public float SeekDelaySpeed;
        public ProceduralProfileSeekingMissilePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileSeekingMissilePrototype), proto); }
    }

    public class ProceduralProfileSeekingMissileUniqueTargetPrototype : ProceduralProfileWithTargetPrototype
    {
        public ProceduralProfileSeekingMissileUniqueTargetPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileSeekingMissileUniqueTargetPrototype), proto); }
    }

    public class ProceduralProfileNoMoveDefaultSensoryPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralProfileNoMoveDefaultSensoryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileNoMoveDefaultSensoryPrototype), proto); }
    }

    public class ProceduralProfileNoMoveSimplifiedSensoryPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralProfileNoMoveSimplifiedSensoryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileNoMoveSimplifiedSensoryPrototype), proto); }
    }

    public class ProceduralProfileNoMoveSimplifiedAllySensoryPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralProfileNoMoveSimplifiedAllySensoryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileNoMoveSimplifiedAllySensoryPrototype), proto); }
    }

    public class ProfKillSelfAfterOnePowerNoMovePrototype : ProceduralProfileWithAttackPrototype
    {
        public ProfKillSelfAfterOnePowerNoMovePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProfKillSelfAfterOnePowerNoMovePrototype), proto); }
    }

    public class ProceduralProfileNoMoveNoSensePrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralProfileNoMoveNoSensePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileNoMoveNoSensePrototype), proto); }
    }

    public class ProceduralProfileMoveToUniqueTargetNoPowerPrototype : ProceduralProfileWithTargetPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public ProceduralProfileMoveToUniqueTargetNoPowerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileMoveToUniqueTargetNoPowerPrototype), proto); }
    }

    public class ProceduralProfileWanderNoPowerPrototype : ProceduralAIProfilePrototype
    {
        public WanderContextPrototype WanderMovement;
        public ProceduralProfileWanderNoPowerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileWanderNoPowerPrototype), proto); }
    }

    public class ProceduralProfileBasicWanderPrototype : ProceduralProfileWithAttackPrototype
    {
        public WanderContextPrototype WanderMovement;
        public ProceduralProfileBasicWanderPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileBasicWanderPrototype), proto); }
    }

    public class ProceduralProfilePvPMeleePrototype : ProceduralProfileWithAttackPrototype
    {
        public float AggroRadius;
        public float AggroDropRadius;
        public float AggroDropByLOSChance;
        public long AttentionSpanMS;
        public ulong PrimaryPower;
        public int PathGroup;
        public PathMethod PathMethod;
        public float PathThreshold;
        public ProceduralProfilePvPMeleePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfilePvPMeleePrototype), proto); }
    }

    public class ProceduralProfilePvPTowerPrototype : ProceduralProfileWithAttackPrototype
    {
        public SelectEntityContextPrototype SelectTarget2;
        public SelectEntityContextPrototype SelectTarget3;
        public SelectEntityContextPrototype SelectTarget4;
        public ProceduralProfilePvPTowerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfilePvPTowerPrototype), proto); }
    }

    public class ProceduralProfileMeleeDropWeaponPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public ProceduralUsePowerContextPrototype PowerMeleeWithWeapon;
        public ProceduralUsePowerContextPrototype PowerMeleeNoWeapon;
        public ProceduralUsePowerContextPrototype PowerDropWeapon;
        public ProceduralUsePowerContextPrototype PowerPickupWeapon;
        public SelectEntityContextPrototype SelectWeaponAsTarget;
        public int DropPickupTimeMax;
        public int DropPickupTimeMin;
        public ProceduralProfileMeleeDropWeaponPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileMeleeDropWeaponPrototype), proto); }
    }

    public class ProceduralProfileMeleeAllyDeathFleePrototype : ProceduralProfileWithAttackPrototype
    {
        public FleeContextPrototype FleeFromTargetOnAllyDeath;
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public ProceduralProfileMeleeAllyDeathFleePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileMeleeAllyDeathFleePrototype), proto); }
    }

    public class ProceduralProfileRangedFlankerAllyDeathFleePrototype : ProceduralProfileWithAttackPrototype
    {
        public FleeContextPrototype FleeFromTargetOnAllyDeath;
        public MoveToContextPrototype MoveToTarget;
        public ProceduralFlankContextPrototype FlankTarget;
        public ProceduralProfileRangedFlankerAllyDeathFleePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileRangedFlankerAllyDeathFleePrototype), proto); }
    }

    public class ProceduralProfileMagnetoPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public ProceduralFlankContextPrototype FlankTarget;
        public ProceduralUsePowerContextPrototype ChasePower;
        public ProceduralProfileMagnetoPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileMagnetoPrototype), proto); }
    }

    public class ProcProfMrSinisterPrototype : ProceduralProfileWithAttackPrototype
    {
        public float CloneCylHealthPctThreshWave1;
        public float CloneCylHealthPctThreshWave2;
        public float CloneCylHealthPctThreshWave3;
        public UsePowerContextPrototype CloneCylSummonFXPower;
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public TriggerSpawnersContextPrototype TriggerCylinderSpawnerAction;
        public ProcProfMrSinisterPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProcProfMrSinisterPrototype), proto); }
    }

    public class ProcProfMrSinisterCloneCylinderPrototype : ProceduralProfileWithAttackPrototype
    {
        public UsePowerContextPrototype CylinderOpenPower;
        public DespawnContextPrototype DespawnAction;
        public int PreOpenDelayMS;
        public int PostOpenDelayMS;
        public ProcProfMrSinisterCloneCylinderPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProcProfMrSinisterCloneCylinderPrototype), proto); }
    }

    public class ProceduralProfileBlobPrototype : ProceduralProfileBasicRangePrototype
    {
        public ProceduralUsePowerContextPrototype SummonToadPower;
        public ulong ToadPrototype;
        public ProceduralProfileBlobPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileBlobPrototype), proto); }
    }

    public class ProceduralProfileRangedHotspotDropperPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public ProceduralUsePowerContextPrototype RangedPower;
        public ProceduralUsePowerContextPrototype HotspotPower;
        public WanderContextPrototype HotspotDroppingMovement;
        public ProceduralProfileRangedHotspotDropperPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileRangedHotspotDropperPrototype), proto); }
    }

    public class ProceduralProfileTeamUpPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public ProceduralFlankContextPrototype FlankTarget;
        public bool IsRanged;
        public MoveToContextPrototype MoveToMaster;
        public TeleportContextPrototype TeleportToMasterIfTooFarAway;
        public int MaxDistToMasterBeforeTeleport;
        public ProceduralUsePowerContextPrototype[] TeamUpPowerProgressionPowers;
        public ProceduralProfileTeamUpPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileTeamUpPrototype), proto); }
    }

    public class ProceduralProfilePetPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype PetFollow;
        public TeleportContextPrototype TeleportToMasterIfTooFarAway;
        public int MaxDistToMasterBeforeTeleport;
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public ProceduralFlankContextPrototype FlankTarget;
        public bool IsRanged;
        public ProceduralProfilePetPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfilePetPrototype), proto); }
    }

    public class ProceduralProfilePetFidgetPrototype : ProceduralProfilePetPrototype
    {
        public ProceduralUsePowerContextPrototype Fidget;
        public ProceduralProfilePetFidgetPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfilePetFidgetPrototype), proto); }
    }

    public class ProceduralProfileSquirrelGirlSquirrelPrototype : ProceduralProfilePetPrototype
    {
        public ProceduralFlankContextPrototype FlankMaster;
        public float DeadzoneAroundFlankTarget;
        public ProceduralProfileSquirrelGirlSquirrelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileSquirrelGirlSquirrelPrototype), proto); }
    }

    public class ProceduralProfileRollingGrenadesPrototype : ProceduralAIProfilePrototype
    {
        public int MaxSpeedDegreeUpdateIntervalMS;
        public int MinSpeedDegreeUpdateIntervalMS;
        public int MovementSpeedVariance;
        public int RandomDegreeFromForward;
        public ProceduralProfileRollingGrenadesPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileRollingGrenadesPrototype), proto); }
    }

    public class ProceduralProfileSquirrelTriplePrototype : ProceduralAIProfilePrototype
    {
        public int JumpDistanceMax;
        public int JumpDistanceMin;
        public DelayContextPrototype PauseSettings;
        public int RandomDirChangeDegrees;
        public ProceduralProfileSquirrelTriplePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileSquirrelTriplePrototype), proto); }
    }

    public class ProceduralProfileFrozenOrbPrototype : ProceduralAIProfilePrototype
    {
        public int ShardBurstsPerSecond;
        public int ShardsPerBurst;
        public int ShardRotationSpeed;
        public ulong ShardPower;
        public ProceduralProfileFrozenOrbPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileFrozenOrbPrototype), proto); }
    }

    public class ProceduralProfileDrDoomPhase1Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public ulong DeathStun;
        public ProceduralUsePowerContextPrototype SummonTurretPowerAnimOnly;
        public UsePowerContextPrototype SummonDoombotBlockades;
        public UsePowerContextPrototype SummonDoombotInfernos;
        public UsePowerContextPrototype SummonDoombotFlyers;
        public ProceduralUsePowerContextPrototype SummonDoombotAnimOnly;
        public ulong SummonDoombotBlockadesCurve;
        public ulong SummonDoombotInfernosCurve;
        public ulong SummonDoombotFlyersCurve;
        public int SummonDoombotWaveIntervalMS;
        public ProceduralUsePowerContextPrototype SummonOrbSpawners;
        public TriggerSpawnersContextPrototype SpawnTurrets;
        public TriggerSpawnersContextPrototype SpawnDrDoomPhase2;
        public TriggerSpawnersContextPrototype DestroyTurretsOnDeath;
        public ProceduralProfileDrDoomPhase1Prototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileDrDoomPhase1Prototype), proto); }
    }

    public class ProceduralProfileDrDoomPhase2Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public ulong DeathStun;
        public ulong StarryExpanseAnimOnly;
        public TriggerSpawnersContextPrototype SpawnDrDoomPhase3;
        public TriggerSpawnersContextPrototype SpawnStarryExpanseAreas;
        public ProceduralProfileDrDoomPhase2Prototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileDrDoomPhase2Prototype), proto); }
    }

    public class ProceduralProfileDrDoomPhase3Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public RotateContextPrototype RapidFireRotate;
        public ProceduralUsePowerContextPrototype RapidFirePower;
        public ulong StarryExpanseAnimOnly;
        public ProceduralUsePowerContextPrototype CosmicSummonsAnimOnly;
        public UsePowerContextPrototype CosmicSummonsPower;
        public ulong[] CosmicSummonEntities;
        public ulong CosmicSummonsNumEntities;
        public TriggerSpawnersContextPrototype SpawnStarryExpanseAreas;
        public ProceduralProfileDrDoomPhase3Prototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileDrDoomPhase3Prototype), proto); }
    }

    public class ProceduralProfileDrDoomPhase1OrbSpawnerPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveTo;
        public ProceduralFlankContextPrototype Flank;
        public ProceduralProfileDrDoomPhase1OrbSpawnerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileDrDoomPhase1OrbSpawnerPrototype), proto); }
    }

    public class ProceduralProfileMODOKPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralUsePowerContextPrototype TeleportToEntityPower;
        public SelectEntityContextPrototype SelectTeleportTarget;
        public ProceduralUsePowerContextPrototype[] SummonProceduralPowers;
        public ProceduralProfileMODOKPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileMODOKPrototype), proto); }
    }

    public class ProceduralProfileSauronPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public ProceduralUsePowerContextPrototype MovementPower;
        public ProceduralUsePowerContextPrototype LowHealthPower;
        public float LowHealthPowerThresholdPct;
        public ProceduralProfileSauronPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileSauronPrototype), proto); }
    }

    public class ProceduralProfileMandarinPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public ProceduralFlankContextPrototype FlankTarget;
        public SelectEntityContextPrototype SequencePowerSelectTarget;
        public ProceduralUsePowerContextPrototype[] SequencePowers;
        public ProceduralProfileMandarinPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileMandarinPrototype), proto); }
    }

    public class ProceduralProfileSabretoothPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public ProceduralUsePowerContextPrototype MovementPower;
        public SelectEntityContextPrototype MovementPowerSelectTarget;
        public ProceduralUsePowerContextPrototype LowHealthPower;
        public float LowHealthPowerThresholdPct;
        public ProceduralProfileSabretoothPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileSabretoothPrototype), proto); }
    }

    public class ProceduralProfileMeleePowerOnHitPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public ProceduralUsePowerContextPrototype PowerOnHit;
        public ProceduralProfileMeleePowerOnHitPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileMeleePowerOnHitPrototype), proto); }
    }

    public class ProceduralProfileGrimReaperPrototype : ProfMeleePwrSpecialAtHealthPctPrototype
    {
        public ProceduralUsePowerContextPrototype TripleShotPower;
        public ProceduralUsePowerContextPrototype SpecialPower;
        public SelectEntityContextPrototype SpecialSelectTarget;
        public int SpecialPowerChangeTgtIntervalMS;
        public ProceduralProfileGrimReaperPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileGrimReaperPrototype), proto); }
    }

    public class ProceduralProfileMoleManPrototype : ProceduralProfileBasicRangePrototype
    {
        public TriggerSpawnersContextPrototype[] GigantoSpawners;
        public ProceduralUsePowerContextPrototype MoloidInvasionPower;
        public TriggerSpawnersContextPrototype MoloidInvasionSpawner;
        public ProceduralUsePowerContextPrototype SummonGigantoAnimPower;
        public ProceduralProfileMoleManPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileMoleManPrototype), proto); }
    }

    public class ProceduralProfileVenomPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralFlankContextPrototype FlankTarget;
        public MoveToContextPrototype MoveToTarget;
        public UsePowerContextPrototype VenomMad;
        public float VenomMadThreshold1;
        public float VenomMadThreshold2;
        public ProceduralProfileVenomPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileVenomPrototype), proto); }
    }

    public class ProceduralProfileVanityPetPrototype : ProceduralProfileWithTargetPrototype
    {
        public MoveToContextPrototype PetFollow;
        public TeleportContextPrototype TeleportToMasterIfTooFarAway;
        public int MinTimerWhileNotMovingFidgetMS;
        public int MaxTimerWhileNotMovingFidgetMS;
        public float MaxDistToMasterBeforeTeleport;
        public ProceduralProfileVanityPetPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileVanityPetPrototype), proto); }
    }

    public class ProceduralProfileDoopPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralFleeContextPrototype Flee;
        public ProceduralUsePowerContextPrototype DisappearPower;
        public int LifeTimeMinMS;
        public int LifeTimeMaxMS;
        public ProceduralProfileDoopPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileDoopPrototype), proto); }
    }

    public class ProceduralProfileGorgonPrototype : ProceduralProfileWithAttackPrototype
    {
        public RotateContextPrototype RotateInStoneGaze;
        public ProceduralUsePowerContextPrototype StoneGaze;
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public ProceduralProfileGorgonPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileGorgonPrototype), proto); }
    }

    public class ProceduralProfileBotAIPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public SelectEntityContextPrototype SelectTargetItem;
        public WanderContextPrototype WanderMovement;
        public ProceduralUsePowerContextPrototype[] SlottedAbilities;
        public ProceduralProfileBotAIPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileBotAIPrototype), proto); }
    }

    public class ProceduralProfileBullseyePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public ProceduralFlankContextPrototype FlankTarget;
        public ProceduralUsePowerContextPrototype MarkForDeath;
        public ProceduralProfileBullseyePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileBullseyePrototype), proto); }
    }

    public class ProceduralProfileRhinoPrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralProfileRhinoPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileRhinoPrototype), proto); }
    }

    public class ProceduralProfileBlackCatPrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralUsePowerContextPrototype TumblePower;
        public ProceduralUsePowerContextPrototype TumbleComboPower;
        public SelectEntityContextPrototype SelectEntityForTumbleCombo;
        public ProceduralProfileBlackCatPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileBlackCatPrototype), proto); }
    }

    public class ProceduralProfileControlledMobOverridePrototype : ProceduralProfileWithTargetPrototype
    {
        public MoveToContextPrototype ControlFollow;
        public TeleportContextPrototype TeleportToMasterIfTooFarAway;
        public float MaxDistToMasterBeforeTeleport;
        public int MaxDistToMasterBeforeFollow;
        public ProceduralProfileControlledMobOverridePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileControlledMobOverridePrototype), proto); }
    }

    public class ProceduralProfileLivingLaserPrototype : ProceduralProfileBasicRangePrototype
    {
        public ProceduralUsePowerContextPrototype SweepingBeamPowerClock;
        public ProceduralUsePowerContextPrototype SweepingBeamPowerCounterClock;
        public RotateContextPrototype SweepingBeamClock;
        public RotateContextPrototype SweepingBeamCounterClock;
        public ProceduralProfileLivingLaserPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileLivingLaserPrototype), proto); }
    }

    public class ProceduralProfileMeleeFlockerPrototype : ProceduralProfileWithAttackPrototype
    {
        public FlockContextPrototype FlockContext;
        public ulong FleeOnAllyDeathOverride;
        public ProceduralProfileMeleeFlockerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileMeleeFlockerPrototype), proto); }
    }

    public class ProceduralProfileLizardBossPrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralUsePowerContextPrototype LizardSwarmPower;
        public ProceduralProfileLizardBossPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileLizardBossPrototype), proto); }
    }

    public class ProceduralProfilePetDirectedPrototype : ProceduralProfilePetPrototype
    {
        public ProceduralUsePowerContextPrototype[] DirectedPowers;
        public ProceduralProfilePetDirectedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfilePetDirectedPrototype), proto); }
    }

    public class ProceduralProfileLokiPhase1Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public ProceduralFlankContextPrototype FlankTarget;
        public ProceduralProfileLokiPhase1Prototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileLokiPhase1Prototype), proto); }
    }

    public class ProceduralProfileLokiPhase2Prototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public ProceduralUsePowerContextPrototype InverseRings;
        public ulong InverseRingsTargetedVFXOnly;
        public ulong LokiBossSafeZoneKeyword;
        public ulong InverseRingsVFXRemoval;
        public ProceduralProfileLokiPhase2Prototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileLokiPhase2Prototype), proto); }
    }

    public class ProceduralProfileDrStrangeProjectionPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralUsePowerContextPrototype[] ProjectionPowers;
        public ProceduralFlankContextPrototype FlankMaster;
        public float DeadzoneAroundFlankTarget;
        public int FlankToMasterDelayMS;
        public TeleportContextPrototype TeleportToMasterIfTooFarAway;
        public int MaxDistToMasterBeforeTeleport;
        public ProceduralProfileDrStrangeProjectionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileDrStrangeProjectionPrototype), proto); }
    }

    public class ProceduralProfileEyeOfAgamottoPrototype : ProceduralProfileStationaryTurretPrototype
    {
        public RotateContextPrototype IdleRotation;
        public ProceduralProfileEyeOfAgamottoPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileEyeOfAgamottoPrototype), proto); }
    }

    public class MistressOfMagmaTeleportDestPrototype : Prototype
    {
        public SelectEntityContextPrototype DestinationSelector;
        public ulong ImmunityBoost;
        public MistressOfMagmaTeleportDestPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MistressOfMagmaTeleportDestPrototype), proto); }
    }

    public class ProceduralProfileSpikedBallPrototype : ProceduralProfileWithTargetPrototype
    {
        public float MoveToSummonerDistance;
        public float IdleDistanceFromSummoner;
        public RotateContextPrototype Rotate;
        public int SeekDelayMS;
        public float Acceleration;
        public MoveToContextPrototype MoveToTarget;
        public WanderContextPrototype Wander;
        public TeleportContextPrototype TeleportToMasterIfTooFarAway;
        public int MaxDistToMasterBeforeTeleport;
        public OrbitContextPrototype OrbitTarget;
        public ProceduralProfileSpikedBallPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileSpikedBallPrototype), proto); }
    }

    public class ProceduralProfileWithEnragePrototype : ProceduralProfileWithAttackPrototype
    {
        public int EnrageTimerInMinutes;
        public ProceduralUsePowerContextPrototype EnragePower;
        public float EnrageTimerAvatarSearchRadius;
        public ProceduralUsePowerContextPrototype[] PostEnragePowers;
        public ProceduralProfileWithEnragePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileWithEnragePrototype), proto); }
    }

    public class ProceduralProfileSyncAttackPrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralSyncAttackContextPrototype[] SyncAttacks;
        public ProceduralProfileSyncAttackPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileSyncAttackPrototype), proto); }
    }

    public class ProceduralProfileBrimstonePrototype : ProceduralProfileWithEnragePrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public MoveToContextPrototype MoveIntoMeleeRange;
        public ProceduralUsePowerContextPrototype MeleePower;
        public ulong HellfireProtoRef;
        public ProceduralProfileBrimstonePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileBrimstonePrototype), proto); }
    }

    public class ProceduralProfileSlagPrototype : ProceduralProfileWithEnragePrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public ProceduralUsePowerContextPrototype PrimaryPower;
        public ProceduralProfileSlagPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileSlagPrototype), proto); }
    }

    public class ProceduralProfileMonolithPrototype : ProceduralProfileWithEnragePrototype
    {
        public ulong ObeliskKeyword;
        public ulong[] ObeliskDamageMonolithPowers;
        public ulong DisableShield;
        public ProceduralProfileMonolithPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileMonolithPrototype), proto); }
    }

    public class ProceduralProfileHellfirePrototype : ProceduralProfileWithEnragePrototype
    {
        public ProceduralFlankContextPrototype FlankTarget;
        public MoveToContextPrototype MoveToTarget;
        public ProceduralUsePowerContextPrototype PrimaryPower;
        public ulong BrimstoneProtoRef;
        public ProceduralUsePowerContextPrototype SpecialPower;
        public ulong SpecialSummonPower;
        public int SpecialPowerNumSummons;
        public float SpecialPowerMaxRadius;
        public float SpecialPowerMinRadius;
        public ProceduralProfileHellfirePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileHellfirePrototype), proto); }
    }

    public class ProceduralProfileMistressOfMagmaPrototype : ProceduralProfileWithEnragePrototype
    {
        public ProceduralFlankContextPrototype FlankTarget;
        public MoveToContextPrototype MoveToTarget;
        public ProceduralUsePowerContextPrototype BombDancePower;
        public ProceduralProfileMistressOfMagmaPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileMistressOfMagmaPrototype), proto); }
    }

    public class ProceduralProfileSurturPrototype : ProceduralProfileWithEnragePrototype
    {
        public ulong FirePillarPower;
        public int FirePillarMinCooldownMS;
        public int FirePillarMaxCooldownMS;
        public int FirePillarPowerMaxTargets;
        public ulong PowerUnlockBrimstone;
        public ulong PowerUnlockHellfire;
        public ulong PowerUnlockMistress;
        public ulong PowerUnlockMonolith;
        public ulong PowerUnlockSlag;
        public ulong MiniBossBrimstone;
        public ulong MiniBossHellfire;
        public ulong MiniBossMistress;
        public ulong MiniBossMonolith;
        public ulong MiniBossSlag;
        public ProceduralProfileSurturPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileSurturPrototype), proto); }
    }

    public class ProceduralProfileSurturPortalPrototype : ProceduralProfileNoMoveNoSensePrototype
    {
        public ProceduralProfileSurturPortalPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileSurturPortalPrototype), proto); }
    }

    public class ProceduralProfileObeliskHealerPrototype : ProceduralProfileBasicMeleePrototype
    {
        public ulong[] ObeliskTargets;
        public ProceduralProfileObeliskHealerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileObeliskHealerPrototype), proto); }
    }

    public class ProceduralProfileObeliskPrototype : ProceduralProfileNoMoveDefaultSensoryPrototype
    {
        public ulong DeadEntityForDetonateIslandPower;
        public ulong DetonateIslandPower;
        public ulong FullyHealedPower;
        public ProceduralProfileObeliskPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileObeliskPrototype), proto); }
    }

    public class ProceduralProfileFireGiantChaserPrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralUsePowerContextPrototype MarkTargetPower;
        public ulong MarkTargetVFXRemoval;
        public ProceduralProfileFireGiantChaserPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileFireGiantChaserPrototype), proto); }
    }

    public class ProceduralProfileMissionAllyPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public ProceduralFlankContextPrototype FlankTarget;
        public MoveToContextPrototype MoveToAvatarAlly;
        public TeleportContextPrototype TeleportToAvatarAllyIfTooFarAway;
        public int MaxDistToAvatarAllyBeforeTele;
        public bool IsRanged;
        public float AvatarAllySearchRadius;
        public ProceduralProfileMissionAllyPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileMissionAllyPrototype), proto); }
    }

    public class ProceduralProfileLOSRangedPrototype : ProceduralProfileBasicRangePrototype
    {
        public ProceduralUsePowerContextPrototype LOSChannelPower;
        public ProceduralProfileLOSRangedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileLOSRangedPrototype), proto); }
    }

    public class ProceduralProfileRedSkullOneShotPrototype : ProceduralProfileWithAttackPrototype
    {
        public ulong[] HulkBustersToActivate;
        public ProceduralUsePowerContextPrototype ActivateHulkBusterAnimOnly;
        public float HulkBusterHealthThreshold1;
        public float HulkBusterHealthThreshold2;
        public float HulkBusterHealthThreshold3;
        public float HulkBusterHealthThreshold4;
        public ulong WeaponsCrate;
        public ProceduralUsePowerContextPrototype[] WeaponsCratesAnimOnlyPowers;
        public MoveToContextPrototype MoveToWeaponsCrate;
        public ulong WeaponCrate1UnlockPower;
        public ulong WeaponCrate2UnlockPower;
        public ulong WeaponCrate3UnlockPower;
        public ulong WeaponCrate4UnlockPower;
        public MoveToContextPrototype MoveToTarget;
        public ProceduralFlankContextPrototype FlankTarget;
        public ProceduralProfileRedSkullOneShotPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileRedSkullOneShotPrototype), proto); }
    }

    public class ProceduralProfileHulkBusterOSPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public ulong RedSkullAxis;
        public ProceduralUsePowerContextPrototype ShieldRedSkull;
        public ProceduralUsePowerContextPrototype DeactivatedAnimOnly;
        public ProceduralUsePowerContextPrototype ActivatingAnimOnly;
        public ProceduralProfileHulkBusterOSPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileHulkBusterOSPrototype), proto); }
    }

    public class ProceduralProfileSymbioteDrainPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralUsePowerContextPrototype SymbiotePower1;
        public ProceduralUsePowerContextPrototype SymbiotePower2;
        public ProceduralUsePowerContextPrototype SymbiotePower3;
        public ProceduralProfileSymbioteDrainPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileSymbioteDrainPrototype), proto); }
    }

    public class ProceduralProfileOnslaughtPrototype : ProceduralProfileWithEnragePrototype
    {
        public ulong PlatformMarkerLeft;
        public ulong PlatformMarkerCenter;
        public ulong PlatformMarkerRight;
        public ProceduralUsePowerContextPrototype PsionicBlastLeft;
        public ProceduralUsePowerContextPrototype PsionicBlastCenter;
        public ProceduralUsePowerContextPrototype PsionicBlastRight;
        public ProceduralUsePowerContextPrototype SpikeDanceVFXOnly;
        public ProceduralUsePowerContextPrototype PrisonBeamPowerCenter;
        public ProceduralUsePowerContextPrototype PrisonPowerCenter;
        public ProceduralUsePowerContextPrototype SpikeDanceSingleVFXOnly;
        public ulong CallSentinelPower;
        public ulong CallSentinelPowerVFXOnly;
        public float SummonPowerThreshold1;
        public float SummonPowerThreshold2;
        public ProceduralUsePowerContextPrototype PrisonBeamPowerLeft;
        public ProceduralUsePowerContextPrototype PrisonBeamPowerRight;
        public ProceduralUsePowerContextPrototype PrisonPowerLeft;
        public ProceduralUsePowerContextPrototype PrisonPowerRight;
        public ulong PrisonKeyword;
        public ulong CenterPlatformKeyword;
        public ulong RightPlatformKeyword;
        public ulong LeftPlatformKeyword;
        public ProceduralProfileOnslaughtPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileOnslaughtPrototype), proto); }
    }

    public class ProcProfileSpikeDanceControllerPrototype : ProceduralAIProfilePrototype
    {
        public ulong Onslaught;
        public ulong SpikeDanceMob;
        public int MaxSpikeDanceActivations;
        public float SpikeDanceMobSearchRadius;
        public ProcProfileSpikeDanceControllerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProcProfileSpikeDanceControllerPrototype), proto); }
    }

    public class ProceduralProfileSpikeDanceMobPrototype : ProceduralProfileWithAttackPrototype
    {
        public ProceduralUsePowerContextPrototype SpikeDanceMissile;
        public ProceduralProfileSpikeDanceMobPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileSpikeDanceMobPrototype), proto); }
    }

    public class ProceduralProfileNullifierPrototype : ProceduralProfileNoMoveNoSensePrototype
    {
        public ulong ShieldEngineerKeyword;
        public ProceduralUsePowerContextPrototype BeamPower;
        public ulong NullifierAntiShield;
        public ProceduralProfileNullifierPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileNullifierPrototype), proto); }
    }

    public class ProceduralProfileStrangeCauldronPrototype : ProceduralProfileNoMoveNoSensePrototype
    {
        public ulong KaeciliusPrototype;
        public ProceduralProfileStrangeCauldronPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileStrangeCauldronPrototype), proto); }
    }

    public class ProceduralProfileShieldEngineerPrototype : ProceduralProfileMissionAllyPrototype
    {
        public AgentPrototype PsychicNullifierTargets;
        public ProceduralUsePowerContextPrototype ChargeNullifierPower;
        public float NullifierSearchRadius;
        public ulong NullifierAntiShield;
        public ProceduralProfileShieldEngineerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileShieldEngineerPrototype), proto); }
    }

    public class ProcProfileNullifierAntiShieldPrototype : ProceduralProfileWithEnragePrototype
    {
        public AgentPrototype Nullifiers;
        public ulong ShieldDamagePower;
        public ulong ShieldEngineerSpawner;
        public float SpawnerSearchRadius;
        public ProcProfileNullifierAntiShieldPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProcProfileNullifierAntiShieldPrototype), proto); }
    }

    public class ProceduralProfileMadameHydraPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public ProceduralFlankContextPrototype FlankTarget;
        public ulong SummonHydraPower;
        public ulong InvulnerablePower;
        public ProceduralUsePowerContextPrototype TeleportPower;
        public int SummonHydraMinCooldownMS;
        public int SummonHydraMaxCooldownMS;
        public ProceduralProfileMadameHydraPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileMadameHydraPrototype), proto); }
    }

    public class ProceduralProfileStarktechSentinelPrototype : ProceduralProfileWithEnragePrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public ProceduralUsePowerContextPrototype SummonSentinels;
        public float SummonPowerThreshold1;
        public float SummonPowerThreshold2;
        public ProceduralProfileStarktechSentinelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileStarktechSentinelPrototype), proto); }
    }

    public class ProceduralProfileKingpinPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public ProceduralUsePowerContextPrototype SummonElektra;
        public ProceduralUsePowerContextPrototype SummonBullseye;
        public float SummonElektraThreshold;
        public float SummonBullseyeThreshold;
        public ProceduralProfileKingpinPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileKingpinPrototype), proto); }
    }

    public class ProceduralProfilePowerRestrictedPrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public ProceduralFlankContextPrototype FlankTarget;
        public bool IsRanged;
        public ProceduralUsePowerContextPrototype RestrictedModeStartPower;
        public ProceduralUsePowerContextPrototype RestrictedModeEndPower;
        public ProceduralUsePowerContextPrototype[] RestrictedModeProceduralPowers;
        public int RestrictedModeMinCooldownMS;
        public int RestrictedModeMaxCooldownMS;
        public int RestrictedModeTimerMS;
        public bool NoMoveInRestrictedMode;
        public ProceduralProfilePowerRestrictedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfilePowerRestrictedPrototype), proto); }
    }

    public class ProceduralProfileUltronEMPPrototype : ProceduralProfileNoMoveNoSensePrototype
    {
        public ProceduralUsePowerContextPrototype EMPPower;
        public ProceduralProfileUltronEMPPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileUltronEMPPrototype), proto); }
    }

    public class ProcProfileQuicksilverTeamUpPrototype : ProceduralProfileTeamUpPrototype
    {
        public ProceduralUsePowerContextPrototype SpecialMovementPower;
        public ProcProfileQuicksilverTeamUpPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProcProfileQuicksilverTeamUpPrototype), proto); }
    }

    public class ProceduralProfileSkrullNickFuryPrototype : ProceduralProfileRangeFlankerPrototype
    {
        public ProceduralUsePowerContextPrototype OpenRocketCratePower;
        public ProceduralUsePowerContextPrototype OpenMinigunCratePower;
        public ProceduralUsePowerContextPrototype UseRocketPower;
        public ProceduralUsePowerContextPrototype UseMinigunPower;
        public MoveToContextPrototype MoveToCrate;
        public ProceduralUsePowerContextPrototype CommandTurretPower;
        public int CratePowerUseCount;
        public ProceduralUsePowerContextPrototype DiscardWeaponPower;
        public ulong CrateUsedState;
        public ProceduralProfileSkrullNickFuryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileSkrullNickFuryPrototype), proto); }
    }

    public class ProceduralProfileNickFuryTurretPrototype : ProceduralProfileRotatingTurretWithTargetPrototype
    {
        public ProceduralUsePowerContextPrototype SpecialCommandPower;
        public ulong SkrullNickFuryRef;
        public ProceduralProfileNickFuryTurretPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileNickFuryTurretPrototype), proto); }
    }

    public class ProceduralProfileKaeciliusPrototype : ProceduralProfileBasicRangePrototype
    {
        public ProceduralPowerWithSpecificTargetsPrototype[] HotspotSpawners;
        public ProceduralThresholdPowerContextPrototype FalseDeathPower;
        public ProceduralUsePowerContextPrototype HealFinalFormPower;
        public ProceduralUsePowerContextPrototype DeathPreventerPower;
        public ulong Cauldron;
        public ProceduralProfileKaeciliusPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileKaeciliusPrototype), proto); }
    }

    public class ProceduralProfileMeleeRevengePrototype : ProceduralProfileBasicMeleePrototype
    {
        public ProceduralUsePowerContextPrototype RevengePower;
        public ulong RevengeSupport;
        public ProceduralProfileMeleeRevengePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileMeleeRevengePrototype), proto); }
    }

    public class ProceduralProfileRangedRevengePrototype : ProceduralProfileBasicRangePrototype
    {
        public ProceduralUsePowerContextPrototype RevengePower;
        public ulong RevengeSupport;
        public ProceduralProfileRangedRevengePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileRangedRevengePrototype), proto); }
    }

    public class ProceduralProfileTaserTrapPrototype : ProceduralProfileWithTargetPrototype
    {
        public ulong TaserHotspot;
        public ProceduralProfileTaserTrapPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileTaserTrapPrototype), proto); }
    }

    public class ProceduralProfileVulturePrototype : ProceduralProfileWithAttackPrototype
    {
        public MoveToContextPrototype MoveToTarget;
        public OrbitContextPrototype OrbitTarget;
        public ProceduralUsePowerContextPrototype LungePower;
        public int MaxLungeActivations;
        public ProceduralProfileVulturePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProceduralProfileVulturePrototype), proto); }
    }
}
