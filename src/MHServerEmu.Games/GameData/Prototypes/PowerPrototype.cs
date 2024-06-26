using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PowerPrototype : Prototype
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        // Local instance refs to speed up access
        private TargetingReachPrototype _targetingReachPtr;
        private TargetingStylePrototype _targetingStylePtr;

        public PrototypePropertyCollection Properties { get; protected set; }
        public PowerEventActionPrototype[] ActionsTriggeredOnPowerEvent { get; protected set; }
        public PowerActivationType Activation { get; protected set; }
        public float AnimationContactTimePercent { get; protected set; }
        public int AnimationTimeMS { get; protected set; }
        [ListMixin(typeof(ConditionPrototype))]
        public PrototypeMixinList AppliesConditions { get; protected set; }
        public bool CancelConditionsOnEnd { get; protected set; }
        public bool CancelledOnDamage { get; protected set; }
        public bool CancelledOnMove { get; protected set; }
        public bool CanBeDodged { get; protected set; }
        public bool CanCrit { get; protected set; }
        public EvalPrototype ChannelLoopTimeMS { get; protected set; }
        public int ChargingTimeMS { get; protected set; }
        [ListMixin(typeof(ConditionEffectPrototype))]
        public PrototypeMixinList ConditionEffects { get; protected set; }
        public EvalPrototype CooldownTimeMS { get; protected set; }
        public DesignWorkflowState DesignState { get; protected set; }
        public LocaleStringId DisplayName { get; protected set; }
        public AssetId IconPath { get; protected set; }
        public bool IsToggled { get; protected set; }
        public PowerCategoryType PowerCategory { get; protected set; }
        public AssetId PowerUnrealClass { get; protected set; }
        public EvalPrototype ProjectileSpeed { get; protected set; }
        public float Radius { get; protected set; }
        public bool RemovedOnUse { get; protected set; }
        public StackingBehaviorPrototype StackingBehaviorLEGACY { get; protected set; }
        public bool MovementStopOnActivate { get; protected set; }
        public PrototypeId TargetingReach { get; protected set; }
        public PrototypeId TargetingStyle { get; protected set; }
        public bool UsableByAll { get; protected set; }
        public bool HideFloatingNumbers { get; protected set; }
        public int PostContactDelayMS { get; protected set; }
        public PrototypeId[] Keywords { get; protected set; }
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
        public LocaleStringId TooltipDescriptionText { get; protected set; }
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
        public PrototypeId[] ConditionsByRef { get; protected set; }   // VectorPrototypeRefPtr ConditionPrototype 
        public bool IsRecurring { get; protected set; }
        public EvalPrototype EvalCanTrigger { get; protected set; }
        public float RangeActivationReduction { get; protected set; }
        public EvalPrototype EvalPowerSynergies { get; protected set; }
        public bool DisableContinuous { get; protected set; }
        public bool CooldownDisableUI { get; protected set; }
        public bool DOTIsDirectionalToCaster { get; protected set; }
        public bool OmniDurationBonusExclude { get; protected set; }
        public PrototypeId ToggleGroup { get; protected set; }
        public bool IsUltimate { get; protected set; }
        public bool PlayNotifySfxOnAvailable { get; protected set; }
        public CurveId BounceDamagePctToSameIdCurve { get; protected set; }
        public PrototypeId[] RefreshDependentPassivePowers { get; protected set; }
        public EvalPrototype TargetRestrictionEval { get; protected set; }
        public bool IsUseableWhileDead { get; protected set; }
        public float OnHitProcChanceMultiplier { get; protected set; }
        public bool ApplyResultsImmediately { get; protected set; }
        public bool AllowHitReactOnClient { get; protected set; }
        public bool CanCauseHitReact { get; protected set; }
        public ProcChanceMultiplierBehaviorType ProcChanceMultiplierBehavior { get; protected set; }
        public bool IsSignature { get; protected set; }
        public PrototypeId TooltipCharacterSelectScreen { get; protected set; }
        public LocaleStringId CharacterSelectDescription { get; protected set; }
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
        public PrototypeId[] TooltipPowerReferences { get; protected set; }
        public bool BreaksStealth { get; protected set; }
        public PrototypeId HUDMessage { get; protected set; }
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
        public LocaleStringId CharacterSelectYouTubeVideoID { get; protected set; }
        public float DamageBaseTuningEnduranceRatio { get; protected set; }
        public AssetId CharacterSelectIconPath { get; protected set; }
        public ManaType[] DisableEnduranceRegenTypes { get; protected set; }
        public bool CanCauseCancelOnDamage { get; protected set; }
        public AssetId IconPathHiRes { get; protected set; }
        public bool PrefetchAsset { get; protected set; }
        public bool IsTravelPower { get; protected set; }
        public PrototypeId GamepadSettings { get; protected set; }
        public EvalPrototype BreaksStealthOverrideEval { get; protected set; }

        [DoNotCopy]
        public KeywordsMask KeywordsMask { get; protected set; }

        [DoNotCopy]
        public TimeSpan ChannelStartTime { get => TimeSpan.FromMilliseconds(ChannelStartTimeMS); }
        [DoNotCopy]
        public TimeSpan ChannelMinTime { get => TimeSpan.FromMilliseconds(ChannelMinTimeMS); }
        [DoNotCopy]
        public TimeSpan ChannelEndTime { get => TimeSpan.FromMilliseconds(ChannelEndTimeMS); }
        [DoNotCopy]
        public TimeSpan ChargeTime { get => TimeSpan.FromMilliseconds(ChargingTimeMS); }

        public static PrototypeId RecursiveGetPowerRefOfPowerTypeInCombo<T>(PrototypeId powerRef) where T : PowerPrototype
        {
            PowerPrototype powerProto = GameDatabase.GetPrototype<PowerPrototype>(powerRef);
            if (powerProto == null) return Logger.WarnReturn(PrototypeId.Invalid, "RecursiveGetPowerRefOfPowerTypeInCombo(): power == null");

            if (powerProto is T)
                return powerRef;

            // for loop here

            return PrototypeId.Invalid;
        }

        public virtual bool IsHighFlyingPower() => false;

        public override bool ApprovedForUse()
        {
            return GameDatabase.DesignStateOk(DesignState);
        }

        public override void PostProcess()
        {
            base.PostProcess();

            // TODO

            KeywordsMask = KeywordPrototype.GetBitMaskForKeywordList(Keywords);

            // TODO 

            // We don't use prototype data ref pointers, so we need to go through the game database
            // to get the prototype for the ref. This can be slow for lookups that happen often, so
            // we cache instance references for often requested prototypes here.
            _targetingReachPtr = TargetingReach.As<TargetingReachPrototype>();
            _targetingStylePtr = TargetingStyle.As<TargetingStylePrototype>();
        }

        public bool HasKeyword(KeywordPrototype keywordProto)
        {
            return (keywordProto != null && KeywordPrototype.TestKeywordBit(KeywordsMask, keywordProto));
        }

        public TargetingReachPrototype GetTargetingReach()
        {
            return _targetingReachPtr;
        }

        public TargetingStylePrototype GetTargetingStyle()
        {
            return _targetingStylePtr;
        }

        public float GetRange(PropertyCollection powerProperties, PropertyCollection ownerProperties)
        {
            if (Range == null) return Logger.WarnReturn(0f, "GetRange(): Range == null");

            EvalContextData contextData = new();
            contextData.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, powerProperties);
            contextData.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, ownerProperties ?? new());

            return Eval.RunFloat(Range, contextData);            
        }

        public TimeSpan GetChannelLoopTime(PropertyCollection powerProperties, PropertyCollection ownerProperties)
        {
            if (ChannelLoopTimeMS == null) return Logger.WarnReturn(TimeSpan.Zero, "GetChannelLoopTime(): ChannelLoopTimeMS == null");

            EvalContextData contextData = new();
            contextData.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, powerProperties);
            contextData.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, ownerProperties);

            int channelLoopTimeMS = Eval.RunInt(ChannelLoopTimeMS, contextData);
            return TimeSpan.FromMilliseconds(channelLoopTimeMS);
        }

        public TimeSpan GetAnimationTime(AssetId originalWorldAssetRef, AssetId entityWorldAssetRef)
        {
            int animationTimeMS = AnimationTimeMS;

            if (PowerUnrealOverrides == null)
                return TimeSpan.FromMilliseconds(animationTimeMS);

            foreach (PowerUnrealOverridePrototype unrealAssetOverrideProto in PowerUnrealOverrides)
            {
                if (unrealAssetOverrideProto.EntityArt != originalWorldAssetRef)
                    continue;

                if (unrealAssetOverrideProto.AnimationTimeMS >= 0)
                    animationTimeMS = unrealAssetOverrideProto.AnimationTimeMS;

                if (unrealAssetOverrideProto.ArtOnlyReplacements == null)
                    continue;

                foreach (PowerUnrealReplacementPrototype replacementProto in unrealAssetOverrideProto.ArtOnlyReplacements)
                {
                    if (replacementProto.EntityArt != entityWorldAssetRef)
                        continue;

                    if (replacementProto.AnimationTimeMS >= 0)
                        animationTimeMS = replacementProto.AnimationTimeMS;
                }
            }

            return TimeSpan.FromMilliseconds(animationTimeMS);
        }

        public TimeSpan GetCooldownDuration(PropertyCollection powerProperties, PropertyCollection ownerProperties)
        {
            if (CooldownTimeMS == null) return Logger.WarnReturn(TimeSpan.Zero, "GetCooldownDuration(): CooldownTimeMS == null");

            EvalContextData contextData = new();
            contextData.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, powerProperties);
            contextData.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, ownerProperties);

            int cooldownTimeMS = Eval.RunInt(CooldownTimeMS, contextData);
            return TimeSpan.FromMilliseconds(cooldownTimeMS);
        }
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

        public override bool IsHighFlyingPower() => HighFlying;
    }

    public class SpecializationPowerPrototype : PowerPrototype
    {
        public PrototypeId MasterPowerDEPRECATED { get; protected set; }
        public EvalPrototype[] EvalCanEnable { get; protected set; }
    }

    public class StealablePowerInfoPrototype : Prototype
    {
        public PrototypeId Power { get; protected set; }
        public LocaleStringId StealablePowerDescription { get; protected set; }
    }

    public class StolenPowerRestrictionPrototype : Prototype
    {
        public PrototypeId RestrictionKeyword { get; protected set; }
        public int RestrictionKeywordCount { get; protected set; }
        public PrototypeId RestrictionBannerMessage { get; protected set; }
    }

    public class PowerEventContextTransformModePrototype : PowerEventContextPrototype
    {
        public PrototypeId TransformMode { get; protected set; }
    }

    public class PowerEventContextShowBannerMessagePrototype : PowerEventContextPrototype
    {
        public PrototypeId BannerMessage { get; protected set; }
        public bool SendToPrimaryTarget { get; protected set; }
    }

    public class PowerEventContextLootTablePrototype : PowerEventContextPrototype
    {
        public PrototypeId LootTable { get; protected set; }
        public bool UseItemLevelForLootRoll { get; protected set; }
        public bool IncludeNearbyAvatars { get; protected set; }
        public bool PlaceLootInGeneralInventory { get; protected set; }
    }

    public class PowerEventContextTeleportRegionPrototype : PowerEventContextPrototype
    {
        public PrototypeId Destination { get; protected set; }
    }

    public class PowerEventContextPetDonateItemPrototype : PowerEventContextPrototype
    {
        public float Radius { get; protected set; }
        public PrototypeId RarityThreshold { get; protected set; }
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
        public PrototypeId Ability { get; protected set; }

        [DoNotCopy]
        public int StartingRank { get; private set; }

        public override void PostProcess()
        {
            base.PostProcess();
            StartingRank = 1;
        }
    }

    public class AbilityAutoAssignmentSlotPrototype : Prototype
    {
        public PrototypeId Ability { get; protected set; }
        public PrototypeId Slot { get; protected set; }
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
        public PrototypeId OriginalPower { get; protected set; }
        public PrototypeId MappedPower { get; protected set; }
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
        public PrototypeId Power { get; protected set; }
        public PowerEventType PowerEvent { get; protected set; }
        public PowerEventContextPrototype PowerEventContext { get; protected set; }
        public PrototypeId[] Keywords { get; protected set; }
        public bool UseTriggerPowerOriginalTargetPos { get; protected set; }
        public bool UseTriggeringPowerTargetVerbatim { get; protected set; }
        public EvalPrototype EvalEventTriggerChance { get; protected set; }
        public EvalPrototype EvalEventParam { get; protected set; }
        public bool ResetFXRandomSeed { get; protected set; }

        [DoNotCopy]
        public bool HasEvalEventTriggerChance { get => EvalEventTriggerChance != null; }
    }

    public class SituationalTriggerPrototype : Prototype
    {
        public PrototypeId TriggerCollider { get; protected set; }
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
        public PrototypeId[] TriggeringProperties { get; protected set; }
        public bool TriggersOnStatusApplied { get; protected set; }
        public PrototypeId[] TriggeringConditionKeywords { get; protected set; }
    }

    public class SituationalTriggerInvAndWorldPrototype : SituationalTriggerPrototype
    {
        public PrototypeId InventoryRef { get; protected set; }
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
        public AssetId EntityArt { get; protected set; }
        public AssetId PowerArt { get; protected set; }
        public float AnimationContactTimePercent { get; protected set; }
        public int AnimationTimeMS { get; protected set; }
    }

    public class PowerUnrealOverridePrototype : Prototype
    {
        public float AnimationContactTimePercent { get; protected set; }
        public int AnimationTimeMS { get; protected set; }
        public AssetId EntityArt { get; protected set; }
        public AssetId PowerArt { get; protected set; }
        public PowerUnrealReplacementPrototype[] ArtOnlyReplacements { get; protected set; }
    }

    public class PowerSynergyTooltipEntryPrototype : Prototype
    {
        public PrototypeId SynergyPower { get; protected set; }
        public PrototypeId Translation { get; protected set; }
    }

    public class PowerEventContextCallbackAIChangeBlackboardPropertyPrototype : PowerEventContextCallbackPrototype
    {
        public BlackboardOperatorType Operation { get; protected set; }
        public PrototypeId PropertyInfoRef { get; protected set; }
        public int Value { get; protected set; }
        public bool UseTargetEntityId { get; protected set; }
    }

    public class PowerEventContextCallbackAISetAssistedEntityFromCreatorPrototype : PowerEventContextCallbackPrototype
    {
    }

    public class PowerEventContextCallbackAISummonsTryActivatePowerPrototype : PowerEventContextCallbackPrototype
    {
        public PrototypeId PowerToActivate { get; protected set; }
        public bool SummonsUsePowerTargetLocation { get; protected set; }
        public PrototypeId SummonsKeywordFilter { get; protected set; }
    }

    public class TransformModeUnrealOverridePrototype : Prototype
    {
        public AssetId IncomingUnrealClass { get; protected set; }
        public AssetId TransformedUnrealClass { get; protected set; }
    }

    public class TransformModePrototype : Prototype
    {
        public AbilityAssignmentPrototype[] DefaultEquippedAbilities { get; protected set; }
        public PrototypeId EnterTransformModePower { get; protected set; }
        public PrototypeId ExitTransformModePower { get; protected set; }
        public AssetId UnrealClass { get; protected set; }
        public PrototypeId[] HiddenPassivePowers { get; protected set; }
        public bool PowersAreSlottable { get; protected set; }
        public EvalPrototype DurationMSEval { get; protected set; }
        public TransformModeUnrealOverridePrototype[] UnrealClassOverrides { get; protected set; }
        public PrototypeId UseRankOfPower { get; protected set; }
    }

    public class TransformModeEntryPrototype : Prototype
    {
        public PrototypeId[] AllowedPowers { get; protected set; }
        public PrototypeId TransformMode { get; protected set; }
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

        public bool TargetsAOE()
        {
            return TargetingShape switch
            {
                TargetingShapeType.ArcArea
                or TargetingShapeType.BeamSweep
                or TargetingShapeType.CapsuleArea
                or TargetingShapeType.CircleArea
                or TargetingShapeType.RingArea
                or TargetingShapeType.WedgeArea => true,
                _ => false,
            };
        }
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
        public InventoryConvenienceLabel TargetsEntitiesInInventory { get; protected set; }
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
        public CurveId DamageIncreasePerSecond { get; protected set; }
        public DamageType DamageIncreaseType { get; protected set; }
        public CurveId EnduranceCostIncreasePerSecond { get; protected set; }
        public int MaxReleaseTimeMS { get; protected set; }
        public int MinReleaseTimeMS { get; protected set; }
        public CurveId RangeIncreasePerSecond { get; protected set; }
        public CurveId RadiusIncreasePerSecond { get; protected set; }
        public bool ActivateOnMaxReleaseTime { get; protected set; }
        public CurveId DefensePenetrationIncrPerSec { get; protected set; }
        public DamageType DefensePenetrationType { get; protected set; }
        public bool FollowsMouseUntilRelease { get; protected set; }
        public ManaType EnduranceCostManaType { get; protected set; }
    }

    public class ExtraActivateOnSubsequentPrototype : ExtraActivatePrototype
    {
        public CurveId NumActivatesBeforeCooldown { get; protected set; }
        public CurveId TimeoutLengthMS { get; protected set; }
        public SubsequentActivateType ExtraActivateEffect { get; protected set; }
    }

    public class ExtraActivateCycleToPowerPrototype : ExtraActivatePrototype
    {
        public PrototypeId[] CyclePowerList { get; protected set; }
    }
}
