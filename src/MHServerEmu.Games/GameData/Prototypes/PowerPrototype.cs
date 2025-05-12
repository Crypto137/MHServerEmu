using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PowerPrototype : Prototype
    {
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

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        // See GetRecurringCostInterval() for why we use 500 ms here.
        private static readonly TimeSpan RecurringCostIntervalDefault = TimeSpan.FromMilliseconds(500);

        private readonly GBitArray _powerEventMask = new();

        // Local instance refs to speed up access
        private TargetingReachPrototype _targetingReachPtr;
        private TargetingStylePrototype _targetingStylePtr;

        [DoNotCopy]
        public float DamageTuningScore { get; private set; }

        [DoNotCopy]
        public bool HasRescheduleActivationEventWithInvalidPowerRef { get; private set; }
        [DoNotCopy]
        public bool LooksAtMousePosition { get; private set; }
        [DoNotCopy]
        public bool IsControlPower { get; private set; }
        [DoNotCopy]
        public bool IsStealingPower { get; private set; }
        [DoNotCopy]
        public virtual bool IsHighFlyingPower { get => false; }

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
        [DoNotCopy]
        public TimeSpan NoInterruptPreWindowTime { get => TimeSpan.FromMilliseconds(Math.Min(NoInterruptPreWindowMS, (int)(AnimationTimeMS * AnimationContactTimePercent))); }
        [DoNotCopy]
        public TimeSpan NoInterruptPostWindowTime { get => TimeSpan.FromMilliseconds(Math.Min(NoInterruptPostWindowMS, (int)(AnimationTimeMS * (1f - AnimationContactTimePercent)))); }

        [DoNotCopy]
        public int PowerPrototypeEnumValue { get; private set; }

        public static PrototypeId RecursiveGetPowerRefOfPowerTypeInCombo<T>(PrototypeId powerRef) where T : PowerPrototype
        {
            PowerPrototype powerProto = GameDatabase.GetPrototype<PowerPrototype>(powerRef);
            if (powerProto == null) return Logger.WarnReturn(PrototypeId.Invalid, "RecursiveGetPowerRefOfPowerTypeInCombo(): power == null");

            if (powerProto is T)
                return powerRef;

            if (powerProto.ActionsTriggeredOnPowerEvent.HasValue())
                foreach (var triggeredPowerEvent in powerProto.ActionsTriggeredOnPowerEvent)
                    if (triggeredPowerEvent?.EventAction == PowerEventActionType.UsePower)
                    {
                        if (triggeredPowerEvent.Power == PrototypeId.Invalid) return PrototypeId.Invalid;
                        if (triggeredPowerEvent.Power == powerRef) 
                            return Logger.WarnReturn(PrototypeId.Invalid, 
                                $"RecursiveGetPowerRefOfPowerTypeInCombo(): Infinite power loop detected in {powerRef.GetNameFormatted()}!");

                        return RecursiveGetPowerRefOfPowerTypeInCombo<T>(triggeredPowerEvent.Power);
                    }

            return PrototypeId.Invalid;
        }

        public static T RecursiveGetPowerPrototypeInCombo<T>(PrototypeId powerRef) where T : PowerPrototype
        {
            PowerPrototype powerProto = GameDatabase.GetPrototype<PowerPrototype>(powerRef);
            if (powerProto == null) return Logger.WarnReturn((T)default, "RecursiveGetPowerPrototypeInCombo(): power == null");

            if (powerProto is T power)
                return power;

            if (powerProto.ActionsTriggeredOnPowerEvent.HasValue())
                foreach (var triggeredPowerEvent in powerProto.ActionsTriggeredOnPowerEvent)
                    if (triggeredPowerEvent?.EventAction == PowerEventActionType.UsePower)
                    {
                        if (triggeredPowerEvent.Power == PrototypeId.Invalid) return default;
                        if (triggeredPowerEvent.Power == powerRef)
                            return Logger.WarnReturn((T)default,
                                $"RecursiveGetPowerPrototypeInCombo(): Infinite power loop detected in {powerRef.GetNameFormatted()}!");

                        return RecursiveGetPowerPrototypeInCombo<T>(triggeredPowerEvent.Power);
                    }

            return default;
        }

        public override bool ApprovedForUse()
        {
            return GameDatabase.DesignStateOk(DesignState);
        }

        public override void PostProcess()
        {
            base.PostProcess();

            // Skip abstract prototypes
            if (DataDirectory.Instance.PrototypeIsAbstract(DataRef))
                return;

            DamageTuningScore = PostProcessTuningScore();

            RangeActivationReduction = Math.Abs(RangeActivationReduction);

            if (ActionsTriggeredOnPowerEvent.HasValue())
            {
                foreach (PowerEventActionPrototype triggeredAction in ActionsTriggeredOnPowerEvent)
                {
                    // Populate lookup for power event actions
                    _powerEventMask.Set((int)triggeredAction.PowerEvent);
                    
                    if (triggeredAction.EventAction == PowerEventActionType.RescheduleActivationInSeconds && triggeredAction.Power == PrototypeId.Invalid)
                        HasRescheduleActivationEventWithInvalidPowerRef = true;
                }
            }

            // Apply condition effect properties to their conditions
            if (ConditionEffects != null)
            {
                // Condition effects are applied to mixin conditions of this power prototype.
                // First, we need to trigger mixin condition copy from parent if it did not happen already.
                var mixinFieldInfo = GameDatabase.PrototypeClassManager.GetMixinFieldInfo(typeof(PowerPrototype), typeof(ConditionPrototype), PrototypeFieldType.ListMixin);
                PrototypeMixinList conditionList = CalligraphySerializer.AcquireOwnedMixinList(this, mixinFieldInfo, true);

                // Post-process mixin conditions
                foreach (PrototypeMixinListItem item in conditionList)
                {
                    // We are fairly certain this list is going to have only condition prototypes.
                    // And if it doesn't, we will know straight away due to this crashing horribly
                    // and be able to fix it.
                    var conditionPrototype = (ConditionPrototype)item.Prototype;
                    conditionPrototype.PostProcess();

                    // Force property collection initialization, but get rid of all properties copied from the parent.
                    // TODO: Do we even need GetPropertyCollectionField()? It would probably be faster to just create a collection directly.
                    // It may break Calligraphy things somehow though.
                    PrototypePropertyCollection conditionProperties = CalligraphySerializer.GetPropertyCollectionField(conditionPrototype);
                    conditionProperties.Clear();
                }

                // Apply condition effects to conditions

                foreach (PrototypeMixinListItem effectItem in ConditionEffects)
                {
                    var effectPrototype = (ConditionEffectPrototype)effectItem.Prototype;
                    bool foundCondition = false;

                    // Look for the condition specified in the effect prototype
                    foreach (PrototypeMixinListItem conditionItem in conditionList)
                    {
                        if (conditionItem.BlueprintCopyNum != effectPrototype.ConditionNum)
                            continue;

                        // Copy effect properties to the condition
                        PrototypePropertyCollection effectProperties = effectPrototype.Properties;
                        var conditionPrototype = (ConditionPrototype)conditionItem.Prototype;

                        PrototypePropertyCollection conditionProperties = conditionPrototype.Properties;
                        conditionProperties.FlattenCopyFrom(effectProperties, false);
                        foundCondition = true;

                        // Set mouse position flag
                        if (conditionPrototype.Scope == ConditionScopeType.User && conditionProperties[PropertyEnum.LookAtMousePosition])
                            LooksAtMousePosition = true;

                        break;
                    }

                    if (foundCondition == false)
                        Logger.Warn($"PostProcess(): Effect found with no matching condition in power {this}");
                }

            }

            // Add indexes to mixin conditions
            if (AppliesConditions != null)
            {
                foreach (PrototypeMixinListItem item in AppliesConditions)
                {
                    if (item.Prototype is ConditionPrototype conditionProto)
                        conditionProto.BlueprintCopyNum = item.BlueprintCopyNum;
                }
            }

            // Initialize keywords
            KeywordsMask = KeywordPrototype.GetBitMaskForKeywordList(Keywords);

            KeywordGlobalsPrototype keywordGlobalsProto = GameDatabase.KeywordGlobalsPrototype;
            IsControlPower = HasKeyword(keywordGlobalsProto.ControlPowerKeywordPrototype.As<KeywordPrototype>());
            IsStealingPower = HasKeyword(keywordGlobalsProto.StealingPowerKeyword.As<KeywordPrototype>());

            PowerPrototypeEnumValue = GetEnumValueFromBlueprint(LiveTuningData.GetPowerBlueprintDataRef());

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

        public bool HasPowerEvent(PowerEventType eventType)
        {
            return _powerEventMask[(int)eventType];
        }

        public TargetingReachPrototype GetTargetingReach()
        {
            return _targetingReachPtr;
        }

        public TargetingStylePrototype GetTargetingStyle()
        {
            return _targetingStylePtr;
        }

        public AssetId GetUnrealClass(AssetId originalWorldAssetRef, AssetId entityWorldAssetRef)
        {
            AssetId powerAssetRef = PowerUnrealClass;

            if (PowerUnrealOverrides.IsNullOrEmpty())
                return powerAssetRef;

            foreach (PowerUnrealOverridePrototype overrideProto in PowerUnrealOverrides)
            {
                if (overrideProto.EntityArt != originalWorldAssetRef)
                    continue;

                powerAssetRef = overrideProto.PowerArt;

                if (overrideProto.ArtOnlyReplacements.IsNullOrEmpty())
                    break;

                foreach (PowerUnrealReplacementPrototype replacementProto in overrideProto.ArtOnlyReplacements)
                {
                    if (replacementProto.EntityArt != entityWorldAssetRef)
                        continue;

                    powerAssetRef = replacementProto.PowerArt;
                    break;
                }

                break;
            }

            return powerAssetRef;
        }

        public float GetRange(PropertyCollection powerProperties, PropertyCollection ownerProperties)
        {
            if (Range == null) return Logger.WarnReturn(0f, "GetRange(): Range == null");

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            using PropertyCollection properties = ObjectPoolManager.Instance.Get<PropertyCollection>();

            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, powerProperties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, ownerProperties ?? properties);

            return Eval.RunFloat(Range, evalContext);            
        }

        public float GetProjectileSpeed(PropertyCollection powerProperties, PropertyCollection ownerProperties)
        {
            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            using PropertyCollection properties = ObjectPoolManager.Instance.Get<PropertyCollection>();

            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, powerProperties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, ownerProperties ?? properties);

            return Eval.RunFloat(ProjectileSpeed, evalContext);
        }

        public TimeSpan GetChannelLoopTime(PropertyCollection powerProperties, PropertyCollection ownerProperties)
        {
            if (ChannelLoopTimeMS == null) return Logger.WarnReturn(TimeSpan.Zero, "GetChannelLoopTime(): ChannelLoopTimeMS == null");

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, powerProperties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, ownerProperties);

            int channelLoopTimeMS = Eval.RunInt(ChannelLoopTimeMS, evalContext);
            return TimeSpan.FromMilliseconds(channelLoopTimeMS);
        }

        public TimeSpan GetAnimationTime(AssetId originalWorldAssetRef, AssetId entityWorldAssetRef)
        {
            int animationTimeMS = AnimationTimeMS;

            if (PowerUnrealOverrides.IsNullOrEmpty())
                return TimeSpan.FromMilliseconds(animationTimeMS);

            foreach (PowerUnrealOverridePrototype unrealAssetOverrideProto in PowerUnrealOverrides)
            {
                if (unrealAssetOverrideProto.EntityArt != originalWorldAssetRef)
                    continue;

                if (unrealAssetOverrideProto.AnimationTimeMS >= 0)
                    animationTimeMS = unrealAssetOverrideProto.AnimationTimeMS;

                if (unrealAssetOverrideProto.ArtOnlyReplacements.IsNullOrEmpty())
                    break;

                foreach (PowerUnrealReplacementPrototype replacementProto in unrealAssetOverrideProto.ArtOnlyReplacements)
                {
                    if (replacementProto.EntityArt != entityWorldAssetRef)
                        continue;

                    if (replacementProto.AnimationTimeMS >= 0)
                        animationTimeMS = replacementProto.AnimationTimeMS;

                    break;
                }

                break;
            }

            return TimeSpan.FromMilliseconds(animationTimeMS);
        }

        public float GetContactTimePercent(AssetId originalWorldAssetRef, AssetId entityWorldAssetRef)
        {
            float contactTimePercent = AnimationContactTimePercent;

            if (PowerUnrealOverrides.IsNullOrEmpty())
                return contactTimePercent;

            foreach (PowerUnrealOverridePrototype unrealAssetOverrideProto in PowerUnrealOverrides)
            {
                if (unrealAssetOverrideProto.EntityArt != originalWorldAssetRef)
                    continue;

                if (unrealAssetOverrideProto.AnimationContactTimePercent >= 0f)
                    contactTimePercent = unrealAssetOverrideProto.AnimationContactTimePercent;

                if (unrealAssetOverrideProto.ArtOnlyReplacements.IsNullOrEmpty())
                    break;

                foreach (PowerUnrealReplacementPrototype replacementProto in unrealAssetOverrideProto.ArtOnlyReplacements)
                {
                    if (replacementProto.EntityArt != entityWorldAssetRef)
                        continue;

                    if (replacementProto.AnimationContactTimePercent >= 0f)
                        contactTimePercent = replacementProto.AnimationContactTimePercent;

                    break;
                }

                break;
            }

            return contactTimePercent;
        }

        public TimeSpan GetOneOffAnimContactTime(AssetId originalWorldAssetRef, AssetId entityWorldAssetRef)
        {
            TimeSpan animationTime = TimeSpan.FromMilliseconds(AnimationTimeMS);
            float animationContactTimePercent = AnimationContactTimePercent;

            if (PowerUnrealOverrides.IsNullOrEmpty())
                return animationTime * animationContactTimePercent;

            foreach (PowerUnrealOverridePrototype unrealAssetOverrideProto in PowerUnrealOverrides)
            {
                if (unrealAssetOverrideProto.EntityArt != originalWorldAssetRef)
                    continue;

                if (unrealAssetOverrideProto.AnimationTimeMS >= 0)
                    animationTime = TimeSpan.FromMilliseconds(unrealAssetOverrideProto.AnimationTimeMS);

                if (unrealAssetOverrideProto.AnimationContactTimePercent >= 0f)
                    animationContactTimePercent = unrealAssetOverrideProto.AnimationContactTimePercent;

                if (unrealAssetOverrideProto.ArtOnlyReplacements.IsNullOrEmpty())
                    break;

                foreach (PowerUnrealReplacementPrototype replacementProto in unrealAssetOverrideProto.ArtOnlyReplacements)
                {
                    if (replacementProto.EntityArt != entityWorldAssetRef)
                        continue;

                    if (replacementProto.AnimationTimeMS >= 0)
                        animationTime = TimeSpan.FromMilliseconds(unrealAssetOverrideProto.AnimationTimeMS);

                    if (replacementProto.AnimationContactTimePercent >= 0f)
                        animationContactTimePercent = unrealAssetOverrideProto.AnimationContactTimePercent;

                    break;
                }

                break;
            }

            return animationTime * animationContactTimePercent;
        }

        public TimeSpan GetCooldownDuration(PropertyCollection powerProperties, PropertyCollection ownerProperties)
        {
            if (CooldownTimeMS == null) return Logger.WarnReturn(TimeSpan.Zero, "GetCooldownDuration(): CooldownTimeMS == null");

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, powerProperties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, ownerProperties);

            int cooldownTimeMS = Eval.RunInt(CooldownTimeMS, evalContext);
            return TimeSpan.FromMilliseconds(cooldownTimeMS);
        }

        public bool TriggersComboPowerOnEvent(PowerEventType eventType, PropertyCollection powerProperties, WorldEntity owner)
        {
            if (ActionsTriggeredOnPowerEvent.IsNullOrEmpty())
                return false;

            foreach (PowerEventActionPrototype triggeredPowerEvent in ActionsTriggeredOnPowerEvent)
            {
                if (triggeredPowerEvent.PowerEvent != eventType)
                    continue;

                if (triggeredPowerEvent.GetEventTriggerChance(powerProperties, owner, owner) < 0f)
                    continue;

                return true;
            }

            return false;
        }

        public virtual void OnEndPower(Power power, WorldEntity owner)
        {
            // Overriden in MovementPowerPrototype
        }

        private float PostProcessTuningScore()
        {
            float score = DamageTuningArea * DamageTuningBuff1 * DamageTuningBuff2 * DamageTuningBuff3
                * DamageTuningCooldown * DamageTuningDebuff1 * DamageTuningDebuff2 * DamageTuningDebuff3
                * DamageTuningDmgBonusFreq * DamageTuningHardCC * DamageTuningMultiHit * DamageTuningAnimationDelay
                * DamageTuningPowerTag1 * DamageTuningPowerTag2 * DamageTuningPowerTag3 * DamageTuningRangeRisk
                * DamageTuningSoftCC * DamageTuningSummon * DamageTuningDuration * DamageTuningTriggerDelay
                * DamageTuningDoTHotspot * DamageTuningHeroSpecific;

            score *= DamageBaseTuningEnduranceCost * DamageBaseTuningEnduranceRatio + (DamageBaseTuningAnimTimeMS / 1000f);
            return score;
        }

        public TimeSpan GetRecurringCostInterval()
        {
            // Most powers use either 250 or 500 ms intervals, with 2/3 of them using 500 ms.
            // A single power (Powers/Player/DrDoom/ChanneledBeam.prototype) uses a 200 ms interval.
            if (RecurringCostIntervalMS > 0)
                return TimeSpan.FromMilliseconds(RecurringCostIntervalMS);

            // Default to 500 ms since it seems to be the most common value.
            return RecurringCostIntervalDefault;
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

        [DoNotCopy]
        public override bool IsHighFlyingPower { get => HighFlying; }

        [DoNotCopy]
        public BlockingCheckFlags BlockingCheckFlags { get; private set; }

        public override void PostProcess()
        {
            base.PostProcess();

            BlockingCheckFlags = BlockingCheckFlags.CheckAllMovementPowers;

            if (IsHighFlyingPower == false && MovementHeightBonus <= 0f)
                BlockingCheckFlags |= BlockingCheckFlags.CheckGroundMovementPowers;
        }

        public override void OnEndPower(Power power, WorldEntity owner)
        {
            if (owner != null && CustomBehavior != null)
            {
                PowerActivationSettings settings = power.LastActivationSettings;
                WorldEntity target = owner.Game.EntityManager.GetEntity<WorldEntity>(settings.TargetEntityId);
                MovementBehaviorPrototype.Context context = new(power, owner, target, settings.TargetPosition);
                CustomBehavior.OnEndPower(in context);
            }
        }

        public bool HasCustomBehaviorOfType<T>() where T: MovementBehaviorPrototype
        {
            return CustomBehavior is T;
        }
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

        //---

        [DoNotCopy]
        public KeywordPrototype RestrictionKeywordPrototype { get; private set; }

        public override void PostProcess()
        {
            base.PostProcess();

            RestrictionKeywordPrototype = RestrictionKeyword.As<KeywordPrototype>();
        }
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

        //---

        [DoNotCopy]
        public int Rank { get => 1; }  // NOTE: This was a real prototype field in 1.48
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

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public virtual bool HandlePowerEvent(WorldEntity user, WorldEntity target, Vector3 targetPosition)
        {
            return Logger.WarnReturn(false, "HandlePowerEvent(): PowerEventContextPrototype should not be called directly");
        }
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

        //---

        [DoNotCopy]
        public bool HasEvalEventTriggerChance { get => EvalEventTriggerChance != null; }

        [DoNotCopy]
        public KeywordsMask KeywordsMask { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();
            KeywordsMask = KeywordPrototype.GetBitMaskForKeywordList(Keywords);
        }

        public float GetEventTriggerChance(PropertyCollection powerProperties, WorldEntity owner, WorldEntity target)
        {
            if (EvalEventTriggerChance == null)
                return 1f;

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, powerProperties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, owner.Properties);
            evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Var1, owner);
            evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Var2, target);
            Eval.InitTeamUpEvalContext(evalContext, owner);

            return Eval.RunFloat(EvalEventTriggerChance, evalContext);
        }

        public float GetEventParam(PropertyCollection powerProperties, WorldEntity owner)
        {
            if (EvalEventParam == null)
                return EventParam;

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, powerProperties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, owner.Properties);
            evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Var1, owner);

            return Eval.RunFloat(EvalEventParam, evalContext);
        }

        public float GetEventParamNoEval()
        {
            return EventParam;
        }
    }

    #region SituationalTriggerPrototype

    public class SituationalTriggerPrototype : Prototype
    {
        public PrototypeId TriggerCollider { get; protected set; }
        public float TriggerRadiusScaling { get; protected set; }
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public bool AllowDead { get; protected set; }
        public bool ActivateOnTriggerSuccess { get; protected set; }

        public virtual SituationalTrigger AllocateTrigger(SituationalPowerComponent powerComponent)
        {
            return new SituationalTrigger(this, powerComponent);
        }
    }

    public class SituationalTriggerOnKilledPrototype : SituationalTriggerPrototype
    {
        public bool Friendly { get; protected set; }
        public bool Hostile { get; protected set; }
        public bool KilledByOther { get; protected set; }
        public bool KilledBySelf { get; protected set; }
        public bool WasLastInRange { get; protected set; }

        // Not used
    }

    public class SituationalTriggerOnHealthThresholdPrototype : SituationalTriggerPrototype
    {
        public bool HealthBelow { get; protected set; }
        public float HealthPercent { get; protected set; }

        // Not used
    }

    public class SituationalTriggerOnStatusEffectPrototype : SituationalTriggerPrototype
    {
        public PrototypeId[] TriggeringProperties { get; protected set; }
        public bool TriggersOnStatusApplied { get; protected set; }
        public PrototypeId[] TriggeringConditionKeywords { get; protected set; }

        public override SituationalTrigger AllocateTrigger(SituationalPowerComponent powerComponent)
        {
            return new SituationalTriggerOnStatusEffect(this, powerComponent);
        }       
    }

    public class SituationalTriggerInvAndWorldPrototype : SituationalTriggerPrototype
    {
        public PrototypeId InventoryRef { get; protected set; }

        public override SituationalTrigger AllocateTrigger(SituationalPowerComponent powerComponent)
        {
            return new SituationalTriggerInvAndWorld(this, powerComponent);
        }       
    }

    #endregion

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

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override bool HandlePowerEvent(WorldEntity user, WorldEntity target, Vector3 targetPosition)
        {
            if (user is Agent agent)
            {
                var controller = agent.AIController;
                if (controller == null) return false;

                ulong targetId = Entity.InvalidId;

                if (UseTargetEntityId && target != null)
                    targetId = target.Id;

                controller.Blackboard.ChangeBlackboardFact(PropertyInfoRef, Value, Operation, targetId);
                return true;
            }

            return false;
        }
    }

    public class PowerEventContextCallbackAISetAssistedEntityFromCreatorPrototype : PowerEventContextCallbackPrototype
    {
        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override bool HandlePowerEvent(WorldEntity user, WorldEntity target, Vector3 targetPosition)
        {
            if (target is Agent targetAgent && user != null)
            {
                var controller = targetAgent.AIController;
                if (controller == null) return Logger.WarnReturn(false, $"HandlePowerEvent: AIController == null");

                controller.Blackboard.PropertyCollection[PropertyEnum.AIAssistedEntityID] = user.Id;
                return true;
            }

            return false;
        }
    }

    public class PowerEventContextCallbackAISummonsTryActivatePowerPrototype : PowerEventContextCallbackPrototype
    {
        public PrototypeId PowerToActivate { get; protected set; }
        public bool SummonsUsePowerTargetLocation { get; protected set; }
        public PrototypeId SummonsKeywordFilter { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override bool HandlePowerEvent(WorldEntity user, WorldEntity target, Vector3 targetPosition)
        {
            if (PowerToActivate == PrototypeId.Invalid)
                return Logger.WarnReturn(false, $"HandlePowerEvent: PowerToActivate == Invalid");

            if (user is Agent summoned)
            {
                var game = summoned.Game;
                if (game == null) return Logger.WarnReturn(false, $"HandlePowerEvent: game == null");

                if (SummonsKeywordFilter == PrototypeId.Invalid || summoned.HasKeyword(SummonsKeywordFilter))
                {
                    var controller = summoned.AIController;
                    if (controller == null) return false;

                    ulong targetId = Entity.InvalidId;
                    if (SummonedEntitiesUsePowerTarget && target != null)
                        targetId = target.Id;

                    var blackboard = controller.Blackboard;
                    var position = SummonsUsePowerTargetLocation ? targetPosition : Vector3.Zero;
                    
                    blackboard.AddCustomPower(PowerToActivate, position, targetId);
                    blackboard.PropertyCollection[PropertyEnum.AICustomThinkRateMS] = (long)game.FixedTimeBetweenUpdates.TotalMilliseconds;
                    controller.ScheduleAIThinkEvent(TimeSpan.Zero, false, true);

                    return true;
                }
            }

            return false;
        }
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

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public TimeSpan GetDuration(Entity owner)
        {
            if (DurationMSEval == null)
                return TimeSpan.Zero;

            Game game = owner.Game;
            if (game == null) return Logger.WarnReturn(TimeSpan.Zero, "GetDuration(): game == null");

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.Game = game;
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, owner?.Properties);

            int durationMS = Eval.RunInt(DurationMSEval, evalContext);
            return TimeSpan.FromMilliseconds(durationMS);
        }
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

        private Vector3 _positionOffset;

        public override void PostProcess()
        {
            base.PostProcess();
            _positionOffset = PositionOffset != null ? PositionOffset.ToVector3() : Vector3.Zero;
        }

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

        public Vector3 GetOwnerOrientedPositionOffset(WorldEntity owner)
        {
            if (owner != null && owner.IsInWorld)
                return Transform3.BuildTransform(Vector3.Zero, owner.Orientation) * _positionOffset;
            return _positionOffset;
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
        private static readonly Logger Logger = LogManager.CreateLogger();

        public CurveId NumActivatesBeforeCooldown { get; protected set; }
        public CurveId TimeoutLengthMS { get; protected set; }
        public SubsequentActivateType ExtraActivateEffect { get; protected set; }

        public int GetNumActivatesBeforeCooldown(int powerRank)
        {
            if (NumActivatesBeforeCooldown == CurveId.Invalid)
                return 0;

            if (powerRank < 0) return Logger.WarnReturn(0, "GetNumActivatesBeforeCooldown(): powerRank < 0");

            Curve curve = CurveDirectory.Instance.GetCurve(NumActivatesBeforeCooldown);
            if (curve == null) return Logger.WarnReturn(0, "GetNumActivatesBeforeCooldown(): curve == null");

            return MathHelper.RoundDownToInt(curve.GetAt(powerRank));
        }

        public int GetTimeoutLengthMS(int powerRank)
        {
            if (TimeoutLengthMS == CurveId.Invalid)
                return 0;

            if (powerRank < 0) return Logger.WarnReturn(0, "GetTimeoutLengthMS(): powerRank < 0");

            Curve curve = CurveDirectory.Instance.GetCurve(TimeoutLengthMS);
            if (curve == null) return Logger.WarnReturn(0, "GetTimeoutLengthMS(): curve == null");

            return MathHelper.RoundDownToInt(curve.GetAt(powerRank));
        }
    }

    public class ExtraActivateCycleToPowerPrototype : ExtraActivatePrototype
    {
        public PrototypeId[] CyclePowerList { get; protected set; }
    }

    public class StackingBehaviorPrototype : Prototype
    {
        public StackingApplicationStyleType ApplicationStyle { get; protected set; }
        public int MaxNumStacks { get; protected set; }
        public bool RemoveStackOnMaxNumStacksReached { get; protected set; }
        public bool StacksFromDifferentCreators { get; protected set; }
        public int NumStacksToApply { get; protected set; }
        public PrototypeId[] StacksByKeyword { get; protected set; }
        public PrototypeId StacksWithOtherPower { get; protected set; }
    }
}
