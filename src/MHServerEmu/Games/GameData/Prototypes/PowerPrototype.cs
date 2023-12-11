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

    #endregion

    public class PowerPrototype : Prototype
    {
        public ulong Properties { get; set; }
        public PowerEventActionPrototype[] ActionsTriggeredOnPowerEvent { get; set; }
        public ActivationType Activation { get; set; }
        public float AnimationContactTimePercent { get; set; }
        public int AnimationTimeMS { get; set; }
        public ConditionPrototype AppliesConditions { get; set; }
        public bool CancelConditionsOnEnd { get; set; }
        public bool CancelledOnDamage { get; set; }
        public bool CancelledOnMove { get; set; }
        public bool CanBeDodged { get; set; }
        public bool CanCrit { get; set; }
        public EvalPrototype ChannelLoopTimeMS { get; set; }
        public int ChargingTimeMS { get; set; }
        public ConditionEffectPrototype ConditionEffects { get; set; }
        public EvalPrototype CooldownTimeMS { get; set; }
        public DesignWorkflowState DesignState { get; set; }
        public ulong DisplayName { get; set; }
        public ulong IconPath { get; set; }
        public bool IsToggled { get; set; }
        public PowerCategoryType PowerCategory { get; set; }
        public ulong PowerUnrealClass { get; set; }
        public EvalPrototype ProjectileSpeed { get; set; }
        public float Radius { get; set; }
        public bool RemovedOnUse { get; set; }
        public StackingBehaviorPrototype StackingBehaviorLEGACY { get; set; }
        public bool MovementStopOnActivate { get; set; }
        public ulong TargetingReach { get; set; }
        public ulong TargetingStyle { get; set; }
        public bool UsableByAll { get; set; }
        public bool HideFloatingNumbers { get; set; }
        public int PostContactDelayMS { get; set; }
        public ulong[] Keywords { get; set; }
        public bool CancelConditionsOnUnassign { get; set; }
        public float HeightCheckPadding { get; set; }
        public bool FlyingUsable { get; set; }
        public ExtraActivatePrototype ExtraActivation { get; set; }
        public bool CancelledOnButtonRelease { get; set; }
        public PowerUnrealOverridePrototype[] PowerUnrealOverrides { get; set; }
        public bool CanBeInterrupted { get; set; }
        public int ChannelStartTimeMS { get; set; }
        public int ChannelEndTimeMS { get; set; }
        public bool ForceNonExclusive { get; set; }
        public WhenOutOfRangeType WhenOutOfRange { get; set; }
        public int NoInterruptPreWindowMS { get; set; }
        public int NoInterruptPostWindowMS { get; set; }
        public ulong TooltipDescriptionText { get; set; }
        public float ProjectileTimeToImpactOverride { get; set; }
        public AbilitySlotRestrictionPrototype SlotRestriction { get; set; }
        public bool ActiveUntilCancelled { get; set; }
        public PowerTooltipEntryPrototype[] TooltipInfoCurrentRank { get; set; }
        public PowerTooltipEntryPrototype[] TooltipInfoNextRank { get; set; }
        public bool StopsContinuousIfTargetMissing { get; set; }
        public bool ResetTargetPositionAtContactTime { get; set; }
        public float RangeMinimum { get; set; }
        public EvalPrototype Range { get; set; }
        public int ChannelMinTimeMS { get; set; }
        public int MaxAOETargets { get; set; }
        public EvalPrototype[] EvalOnActivate { get; set; }
        public EvalPrototype[] EvalOnCreate { get; set; }
        public bool CooldownOnPlayer { get; set; }
        public bool DisableEnduranceRegenOnEnd { get; set; }
        public PowerSynergyTooltipEntryPrototype[] TooltipPowerSynergyBonuses { get; set; }
        public SituationalPowerComponentPrototype SituationalComponent { get; set; }
        public bool DisableEnduranceRegenOnActivate { get; set; }
        public EvalPrototype[] EvalOnPreApply { get; set; }
        public int RecurringCostIntervalMS { get; set; }
        public ConditionPrototype ConditionsByRef { get; set; }
        public bool IsRecurring { get; set; }
        public EvalPrototype EvalCanTrigger { get; set; }
        public float RangeActivationReduction { get; set; }
        public EvalPrototype EvalPowerSynergies { get; set; }
        public bool DisableContinuous { get; set; }
        public bool CooldownDisableUI { get; set; }
        public bool DOTIsDirectionalToCaster { get; set; }
        public bool OmniDurationBonusExclude { get; set; }
        public ulong ToggleGroup { get; set; }
        public bool IsUltimate { get; set; }
        public bool PlayNotifySfxOnAvailable { get; set; }
        public ulong BounceDamagePctToSameIdCurve { get; set; }
        public ulong[] RefreshDependentPassivePowers { get; set; }
        public EvalPrototype TargetRestrictionEval { get; set; }
        public bool IsUseableWhileDead { get; set; }
        public float OnHitProcChanceMultiplier { get; set; }
        public bool ApplyResultsImmediately { get; set; }
        public bool AllowHitReactOnClient { get; set; }
        public bool CanCauseHitReact { get; set; }
        public ProcChanceMultiplierBehaviorType ProcChanceMultiplierBehavior { get; set; }
        public bool IsSignature { get; set; }
        public ulong TooltipCharacterSelectScreen { get; set; }
        public ulong CharacterSelectDescription { get; set; }
        public bool CooldownIsPersistentToDatabase { get; set; }
        public float DamageTuningArea { get; set; }
        public float DamageTuningBuff1 { get; set; }
        public float DamageTuningBuff2 { get; set; }
        public float DamageTuningBuff3 { get; set; }
        public float DamageTuningCooldown { get; set; }
        public float DamageTuningDebuff1 { get; set; }
        public float DamageTuningDebuff2 { get; set; }
        public float DamageTuningDebuff3 { get; set; }
        public float DamageTuningDmgBonusFreq { get; set; }
        public float DamageTuningDoTHotspot { get; set; }
        public float DamageTuningHardCC { get; set; }
        public float DamageTuningMultiHit { get; set; }
        public float DamageTuningAnimationDelay { get; set; }
        public float DamageTuningPowerTag1 { get; set; }
        public float DamageTuningPowerTag2 { get; set; }
        public float DamageTuningPowerTag3 { get; set; }
        public float DamageTuningRangeRisk { get; set; }
        public float DamageTuningSoftCC { get; set; }
        public float DamageTuningSummon { get; set; }
        public float DamageTuningDuration { get; set; }
        public float DamageTuningTriggerDelay { get; set; }
        public bool CanBeBlocked { get; set; }
        public PowerTooltipEntryPrototype[] TooltipInfoAntirequisiteLockout { get; set; }
        public bool CancelledOnTargetKilled { get; set; }
        public bool ProjectileReturnsToUser { get; set; }
        public bool CanCauseTag { get; set; }
        public ulong[] TooltipPowerReferences { get; set; }
        public bool BreaksStealth { get; set; }
        public ulong HUDMessage { get; set; }
        public bool CancelConditionsOnExitWorld { get; set; }
        public int TooltipWidthOverride { get; set; }
        public bool ResetUserPositionAtContactTime { get; set; }
        public bool MovementOrientToTargetOnActivate { get; set; }
        public bool MovementPreventWhileActive { get; set; }
        public float DamageBaseTuningEnduranceCost { get; set; }
        public int DamageBaseTuningAnimTimeMS { get; set; }
        public float DamageTuningHeroSpecific { get; set; }
        public bool MovementPreventChannelEnd { get; set; }
        public bool MovementPreventChannelLoop { get; set; }
        public bool MovementPreventChannelStart { get; set; }
        public ulong CharacterSelectYouTubeVideoID { get; set; }
        public float DamageBaseTuningEnduranceRatio { get; set; }
        public ulong CharacterSelectIconPath { get; set; }
        public ManaType[] DisableEnduranceRegenTypes { get; set; }
        public bool CanCauseCancelOnDamage { get; set; }
        public ulong IconPathHiRes { get; set; }
        public bool PrefetchAsset { get; set; }
        public bool IsTravelPower { get; set; }
        public ulong GamepadSettings { get; set; }
        public EvalPrototype BreaksStealthOverrideEval { get; set; }
    }

    public class MovementPowerPrototype : PowerPrototype
    {
        public bool MoveToExactTargetLocation { get; set; }
        public bool NoCollideIncludesTarget { get; set; }
        public bool MoveToOppositeEdgeOfTarget { get; set; }
        public bool ConstantMoveTime { get; set; }
        public float AdditionalTargetPosOffset { get; set; }
        public bool MoveToSecondaryTarget { get; set; }
        public bool MoveFullDistance { get; set; }
        public bool IsTeleportDEPRECATED { get; set; }
        public float MoveMinDistance { get; set; }
        public bool UserNoEntityCollide { get; set; }
        public bool AllowOrientationChange { get; set; }
        public float PowerMovementPathPct { get; set; }
        public int MovementHeightBonus { get; set; }
        public bool FollowsMouseWhileActive { get; set; }
        public EvalPrototype EvalUserMoveSpeed { get; set; }
        public bool ChanneledMoveTime { get; set; }
        public MovementBehaviorPrototype CustomBehavior { get; set; }
        public bool IgnoreTeleportBlockers { get; set; }
        public bool HighFlying { get; set; }
        public TeleportMethodType TeleportMethod { get; set; }
    }

    public class SpecializationPowerPrototype : PowerPrototype
    {
        public ulong MasterPowerDEPRECATED { get; set; }
        public EvalPrototype[] EvalCanEnable { get; set; }
    }

    public class StealablePowerInfoPrototype : Prototype
    {
        public ulong Power { get; set; }
        public ulong StealablePowerDescription { get; set; }
    }

    public class StolenPowerRestrictionPrototype : Prototype
    {
        public ulong RestrictionKeyword { get; set; }
        public int RestrictionKeywordCount { get; set; }
        public ulong RestrictionBannerMessage { get; set; }
    }

    public class PowerEventContextTransformModePrototype : PowerEventContextPrototype
    {
        public ulong TransformMode { get; set; }
    }

    public class PowerEventContextShowBannerMessagePrototype : PowerEventContextPrototype
    {
        public ulong BannerMessage { get; set; }
        public bool SendToPrimaryTarget { get; set; }
    }

    public class PowerEventContextLootTablePrototype : PowerEventContextPrototype
    {
        public ulong LootTable { get; set; }
        public bool UseItemLevelForLootRoll { get; set; }
        public bool IncludeNearbyAvatars { get; set; }
        public bool PlaceLootInGeneralInventory { get; set; }
    }

    public class PowerEventContextTeleportRegionPrototype : PowerEventContextPrototype
    {
        public ulong Destination { get; set; }
    }

    public class PowerEventContextPetDonateItemPrototype : PowerEventContextPrototype
    {
        public float Radius { get; set; }
        public ulong RarityThreshold { get; set; }
    }

    public class PowerEventContextCooldownChangePrototype : PowerEventContextPrototype
    {
        public bool TargetsOwner { get; set; }
    }

    public class PowerToggleGroupPrototype : Prototype
    {
    }

    public class PowerEventContextPrototype : Prototype
    {
    }

    public class PowerEventContextOffsetActivationAOEPrototype : PowerEventContextPrototype
    {
        public float PositionOffsetMagnitude { get; set; }
        public float RotationOffsetDegrees { get; set; }
        public bool UseIncomingTargetPosAsUserPos { get; set; }
    }

    public class AbilityAssignmentPrototype : Prototype
    {
        public ulong Ability { get; set; }
    }

    public class AbilityAutoAssignmentSlotPrototype : Prototype
    {
        public ulong Ability { get; set; }
        public ulong Slot { get; set; }
    }

    public class PowerEventContextCallbackPrototype : PowerEventContextPrototype
    {
        public bool SetContextOnOwnerAgent { get; set; }
        public bool SetContextOnOwnerSummonEntities { get; set; }
        public bool SummonedEntitiesUsePowerTarget { get; set; }
        public bool SetContextOnTargetEntity { get; set; }
    }

    public class MapPowerPrototype : Prototype
    {
        public ulong OriginalPower { get; set; }
        public ulong MappedPower { get; set; }
    }

    public class PowerEventContextMapPowersPrototype : PowerEventContextPrototype
    {
        public MapPowerPrototype[] MappedPowers { get; set; }
    }

    public class PowerEventContextUnassignMappedPowersPrototype : PowerEventContextPrototype
    {
        public MapPowerPrototype[] MappedPowersToUnassign { get; set; }
    }

    public class AbilitySlotRestrictionPrototype : Prototype
    {
        public bool ActionKeySlotOK { get; set; }
        public bool LeftMouseSlotOK { get; set; }
        public bool RightMouseSlotOK { get; set; }
    }

    public class PowerEventActionPrototype : Prototype
    {
        public PowerEventActionType EventAction { get; set; }
        public float EventParam { get; set; }
        public ulong Power { get; set; }
        public PowerEventType PowerEvent { get; set; }
        public PowerEventContextPrototype PowerEventContext { get; set; }
        public ulong[] Keywords { get; set; }
        public bool UseTriggerPowerOriginalTargetPos { get; set; }
        public bool UseTriggeringPowerTargetVerbatim { get; set; }
        public EvalPrototype EvalEventTriggerChance { get; set; }
        public EvalPrototype EvalEventParam { get; set; }
        public bool ResetFXRandomSeed { get; set; }
    }

    public class SituationalTriggerPrototype : Prototype
    {
        public ulong TriggerCollider { get; set; }
        public float TriggerRadiusScaling { get; set; }
        public EntityFilterPrototype EntityFilter { get; set; }
        public bool AllowDead { get; set; }
        public bool ActivateOnTriggerSuccess { get; set; }
    }

    public class SituationalTriggerOnKilledPrototype : SituationalTriggerPrototype
    {
        public bool Friendly { get; set; }
        public bool Hostile { get; set; }
        public bool KilledByOther { get; set; }
        public bool KilledBySelf { get; set; }
        public bool WasLastInRange { get; set; }
    }

    public class SituationalTriggerOnHealthThresholdPrototype : SituationalTriggerPrototype
    {
        public bool HealthBelow { get; set; }
        public float HealthPercent { get; set; }
    }

    public class SituationalTriggerOnStatusEffectPrototype : SituationalTriggerPrototype
    {
        public ulong[] TriggeringProperties { get; set; }
        public bool TriggersOnStatusApplied { get; set; }
        public ulong[] TriggeringConditionKeywords { get; set; }
    }

    public class SituationalTriggerInvAndWorldPrototype : SituationalTriggerPrototype
    {
        public ulong InventoryRef { get; set; }
    }

    public class SituationalPowerComponentPrototype : Prototype
    {
        public int ActivationWindowMS { get; set; }
        public EvalPrototype ChanceToTrigger { get; set; }
        public bool ForceRelockOnTriggerRevert { get; set; }
        public bool RemoveTriggeringEntityOnActivate { get; set; }
        public SituationalTriggerPrototype SituationalTrigger { get; set; }
        public bool TargetsTriggeringEntity { get; set; }
        public bool ForceRelockOnActivate { get; set; }
    }

    public class PowerUnrealReplacementPrototype : Prototype
    {
        public ulong EntityArt { get; set; }
        public ulong PowerArt { get; set; }
        public float AnimationContactTimePercent { get; set; }
        public int AnimationTimeMS { get; set; }
    }

    public class PowerUnrealOverridePrototype : Prototype
    {
        public float AnimationContactTimePercent { get; set; }
        public int AnimationTimeMS { get; set; }
        public ulong EntityArt { get; set; }
        public ulong PowerArt { get; set; }
        public PowerUnrealReplacementPrototype[] ArtOnlyReplacements { get; set; }
    }

    public class PowerSynergyTooltipEntryPrototype : Prototype
    {
        public ulong SynergyPower { get; set; }
        public ulong Translation { get; set; }
    }

    public class PowerEventContextCallbackAIChangeBlackboardPropertyPrototype : PowerEventContextCallbackPrototype
    {
        public BlackboardOperatorType Operation { get; set; }
        public ulong PropertyInfoRef { get; set; }
        public int Value { get; set; }
        public bool UseTargetEntityId { get; set; }
    }

    public class PowerEventContextCallbackAISetAssistedEntityFromCreatorPrototype : PowerEventContextCallbackPrototype
    {
    }

    public class PowerEventContextCallbackAISummonsTryActivatePowerPrototype : PowerEventContextCallbackPrototype
    {
        public ulong PowerToActivate { get; set; }
        public bool SummonsUsePowerTargetLocation { get; set; }
        public ulong SummonsKeywordFilter { get; set; }
    }

    public class TransformModeUnrealOverridePrototype : Prototype
    {
        public ulong IncomingUnrealClass { get; set; }
        public ulong TransformedUnrealClass { get; set; }
    }

    public class TransformModePrototype : Prototype
    {
        public AbilityAssignmentPrototype[] DefaultEquippedAbilities { get; set; }
        public ulong EnterTransformModePower { get; set; }
        public ulong ExitTransformModePower { get; set; }
        public ulong UnrealClass { get; set; }
        public ulong[] HiddenPassivePowers { get; set; }
        public bool PowersAreSlottable { get; set; }
        public EvalPrototype DurationMSEval { get; set; }
        public TransformModeUnrealOverridePrototype[] UnrealClassOverrides { get; set; }
        public ulong UseRankOfPower { get; set; }
    }

    public class TransformModeEntryPrototype : Prototype
    {
        public ulong AllowedPowers { get; set; }
        public ulong TransformMode { get; set; }
    }

    public class GamepadSettingsPrototype : Prototype
    {
        public bool ClearContinuousInitialTarget { get; set; }
        public float Range { get; set; }
        public bool TeleportToTarget { get; set; }
        public bool MeleeMoveIntoRange { get; set; }
        public bool ChannelPowerOrientToEnemy { get; set; }
    }

    public class TargetingStylePrototype : Prototype
    {
        public AOEAngleType AOEAngle { get; set; }
        public bool AOESelfCentered { get; set; }
        public bool NeedsTarget { get; set; }
        public bool OffsetWedgeBehindUser { get; set; }
        public float OrientationOffset { get; set; }
        public TargetingShapeType TargetingShape { get; set; }
        public bool TurnsToFaceTarget { get; set; }
        public float Width { get; set; }
        public bool AlwaysTargetMousePos { get; set; }
        public bool MovesToRangeOfPrimaryTarget { get; set; }
        public bool UseDefaultRotationSpeed { get; set; }
        public Vector2Prototype PositionOffset { get; set; }
        public int RandomPositionRadius { get; set; }
        public bool DisableOrientationDuringPower { get; set; }
    }

    public class TargetingReachPrototype : Prototype
    {
        public bool ExcludesPrimaryTarget { get; set; }
        public bool Melee { get; set; }
        public bool RequiresLineOfSight { get; set; }
        public bool TargetsEnemy { get; set; }
        public bool TargetsFlying { get; set; }
        public bool TargetsFriendly { get; set; }
        public bool TargetsGround { get; set; }
        public bool WillTargetCaster { get; set; }
        public bool LowestHealth { get; set; }
        public TargetingHeightType TargetingHeightType { get; set; }
        public bool PartyOnly { get; set; }
        public bool WillTargetCreator { get; set; }
        public bool TargetsDestructibles { get; set; }
        public bool LOSCheckAlongGround { get; set; }
        public EntityHealthState EntityHealthState { get; set; }
        public ConvenienceLabel TargetsEntitiesInInventory { get; set; }
        public bool TargetsFrontSideOnly { get; set; }
        public bool TargetsNonEnemies { get; set; }
        public bool WillTargetUltimateCreator { get; set; }
        public bool RandomAOETargets { get; set; }
    }

    public class ExtraActivatePrototype : Prototype
    {
    }

    public class SecondaryActivateOnReleasePrototype : ExtraActivatePrototype
    {
        public ulong DamageIncreasePerSecond { get; set; }
        public DamageType DamageIncreaseType { get; set; }
        public ulong EnduranceCostIncreasePerSecond { get; set; }
        public int MaxReleaseTimeMS { get; set; }
        public int MinReleaseTimeMS { get; set; }
        public ulong RangeIncreasePerSecond { get; set; }
        public ulong RadiusIncreasePerSecond { get; set; }
        public bool ActivateOnMaxReleaseTime { get; set; }
        public ulong DefensePenetrationIncrPerSec { get; set; }
        public DamageType DefensePenetrationType { get; set; }
        public bool FollowsMouseUntilRelease { get; set; }
        public ManaType EnduranceCostManaType { get; set; }
    }

    public class ExtraActivateOnSubsequentPrototype : ExtraActivatePrototype
    {
        public ulong NumActivatesBeforeCooldown { get; set; }
        public ulong TimeoutLengthMS { get; set; }
        public SubsequentActivateType ExtraActivateEffect { get; set; }
    }

    public class ExtraActivateCycleToPowerPrototype : ExtraActivatePrototype
    {
        public ulong CyclePowerList { get; set; }
    }
}
