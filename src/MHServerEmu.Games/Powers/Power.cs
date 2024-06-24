using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;
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
        public bool IsChannelingPower { get; set; }
        public TargetingStylePrototype TargetingStylePrototype { get; set; }
        public bool IsOnExtraActivation { get; set; }
        public bool IsOwnerCenteredAOE { get; set; }
        public bool LOSCheckAlongGround { get; set; }
        public bool AlwaysTargetsMousePosition { get; set; }
        public bool IsMelee { get; set; }

        public bool IsExclusiveActivation { get; }
        public bool IsEnding { get; set; }
        public TimeSpan LastActivateGameTime { get; set; }
        public TimeSpan AnimationTime { get; set; }
        public bool IsItemPower { get; set; }
        public bool IsPartOfAMovementPower { get; set; }
        public bool PreventsNewMovementWhileActive { get; set; }
        public bool IsNonCancellableChannelPower { get; set; }
        public bool IsCancelledOnMove { get; set; }
        public bool DisableOrientationWhileActive { get; set; }
        public bool ShouldOrientToTarget { get; set; }

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

        public static void GeneratePowerProperties(PropertyCollection primaryCollection, PowerPrototype powerProto, PropertyCollection initializeProperties, WorldEntity owner)
        {
            // Start with a clean copy from the prototype
            if (powerProto.Properties != null)
                primaryCollection.FlattenCopyFrom(powerProto.Properties, true);

            // Add properties from the collection passed in the Initialize() method if we have one
            if (initializeProperties != null)
                primaryCollection.FlattenCopyFrom(initializeProperties, false);

            // Set properties for all keywords assigned in the prototype
            if (powerProto.Keywords != null)
            {
                foreach (PrototypeId keywordRef in powerProto.Keywords)
                    primaryCollection[PropertyEnum.HasPowerKeyword, keywordRef] = true;
            }

            // Run evals
            if (powerProto.EvalOnCreate.HasValue())
            {
                EvalContextData contextData = new(owner?.Game);
                contextData.SetVar_PropertyCollectionPtr(EvalContext.Default, primaryCollection);
                contextData.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, owner.Properties);
                contextData.SetReadOnlyVar_EntityPtr(EvalContext.Var1, owner);

                Eval.InitTeamUpEvalContext(contextData, owner);

                foreach (EvalPrototype evalProto in powerProto.EvalOnCreate)
                {
                    if (Eval.RunBool(evalProto, contextData) == false)
                        Logger.Warn($"GeneratePowerProperties(): The following EvalOnCreate Eval in a power failed:\nEval: [{evalProto.ExpressionString()}]\nPower: [{powerProto}]");
                }

            }

            if (powerProto.EvalPowerSynergies != null)
            {
                EvalContextData contextData = new(owner?.Game);
                contextData.SetVar_PropertyCollectionPtr(EvalContext.Default, primaryCollection);
                contextData.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, owner.Properties);
                contextData.SetReadOnlyVar_ConditionCollectionPtr(EvalContext.Var1, owner?.ConditionCollection);
                contextData.SetReadOnlyVar_EntityPtr(EvalContext.Var2, owner);

                Eval.InitTeamUpEvalContext(contextData, owner);

                if (Eval.RunBool(powerProto.EvalPowerSynergies, contextData) == false)
                    Logger.Warn($"GeneratePowerProperties(): The EvalPowerSynergies in a power failed:\nPower: [{powerProto}]");
            }
        }

        public override string ToString()
        {
            return $"powerProtoRef={GameDatabase.GetPrototypeName(PrototypeDataRef)}, owner={Owner}";
        }

        public PowerUseResult CanActivate(WorldEntity target, Vector3 targetPosition, PowerActivationSettingsFlags flags)
        {
            throw new NotImplementedException();
        }

        public PowerUseResult Activate(PowerActivationSettings powerSettings)
        {
            throw new NotImplementedException();
        }

        public TimeSpan GetCooldownTimeRemaining()
        {
            throw new NotImplementedException();
        }

        public bool IsInRange(WorldEntity target, RangeCheckType checkType)
        {
            throw new NotImplementedException();
        }

        public bool IsInRange(Vector3 position, RangeCheckType activation)
        {
            throw new NotImplementedException();
        }

        public bool EndPower(EndFlag endFlag)
        {
            throw new NotImplementedException();
        }

        public float GetRange()
        {
            throw new NotImplementedException();
        }

        public TimeSpan GetFullExecutionTime()
        {
            throw new NotImplementedException();
        }

        public TargetingShapeType GetTargetingShape()
        {
            throw new NotImplementedException();
        }

        public bool TargetsAOE()
        { 
            throw new NotImplementedException(); 
        }

        public bool TargetsAOE(PowerPrototype powerProto)
        {
            throw new NotImplementedException();
        }

        public bool IsTargetInAOE(WorldEntity target, WorldEntity owner, Vector3 userPos, Vector3 aimPos, float aoeRadius,
            int beamSweepCount, TimeSpan beamSweepTime, PowerPrototype powerProto, PropertyCollection properties)
        {
            throw new NotImplementedException();
        }

        public float GetApplicationRange()
        {
            throw new NotImplementedException();
        }

        public bool NeedsTarget() 
        {
            throw new NotImplementedException(); 
        }

        public PowerPositionSweepResult PowerPositionSweep(RegionLocation startLocation, Vector3 targetPosition, ulong targetEntityId, Vector3 resultPosition, bool forceDoNotPassTarget = false, float maxRangeOverride = 0f)
        {
            throw new NotImplementedException();
        }

        public bool PowerLOSCheck(RegionLocation regionLocation, Vector3 position, ulong targetId, out Vector3 resultPos, bool lOSCheckAlongGround)
        {
            throw new NotImplementedException();
        }

        public bool TriggersComboPowerOnEvent(PowerEventType onPowerEnd)
        {
            throw new NotImplementedException();
        }

        #region Static

        public static PowerCategoryType GetPowerCategory(PowerPrototype powerProto)
        {
            return powerProto.PowerCategory;
        }

        public static bool IsComboEffect(PowerPrototype powerProto)
        {
            return GetPowerCategory(powerProto) == PowerCategoryType.ComboEffect;
        }

        public static bool IsUltimatePower(PowerPrototype powerProto)
        {
            return powerProto.IsUltimate;
        }

        public static TargetingShapeType GetTargetingShape(PowerPrototype powerProto)
        {
            throw new NotImplementedException();
        }

        public static int ComputeNearbyPlayers(Region region, Vector3 position, int min, bool combatActive, HashSet<ulong> nearbyPlayers = null)
        {
            throw new NotImplementedException();
        }

        public static bool ValidateAOETarget(WorldEntity target, PowerPrototype powerProto, WorldEntity user, Vector3 powerUserPosition,
            AlliancePrototype userAllianceProto, bool needsLineOfSight)
        {
            throw new NotImplementedException();
        }

        public static bool CanBeUsedInRegion(PowerPrototype powerProto, PropertyCollection powerProperties, Region region)
        {
            throw new NotImplementedException();
        }

        public static bool IsMovementPower(PowerPrototype powerProto)
        {
            throw new NotImplementedException();
        }

        public static bool IsValidTarget(PowerPrototype powerProto, WorldEntity worldEntity1, AlliancePrototype alliance, WorldEntity worldEntity2)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
