using MHServerEmu.Games.GameData.Calligraphy.Attributes;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)Type1)]
    public enum ManaType
    {
        Type1 = 0,
        Type2 = 1,
        NumTypes = 2,
        TypeAll = 3,
    }

    [AssetEnum((int)Force)]
    public enum ResourceType
    {
        Force = 0,
        Focus = 1,
        Fury = 2,
        Secondary_Pips = 3,
        Secondary_Gauge = 4,
    }

    #endregion

    public class ManaBehaviorPrototype : Prototype
    {
        public LocaleStringId DisplayName { get; protected set; }
        public ResourceType MeterType { get; protected set; }
        public PrototypeId[] Powers { get; protected set; }
        public bool StartsEmpty { get; protected set; }
        public LocaleStringId Description { get; protected set; }
        public AssetId MeterColor { get; protected set; }
        public AssetId ResourceBarStyle { get; protected set; }
        public AssetId ResourcePipStyle { get; protected set; }
        public bool DepleteOnDeath { get; protected set; }
    }

    public class PrimaryResourceManaBehaviorPrototype : ManaBehaviorPrototype
    {
        public bool StartsWithRegenEnabled { get; protected set; }
        public int RegenUpdateTimeMS { get; protected set; }
        public EvalPrototype EvalOnEnduranceUpdate { get; protected set; }
        public ManaType ManaType { get; protected set; }
        public CurveId BaseEndurancePerLevel { get; protected set; }
        public bool RestoreToMaxOnLevelUp { get; protected set; }
    }

    public class SecondaryResourceManaBehaviorPrototype : ManaBehaviorPrototype
    {
        public EvalPrototype EvalGetCurrentForDisplay { get; protected set; }
        public EvalPrototype EvalGetCurrentPipsForDisplay { get; protected set; }
        public EvalPrototype EvalGetMaxForDisplay { get; protected set; }
        public EvalPrototype EvalGetMaxPipsForDisplay { get; protected set; }
        public bool DepleteOnExitWorld { get; protected set; }
        public bool ResetOnAvatarSwap { get; protected set; }
    }
}
