using System.Text;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Powers
{
    public class Power
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private bool _isTeamUpPassivePowerWhileAway;

        public Game Game { get; }
        public PrototypeId PrototypeDataRef { get; }
        public PowerPrototype Prototype { get; }
        public WorldEntity Owner { get; private set; }
        public bool RequiresLineOfSight { get; private set; }

        public PowerCategoryType PowerCategory { get => Prototype != null ? Prototype.PowerCategory : PowerCategoryType.None; }
        public bool IsNormalPower { get => PowerCategory == PowerCategoryType.NormalPower; }
        public bool IsGameFunctionPower { get => PowerCategory == PowerCategoryType.GameFunctionPower; }
        public bool IsEmotePower { get => PowerCategory == PowerCategoryType.EmotePower; }
        public bool IsThrowablePower { get => PowerCategory == PowerCategoryType.ThrowablePower; }
        public bool IsComboEffect() => PowerCategory == PowerCategoryType.ComboEffect;  // This needs to be a method because it has the same name as the static method

        public PropertyCollection Properties { get; } = new();
        public bool IsTravelPower { get => Prototype != null && Prototype.IsTravelPower; }
        public bool IsChannelingPower { get; internal set; }
        public TargetingStylePrototype TargetingStylePrototype { get; internal set; }
        public bool IsOnExtraActivation { get; internal set; }
        public bool IsOwnerCenteredAOE { get; internal set; }
        public bool LOSCheckAlongGround { get; internal set; }
        public bool AlwaysTargetsMousePosition { get; internal set; }
        public bool IsMelee { get; internal set; }

        public Power(Game game, PrototypeId prototypeDataRef)
        {
            Game = game;
            PrototypeDataRef = prototypeDataRef;
            Prototype = prototypeDataRef.As<PowerPrototype>();
        }

        public bool Initialize(WorldEntity owner, bool isTeamUpPassivePowerWhileAway, PropertyCollection initializeProperties)
        {
            Owner = owner;
            _isTeamUpPassivePowerWhileAway = isTeamUpPassivePowerWhileAway;

            if (Prototype == null)
                return Logger.WarnReturn(false, $"Initialize(): Prototype == null");

            GeneratePowerProperties(Properties, Prototype, initializeProperties, Owner);
            // TODO: Power::createSituationalComponent()

            return true;
        }

        public void OnAssign()
        {
            // TODO
        }

        public void OnOwnerExitedWorld()
        {
        }

        public static void GeneratePowerProperties(PropertyCollection primaryCollection, PowerPrototype prototype, PropertyCollection initializeProperties, WorldEntity owner)
        {
            // Start with a clean copy from the prototype
            if (prototype.Properties != null)
                primaryCollection.FlattenCopyFrom(prototype.Properties, true);

            // Add properties from the collection passed in the Initialize() method if we have one
            if (initializeProperties != null)
                primaryCollection.FlattenCopyFrom(initializeProperties, false);

            // Set properties for all keywords assigned in the prototype
            if (prototype.Keywords != null)
            {
                foreach (PrototypeId keywordRef in prototype.Keywords)
                    primaryCollection[PropertyEnum.HasPowerKeyword, keywordRef] = true;
            }

            if (prototype.EvalOnCreate != null)
            {
                // TODO
            }

            if (prototype.EvalPowerSynergies != null)
            {
                // TODO
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(PrototypeDataRef)}: {GameDatabase.GetPrototypeName(PrototypeDataRef)}");
            sb.AppendLine($"{nameof(Owner)}: {(Owner != null ? Owner.Id : 0)}");
            sb.AppendLine($"{nameof(Properties)}: {Properties}");
            return sb.ToString();
        }

        // Static accessors
        public static PowerCategoryType GetPowerCategory(PowerPrototype powerProto) => powerProto.PowerCategory;
        public static bool IsComboEffect(PowerPrototype powerProto) => GetPowerCategory(powerProto) == PowerCategoryType.ComboEffect;
        public static bool IsUltimatePower(PowerPrototype powerProto) => powerProto.IsUltimate;

        public static TargetingShapeType GetTargetingShape(PowerPrototype powerProto)
        {
            throw new NotImplementedException();
        }

        internal TimeSpan GetCooldownTimeRemaining()
        {
            throw new NotImplementedException();
        }

        internal bool IsInRange(WorldEntity target, RangeCheckType checkType)
        {
            throw new NotImplementedException();
        }

        internal bool IsInRange(Vector3 position, RangeCheckType activation)
        {
            throw new NotImplementedException();
        }

        internal bool EndPower(EndFlag endFlag)
        {
            throw new NotImplementedException();
        }

        internal static int ComputeNearbyPlayers(Region region, Vector3 position, int min, bool combatActive, HashSet<ulong> nearbyPlayers = null)
        {
            throw new NotImplementedException();
        }

        internal float GetRange()
        {
            throw new NotImplementedException();
        }

        internal TimeSpan GetFullExecutionTime()
        {
            throw new NotImplementedException();
        }

        internal TargetingShapeType GetTargetingShape()
        {
            throw new NotImplementedException();
        }

        internal bool TargetsAOE()
        { 
            throw new NotImplementedException(); 
        }

        internal bool TargetsAOE(PowerPrototype powerProto)
        {
            throw new NotImplementedException();
        }

        internal bool IsTargetInAOE(WorldEntity target, WorldEntity owner, Vector3 userPos, Vector3 aimPos, float aoeRadius,
            int beamSweepCount, TimeSpan beamSweepTime, PowerPrototype powerProto, PropertyCollection properties)
        {
            throw new NotImplementedException();
        }

        internal float GetApplicationRange()
        {
            throw new NotImplementedException();
        }

        static internal bool ValidateAOETarget(WorldEntity target, PowerPrototype powerProto, WorldEntity user, Vector3 powerUserPosition,
            AlliancePrototype userAllianceProto, bool needsLineOfSight)
        {
            throw new NotImplementedException();
        }

        internal bool NeedsTarget() 
        {
            throw new NotImplementedException(); 
        }

        internal PowerPositionSweepResult PowerPositionSweep(RegionLocation startLocation, Vector3 targetPosition, ulong targetEntityId, Vector3 resultPosition, bool forceDoNotPassTarget = false, float maxRangeOverride = 0f)
        {
            throw new NotImplementedException();
        }

        internal PowerUseResult Activate(PowerActivationSettings powerSettings)
        {
            throw new NotImplementedException();
        }

        internal bool IsExclusiveActivation { get; }
        public bool IsEnding { get; internal set; }
        public TimeSpan LastActivateGameTime { get; internal set; }
        public TimeSpan AnimationTime { get; internal set; }
        public bool IsItemPower { get; internal set; }
        public bool IsPartOfAMovementPower { get; internal set; }
        public bool PreventsNewMovementWhileActive { get; internal set; }
        public bool IsNonCancellableChannelPower { get; internal set; }
        public bool IsCancelledOnMove { get; internal set; }
        public bool DisableOrientationWhileActive { get; internal set; }
        public bool ShouldOrientToTarget { get; internal set; }

        internal bool PowerLOSCheck(RegionLocation regionLocation, Vector3 position, ulong targetId, out Vector3 resultPos, bool lOSCheckAlongGround)
        {
            throw new NotImplementedException();
        }

        internal PowerUseResult CanActivate(WorldEntity target, Vector3 targetPosition, PowerActivationSettingsFlags flags)
        {
            throw new NotImplementedException();
        }

        internal static bool CanBeUsedInRegion(PowerPrototype powerProto, PropertyCollection powerProperties, Region region)
        {
            throw new NotImplementedException();
        }

        internal static bool IsMovementPower(PowerPrototype powerProto)
        {
            throw new NotImplementedException();
        }

        internal static bool IsValidTarget(PowerPrototype powerProto, WorldEntity worldEntity1, AlliancePrototype alliance, WorldEntity worldEntity2)
        {
            throw new NotImplementedException();
        }

        internal bool TriggersComboPowerOnEvent(PowerEventType onPowerEnd)
        {
            throw new NotImplementedException();
        }
    }

    [Flags]
    public enum EndFlag
    {
        None = 0,
        ExplicitCancel = 1 << 0,
        Interrupting = 1 << 3,
        Force = 1 << 8,
    }

    public enum PowerPositionSweepResult
    {
        Error,
        Success,
        TargetPositionInvalid,
        Clipped,
    };

    public enum RangeCheckType
    {
        Activation,
        Application
    }

    public enum PowerUseResult
    {
        Success = 0,
        Cooldown = 1,
        RestrictiveCondition = 2,
        BadTarget = 3,
        AbilityMissing = 4,
        TargetIsMissing = 5,
        InsufficientCharges = 6,
        InsufficientEndurance = 7,
        InsufficientSecondaryResource = 8,
        PowerInProgress = 9,
        OutOfPosition = 10,
        SummonSimultaneousLimit = 11,
        SummonLifetimeLimit = 12,
        WeaponMissing = 13,
        RegionRestricted = 14,
        NoFlyingUse = 15,
        ExtraActivationFailed = 16,
        GenericError = 17,
        OwnerNotSimulated = 18,
        OwnerDead = 19,
        ItemUseRestricted = 20,
        MinimumReactivateTime = 21,
        DisabledByLiveTuning = 22,
        NotAllowedByTransformMode = 23,
        FullscreenMovie = 24,
        ForceFailed = 25,
    }

    [Flags]
    public enum PowerActivationSettingsFlags
    {
        None = 0,
        Flag7 = 1 << 7,
    }

    public struct PowerActivationSettings
    {
        public ulong TargetEntityId;
        public Vector3 UserPosition;
        public Vector3 TargetPosition;

        public PowerActivationSettings(ulong targetEntityId, Vector3 userPosition, Vector3 targetPosition)
        {
            TargetEntityId = targetEntityId;
            UserPosition = userPosition;
            TargetPosition = targetPosition;
        }
    }
}
