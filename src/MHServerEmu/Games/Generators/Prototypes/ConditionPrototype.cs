using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{

    public class ConditionUnrealPrototype : Prototype
    {
        public ulong ConditionArt;
        public ulong EntityArt;
        public ConditionUnrealPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ConditionUnrealPrototype), proto); }
    }

    public class ConditionPrototype : Prototype
    {
        public bool CancelOnHit;
        public bool CancelOnPowerUse;
        public long DurationMS;
        public ulong TooltipText;
        public ulong IconPath;
        public bool PauseDurationCountdown;
        public ulong Properties;
        public ConditionScopeType Scope;
        public ulong UnrealClass;
        public EvalPrototype ChanceToApplyCondition;
        public ConditionType ConditionType;
        public bool VisualOnly;
        public ConditionUnrealPrototype[] UnrealOverrides;
        public ulong[] Keywords;
        public ulong DurationMSCurve;
        public ulong DurationMSCurveIndex;
        public bool ForceShowClientConditionFX;
        public ProcTriggerType[] CancelOnProcTriggers;
        public int UpdateIntervalMS;
        public EvalPrototype DurationMSEval;
        public ulong TooltipStyle;
        public ulong TooltipFont;
        public EvalPrototype[] EvalOnCreate;
        public ulong CancelOnPowerUseKeyword;
        public bool CancelOnPowerUsePost;
        public bool PersistToDB;
        public bool CancelOnKilled;
        public bool ApplyOverTimeEffectsToOriginator;
        public bool TransferToCurrentAvatar;
        public bool CancelOnTransfer;
        public bool RealTime;
        public bool IsBoost;
        public UIConditionType ConditionTypeUI;
        public bool ApplyInitialTickImmediately;
        public bool ForceOpenBuffPage;
        public bool IsPartyBoost;
        public EvalPrototype[] EvalPartyBoost;
        public StackingBehaviorPrototype StackingBehavior;
        public bool CancelOnIntraRegionTeleport;
        public ulong DisplayName;
        public int UrgentTimeMS;
        public ulong IconPathHiRes;
        public ConditionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ConditionPrototype), proto); }
    }
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

    public class ConditionEffectPrototype : Prototype
    {
        public ulong Properties;
        public int ConditionNum;
        public ConditionEffectPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ConditionEffectPrototype), proto); }
    }

}
