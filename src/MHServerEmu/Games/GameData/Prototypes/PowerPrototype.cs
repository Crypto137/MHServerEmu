using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum WhenOutOfRangeType
    {
        MoveIntoRange = 0,
        DoNothing = 1,
        ActivateInDirection = 2,
        MoveIfTargetingMOB = 3,
        ActivateComboMovementPower = 4,
    }

    [AssetEnum]
    public enum ActivationType
    {
        None = 0,
        Passive = 1,
        Instant = 2,
        InstantTargeted = 3,
        TwoStageTargeted = 4,
    }

    [AssetEnum]
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

    [AssetEnum]
    public enum ProcChanceMultiplierBehaviorType
    {
        AllowProcChanceMultiplier = 0,
        IgnoreProcChanceMultiplier = 1,
        IgnoreProcChanceMultiplierUnlessZero = 2,
    }

    [AssetEnum]
    public enum TeleportMethodType
    {
        Teleport = 1,
        Phase = 2,
    }

    [AssetEnum]
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

    [AssetEnum]
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

    [AssetEnum]
    public enum BlackboardOperatorType
    {
        Add = 0,
        Div = 1,
        Mul = 2,
        Set = 3,
        Sub = 4,
        SetTargetId = 5,
        ClearTargetId = 6,
    }

    [AssetEnum]
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

    [AssetEnum]
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

    [AssetEnum]
    public enum EntityHealthState
    {
        Alive = 0,
        Dead = 1,
        AliveOrDead = 2,
    }

    [AssetEnum]
    public enum TargetingHeightType
    {
        All = 0,
        GroundOnly = 1,
        SameHeight = 2,
        FlyingOnly = 3,
    }

    [AssetEnum]
    public enum SubsequentActivateType
    {
        None = 0,
        DestroySummonedEntity = 1,
        RepeatActivation = 2,
    }

    [AssetEnum]
    public enum TargetRestrictionType
    {
        None = 0,
        HealthGreaterThanPercentage = 1,
        HealthLessThanPercentage = 2,
        EnduranceGreaterThanPercentage = 3,
        EnduranceLessThanPercentage = 4,
        HealthOrEnduranceGreaterThanPercentage = 5,
        HealthOrEnduranceLessThanPercentage = 6,
        SecondaryResourceLessThanPercentage = 7,
        HasKeyword = 8,
        DoesNotHaveKeyword = 9,
        HasAI = 10,
        IsPrototypeOf = 11,
        HasProperty = 12,
        DoesNotHaveProperty = 13,
    }

    #endregion

    public class PowerPrototype : Prototype
    {
        public ulong Properties { get; private set; }
        public PowerEventActionPrototype[] ActionsTriggeredOnPowerEvent { get; private set; }
        public ActivationType Activation { get; private set; }
        public float AnimationContactTimePercent { get; private set; }
        public int AnimationTimeMS { get; private set; }
        public ConditionPrototype AppliesConditions { get; private set; }
        public bool CancelConditionsOnEnd { get; private set; }
        public bool CancelledOnDamage { get; private set; }
        public bool CancelledOnMove { get; private set; }
        public bool CanBeDodged { get; private set; }
        public bool CanCrit { get; private set; }
        public EvalPrototype ChannelLoopTimeMS { get; private set; }
        public int ChargingTimeMS { get; private set; }
        public ConditionEffectPrototype ConditionEffects { get; private set; }
        public EvalPrototype CooldownTimeMS { get; private set; }
        public DesignWorkflowState DesignState { get; private set; }
        public ulong DisplayName { get; private set; }
        public ulong IconPath { get; private set; }
        public bool IsToggled { get; private set; }
        public PowerCategoryType PowerCategory { get; private set; }
        public ulong PowerUnrealClass { get; private set; }
        public EvalPrototype ProjectileSpeed { get; private set; }
        public float Radius { get; private set; }
        public bool RemovedOnUse { get; private set; }
        public StackingBehaviorPrototype StackingBehaviorLEGACY { get; private set; }
        public bool MovementStopOnActivate { get; private set; }
        public ulong TargetingReach { get; private set; }
        public ulong TargetingStyle { get; private set; }
        public bool UsableByAll { get; private set; }
        public bool HideFloatingNumbers { get; private set; }
        public int PostContactDelayMS { get; private set; }
        public ulong[] Keywords { get; private set; }
        public bool CancelConditionsOnUnassign { get; private set; }
        public float HeightCheckPadding { get; private set; }
        public bool FlyingUsable { get; private set; }
        public ExtraActivatePrototype ExtraActivation { get; private set; }
        public bool CancelledOnButtonRelease { get; private set; }
        public PowerUnrealOverridePrototype[] PowerUnrealOverrides { get; private set; }
        public bool CanBeInterrupted { get; private set; }
        public int ChannelStartTimeMS { get; private set; }
        public int ChannelEndTimeMS { get; private set; }
        public bool ForceNonExclusive { get; private set; }
        public WhenOutOfRangeType WhenOutOfRange { get; private set; }
        public int NoInterruptPreWindowMS { get; private set; }
        public int NoInterruptPostWindowMS { get; private set; }
        public ulong TooltipDescriptionText { get; private set; }
        public float ProjectileTimeToImpactOverride { get; private set; }
        public AbilitySlotRestrictionPrototype SlotRestriction { get; private set; }
        public bool ActiveUntilCancelled { get; private set; }
        public PowerTooltipEntryPrototype[] TooltipInfoCurrentRank { get; private set; }
        public PowerTooltipEntryPrototype[] TooltipInfoNextRank { get; private set; }
        public bool StopsContinuousIfTargetMissing { get; private set; }
        public bool ResetTargetPositionAtContactTime { get; private set; }
        public float RangeMinimum { get; private set; }
        public EvalPrototype Range { get; private set; }
        public int ChannelMinTimeMS { get; private set; }
        public int MaxAOETargets { get; private set; }
        public EvalPrototype[] EvalOnActivate { get; private set; }
        public EvalPrototype[] EvalOnCreate { get; private set; }
        public bool CooldownOnPlayer { get; private set; }
        public bool DisableEnduranceRegenOnEnd { get; private set; }
        public PowerSynergyTooltipEntryPrototype[] TooltipPowerSynergyBonuses { get; private set; }
        public SituationalPowerComponentPrototype SituationalComponent { get; private set; }
        public bool DisableEnduranceRegenOnActivate { get; private set; }
        public EvalPrototype[] EvalOnPreApply { get; private set; }
        public int RecurringCostIntervalMS { get; private set; }
        public ConditionPrototype ConditionsByRef { get; private set; }
        public bool IsRecurring { get; private set; }
        public EvalPrototype EvalCanTrigger { get; private set; }
        public float RangeActivationReduction { get; private set; }
        public EvalPrototype EvalPowerSynergies { get; private set; }
        public bool DisableContinuous { get; private set; }
        public bool CooldownDisableUI { get; private set; }
        public bool DOTIsDirectionalToCaster { get; private set; }
        public bool OmniDurationBonusExclude { get; private set; }
        public ulong ToggleGroup { get; private set; }
        public bool IsUltimate { get; private set; }
        public bool PlayNotifySfxOnAvailable { get; private set; }
        public ulong BounceDamagePctToSameIdCurve { get; private set; }
        public ulong[] RefreshDependentPassivePowers { get; private set; }
        public EvalPrototype TargetRestrictionEval { get; private set; }
        public bool IsUseableWhileDead { get; private set; }
        public float OnHitProcChanceMultiplier { get; private set; }
        public bool ApplyResultsImmediately { get; private set; }
        public bool AllowHitReactOnClient { get; private set; }
        public bool CanCauseHitReact { get; private set; }
        public ProcChanceMultiplierBehaviorType ProcChanceMultiplierBehavior { get; private set; }
        public bool IsSignature { get; private set; }
        public ulong TooltipCharacterSelectScreen { get; private set; }
        public ulong CharacterSelectDescription { get; private set; }
        public bool CooldownIsPersistentToDatabase { get; private set; }
        public float DamageTuningArea { get; private set; }
        public float DamageTuningBuff1 { get; private set; }
        public float DamageTuningBuff2 { get; private set; }
        public float DamageTuningBuff3 { get; private set; }
        public float DamageTuningCooldown { get; private set; }
        public float DamageTuningDebuff1 { get; private set; }
        public float DamageTuningDebuff2 { get; private set; }
        public float DamageTuningDebuff3 { get; private set; }
        public float DamageTuningDmgBonusFreq { get; private set; }
        public float DamageTuningDoTHotspot { get; private set; }
        public float DamageTuningHardCC { get; private set; }
        public float DamageTuningMultiHit { get; private set; }
        public float DamageTuningAnimationDelay { get; private set; }
        public float DamageTuningPowerTag1 { get; private set; }
        public float DamageTuningPowerTag2 { get; private set; }
        public float DamageTuningPowerTag3 { get; private set; }
        public float DamageTuningRangeRisk { get; private set; }
        public float DamageTuningSoftCC { get; private set; }
        public float DamageTuningSummon { get; private set; }
        public float DamageTuningDuration { get; private set; }
        public float DamageTuningTriggerDelay { get; private set; }
        public bool CanBeBlocked { get; private set; }
        public PowerTooltipEntryPrototype[] TooltipInfoAntirequisiteLockout { get; private set; }
        public bool CancelledOnTargetKilled { get; private set; }
        public bool ProjectileReturnsToUser { get; private set; }
        public bool CanCauseTag { get; private set; }
        public ulong[] TooltipPowerReferences { get; private set; }
        public bool BreaksStealth { get; private set; }
        public ulong HUDMessage { get; private set; }
        public bool CancelConditionsOnExitWorld { get; private set; }
        public int TooltipWidthOverride { get; private set; }
        public bool ResetUserPositionAtContactTime { get; private set; }
        public bool MovementOrientToTargetOnActivate { get; private set; }
        public bool MovementPreventWhileActive { get; private set; }
        public float DamageBaseTuningEnduranceCost { get; private set; }
        public int DamageBaseTuningAnimTimeMS { get; private set; }
        public float DamageTuningHeroSpecific { get; private set; }
        public bool MovementPreventChannelEnd { get; private set; }
        public bool MovementPreventChannelLoop { get; private set; }
        public bool MovementPreventChannelStart { get; private set; }
        public ulong CharacterSelectYouTubeVideoID { get; private set; }
        public float DamageBaseTuningEnduranceRatio { get; private set; }
        public ulong CharacterSelectIconPath { get; private set; }
        public ManaType[] DisableEnduranceRegenTypes { get; private set; }
        public bool CanCauseCancelOnDamage { get; private set; }
        public ulong IconPathHiRes { get; private set; }
        public bool PrefetchAsset { get; private set; }
        public bool IsTravelPower { get; private set; }
        public ulong GamepadSettings { get; private set; }
        public EvalPrototype BreaksStealthOverrideEval { get; private set; }
    }

    public class MovementPowerPrototype : PowerPrototype
    {
        public bool MoveToExactTargetLocation { get; private set; }
        public bool NoCollideIncludesTarget { get; private set; }
        public bool MoveToOppositeEdgeOfTarget { get; private set; }
        public bool ConstantMoveTime { get; private set; }
        public float AdditionalTargetPosOffset { get; private set; }
        public bool MoveToSecondaryTarget { get; private set; }
        public bool MoveFullDistance { get; private set; }
        public bool IsTeleportDEPRECATED { get; private set; }
        public float MoveMinDistance { get; private set; }
        public bool UserNoEntityCollide { get; private set; }
        public bool AllowOrientationChange { get; private set; }
        public float PowerMovementPathPct { get; private set; }
        public int MovementHeightBonus { get; private set; }
        public bool FollowsMouseWhileActive { get; private set; }
        public EvalPrototype EvalUserMoveSpeed { get; private set; }
        public bool ChanneledMoveTime { get; private set; }
        public MovementBehaviorPrototype CustomBehavior { get; private set; }
        public bool IgnoreTeleportBlockers { get; private set; }
        public bool HighFlying { get; private set; }
        public TeleportMethodType TeleportMethod { get; private set; }
    }

    public class SpecializationPowerPrototype : PowerPrototype
    {
        public ulong MasterPowerDEPRECATED { get; private set; }
        public EvalPrototype[] EvalCanEnable { get; private set; }
    }

    public class StealablePowerInfoPrototype : Prototype
    {
        public ulong Power { get; private set; }
        public ulong StealablePowerDescription { get; private set; }
    }

    public class StolenPowerRestrictionPrototype : Prototype
    {
        public ulong RestrictionKeyword { get; private set; }
        public int RestrictionKeywordCount { get; private set; }
        public ulong RestrictionBannerMessage { get; private set; }
    }

    public class PowerEventContextTransformModePrototype : PowerEventContextPrototype
    {
        public ulong TransformMode { get; private set; }
    }

    public class PowerEventContextShowBannerMessagePrototype : PowerEventContextPrototype
    {
        public ulong BannerMessage { get; private set; }
        public bool SendToPrimaryTarget { get; private set; }
    }

    public class PowerEventContextLootTablePrototype : PowerEventContextPrototype
    {
        public ulong LootTable { get; private set; }
        public bool UseItemLevelForLootRoll { get; private set; }
        public bool IncludeNearbyAvatars { get; private set; }
        public bool PlaceLootInGeneralInventory { get; private set; }
    }

    public class PowerEventContextTeleportRegionPrototype : PowerEventContextPrototype
    {
        public ulong Destination { get; private set; }
    }

    public class PowerEventContextPetDonateItemPrototype : PowerEventContextPrototype
    {
        public float Radius { get; private set; }
        public ulong RarityThreshold { get; private set; }
    }

    public class PowerEventContextCooldownChangePrototype : PowerEventContextPrototype
    {
        public bool TargetsOwner { get; private set; }
    }

    public class PowerToggleGroupPrototype : Prototype
    {
    }

    public class PowerEventContextPrototype : Prototype
    {
    }

    public class PowerEventContextOffsetActivationAOEPrototype : PowerEventContextPrototype
    {
        public float PositionOffsetMagnitude { get; private set; }
        public float RotationOffsetDegrees { get; private set; }
        public bool UseIncomingTargetPosAsUserPos { get; private set; }
    }

    public class AbilityAssignmentPrototype : Prototype
    {
        public ulong Ability { get; private set; }
    }

    public class AbilityAutoAssignmentSlotPrototype : Prototype
    {
        public ulong Ability { get; private set; }
        public ulong Slot { get; private set; }
    }

    public class PowerEventContextCallbackPrototype : PowerEventContextPrototype
    {
        public bool SetContextOnOwnerAgent { get; private set; }
        public bool SetContextOnOwnerSummonEntities { get; private set; }
        public bool SummonedEntitiesUsePowerTarget { get; private set; }
        public bool SetContextOnTargetEntity { get; private set; }
    }

    public class MapPowerPrototype : Prototype
    {
        public ulong OriginalPower { get; private set; }
        public ulong MappedPower { get; private set; }
    }

    public class PowerEventContextMapPowersPrototype : PowerEventContextPrototype
    {
        public MapPowerPrototype[] MappedPowers { get; private set; }
    }

    public class PowerEventContextUnassignMappedPowersPrototype : PowerEventContextPrototype
    {
        public MapPowerPrototype[] MappedPowersToUnassign { get; private set; }
    }

    public class AbilitySlotRestrictionPrototype : Prototype
    {
        public bool ActionKeySlotOK { get; private set; }
        public bool LeftMouseSlotOK { get; private set; }
        public bool RightMouseSlotOK { get; private set; }
    }

    public class PowerEventActionPrototype : Prototype
    {
        public PowerEventActionType EventAction { get; private set; }
        public float EventParam { get; private set; }
        public ulong Power { get; private set; }
        public PowerEventType PowerEvent { get; private set; }
        public PowerEventContextPrototype PowerEventContext { get; private set; }
        public ulong[] Keywords { get; private set; }
        public bool UseTriggerPowerOriginalTargetPos { get; private set; }
        public bool UseTriggeringPowerTargetVerbatim { get; private set; }
        public EvalPrototype EvalEventTriggerChance { get; private set; }
        public EvalPrototype EvalEventParam { get; private set; }
        public bool ResetFXRandomSeed { get; private set; }
    }

    public class SituationalTriggerPrototype : Prototype
    {
        public ulong TriggerCollider { get; private set; }
        public float TriggerRadiusScaling { get; private set; }
        public EntityFilterPrototype EntityFilter { get; private set; }
        public bool AllowDead { get; private set; }
        public bool ActivateOnTriggerSuccess { get; private set; }
    }

    public class SituationalTriggerOnKilledPrototype : SituationalTriggerPrototype
    {
        public bool Friendly { get; private set; }
        public bool Hostile { get; private set; }
        public bool KilledByOther { get; private set; }
        public bool KilledBySelf { get; private set; }
        public bool WasLastInRange { get; private set; }
    }

    public class SituationalTriggerOnHealthThresholdPrototype : SituationalTriggerPrototype
    {
        public bool HealthBelow { get; private set; }
        public float HealthPercent { get; private set; }
    }

    public class SituationalTriggerOnStatusEffectPrototype : SituationalTriggerPrototype
    {
        public ulong[] TriggeringProperties { get; private set; }
        public bool TriggersOnStatusApplied { get; private set; }
        public ulong[] TriggeringConditionKeywords { get; private set; }
    }

    public class SituationalTriggerInvAndWorldPrototype : SituationalTriggerPrototype
    {
        public ulong InventoryRef { get; private set; }
    }

    public class SituationalPowerComponentPrototype : Prototype
    {
        public int ActivationWindowMS { get; private set; }
        public EvalPrototype ChanceToTrigger { get; private set; }
        public bool ForceRelockOnTriggerRevert { get; private set; }
        public bool RemoveTriggeringEntityOnActivate { get; private set; }
        public SituationalTriggerPrototype SituationalTrigger { get; private set; }
        public bool TargetsTriggeringEntity { get; private set; }
        public bool ForceRelockOnActivate { get; private set; }
    }

    public class PowerUnrealReplacementPrototype : Prototype
    {
        public ulong EntityArt { get; private set; }
        public ulong PowerArt { get; private set; }
        public float AnimationContactTimePercent { get; private set; }
        public int AnimationTimeMS { get; private set; }
    }

    public class PowerUnrealOverridePrototype : Prototype
    {
        public float AnimationContactTimePercent { get; private set; }
        public int AnimationTimeMS { get; private set; }
        public ulong EntityArt { get; private set; }
        public ulong PowerArt { get; private set; }
        public PowerUnrealReplacementPrototype[] ArtOnlyReplacements { get; private set; }
    }

    public class PowerSynergyTooltipEntryPrototype : Prototype
    {
        public ulong SynergyPower { get; private set; }
        public ulong Translation { get; private set; }
    }

    public class PowerEventContextCallbackAIChangeBlackboardPropertyPrototype : PowerEventContextCallbackPrototype
    {
        public BlackboardOperatorType Operation { get; private set; }
        public ulong PropertyInfoRef { get; private set; }
        public int Value { get; private set; }
        public bool UseTargetEntityId { get; private set; }
    }

    public class PowerEventContextCallbackAISetAssistedEntityFromCreatorPrototype : PowerEventContextCallbackPrototype
    {
    }

    public class PowerEventContextCallbackAISummonsTryActivatePowerPrototype : PowerEventContextCallbackPrototype
    {
        public ulong PowerToActivate { get; private set; }
        public bool SummonsUsePowerTargetLocation { get; private set; }
        public ulong SummonsKeywordFilter { get; private set; }
    }

    public class TransformModeUnrealOverridePrototype : Prototype
    {
        public ulong IncomingUnrealClass { get; private set; }
        public ulong TransformedUnrealClass { get; private set; }
    }

    public class TransformModePrototype : Prototype
    {
        public AbilityAssignmentPrototype[] DefaultEquippedAbilities { get; private set; }
        public ulong EnterTransformModePower { get; private set; }
        public ulong ExitTransformModePower { get; private set; }
        public ulong UnrealClass { get; private set; }
        public ulong[] HiddenPassivePowers { get; private set; }
        public bool PowersAreSlottable { get; private set; }
        public EvalPrototype DurationMSEval { get; private set; }
        public TransformModeUnrealOverridePrototype[] UnrealClassOverrides { get; private set; }
        public ulong UseRankOfPower { get; private set; }
    }

    public class TransformModeEntryPrototype : Prototype
    {
        public ulong AllowedPowers { get; private set; }
        public ulong TransformMode { get; private set; }
    }

    public class GamepadSettingsPrototype : Prototype
    {
        public bool ClearContinuousInitialTarget { get; private set; }
        public float Range { get; private set; }
        public bool TeleportToTarget { get; private set; }
        public bool MeleeMoveIntoRange { get; private set; }
        public bool ChannelPowerOrientToEnemy { get; private set; }
    }

    public class TargetingStylePrototype : Prototype
    {
        public AOEAngleType AOEAngle { get; private set; }
        public bool AOESelfCentered { get; private set; }
        public bool NeedsTarget { get; private set; }
        public bool OffsetWedgeBehindUser { get; private set; }
        public float OrientationOffset { get; private set; }
        public TargetingShapeType TargetingShape { get; private set; }
        public bool TurnsToFaceTarget { get; private set; }
        public float Width { get; private set; }
        public bool AlwaysTargetMousePos { get; private set; }
        public bool MovesToRangeOfPrimaryTarget { get; private set; }
        public bool UseDefaultRotationSpeed { get; private set; }
        public Vector2Prototype PositionOffset { get; private set; }
        public int RandomPositionRadius { get; private set; }
        public bool DisableOrientationDuringPower { get; private set; }
    }

    public class TargetingReachPrototype : Prototype
    {
        public bool ExcludesPrimaryTarget { get; private set; }
        public bool Melee { get; private set; }
        public bool RequiresLineOfSight { get; private set; }
        public bool TargetsEnemy { get; private set; }
        public bool TargetsFlying { get; private set; }
        public bool TargetsFriendly { get; private set; }
        public bool TargetsGround { get; private set; }
        public bool WillTargetCaster { get; private set; }
        public bool LowestHealth { get; private set; }
        public TargetingHeightType TargetingHeightType { get; private set; }
        public bool PartyOnly { get; private set; }
        public bool WillTargetCreator { get; private set; }
        public bool TargetsDestructibles { get; private set; }
        public bool LOSCheckAlongGround { get; private set; }
        public EntityHealthState EntityHealthState { get; private set; }
        public ConvenienceLabel TargetsEntitiesInInventory { get; private set; }
        public bool TargetsFrontSideOnly { get; private set; }
        public bool TargetsNonEnemies { get; private set; }
        public bool WillTargetUltimateCreator { get; private set; }
        public bool RandomAOETargets { get; private set; }
    }

    public class ExtraActivatePrototype : Prototype
    {
    }

    public class SecondaryActivateOnReleasePrototype : ExtraActivatePrototype
    {
        public ulong DamageIncreasePerSecond { get; private set; }
        public DamageType DamageIncreaseType { get; private set; }
        public ulong EnduranceCostIncreasePerSecond { get; private set; }
        public int MaxReleaseTimeMS { get; private set; }
        public int MinReleaseTimeMS { get; private set; }
        public ulong RangeIncreasePerSecond { get; private set; }
        public ulong RadiusIncreasePerSecond { get; private set; }
        public bool ActivateOnMaxReleaseTime { get; private set; }
        public ulong DefensePenetrationIncrPerSec { get; private set; }
        public DamageType DefensePenetrationType { get; private set; }
        public bool FollowsMouseUntilRelease { get; private set; }
        public ManaType EnduranceCostManaType { get; private set; }
    }

    public class ExtraActivateOnSubsequentPrototype : ExtraActivatePrototype
    {
        public ulong NumActivatesBeforeCooldown { get; private set; }
        public ulong TimeoutLengthMS { get; private set; }
        public SubsequentActivateType ExtraActivateEffect { get; private set; }
    }

    public class ExtraActivateCycleToPowerPrototype : ExtraActivatePrototype
    {
        public ulong CyclePowerList { get; private set; }
    }
}
