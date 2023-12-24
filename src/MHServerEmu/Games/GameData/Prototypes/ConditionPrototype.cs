using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum PowerConditionType
    {
        Neither = 0,
        Buff = 1,
        Boost = 2,
        Debuff = 3,
    }

    [AssetEnum]
    public enum ConditionScopeType
    {
        Target = 0,
        User = 1,
    }

    [AssetEnum]
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
        public ulong ConditionArt { get; private set; }
        public ulong EntityArt { get; private set; }
    }

    public class ConditionPrototype : Prototype
    {
        public bool CancelOnHit { get; private set; }
        public bool CancelOnPowerUse { get; private set; }
        public long DurationMS { get; private set; }
        public ulong TooltipText { get; private set; }
        public ulong IconPath { get; private set; }
        public bool PauseDurationCountdown { get; private set; }
        public ulong Properties { get; private set; }
        public ConditionScopeType Scope { get; private set; }
        public ulong UnrealClass { get; private set; }
        public EvalPrototype ChanceToApplyCondition { get; private set; }
        public PowerConditionType ConditionType { get; private set; }
        public bool VisualOnly { get; private set; }
        public ConditionUnrealPrototype[] UnrealOverrides { get; private set; }
        public ulong[] Keywords { get; private set; }
        public ulong DurationMSCurve { get; private set; }
        public ulong DurationMSCurveIndex { get; private set; }
        public bool ForceShowClientConditionFX { get; private set; }
        public ProcTriggerType[] CancelOnProcTriggers { get; private set; }
        public int UpdateIntervalMS { get; private set; }
        public EvalPrototype DurationMSEval { get; private set; }
        public ulong TooltipStyle { get; private set; }
        public ulong TooltipFont { get; private set; }
        public EvalPrototype[] EvalOnCreate { get; private set; }
        public ulong CancelOnPowerUseKeyword { get; private set; }
        public bool CancelOnPowerUsePost { get; private set; }
        public bool PersistToDB { get; private set; }
        public bool CancelOnKilled { get; private set; }
        public bool ApplyOverTimeEffectsToOriginator { get; private set; }
        public bool TransferToCurrentAvatar { get; private set; }
        public bool CancelOnTransfer { get; private set; }
        public bool RealTime { get; private set; }
        public bool IsBoost { get; private set; }
        public UIConditionType ConditionTypeUI { get; private set; }
        public bool ApplyInitialTickImmediately { get; private set; }
        public bool ForceOpenBuffPage { get; private set; }
        public bool IsPartyBoost { get; private set; }
        public EvalPrototype[] EvalPartyBoost { get; private set; }
        public StackingBehaviorPrototype StackingBehavior { get; private set; }
        public bool CancelOnIntraRegionTeleport { get; private set; }
        public ulong DisplayName { get; private set; }
        public int UrgentTimeMS { get; private set; }
        public ulong IconPathHiRes { get; private set; }
    }

    public class ConditionEffectPrototype : Prototype
    {
        public ulong Properties { get; private set; }
        public int ConditionNum { get; private set; }
    }
}
