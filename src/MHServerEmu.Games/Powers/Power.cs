using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
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
        public TargetingStylePrototype TargetingStylePrototype { get; }
        public GamepadSettingsPrototype GamepadSettingsPrototype { get; }

        public WorldEntity Owner { get; private set; }
        public PropertyCollection Properties { get; } = new();

        public float AnimSpeedCache { get; private set; } = -1f;

        public bool IsChannelingPower { get; set; }
        public bool IsOnExtraActivation { get; set; }
        public bool LOSCheckAlongGround { get; set; }
        public bool AlwaysTargetsMousePosition { get; set; }
        public bool RequiresLineOfSight { get; private set; }

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

            TargetingStylePrototype = Prototype.TargetingStyle.As<TargetingStylePrototype>();
            GamepadSettingsPrototype = Prototype.GamepadSettings.As<GamepadSettingsPrototype>();
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

        public void OnOwnerCastSpeedChange()
        {
            AnimSpeedCache = -1f;
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

        public bool EndPower(EndPowerFlags flags)
        {
            throw new NotImplementedException();
        }

        public bool IsTargetInAOE(WorldEntity target, WorldEntity owner, Vector3 userPos, Vector3 aimPos, float aoeRadius,
            int beamSweepCount, TimeSpan beamSweepTime, PowerPrototype powerProto, PropertyCollection properties)
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

        public static bool IsValidTarget(PowerPrototype powerProto, WorldEntity worldEntity1, AlliancePrototype alliance, WorldEntity worldEntity2)
        {
            throw new NotImplementedException();
        }

        #region Prototype Accessors

        // NOTE: We have to use methods instead of properties here because we can't have static methods and properties share the same name.

        public PowerCategoryType GetPowerCategory()
        {
            return Prototype != null ? Prototype.PowerCategory : PowerCategoryType.None;
        }

        public bool IsNormalPower()
        {
            return GetPowerCategory() == PowerCategoryType.NormalPower;
        }

        public bool IsGameFunctionPower()
        {
            return GetPowerCategory() == PowerCategoryType.GameFunctionPower;
        }

        public bool IsEmotePower()
        {
            return GetPowerCategory() == PowerCategoryType.EmotePower;
        }

        public bool IsThrowablePower()
        {
            return GetPowerCategory() == PowerCategoryType.ThrowablePower;
        }

        public static PowerCategoryType GetPowerCategory(PowerPrototype powerProto)
        {
            return powerProto.PowerCategory;
        }

        public PowerActivationType GetPowerActivationType()
        {
            return Prototype != null ? Prototype.Activation : PowerActivationType.None;
        }

        public static PowerActivationType GetPowerActivationType(PowerPrototype powerProto)
        {
            return powerProto.Activation;
        }

        public bool IsComboEffect()
        {
            return GetPowerCategory() == PowerCategoryType.ComboEffect;
        }

        public static bool IsComboEffect(PowerPrototype powerProto)
        {
            return GetPowerCategory(powerProto) == PowerCategoryType.ComboEffect;
        }

        public static bool IsUltimatePower(PowerPrototype powerProto)
        {
            return powerProto.IsUltimate;
        }

        public bool IsTravelPower()
        {
            return Prototype != null && Prototype.IsTravelPower;
        }

        public static bool IsTravelPower(PowerPrototype powerProto)
        {
            return powerProto.IsTravelPower;
        }

        public bool IsMovementPower()
        {
            return Prototype is MovementPowerPrototype;
        }

        public static bool IsMovementPower(PowerPrototype powerProto)
        {
            return powerProto is MovementPowerPrototype;
        }

        public bool NeedsTarget()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "NeedsTarget(): powerProto == null");
            return NeedsTarget(powerProto);
        }

        public static bool NeedsTarget(PowerPrototype powerProto)
        {
            TargetingStylePrototype stylePrototype = powerProto.GetTargetingStyle();
            if (stylePrototype == null) return Logger.WarnReturn(false, "NeedsTarget(): stylePrototype == null");
            return stylePrototype.NeedsTarget;
        }

        public bool TargetsAOE()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "TargetsAOE(): powerProto == null");
            return TargetsAOE(powerProto);
        }

        public static bool TargetsAOE(PowerPrototype powerProto)
        {
            TargetingStylePrototype stylePrototype = powerProto.GetTargetingStyle();
            if (stylePrototype == null) return Logger.WarnReturn(false, "TargetsAOE(): stylePrototype == null");
            return stylePrototype.TargetsAOE();
        }

        public bool IsOwnerCenteredAOE()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsOwnerCenteredAOE(): powerProto == null");
            return IsOwnerCenteredAOE(powerProto);
        }

        public static bool IsOwnerCenteredAOE(PowerPrototype powerProto)
        {
            TargetingStylePrototype stylePrototype = powerProto.GetTargetingStyle();
            if (stylePrototype == null) return Logger.WarnReturn(false, "IsOwnerCenteredAOE(): stylePrototype == null");
            return stylePrototype.AOESelfCentered;
        }

        public float GetAOERadius()
        {
            if (Owner == null) return Logger.WarnReturn(0f, "GetAOERadius(): Owner == null");
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(0f, "GetAOERadius(): powerProto == null");
            return GetAOERadius();
        }

        public static float GetAOERadius(PowerPrototype powerProto, PropertyCollection ownerProperties = null)
        {
            float radius = powerProto.Radius;
            radius *= GetAOESizePctModifier(powerProto, ownerProperties);
            return radius;
        }

        public static float GetAOESizePctModifier(PowerPrototype powerProto, PropertyCollection ownerProperties)
        {
            // TODO
            return 1f;
        }

        public TargetingShapeType GetTargetingShape()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(TargetingShapeType.None, "GetTargetingShape(): powerProto == null");
            return GetTargetingShape(powerProto);
        }

        public static TargetingShapeType GetTargetingShape(PowerPrototype powerProto)
        {
            TargetingStylePrototype stylePrototype = powerProto.GetTargetingStyle();
            if (stylePrototype == null) return Logger.WarnReturn(TargetingShapeType.None, "GetTargetingShape(): stylePrototype == null");
            return stylePrototype.TargetingShape;
        }

        public bool IsMelee()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsMelee(): powerProto == null");
            return IsMelee(powerProto);
        }

        public static bool IsMelee(PowerPrototype powerProto)
        {
            var reachProto = powerProto.TargetingReach.As<TargetingReachPrototype>();
            if (reachProto == null) return Logger.WarnReturn(false, "IsMelee(): reachProto == null");
            return reachProto.Melee;
        }

        public float GetRange()
        {
            if (Owner == null) return Logger.WarnReturn(0f, "GetRange(): Owner == null");

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(0f, "GetRange(): powerProto == null");

            float range;

            if (Owner is Avatar avatar && avatar.IsUsingGamepadInput && GetGamepadRange() > 0f)
                range = GetGamepadRange();
            else
                range = GetRange(powerProto, Properties, Owner.Properties);

            if (powerProto.PowerCategory == PowerCategoryType.MissileEffect)
                range = Math.Max(range, Owner.EntityCollideBounds.Radius);

            return range;
        }

        public static float GetRange(PowerPrototype powerProto, PropertyCollection powerProperties, PropertyCollection ownerProperties)
        {
            float range = IsOwnerCenteredAOE(powerProto) ? GetAOERadius(powerProto) : powerProto.GetRange(powerProperties, ownerProperties);

            if (ownerProperties != null && range > 0f && IsMelee(powerProto) == false && IsOwnerCenteredAOE(powerProto) == false)
            {
                range += ownerProperties[PropertyEnum.RangeModifier];
                // TODO: Power::AccumulateKeywordProperties<float>()
            }

            return range;
        }

        public float GetGamepadRange()
        {
            if (GamepadSettingsPrototype == null)
                return 0f;

            return GamepadSettingsPrototype.Range;
        }

        public float GetAnimSpeed()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(1f, "GetAnimSpeed(): powerProto == null");
            return GetAnimSpeed(powerProto, Owner, this);
        }

        public static float GetAnimSpeed(PowerPrototype powerProto, WorldEntity owner, Power power)
        {
            // -1 is invalid speed cache
            if (power != null && power.AnimSpeedCache >= 0f)
                return power.AnimSpeedCache;

            float animSpeed = 1f;

            // No owner to get speed bonuses from
            if (owner == null)
                return Logger.WarnReturn(animSpeed, "GetAnimSpeed(): powerOwner == null");

            // Movement power animations don't scale with cast speed
            if (IsMovementPower(powerProto) == false)
                animSpeed = owner.GetCastSpeedPct(powerProto);

            // Update cache
            if (power != null)
                power.AnimSpeedCache = animSpeed;

            return animSpeed;
        }

        public TimeSpan GetAnimationTime()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(TimeSpan.Zero, "GetAnimationTime(): powerProto == null");
            return GetAnimationTime(powerProto, Owner, this);
        }

        public static TimeSpan GetAnimationTime(PowerPrototype powerProto, WorldEntity owner, Power power)
        {
            if (owner == null) return Logger.WarnReturn(TimeSpan.Zero, "GetAnimationTime(): owner == null");

            TimeSpan baseTime = powerProto.GetAnimationTime(owner.GetOriginalWorldAsset(), owner.GetEntityWorldAsset());
            float animSpeed = GetAnimSpeed(powerProto, owner, power);
            TimeSpan result = animSpeed > 0f ? baseTime / animSpeed : TimeSpan.Zero;

            // What exactly are these Bad Things? o_o
            if (baseTime != TimeSpan.Zero && result <= TimeSpan.Zero)
                Logger.Warn($"GetAnimationTime(): The following power has a non-zero animation time, but bonuses on the character are such that the time is being reduced to 0, which will cause Bad Things to happen...\n[{powerProto}]");

            return result;
        }

        public TimeSpan GetChannelStartTime()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(TimeSpan.Zero, "GetChannelStartTime(): powerProto == null");
            return GetChannelStartTime(powerProto, Owner, this);
        }

        public static TimeSpan GetChannelStartTime(PowerPrototype powerProto, WorldEntity owner, Power power)
        {
            float animSpeed = GetAnimSpeed(powerProto, owner, power);
            float timeMult = animSpeed > 0f ? 1f / animSpeed : 0f;     // Avoid division by 0 / negative
            return powerProto.ChannelStartTime * timeMult;
        }

        public TimeSpan GetChannelLoopTime()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(TimeSpan.Zero, "GetChannelLoopTime(): powerProto == null");
            return GetChannelLoopTime(powerProto, Owner, Properties, this);
        }

        public static TimeSpan GetChannelLoopTime(PowerPrototype powerProto, WorldEntity owner, PropertyCollection powerProperties, Power power)
        {
            if (owner == null) return Logger.WarnReturn(TimeSpan.Zero, "GetChannelLoopTime(): owner == null");

            float timeMult = 1f;

            if (powerProto.IsRecurring)
            {
                float animSpeed = GetAnimSpeed(powerProto, owner, power);
                timeMult = animSpeed > 0f ? 1f / animSpeed : 0f;     // Avoid division by 0 / negative
            }

            if (powerProto.OmniDurationBonusExclude == false)
            {
                timeMult += owner.Properties[PropertyEnum.OmniDurationBonusPct];
                timeMult = MathF.Max(timeMult, 0.5f);
            }

            return powerProto.GetChannelLoopTime(powerProperties, owner.Properties) * timeMult;
        }

        public TimeSpan GetChannelEndTime()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(TimeSpan.Zero, "GetChannelEndTime(): powerProto == null");
            return GetChannelEndTime(powerProto, Owner, this);
        }

        public static TimeSpan GetChannelEndTime(PowerPrototype powerProto, WorldEntity owner, Power power)
        {
            float animSpeed = GetAnimSpeed(powerProto, owner, power);
            float timeMult = animSpeed > 0f ? 1f / animSpeed : 0f;     // Avoid division by 0 / negative
            return powerProto.ChannelEndTime * timeMult;
        }

        public TimeSpan GetChannelMinTime()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(TimeSpan.Zero, "GetChannelMinTime(): powerProto == null");
            return GetChannelMinTime(powerProto, Owner, Properties, this);
        }

        public static TimeSpan GetChannelMinTime(PowerPrototype powerProto, WorldEntity owner, PropertyCollection powerProperties, Power power)
        {
            if (powerProto.IsRecurring)
            {
                // NOTE: We calculate using ticks here to avoid unnecessary conversions to float / double
                long channelStartTime = GetChannelStartTime(powerProto, owner, power).Ticks;
                long channelLoopTime = GetChannelLoopTime(powerProto, owner, powerProperties, power).Ticks;
                long channelMinTime = powerProto.ChannelMinTime.Ticks;
                return TimeSpan.FromTicks(Math.Max(channelStartTime + channelLoopTime, channelMinTime));
            }

            float animSpeed = GetAnimSpeed(powerProto, owner, power);
            float timeMult = animSpeed > 0f ? 1f / animSpeed : 0f;     // Avoid division by 0 / negative
            return powerProto.ChannelMinTime * timeMult;
        }

        public TimeSpan GetTotalChannelingTime()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(TimeSpan.Zero, "GetTotalChannelingTime(): powerProto == null");
            return GetTotalChannelingTime(powerProto, Owner, Properties, this);
        }

        public static TimeSpan GetTotalChannelingTime(PowerPrototype powerProto, WorldEntity owner, PropertyCollection powerProperties, Power power)
        {
            TimeSpan channelStartTime = GetChannelStartTime(powerProto, owner, power);
            TimeSpan channelLoopTime = GetChannelLoopTime(powerProto, owner, powerProperties, power);
            TimeSpan channelEndTime = GetChannelEndTime(powerProto, owner, power);
            return channelStartTime + channelLoopTime + channelEndTime;
        }

        public TimeSpan GetChargingTime()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(TimeSpan.Zero, "GetChargingTime(): powerProto == null");
            return GetChargingTime(powerProto, Owner, this);
        }

        public static TimeSpan GetChargingTime(PowerPrototype powerProto, WorldEntity owner, Power power)
        {
            float animSpeed = GetAnimSpeed(powerProto, owner, power);
            return animSpeed > 0f ? powerProto.ChargeTime / animSpeed : TimeSpan.Zero;  // Avoid division by 0 / negative
        }

        #endregion

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

        public TimeSpan GetFullExecutionTime()
        {
            throw new NotImplementedException();
        }

        public float GetApplicationRange()
        {
            throw new NotImplementedException();
        }

        public bool TriggersComboPowerOnEvent(PowerEventType onPowerEnd)
        {
            throw new NotImplementedException();
        }
    }
}
