namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    public enum ConditionType
    {
        Neither = 0,
        Buff = 1,
        Boost = 2,
        Debuff = 3,
    }

    public enum ConditionScopeType
    {
        Target = 0,
        User = 1,
    }

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
        public ulong ConditionArt { get; set; }
        public ulong EntityArt { get; set; }
    }

    public class ConditionPrototype : Prototype
    {
        public bool CancelOnHit { get; set; }
        public bool CancelOnPowerUse { get; set; }
        public long DurationMS { get; set; }
        public ulong TooltipText { get; set; }
        public ulong IconPath { get; set; }
        public bool PauseDurationCountdown { get; set; }
        public ulong Properties { get; set; }
        public ConditionScopeType Scope { get; set; }
        public ulong UnrealClass { get; set; }
        public EvalPrototype ChanceToApplyCondition { get; set; }
        public ConditionType ConditionType { get; set; }
        public bool VisualOnly { get; set; }
        public ConditionUnrealPrototype[] UnrealOverrides { get; set; }
        public ulong[] Keywords { get; set; }
        public ulong DurationMSCurve { get; set; }
        public ulong DurationMSCurveIndex { get; set; }
        public bool ForceShowClientConditionFX { get; set; }
        public ProcTriggerType[] CancelOnProcTriggers { get; set; }
        public int UpdateIntervalMS { get; set; }
        public EvalPrototype DurationMSEval { get; set; }
        public ulong TooltipStyle { get; set; }
        public ulong TooltipFont { get; set; }
        public EvalPrototype[] EvalOnCreate { get; set; }
        public ulong CancelOnPowerUseKeyword { get; set; }
        public bool CancelOnPowerUsePost { get; set; }
        public bool PersistToDB { get; set; }
        public bool CancelOnKilled { get; set; }
        public bool ApplyOverTimeEffectsToOriginator { get; set; }
        public bool TransferToCurrentAvatar { get; set; }
        public bool CancelOnTransfer { get; set; }
        public bool RealTime { get; set; }
        public bool IsBoost { get; set; }
        public UIConditionType ConditionTypeUI { get; set; }
        public bool ApplyInitialTickImmediately { get; set; }
        public bool ForceOpenBuffPage { get; set; }
        public bool IsPartyBoost { get; set; }
        public EvalPrototype[] EvalPartyBoost { get; set; }
        public StackingBehaviorPrototype StackingBehavior { get; set; }
        public bool CancelOnIntraRegionTeleport { get; set; }
        public ulong DisplayName { get; set; }
        public int UrgentTimeMS { get; set; }
        public ulong IconPathHiRes { get; set; }
    }

    public class ConditionEffectPrototype : Prototype
    {
        public ulong Properties { get; set; }
        public int ConditionNum { get; set; }
    }
}
