using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)MoveIntoRange)]
    public enum WhenOutOfRangeType
    {
        MoveIntoRange = 0,
        DoNothing = 1,
        ActivateInDirection = 2,
        MoveIfTargetingMOB = 3,
        ActivateComboMovementPower = 4,
    }

    [AssetEnum((int)None)]
    public enum ActivationType
    {
        None = 0,
        Passive = 1,
        Instant = 2,
        InstantTargeted = 3,
        TwoStageTargeted = 4,
    }

    [AssetEnum((int)None)]
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

    [AssetEnum((int)AllowProcChanceMultiplier)]
    public enum ProcChanceMultiplierBehaviorType
    {
        AllowProcChanceMultiplier = 0,
        IgnoreProcChanceMultiplier = 1,
        IgnoreProcChanceMultiplierUnlessZero = 2,
    }

    [AssetEnum((int)None)]
    public enum TeleportMethodType
    {
        None = 0,
        Teleport = 1,
        Phase = 2,
    }

    [AssetEnum((int)None)]
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

    [AssetEnum((int)None)]
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

    [AssetEnum((int)Set)]
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

    [AssetEnum((int)None)]
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

    [AssetEnum((int)_0)]
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

    [AssetEnum((int)Alive)]
    public enum EntityHealthState
    {
        Alive = 0,
        Dead = 1,
        AliveOrDead = 2,
    }

    [AssetEnum((int)All)]
    public enum TargetingHeightType
    {
        All = 0,
        GroundOnly = 1,
        SameHeight = 2,
        FlyingOnly = 3,
    }

    [AssetEnum((int)None)]
    public enum SubsequentActivateType
    {
        None = 0,
        DestroySummonedEntity = 1,
        RepeatActivation = 2,
    }

    [AssetEnum((int)None)]
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
        public ulong Properties { get; protected set; }
        public PowerEventActionPrototype[] ActionsTriggeredOnPowerEvent { get; protected set; }
        public ActivationType Activation { get; protected set; }
        public float AnimationContactTimePercent { get; protected set; }
        public int AnimationTimeMS { get; protected set; }
        [ListMixin(typeof(ConditionPrototype))]
        public List<PrototypeMixinListItem> AppliesConditions { get; protected set; }
        public bool CancelConditionsOnEnd { get; protected set; }
        public bool CancelledOnDamage { get; protected set; }
        public bool CancelledOnMove { get; protected set; }
        public bool CanBeDodged { get; protected set; }
        public bool CanCrit { get; protected set; }
        public EvalPrototype ChannelLoopTimeMS { get; protected set; }
        public int ChargingTimeMS { get; protected set; }
        [ListMixin(typeof(ConditionEffectPrototype))]
        public List<PrototypeMixinListItem> ConditionEffects { get; protected set; }
        public EvalPrototype CooldownTimeMS { get; protected set; }
        public DesignWorkflowState DesignState { get; protected set; }
        public ulong DisplayName { get; protected set; }
        public ulong IconPath { get; protected set; }
        public bool IsToggled { get; protected set; }
        public PowerCategoryType PowerCategory { get; protected set; }
        public ulong PowerUnrealClass { get; protected set; }
        public EvalPrototype ProjectileSpeed { get; protected set; }
        public float Radius { get; protected set; }
        public bool RemovedOnUse { get; protected set; }
        public StackingBehaviorPrototype StackingBehaviorLEGACY { get; protected set; }
        public bool MovementStopOnActivate { get; protected set; }
        public ulong TargetingReach { get; protected set; }
        public ulong TargetingStyle { get; protected set; }
        public bool UsableByAll { get; protected set; }
        public bool HideFloatingNumbers { get; protected set; }
        public int PostContactDelayMS { get; protected set; }
        public ulong[] Keywords { get; protected set; }
        public bool CancelConditionsOnUnassign { get; protected set; }
        public float HeightCheckPadding { get; protected set; }
        public bool FlyingUsable { get; protected set; }
        public ExtraActivatePrototype ExtraActivation { get; protected set; }
        public bool CancelledOnButtonRelease { get; protected set; }
        public PowerUnrealOverridePrototype[] PowerUnrealOverrides { get; protected set; }
        public bool CanBeInterrupted { get; protected set; }
        public int ChannelStartTimeMS { get; protected set; }
        public int ChannelEndTimeMS { get; protected set; }
        public bool ForceNonExclusive { get; protected set; }
        public WhenOutOfRangeType WhenOutOfRange { get; protected set; }
        public int NoInterruptPreWindowMS { get; protected set; }
        public int NoInterruptPostWindowMS { get; protected set; }
        public ulong TooltipDescriptionText { get; protected set; }
        public float ProjectileTimeToImpactOverride { get; protected set; }
        public AbilitySlotRestrictionPrototype SlotRestriction { get; protected set; }
        public bool ActiveUntilCancelled { get; protected set; }
        public PowerTooltipEntryPrototype[] TooltipInfoCurrentRank { get; protected set; }
        public PowerTooltipEntryPrototype[] TooltipInfoNextRank { get; protected set; }
        public bool StopsContinuousIfTargetMissing { get; protected set; }
        public bool ResetTargetPositionAtContactTime { get; protected set; }
        public float RangeMinimum { get; protected set; }
        public EvalPrototype Range { get; protected set; }
        public int ChannelMinTimeMS { get; protected set; }
        public int MaxAOETargets { get; protected set; }
        public EvalPrototype[] EvalOnActivate { get; protected set; }
        public EvalPrototype[] EvalOnCreate { get; protected set; }
        public bool CooldownOnPlayer { get; protected set; }
        public bool DisableEnduranceRegenOnEnd { get; protected set; }
        public PowerSynergyTooltipEntryPrototype[] TooltipPowerSynergyBonuses { get; protected set; }
        public SituationalPowerComponentPrototype SituationalComponent { get; protected set; }
        public bool DisableEnduranceRegenOnActivate { get; protected set; }
        public EvalPrototype[] EvalOnPreApply { get; protected set; }
        public int RecurringCostIntervalMS { get; protected set; }
        public ulong[] ConditionsByRef { get; protected set; }   // VectorPrototypeRefPtr ConditionPrototype 
        public bool IsRecurring { get; protected set; }
        public EvalPrototype EvalCanTrigger { get; protected set; }
        public float RangeActivationReduction { get; protected set; }
        public EvalPrototype EvalPowerSynergies { get; protected set; }
        public bool DisableContinuous { get; protected set; }
        public bool CooldownDisableUI { get; protected set; }
        public bool DOTIsDirectionalToCaster { get; protected set; }
        public bool OmniDurationBonusExclude { get; protected set; }
        public ulong ToggleGroup { get; protected set; }
        public bool IsUltimate { get; protected set; }
        public bool PlayNotifySfxOnAvailable { get; protected set; }
        public ulong BounceDamagePctToSameIdCurve { get; protected set; }
        public ulong[] RefreshDependentPassivePowers { get; protected set; }
        public EvalPrototype TargetRestrictionEval { get; protected set; }
        public bool IsUseableWhileDead { get; protected set; }
        public float OnHitProcChanceMultiplier { get; protected set; }
        public bool ApplyResultsImmediately { get; protected set; }
        public bool AllowHitReactOnClient { get; protected set; }
        public bool CanCauseHitReact { get; protected set; }
        public ProcChanceMultiplierBehaviorType ProcChanceMultiplierBehavior { get; protected set; }
        public bool IsSignature { get; protected set; }
        public ulong TooltipCharacterSelectScreen { get; protected set; }
        public ulong CharacterSelectDescription { get; protected set; }
        public bool CooldownIsPersistentToDatabase { get; protected set; }
        public float DamageTuningArea { get; protected set; }
        public float DamageTuningBuff1 { get; protected set; }
        public float DamageTuningBuff2 { get; protected set; }
        public float DamageTuningBuff3 { get; protected set; }
        public float DamageTuningCooldown { get; protected set; }
        public float DamageTuningDebuff1 { get; protected set; }
        public float DamageTuningDebuff2 { get; protected set; }
        public float DamageTuningDebuff3 { get; protected set; }
        public float DamageTuningDmgBonusFreq { get; protected set; }
        public float DamageTuningDoTHotspot { get; protected set; }
        public float DamageTuningHardCC { get; protected set; }
        public float DamageTuningMultiHit { get; protected set; }
        public float DamageTuningAnimationDelay { get; protected set; }
        public float DamageTuningPowerTag1 { get; protected set; }
        public float DamageTuningPowerTag2 { get; protected set; }
        public float DamageTuningPowerTag3 { get; protected set; }
        public float DamageTuningRangeRisk { get; protected set; }
        public float DamageTuningSoftCC { get; protected set; }
        public float DamageTuningSummon { get; protected set; }
        public float DamageTuningDuration { get; protected set; }
        public float DamageTuningTriggerDelay { get; protected set; }
        public bool CanBeBlocked { get; protected set; }
        public PowerTooltipEntryPrototype[] TooltipInfoAntirequisiteLockout { get; protected set; }
        public bool CancelledOnTargetKilled { get; protected set; }
        public bool ProjectileReturnsToUser { get; protected set; }
        public bool CanCauseTag { get; protected set; }
        public ulong[] TooltipPowerReferences { get; protected set; }
        public bool BreaksStealth { get; protected set; }
        public ulong HUDMessage { get; protected set; }
        public bool CancelConditionsOnExitWorld { get; protected set; }
        public int TooltipWidthOverride { get; protected set; }
        public bool ResetUserPositionAtContactTime { get; protected set; }
        public bool MovementOrientToTargetOnActivate { get; protected set; }
        public bool MovementPreventWhileActive { get; protected set; }
        public float DamageBaseTuningEnduranceCost { get; protected set; }
        public int DamageBaseTuningAnimTimeMS { get; protected set; }
        public float DamageTuningHeroSpecific { get; protected set; }
        public bool MovementPreventChannelEnd { get; protected set; }
        public bool MovementPreventChannelLoop { get; protected set; }
        public bool MovementPreventChannelStart { get; protected set; }
        public ulong CharacterSelectYouTubeVideoID { get; protected set; }
        public float DamageBaseTuningEnduranceRatio { get; protected set; }
        public ulong CharacterSelectIconPath { get; protected set; }
        public ManaType[] DisableEnduranceRegenTypes { get; protected set; }
        public bool CanCauseCancelOnDamage { get; protected set; }
        public ulong IconPathHiRes { get; protected set; }
        public bool PrefetchAsset { get; protected set; }
        public bool IsTravelPower { get; protected set; }
        public ulong GamepadSettings { get; protected set; }
        public EvalPrototype BreaksStealthOverrideEval { get; protected set; }
    }

    public class MovementPowerPrototype : PowerPrototype
    {
        public bool MoveToExactTargetLocation { get; protected set; }
        public bool NoCollideIncludesTarget { get; protected set; }
        public bool MoveToOppositeEdgeOfTarget { get; protected set; }
        public bool ConstantMoveTime { get; protected set; }
        public float AdditionalTargetPosOffset { get; protected set; }
        public bool MoveToSecondaryTarget { get; protected set; }
        public bool MoveFullDistance { get; protected set; }
        public bool IsTeleportDEPRECATED { get; protected set; }
        public float MoveMinDistance { get; protected set; }
        public bool UserNoEntityCollide { get; protected set; }
        public bool AllowOrientationChange { get; protected set; }
        public float PowerMovementPathPct { get; protected set; }
        public int MovementHeightBonus { get; protected set; }
        public bool FollowsMouseWhileActive { get; protected set; }
        public EvalPrototype EvalUserMoveSpeed { get; protected set; }
        public bool ChanneledMoveTime { get; protected set; }
        public MovementBehaviorPrototype CustomBehavior { get; protected set; }
        public bool IgnoreTeleportBlockers { get; protected set; }
        public bool HighFlying { get; protected set; }
        public TeleportMethodType TeleportMethod { get; protected set; }
    }

    public class SpecializationPowerPrototype : PowerPrototype
    {
        public ulong MasterPowerDEPRECATED { get; protected set; }
        public EvalPrototype[] EvalCanEnable { get; protected set; }
    }

    public class StealablePowerInfoPrototype : Prototype
    {
        public ulong Power { get; protected set; }
        public ulong StealablePowerDescription { get; protected set; }
    }

    public class StolenPowerRestrictionPrototype : Prototype
    {
        public ulong RestrictionKeyword { get; protected set; }
        public int RestrictionKeywordCount { get; protected set; }
        public ulong RestrictionBannerMessage { get; protected set; }
    }

    public class PowerEventContextTransformModePrototype : PowerEventContextPrototype
    {
        public ulong TransformMode { get; protected set; }
    }

    public class PowerEventContextShowBannerMessagePrototype : PowerEventContextPrototype
    {
        public ulong BannerMessage { get; protected set; }
        public bool SendToPrimaryTarget { get; protected set; }
    }

    public class PowerEventContextLootTablePrototype : PowerEventContextPrototype
    {
        public ulong LootTable { get; protected set; }
        public bool UseItemLevelForLootRoll { get; protected set; }
        public bool IncludeNearbyAvatars { get; protected set; }
        public bool PlaceLootInGeneralInventory { get; protected set; }
    }

    public class PowerEventContextTeleportRegionPrototype : PowerEventContextPrototype
    {
        public ulong Destination { get; protected set; }
    }

    public class PowerEventContextPetDonateItemPrototype : PowerEventContextPrototype
    {
        public float Radius { get; protected set; }
        public ulong RarityThreshold { get; protected set; }
    }

    public class PowerEventContextCooldownChangePrototype : PowerEventContextPrototype
    {
        public bool TargetsOwner { get; protected set; }
    }

    public class PowerToggleGroupPrototype : Prototype
    {
    }

    public class PowerEventContextPrototype : Prototype
    {
    }

    public class PowerEventContextOffsetActivationAOEPrototype : PowerEventContextPrototype
    {
        public float PositionOffsetMagnitude { get; protected set; }
        public float RotationOffsetDegrees { get; protected set; }
        public bool UseIncomingTargetPosAsUserPos { get; protected set; }
    }

    public class AbilityAssignmentPrototype : Prototype
    {
        public ulong Ability { get; protected set; }
    }

    public class AbilityAutoAssignmentSlotPrototype : Prototype
    {
        public ulong Ability { get; protected set; }
        public ulong Slot { get; protected set; }
    }

    public class PowerEventContextCallbackPrototype : PowerEventContextPrototype
    {
        public bool SetContextOnOwnerAgent { get; protected set; }
        public bool SetContextOnOwnerSummonEntities { get; protected set; }
        public bool SummonedEntitiesUsePowerTarget { get; protected set; }
        public bool SetContextOnTargetEntity { get; protected set; }
    }

    public class MapPowerPrototype : Prototype
    {
        public ulong OriginalPower { get; protected set; }
        public ulong MappedPower { get; protected set; }
    }

    public class PowerEventContextMapPowersPrototype : PowerEventContextPrototype
    {
        public MapPowerPrototype[] MappedPowers { get; protected set; }
    }

    public class PowerEventContextUnassignMappedPowersPrototype : PowerEventContextPrototype
    {
        public MapPowerPrototype[] MappedPowersToUnassign { get; protected set; }
    }

    public class AbilitySlotRestrictionPrototype : Prototype
    {
        public bool ActionKeySlotOK { get; protected set; }
        public bool LeftMouseSlotOK { get; protected set; }
        public bool RightMouseSlotOK { get; protected set; }
    }

    public class PowerEventActionPrototype : Prototype
    {
        public PowerEventActionType EventAction { get; protected set; }
        public float EventParam { get; protected set; }
        public ulong Power { get; protected set; }
        public PowerEventType PowerEvent { get; protected set; }
        public PowerEventContextPrototype PowerEventContext { get; protected set; }
        public ulong[] Keywords { get; protected set; }
        public bool UseTriggerPowerOriginalTargetPos { get; protected set; }
        public bool UseTriggeringPowerTargetVerbatim { get; protected set; }
        public EvalPrototype EvalEventTriggerChance { get; protected set; }
        public EvalPrototype EvalEventParam { get; protected set; }
        public bool ResetFXRandomSeed { get; protected set; }
    }

    public class SituationalTriggerPrototype : Prototype
    {
        public ulong TriggerCollider { get; protected set; }
        public float TriggerRadiusScaling { get; protected set; }
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public bool AllowDead { get; protected set; }
        public bool ActivateOnTriggerSuccess { get; protected set; }
    }

    public class SituationalTriggerOnKilledPrototype : SituationalTriggerPrototype
    {
        public bool Friendly { get; protected set; }
        public bool Hostile { get; protected set; }
        public bool KilledByOther { get; protected set; }
        public bool KilledBySelf { get; protected set; }
        public bool WasLastInRange { get; protected set; }
    }

    public class SituationalTriggerOnHealthThresholdPrototype : SituationalTriggerPrototype
    {
        public bool HealthBelow { get; protected set; }
        public float HealthPercent { get; protected set; }
    }

    public class SituationalTriggerOnStatusEffectPrototype : SituationalTriggerPrototype
    {
        public ulong[] TriggeringProperties { get; protected set; }
        public bool TriggersOnStatusApplied { get; protected set; }
        public ulong[] TriggeringConditionKeywords { get; protected set; }
    }

    public class SituationalTriggerInvAndWorldPrototype : SituationalTriggerPrototype
    {
        public ulong InventoryRef { get; protected set; }
    }

    public class SituationalPowerComponentPrototype : Prototype
    {
        public int ActivationWindowMS { get; protected set; }
        public EvalPrototype ChanceToTrigger { get; protected set; }
        public bool ForceRelockOnTriggerRevert { get; protected set; }
        public bool RemoveTriggeringEntityOnActivate { get; protected set; }
        public SituationalTriggerPrototype SituationalTrigger { get; protected set; }
        public bool TargetsTriggeringEntity { get; protected set; }
        public bool ForceRelockOnActivate { get; protected set; }
    }

    public class PowerUnrealReplacementPrototype : Prototype
    {
        public ulong EntityArt { get; protected set; }
        public ulong PowerArt { get; protected set; }
        public float AnimationContactTimePercent { get; protected set; }
        public int AnimationTimeMS { get; protected set; }
    }

    public class PowerUnrealOverridePrototype : Prototype
    {
        public float AnimationContactTimePercent { get; protected set; }
        public int AnimationTimeMS { get; protected set; }
        public ulong EntityArt { get; protected set; }
        public ulong PowerArt { get; protected set; }
        public PowerUnrealReplacementPrototype[] ArtOnlyReplacements { get; protected set; }
    }

    public class PowerSynergyTooltipEntryPrototype : Prototype
    {
        public ulong SynergyPower { get; protected set; }
        public ulong Translation { get; protected set; }
    }

    public class PowerEventContextCallbackAIChangeBlackboardPropertyPrototype : PowerEventContextCallbackPrototype
    {
        public BlackboardOperatorType Operation { get; protected set; }
        public ulong PropertyInfoRef { get; protected set; }
        public int Value { get; protected set; }
        public bool UseTargetEntityId { get; protected set; }
    }

    public class PowerEventContextCallbackAISetAssistedEntityFromCreatorPrototype : PowerEventContextCallbackPrototype
    {
    }

    public class PowerEventContextCallbackAISummonsTryActivatePowerPrototype : PowerEventContextCallbackPrototype
    {
        public ulong PowerToActivate { get; protected set; }
        public bool SummonsUsePowerTargetLocation { get; protected set; }
        public ulong SummonsKeywordFilter { get; protected set; }
    }

    public class TransformModeUnrealOverridePrototype : Prototype
    {
        public ulong IncomingUnrealClass { get; protected set; }
        public ulong TransformedUnrealClass { get; protected set; }
    }

    public class TransformModePrototype : Prototype
    {
        public AbilityAssignmentPrototype[] DefaultEquippedAbilities { get; protected set; }
        public ulong EnterTransformModePower { get; protected set; }
        public ulong ExitTransformModePower { get; protected set; }
        public ulong UnrealClass { get; protected set; }
        public ulong[] HiddenPassivePowers { get; protected set; }
        public bool PowersAreSlottable { get; protected set; }
        public EvalPrototype DurationMSEval { get; protected set; }
        public TransformModeUnrealOverridePrototype[] UnrealClassOverrides { get; protected set; }
        public ulong UseRankOfPower { get; protected set; }
    }

    public class TransformModeEntryPrototype : Prototype
    {
        public ulong[] AllowedPowers { get; protected set; }
        public ulong TransformMode { get; protected set; }
    }

    public class GamepadSettingsPrototype : Prototype
    {
        public bool ClearContinuousInitialTarget { get; protected set; }
        public float Range { get; protected set; }
        public bool TeleportToTarget { get; protected set; }
        public bool MeleeMoveIntoRange { get; protected set; }
        public bool ChannelPowerOrientToEnemy { get; protected set; }
    }

    public class TargetingStylePrototype : Prototype
    {
        public AOEAngleType AOEAngle { get; protected set; }
        public bool AOESelfCentered { get; protected set; }
        public bool NeedsTarget { get; protected set; }
        public bool OffsetWedgeBehindUser { get; protected set; }
        public float OrientationOffset { get; protected set; }
        public TargetingShapeType TargetingShape { get; protected set; }
        public bool TurnsToFaceTarget { get; protected set; }
        public float Width { get; protected set; }
        public bool AlwaysTargetMousePos { get; protected set; }
        public bool MovesToRangeOfPrimaryTarget { get; protected set; }
        public bool UseDefaultRotationSpeed { get; protected set; }
        public Vector2Prototype PositionOffset { get; protected set; }
        public int RandomPositionRadius { get; protected set; }
        public bool DisableOrientationDuringPower { get; protected set; }
    }

    public class TargetingReachPrototype : Prototype
    {
        public bool ExcludesPrimaryTarget { get; protected set; }
        public bool Melee { get; protected set; }
        public bool RequiresLineOfSight { get; protected set; }
        public bool TargetsEnemy { get; protected set; }
        public bool TargetsFlying { get; protected set; }
        public bool TargetsFriendly { get; protected set; }
        public bool TargetsGround { get; protected set; }
        public bool WillTargetCaster { get; protected set; }
        public bool LowestHealth { get; protected set; }
        public TargetingHeightType TargetingHeightType { get; protected set; }
        public bool PartyOnly { get; protected set; }
        public bool WillTargetCreator { get; protected set; }
        public bool TargetsDestructibles { get; protected set; }
        public bool LOSCheckAlongGround { get; protected set; }
        public EntityHealthState EntityHealthState { get; protected set; }
        public ConvenienceLabel TargetsEntitiesInInventory { get; protected set; }
        public bool TargetsFrontSideOnly { get; protected set; }
        public bool TargetsNonEnemies { get; protected set; }
        public bool WillTargetUltimateCreator { get; protected set; }
        public bool RandomAOETargets { get; protected set; }
    }

    public class ExtraActivatePrototype : Prototype
    {
    }

    public class SecondaryActivateOnReleasePrototype : ExtraActivatePrototype
    {
        public ulong DamageIncreasePerSecond { get; protected set; }
        public DamageType DamageIncreaseType { get; protected set; }
        public ulong EnduranceCostIncreasePerSecond { get; protected set; }
        public int MaxReleaseTimeMS { get; protected set; }
        public int MinReleaseTimeMS { get; protected set; }
        public ulong RangeIncreasePerSecond { get; protected set; }
        public ulong RadiusIncreasePerSecond { get; protected set; }
        public bool ActivateOnMaxReleaseTime { get; protected set; }
        public ulong DefensePenetrationIncrPerSec { get; protected set; }
        public DamageType DefensePenetrationType { get; protected set; }
        public bool FollowsMouseUntilRelease { get; protected set; }
        public ManaType EnduranceCostManaType { get; protected set; }
    }

    public class ExtraActivateOnSubsequentPrototype : ExtraActivatePrototype
    {
        public ulong NumActivatesBeforeCooldown { get; protected set; }
        public ulong TimeoutLengthMS { get; protected set; }
        public SubsequentActivateType ExtraActivateEffect { get; protected set; }
    }

    public class ExtraActivateCycleToPowerPrototype : ExtraActivatePrototype
    {
        public ulong[] CyclePowerList { get; protected set; }
    }
}
