using MHServerEmu.Games.Behavior;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)Invalid)]
    public enum PathMethod  // AI/Misc/Types/MoveToPathMethodType.type
    {
        Invalid = 0,
        Forward = 1,
        ForwardLoop = 5,
        ForwardBackAndForth = 3,
        Reverse = 2,
        ReverseLoop = 6,
        ReverseBackAndForth = 4,
    }

    #endregion

    public class ProceduralContextPrototype : Prototype
    {
        public virtual void OnStart(AIController owningController, ProceduralAIProfilePrototype procedurealProfile) { }
        public virtual void OnEnd(AIController owningController, ProceduralAIProfilePrototype procedurealProfile) { }
    }

    public class ProceduralUsePowerContextSwitchTargetPrototype : Prototype
    {
        public SelectEntityContextPrototype SelectTarget { get; protected set; }
        public bool SwitchPermanently { get; protected set; }
        public bool UsePowerOnCurTargetIfSwitchFails { get; protected set; }
    }

    public class ProceduralUsePowerContextPrototype : ProceduralContextPrototype
    {
        public int InitialCooldownMinMS { get; protected set; }
        public int MaxCooldownMS { get; protected set; }
        public int MinCooldownMS { get; protected set; }
        public UsePowerContextPrototype PowerContext { get; protected set; }
        public int PickWeight { get; protected set; }
        public ProceduralUsePowerContextSwitchTargetPrototype TargetSwitch { get; protected set; }
        public int InitialCooldownMaxMS { get; protected set; }
        public PrototypeId RestrictToDifficultyMin { get; protected set; }
        public PrototypeId RestrictToDifficultyMax { get; protected set; }
    }

    public class ProceduralUseAffixPowerContextPrototype : ProceduralContextPrototype
    {
        public UseAffixPowerContextPrototype AffixContext { get; protected set; }
        public int PickWeight { get; protected set; }
    }

    public class ProceduralFlankContextPrototype : ProceduralContextPrototype
    {
        public int MaxFlankCooldownMS { get; protected set; }
        public int MinFlankCooldownMS { get; protected set; }
        public FlankContextPrototype FlankContext { get; protected set; }
    }

    public class ProceduralInteractContextPrototype : ProceduralContextPrototype
    {
        public InteractContextPrototype InteractContext { get; protected set; }
    }

    public class ProceduralFleeContextPrototype : ProceduralContextPrototype
    {
        public int MaxFleeCooldownMS { get; protected set; }
        public int MinFleeCooldownMS { get; protected set; }
        public FleeContextPrototype FleeContext { get; protected set; }
    }

    public class ProceduralSyncAttackContextPrototype : Prototype
    {
        public PrototypeId TargetEntity { get; protected set; }
        public PrototypeId TargetEntityPower { get; protected set; }
        public ProceduralUsePowerContextPrototype LeaderPower { get; protected set; }
    }

    public class ProceduralThresholdPowerContextPrototype : ProceduralUsePowerContextPrototype
    {
        public float HealthThreshold { get; protected set; }
    }

    public class ProceduralPowerWithSpecificTargetsPrototype : Prototype
    {
        public float HealthThreshold { get; protected set; }
        public PrototypeId PowerToUse { get; protected set; }
        public PrototypeId[] Targets { get; protected set; }   // VectorPrototypeRefPtr AgentPrototype
    }

    
}
