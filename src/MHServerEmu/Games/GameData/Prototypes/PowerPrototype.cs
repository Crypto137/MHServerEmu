namespace MHServerEmu.Games.GameData.Prototypes
{

    public class PowerPrototype : Prototype
    {
        public ulong Properties;
        public PowerEventActionPrototype[] ActionsTriggeredOnPowerEvent;
        public ActivationType Activation;
        public float AnimationContactTimePercent;
        public int AnimationTimeMS;
        public ConditionPrototype AppliesConditions;
        public bool CancelConditionsOnEnd;
        public bool CancelledOnDamage;
        public bool CancelledOnMove;
        public bool CanBeDodged;
        public bool CanCrit;
        public EvalPrototype ChannelLoopTimeMS;
        public int ChargingTimeMS;
        public ConditionEffectPrototype ConditionEffects;
        public EvalPrototype CooldownTimeMS;
        public DesignWorkflowState DesignState;
        public ulong DisplayName;
        public ulong IconPath;
        public bool IsToggled;
        public PowerCategoryType PowerCategory;
        public ulong PowerUnrealClass;
        public EvalPrototype ProjectileSpeed;
        public float Radius;
        public bool RemovedOnUse;
        public StackingBehaviorPrototype StackingBehaviorLEGACY;
        public bool MovementStopOnActivate;
        public ulong TargetingReach;
        public ulong TargetingStyle;
        public bool UsableByAll;
        public bool HideFloatingNumbers;
        public int PostContactDelayMS;
        public ulong[] Keywords;
        public bool CancelConditionsOnUnassign;
        public float HeightCheckPadding;
        public bool FlyingUsable;
        public ExtraActivatePrototype ExtraActivation;
        public bool CancelledOnButtonRelease;
        public PowerUnrealOverridePrototype[] PowerUnrealOverrides;
        public bool CanBeInterrupted;
        public int ChannelStartTimeMS;
        public int ChannelEndTimeMS;
        public bool ForceNonExclusive;
        public WhenOutOfRangeType WhenOutOfRange;
        public int NoInterruptPreWindowMS;
        public int NoInterruptPostWindowMS;
        public ulong TooltipDescriptionText;
        public float ProjectileTimeToImpactOverride;
        public AbilitySlotRestrictionPrototype SlotRestriction;
        public bool ActiveUntilCancelled;
        public PowerTooltipEntryPrototype[] TooltipInfoCurrentRank;
        public PowerTooltipEntryPrototype[] TooltipInfoNextRank;
        public bool StopsContinuousIfTargetMissing;
        public bool ResetTargetPositionAtContactTime;
        public float RangeMinimum;
        public EvalPrototype Range;
        public int ChannelMinTimeMS;
        public int MaxAOETargets;
        public EvalPrototype[] EvalOnActivate;
        public EvalPrototype[] EvalOnCreate;
        public bool CooldownOnPlayer;
        public bool DisableEnduranceRegenOnEnd;
        public PowerSynergyTooltipEntryPrototype[] TooltipPowerSynergyBonuses;
        public SituationalPowerComponentPrototype SituationalComponent;
        public bool DisableEnduranceRegenOnActivate;
        public EvalPrototype[] EvalOnPreApply;
        public int RecurringCostIntervalMS;
        public ConditionPrototype ConditionsByRef;
        public bool IsRecurring;
        public EvalPrototype EvalCanTrigger;
        public float RangeActivationReduction;
        public EvalPrototype EvalPowerSynergies;
        public bool DisableContinuous;
        public bool CooldownDisableUI;
        public bool DOTIsDirectionalToCaster;
        public bool OmniDurationBonusExclude;
        public ulong ToggleGroup;
        public bool IsUltimate;
        public bool PlayNotifySfxOnAvailable;
        public ulong BounceDamagePctToSameIdCurve;
        public ulong[] RefreshDependentPassivePowers;
        public EvalPrototype TargetRestrictionEval;
        public bool IsUseableWhileDead;
        public float OnHitProcChanceMultiplier;
        public bool ApplyResultsImmediately;
        public bool AllowHitReactOnClient;
        public bool CanCauseHitReact;
        public ProcChanceMultiplierBehaviorType ProcChanceMultiplierBehavior;
        public bool IsSignature;
        public ulong TooltipCharacterSelectScreen;
        public ulong CharacterSelectDescription;
        public bool CooldownIsPersistentToDatabase;
        public float DamageTuningArea;
        public float DamageTuningBuff1;
        public float DamageTuningBuff2;
        public float DamageTuningBuff3;
        public float DamageTuningCooldown;
        public float DamageTuningDebuff1;
        public float DamageTuningDebuff2;
        public float DamageTuningDebuff3;
        public float DamageTuningDmgBonusFreq;
        public float DamageTuningDoTHotspot;
        public float DamageTuningHardCC;
        public float DamageTuningMultiHit;
        public float DamageTuningAnimationDelay;
        public float DamageTuningPowerTag1;
        public float DamageTuningPowerTag2;
        public float DamageTuningPowerTag3;
        public float DamageTuningRangeRisk;
        public float DamageTuningSoftCC;
        public float DamageTuningSummon;
        public float DamageTuningDuration;
        public float DamageTuningTriggerDelay;
        public bool CanBeBlocked;
        public PowerTooltipEntryPrototype[] TooltipInfoAntirequisiteLockout;
        public bool CancelledOnTargetKilled;
        public bool ProjectileReturnsToUser;
        public bool CanCauseTag;
        public ulong[] TooltipPowerReferences;
        public bool BreaksStealth;
        public ulong HUDMessage;
        public bool CancelConditionsOnExitWorld;
        public int TooltipWidthOverride;
        public bool ResetUserPositionAtContactTime;
        public bool MovementOrientToTargetOnActivate;
        public bool MovementPreventWhileActive;
        public float DamageBaseTuningEnduranceCost;
        public int DamageBaseTuningAnimTimeMS;
        public float DamageTuningHeroSpecific;
        public bool MovementPreventChannelEnd;
        public bool MovementPreventChannelLoop;
        public bool MovementPreventChannelStart;
        public ulong CharacterSelectYouTubeVideoID;
        public float DamageBaseTuningEnduranceRatio;
        public ulong CharacterSelectIconPath;
        public ManaType[] DisableEnduranceRegenTypes;
        public bool CanCauseCancelOnDamage;
        public ulong IconPathHiRes;
        public bool PrefetchAsset;
        public bool IsTravelPower;
        public ulong GamepadSettings;
        public EvalPrototype BreaksStealthOverrideEval;
        public PowerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerPrototype), proto); }
    }

    public enum WhenOutOfRangeType
    {
        MoveIntoRange = 0,
        DoNothing = 1,
        ActivateInDirection = 2,
        MoveIfTargetingMOB = 3,
        ActivateComboMovementPower = 4,
    }

    public enum ActivationType
    {
        None = 0,
        Passive = 1,
        Instant = 2,
        InstantTargeted = 3,
        TwoStageTargeted = 4,
    }
    public enum PowerCategoryType
    {
        None = 0,
        ComboEffect = 1,
        EmotePower = 2,
        GameFunctionPower = 3,
        HiddenPassivePower = 4,
        HotspotEffect = 5,
        ItemPower = 6,
        MissileEffect = 7,
        NormalPower = 8,
        ProcEffect = 9,
        ThrowableCancelPower = 10,
        ThrowablePower = 11,
    }
    public enum ProcChanceMultiplierBehaviorType
    {
        AllowProcChanceMultiplier = 0,
        IgnoreProcChanceMultiplier = 1,
        IgnoreProcChanceMultiplierUnlessZero = 2,
    }
    public class MovementPowerPrototype : PowerPrototype
    {
        public bool MoveToExactTargetLocation;
        public bool NoCollideIncludesTarget;
        public bool MoveToOppositeEdgeOfTarget;
        public bool ConstantMoveTime;
        public float AdditionalTargetPosOffset;
        public bool MoveToSecondaryTarget;
        public bool MoveFullDistance;
        public bool IsTeleportDEPRECATED;
        public float MoveMinDistance;
        public bool UserNoEntityCollide;
        public bool AllowOrientationChange;
        public float PowerMovementPathPct;
        public int MovementHeightBonus;
        public bool FollowsMouseWhileActive;
        public EvalPrototype EvalUserMoveSpeed;
        public bool ChanneledMoveTime;
        public MovementBehaviorPrototype CustomBehavior;
        public bool IgnoreTeleportBlockers;
        public bool HighFlying;
        public TeleportMethodType TeleportMethod;
        public MovementPowerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MovementPowerPrototype), proto); }
    }
    public enum TeleportMethodType
    {
        Teleport = 1,
        Phase = 2,
    }
    public class SpecializationPowerPrototype : PowerPrototype
    {
        public ulong MasterPowerDEPRECATED;
        public EvalPrototype[] EvalCanEnable;
        public SpecializationPowerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SpecializationPowerPrototype), proto); }
    }

    public class StealablePowerInfoPrototype : Prototype
    {
        public ulong Power;
        public ulong StealablePowerDescription;
        public StealablePowerInfoPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(StealablePowerInfoPrototype), proto); }
    }

    public class StolenPowerRestrictionPrototype : Prototype
    {
        public ulong RestrictionKeyword;
        public int RestrictionKeywordCount;
        public ulong RestrictionBannerMessage;
        public StolenPowerRestrictionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(StolenPowerRestrictionPrototype), proto); }
    }

    public class PowerEventContextTransformModePrototype : PowerEventContextPrototype
    {
        public ulong TransformMode;
        public PowerEventContextTransformModePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerEventContextTransformModePrototype), proto); }
    }

    public class PowerEventContextShowBannerMessagePrototype : PowerEventContextPrototype
    {
        public ulong BannerMessage;
        public bool SendToPrimaryTarget;
        public PowerEventContextShowBannerMessagePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerEventContextShowBannerMessagePrototype), proto); }
    }

    public class PowerEventContextLootTablePrototype : PowerEventContextPrototype
    {
        public ulong LootTable;
        public bool UseItemLevelForLootRoll;
        public bool IncludeNearbyAvatars;
        public bool PlaceLootInGeneralInventory;
        public PowerEventContextLootTablePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerEventContextLootTablePrototype), proto); }
    }

    public class PowerEventContextTeleportRegionPrototype : PowerEventContextPrototype
    {
        public ulong Destination;
        public PowerEventContextTeleportRegionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerEventContextTeleportRegionPrototype), proto); }
    }

    public class PowerEventContextPetDonateItemPrototype : PowerEventContextPrototype
    {
        public float Radius;
        public ulong RarityThreshold;
        public PowerEventContextPetDonateItemPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerEventContextPetDonateItemPrototype), proto); }
    }

    public class PowerEventContextCooldownChangePrototype : PowerEventContextPrototype
    {
        public bool TargetsOwner;
        public PowerEventContextCooldownChangePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerEventContextCooldownChangePrototype), proto); }
    }

    public class PowerToggleGroupPrototype : Prototype
    {
        public PowerToggleGroupPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerToggleGroupPrototype), proto); }
    }

    public class PowerEventContextPrototype : Prototype
    {
        public PowerEventContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerEventContextPrototype), proto); }
    }

    public class PowerEventContextOffsetActivationAOEPrototype : PowerEventContextPrototype
    {
        public float PositionOffsetMagnitude;
        public float RotationOffsetDegrees;
        public bool UseIncomingTargetPosAsUserPos;
        public PowerEventContextOffsetActivationAOEPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerEventContextOffsetActivationAOEPrototype), proto); }
    }

    public class AbilityAssignmentPrototype : Prototype
    {
        public ulong Ability;
        public AbilityAssignmentPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AbilityAssignmentPrototype), proto); }
    }

    public class AbilityAutoAssignmentSlotPrototype : Prototype
    {
        public ulong Ability;
        public ulong Slot;
        public AbilityAutoAssignmentSlotPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AbilityAutoAssignmentSlotPrototype), proto); }
    }

    public class PowerEventContextCallbackPrototype : PowerEventContextPrototype
    {
        public bool SetContextOnOwnerAgent;
        public bool SetContextOnOwnerSummonEntities;
        public bool SummonedEntitiesUsePowerTarget;
        public bool SetContextOnTargetEntity;
        public PowerEventContextCallbackPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerEventContextCallbackPrototype), proto); }
    }

    public class MapPowerPrototype : Prototype
    {
        public ulong OriginalPower;
        public ulong MappedPower;
        public MapPowerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MapPowerPrototype), proto); }
    }

    public class PowerEventContextMapPowersPrototype : PowerEventContextPrototype
    {
        public MapPowerPrototype[] MappedPowers;
        public PowerEventContextMapPowersPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerEventContextMapPowersPrototype), proto); }
    }

    public class PowerEventContextUnassignMappedPowersPrototype : PowerEventContextPrototype
    {
        public MapPowerPrototype[] MappedPowersToUnassign;
        public PowerEventContextUnassignMappedPowersPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerEventContextUnassignMappedPowersPrototype), proto); }
    }

    public class AbilitySlotRestrictionPrototype : Prototype
    {
        public bool ActionKeySlotOK;
        public bool LeftMouseSlotOK;
        public bool RightMouseSlotOK;
        public AbilitySlotRestrictionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AbilitySlotRestrictionPrototype), proto); }
    }

    public class PowerEventActionPrototype : Prototype
    {
        public PowerEventActionType EventAction;
        public float EventParam;
        public ulong Power;
        public PowerEventType PowerEvent;
        public PowerEventContextPrototype PowerEventContext;
        public ulong[] Keywords;
        public bool UseTriggerPowerOriginalTargetPos;
        public bool UseTriggeringPowerTargetVerbatim;
        public EvalPrototype EvalEventTriggerChance;
        public EvalPrototype EvalEventParam;
        public bool ResetFXRandomSeed;
        public PowerEventActionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerEventActionPrototype), proto); }
    }
    public enum PowerEventType
    {
        None = 0,
        OnContactTime = 1,
        OnCriticalHit = 2,
        OnHitKeyword = 3,
        OnPowerApply = 4,
        OnPowerStopped = 24,
        OnPowerEnd = 5,
        OnPowerLoopEnd = 26,
        OnPowerHit = 6,
        OnPowerStart = 7,
        OnPowerToggleOn = 22,
        OnPowerToggleOff = 23,
        OnProjectileHit = 8,
        OnStackCount = 9,
        OnTargetKill = 10,
        OnSummonEntity = 11,
        OnHoldBegin = 12,
        OnMissileHit = 13,
        OnMissileKilled = 14,
        OnHotspotNegated = 15,
        OnHotspotNegatedByOther = 16,
        OnHotspotOverlapBegin = 17,
        OnHotspotOverlapEnd = 18,
        OnRemoveCondition = 19,
        OnRemoveNegStatusEffect = 20,
        OnExtraActivationCooldown = 25,
        OnPowerPivot = 21,
        OnSpecializationPowerAssigned = 27,
        OnSpecializationPowerUnassigned = 28,
        OnEntityControlled = 29,
        OnOutOfRangeActivateMovementPower = 30,
    }
    public enum PowerEventActionType
    {
        None = 0,
        BodySlide = 1,
        CancelScheduledActivation = 2,
        CancelScheduledActivationOnTriggeredPower = 3,
        ContextCallback = 4,
        ControlAgentAI = 21,
        CooldownEnd = 25,
        CooldownModifySecs = 26,
        CooldownModifyPct = 27,
        CooldownStart = 24,
        DespawnTarget = 5,
        EndPower = 23,
        ChargesIncrement = 6,
        InteractFinish = 7,
        RemoveAndKillControlledAgentsFromInv = 22,
        RestoreThrowable = 9,
        ScheduleActivationAtPercent = 10,
        ScheduleActivationInSeconds = 11,
        RescheduleActivationInSeconds = 8,
        ShowBannerMessage = 12,
        SpawnLootTable = 13,
        SwitchAvatar = 14,
        ToggleOnPower = 15,
        ToggleOffPower = 16,
        TeamUpAgentSummon = 28,
        TeleportToPartyMember = 20,
        TransformModeChange = 17,
        TransformModeStart = 18,
        UsePower = 19,
        TeleportToRegion = 29,
        StealPower = 30,
        PetItemDonate = 31,
        MapPowers = 32,
        UnassignMappedPowers = 33,
        RemoveSummonedAgentsWithKeywords = 34,
        SpawnControlledAgentWithSummonDuration = 35,
        LocalCoopEnd = 36,
    }
    public class SituationalTriggerPrototype : Prototype
    {
        public ulong TriggerCollider;
        public float TriggerRadiusScaling;
        public EntityFilterPrototype EntityFilter;
        public bool AllowDead;
        public bool ActivateOnTriggerSuccess;
        public SituationalTriggerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SituationalTriggerPrototype), proto); }
    }

    public class SituationalTriggerOnKilledPrototype : SituationalTriggerPrototype
    {
        public bool Friendly;
        public bool Hostile;
        public bool KilledByOther;
        public bool KilledBySelf;
        public bool WasLastInRange;
        public SituationalTriggerOnKilledPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SituationalTriggerOnKilledPrototype), proto); }
    }

    public class SituationalTriggerOnHealthThresholdPrototype : SituationalTriggerPrototype
    {
        public bool HealthBelow;
        public float HealthPercent;
        public SituationalTriggerOnHealthThresholdPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SituationalTriggerOnHealthThresholdPrototype), proto); }
    }

    public class SituationalTriggerOnStatusEffectPrototype : SituationalTriggerPrototype
    {
        public ulong[] TriggeringProperties;
        public bool TriggersOnStatusApplied;
        public ulong[] TriggeringConditionKeywords;
        public SituationalTriggerOnStatusEffectPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SituationalTriggerOnStatusEffectPrototype), proto); }
    }

    public class SituationalTriggerInvAndWorldPrototype : SituationalTriggerPrototype
    {
        public ulong InventoryRef;
        public SituationalTriggerInvAndWorldPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SituationalTriggerInvAndWorldPrototype), proto); }
    }

    public class SituationalPowerComponentPrototype : Prototype
    {
        public int ActivationWindowMS;
        public EvalPrototype ChanceToTrigger;
        public bool ForceRelockOnTriggerRevert;
        public bool RemoveTriggeringEntityOnActivate;
        public SituationalTriggerPrototype SituationalTrigger;
        public bool TargetsTriggeringEntity;
        public bool ForceRelockOnActivate;
        public SituationalPowerComponentPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SituationalPowerComponentPrototype), proto); }
    }

    public class PowerUnrealReplacementPrototype : Prototype
    {
        public ulong EntityArt;
        public ulong PowerArt;
        public float AnimationContactTimePercent;
        public int AnimationTimeMS;
        public PowerUnrealReplacementPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerUnrealReplacementPrototype), proto); }
    }

    public class PowerUnrealOverridePrototype : Prototype
    {
        public float AnimationContactTimePercent;
        public int AnimationTimeMS;
        public ulong EntityArt;
        public ulong PowerArt;
        public PowerUnrealReplacementPrototype[] ArtOnlyReplacements;
        public PowerUnrealOverridePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerUnrealOverridePrototype), proto); }
    }

    public class PowerSynergyTooltipEntryPrototype : Prototype
    {
        public ulong SynergyPower;
        public ulong Translation;
        public PowerSynergyTooltipEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerSynergyTooltipEntryPrototype), proto); }
    }

    public class PowerEventContextCallbackAIChangeBlackboardPropertyPrototype : PowerEventContextCallbackPrototype
    {
        public BlackboardOpertatorType Operation;
        public ulong PropertyInfoRef;
        public int Value;
        public bool UseTargetEntityId;
        public PowerEventContextCallbackAIChangeBlackboardPropertyPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerEventContextCallbackAIChangeBlackboardPropertyPrototype), proto); }
    }
    public enum BlackboardOpertatorType
    {
        Add = 0,
        Div = 1,
        Mul = 2,
        Set = 3,
        Sub = 4,
        SetTargetId = 5,
        ClearTargetId = 6,
    }
    public class PowerEventContextCallbackAISetAssistedEntityFromCreatorPrototype : PowerEventContextCallbackPrototype
    {
        public PowerEventContextCallbackAISetAssistedEntityFromCreatorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerEventContextCallbackAISetAssistedEntityFromCreatorPrototype), proto); }
    }

    public class PowerEventContextCallbackAISummonsTryActivatePowerPrototype : PowerEventContextCallbackPrototype
    {
        public ulong PowerToActivate;
        public bool SummonsUsePowerTargetLocation;
        public ulong SummonsKeywordFilter;
        public PowerEventContextCallbackAISummonsTryActivatePowerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerEventContextCallbackAISummonsTryActivatePowerPrototype), proto); }
    }

    public class TransformModeUnrealOverridePrototype : Prototype
    {
        public ulong IncomingUnrealClass;
        public ulong TransformedUnrealClass;
        public TransformModeUnrealOverridePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TransformModeUnrealOverridePrototype), proto); }
    }

    public class TransformModePrototype : Prototype
    {
        public AbilityAssignmentPrototype[] DefaultEquippedAbilities;
        public ulong EnterTransformModePower;
        public ulong ExitTransformModePower;
        public ulong UnrealClass;
        public ulong[] HiddenPassivePowers;
        public bool PowersAreSlottable;
        public EvalPrototype DurationMSEval;
        public TransformModeUnrealOverridePrototype[] UnrealClassOverrides;
        public ulong UseRankOfPower;
        public TransformModePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TransformModePrototype), proto); }
    }

    public class TransformModeEntryPrototype : Prototype
    {
        public ulong AllowedPowers;
        public ulong TransformMode;
        public TransformModeEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TransformModeEntryPrototype), proto); }
    }

    public class GamepadSettingsPrototype : Prototype
    {
        public bool ClearContinuousInitialTarget;
        public float Range;
        public bool TeleportToTarget;
        public bool MeleeMoveIntoRange;
        public bool ChannelPowerOrientToEnemy;
        public GamepadSettingsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(GamepadSettingsPrototype), proto); }
    }

    public class TargetingStylePrototype : Prototype
    {
        public AOEAngleType AOEAngle;
        public bool AOESelfCentered;
        public bool NeedsTarget;
        public bool OffsetWedgeBehindUser;
        public float OrientationOffset;
        public TargetingShapeType TargetingShape;
        public bool TurnsToFaceTarget;
        public float Width;
        public bool AlwaysTargetMousePos;
        public bool MovesToRangeOfPrimaryTarget;
        public bool UseDefaultRotationSpeed;
        public Vector2Prototype PositionOffset;
        public int RandomPositionRadius;
        public bool DisableOrientationDuringPower;
        public TargetingStylePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TargetingStylePrototype), proto); }
    }
    public enum TargetingShapeType
    {
        None = 0,
        ArcArea = 1,
        BeamSweep = 2,
        CapsuleArea = 3,
        CircleArea = 4,
        RingArea = 5,
        Self = 6,
        TeamUp = 7,
        SingleTarget = 8,
        SingleTargetOwner = 9,
        SingleTargetRandom = 10,
        SkillShot = 11,
        SkillShotAlongGround = 12,
        WedgeArea = 13,
    }
    public enum AOEAngleType
    {
        _0 = 0,
        _1 = 1,
        _10 = 2,
        _30 = 3,
        _45 = 4,
        _60 = 5,
        _90 = 6,
        _120 = 7,
        _180 = 8,
        _240 = 9,
        _300 = 10,
        _360 = 11,
    }

    public class TargetingReachPrototype : Prototype
    {
        public bool ExcludesPrimaryTarget;
        public bool Melee;
        public bool RequiresLineOfSight;
        public bool TargetsEnemy;
        public bool TargetsFlying;
        public bool TargetsFriendly;
        public bool TargetsGround;
        public bool WillTargetCaster;
        public bool LowestHealth;
        public TargetingHeightType TargetingHeightType;
        public bool PartyOnly;
        public bool WillTargetCreator;
        public bool TargetsDestructibles;
        public bool LOSCheckAlongGround;
        public EntityHealthState EntityHealthState;
        public ConvenienceLabel TargetsEntitiesInInventory;
        public bool TargetsFrontSideOnly;
        public bool TargetsNonEnemies;
        public bool WillTargetUltimateCreator;
        public bool RandomAOETargets;
        public TargetingReachPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TargetingReachPrototype), proto); }
    }
    public enum EntityHealthState
    {
        Alive = 0,
        Dead = 1,
        AliveOrDead = 2,
    }
    public enum TargetingHeightType
    {
        All = 0,
        GroundOnly = 1,
        SameHeight = 2,
        FlyingOnly = 3,
    }

    public class ExtraActivatePrototype : Prototype
    {
        public ExtraActivatePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ExtraActivatePrototype), proto); }
    }

    public class SecondaryActivateOnReleasePrototype : ExtraActivatePrototype
    {
        public ulong DamageIncreasePerSecond;
        public DamageType DamageIncreaseType;
        public ulong EnduranceCostIncreasePerSecond;
        public int MaxReleaseTimeMS;
        public int MinReleaseTimeMS;
        public ulong RangeIncreasePerSecond;
        public ulong RadiusIncreasePerSecond;
        public bool ActivateOnMaxReleaseTime;
        public ulong DefensePenetrationIncrPerSec;
        public DamageType DefensePenetrationType;
        public bool FollowsMouseUntilRelease;
        public ManaType EnduranceCostManaType;
        public SecondaryActivateOnReleasePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SecondaryActivateOnReleasePrototype), proto); }
    }

    public class ExtraActivateOnSubsequentPrototype : ExtraActivatePrototype
    {
        public ulong NumActivatesBeforeCooldown;
        public ulong TimeoutLengthMS;
        public SubsequentActivateType ExtraActivateEffect;
        public ExtraActivateOnSubsequentPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ExtraActivateOnSubsequentPrototype), proto); }
    }
    public enum SubsequentActivateType
    {
        None = 0,
        DestroySummonedEntity = 1,
        RepeatActivation = 2,
    }
    public class ExtraActivateCycleToPowerPrototype : ExtraActivatePrototype
    {
        public ulong CyclePowerList;
        public ExtraActivateCycleToPowerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ExtraActivateCycleToPowerPrototype), proto); }
    }

}
