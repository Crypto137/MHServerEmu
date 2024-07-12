using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)Neither)]
    public enum PowerConditionType
    {
        Neither = 0,
        Buff = 1,
        Boost = 2,
        Debuff = 3,
    }

    [AssetEnum((int)Target)]
    public enum ConditionScopeType
    {
        Target = 0,
        User = 1,
    }

    [AssetEnum((int)None)]
    public enum ProcTriggerType
    {
        None = 0,
        OnAnyHit = 1,
        OnAnyHitForPctHealth = 2,
        OnAnyHitTargetHealthBelowPct = 3,
        OnBlock = 4,
        OnCollide = 5,
        OnCollideEntity = 6,
        OnCollideWorldGeo = 7,
        OnConditionEnd = 8,
        OnConditionStackCount = 9,
        OnCrit = 10,
        OnDeath = 12,
        OnEnduranceAbove = 14,
        OnEnduranceBelow = 15,
        OnDodge = 13,
        OnGotAttacked = 16,
        OnGotDamaged = 17,
        OnGotDamagedPriorResist = 18,
        OnGotDamagedByCrit = 11,
        OnGotDamagedEnergy = 19,
        OnGotDamagedEnergyPriorResist = 20,
        OnGotDamagedForPctHealth = 21,
        OnGotDamagedHealthBelowPct = 22,
        OnGotDamagedMental = 23,
        OnGotDamagedMentalPriorResist = 24,
        OnGotDamagedPhysical = 25,
        OnGotDamagedPhysicalPriorResist = 26,
        OnGotDamagedBySuperCrit = 27,
        OnHealthAbove = 28,
        OnHealthAboveToggle = 29,
        OnHealthBelow = 30,
        OnHealthBelowToggle = 31,
        OnInCombat = 32,
        OnInteractedWith = 33,
        OnInteractedWithOutOfUses = 34,
        OnKillAlly = 35,
        OnKillDestructible = 36,
        OnKillOther = 37,
        OnKillOtherCritical = 38,
        OnKillOtherSuperCrit = 39,
        OnKnockdownEnd = 40,
        OnLifespanExpired = 41,
        OnLootPickup = 42,
        OnMovementStarted = 44,
        OnMovementStopped = 45,
        OnMissileAbsorbed = 43,
        OnNegStatusApplied = 46,
        OnOrbPickup = 47,
        OnOutCombat = 48,
        OnOverlapBegin = 49,
        OnPetDeath = 50,
        OnPetHit = 51,
        OnPowerHit = 52,
        OnPowerHitEnergy = 53,
        OnPowerHitMental = 54,
        OnPowerHitNormal = 55,
        OnPowerHitNotOverTime = 56,
        OnPowerHitPhysical = 57,
        OnPowerUseComboEffect = 58,
        OnPowerUseConsumable = 59,
        OnPowerUseGameFunction = 60,
        OnPowerUseNormal = 61,
        OnPowerUseProcEffect = 62,
        OnRunestonePickup = 63,
        OnSecondaryResourceEmpty = 64,
        OnSecondaryResourcePipGain = 65,
        OnSecondaryResourcePipLoss = 66,
        OnSecondaryResourcePipMax = 67,
        OnSecondaryResourcePipZero = 68,
        OnSkillshotReflect = 69,
        OnSummonPet = 70,
        OnSuperCrit = 71,
        OnMissileHit = 72,
        OnHotspotNegated = 73,
        OnControlledEntityReleased = 74,
    }

    [AssetEnum]
    public enum UIConditionType
    {
        None = 0,
        Buff = 1,
        Boost = 2,
        Debuff = 3,
        Raid = 5,
        LiveTune = 6,
        Event = 7,
        Environment = 8,
        Team = 9,
        PlayerPower = 10,
    }

    #endregion

    public class ConditionUnrealPrototype : Prototype
    {
        public AssetId ConditionArt { get; protected set; }
        public AssetId EntityArt { get; protected set; }
    }

    public class ConditionPrototype : Prototype
    {
        public bool CancelOnHit { get; protected set; }
        public bool CancelOnPowerUse { get; protected set; }
        public long DurationMS { get; protected set; }
        public LocaleStringId TooltipText { get; protected set; }
        public AssetId IconPath { get; protected set; }
        public bool PauseDurationCountdown { get; protected set; }
        public PrototypePropertyCollection Properties { get; protected set; }
        public ConditionScopeType Scope { get; protected set; }
        public AssetId UnrealClass { get; protected set; }
        public EvalPrototype ChanceToApplyCondition { get; protected set; }
        public PowerConditionType ConditionType { get; protected set; }
        public bool VisualOnly { get; protected set; }
        public ConditionUnrealPrototype[] UnrealOverrides { get; protected set; }
        public PrototypeId[] Keywords { get; protected set; }
        public CurveId DurationMSCurve { get; protected set; }
        public PrototypeId DurationMSCurveIndex { get; protected set; }
        public bool ForceShowClientConditionFX { get; protected set; }
        public ProcTriggerType[] CancelOnProcTriggers { get; protected set; }
        public int UpdateIntervalMS { get; protected set; }
        public EvalPrototype DurationMSEval { get; protected set; }
        public PrototypeId TooltipStyle { get; protected set; }
        public AssetId TooltipFont { get; protected set; }
        public EvalPrototype[] EvalOnCreate { get; protected set; }
        public PrototypeId CancelOnPowerUseKeyword { get; protected set; }
        public bool CancelOnPowerUsePost { get; protected set; }
        public bool PersistToDB { get; protected set; }
        public bool CancelOnKilled { get; protected set; }
        public bool ApplyOverTimeEffectsToOriginator { get; protected set; }
        public bool TransferToCurrentAvatar { get; protected set; }
        public bool CancelOnTransfer { get; protected set; }
        public bool RealTime { get; protected set; }
        public bool IsBoost { get; protected set; }
        public UIConditionType ConditionTypeUI { get; protected set; }
        public bool ApplyInitialTickImmediately { get; protected set; }
        public bool ForceOpenBuffPage { get; protected set; }
        public bool IsPartyBoost { get; protected set; }
        public EvalPrototype[] EvalPartyBoost { get; protected set; }
        public StackingBehaviorPrototype StackingBehavior { get; protected set; }
        public bool CancelOnIntraRegionTeleport { get; protected set; }
        public LocaleStringId DisplayName { get; protected set; }
        public int UrgentTimeMS { get; protected set; }
        public AssetId IconPathHiRes { get; protected set; }

        [DoNotCopy]
        public KeywordsMask KeywordsMask { get; protected set; }
        [DoNotCopy]
        public TimeSpan UpdateInterval { get => TimeSpan.FromMilliseconds(UpdateIntervalMS); }
        [DoNotCopy]
        public ConditionCancelOnFlags CancelOnFlags { get; private set; } = ConditionCancelOnFlags.None;
        [DoNotCopy]
        public bool IsHitReactCondition { get => Properties != null && Properties.HasProperty(PropertyEnum.HitReact); }

        [DoNotCopy]
        public int BlueprintCopyNum { get; set; }

        [DoNotCopy]
        public int ConditionPrototypeEnumValue { get; private set; }

        public override void PostProcess()
        {
            base.PostProcess();

            KeywordsMask = KeywordPrototype.GetBitMaskForKeywordList(Keywords);

            // TODO: stuff

            // Combine cancel flags into a single bit field (TODO: flag enum)
            if (CancelOnHit)                    CancelOnFlags |= ConditionCancelOnFlags.OnHit;
            if (CancelOnKilled)                 CancelOnFlags |= ConditionCancelOnFlags.OnKilled;
            if (CancelOnPowerUse)               CancelOnFlags |= ConditionCancelOnFlags.OnPowerUse;
            if (CancelOnPowerUsePost)           CancelOnFlags |= ConditionCancelOnFlags.OnPowerUsePost;
            if (CancelOnTransfer)               CancelOnFlags |= ConditionCancelOnFlags.OnTransfer;
            if (CancelOnIntraRegionTeleport)    CancelOnFlags |= ConditionCancelOnFlags.OnIntraRegionTeleport;

            ConditionPrototypeEnumValue = GetEnumValueFromBlueprint(LiveTuningData.GetConditionBlueprintDataRef());

            // TODO: more stuff
        }
    }

    public class ConditionEffectPrototype : Prototype
    {
        public PrototypePropertyCollection Properties { get; protected set; }
        public int ConditionNum { get; protected set; }
    }

}
