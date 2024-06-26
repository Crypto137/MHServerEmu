using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Time;
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
        private SituationalPowerComponent _situationalComponent;
        private KeywordsMask _keywordsMask = new();

        private PowerActivationPhase _activationPhase = PowerActivationPhase.Inactive;

        public Game Game { get; }
        public PrototypeId PrototypeDataRef { get; }
        public PowerPrototype Prototype { get; }
        public TargetingStylePrototype TargetingStylePrototype { get; }
        public GamepadSettingsPrototype GamepadSettingsPrototype { get; }

        public WorldEntity Owner { get; private set; }
        public PropertyCollection Properties { get; } = new();

        public float AnimSpeedCache { get; private set; } = -1f;
        public TimeSpan LastActivateGameTime { get; private set; }

        public bool IsSituationalPower { get => _situationalComponent != null; }

        public int Rank { get => Properties[PropertyEnum.PowerRank]; }

        public bool IsInActivation { get => _activationPhase == PowerActivationPhase.Active; }
        public bool IsChanneling { get => _activationPhase == PowerActivationPhase.Channeling || _activationPhase == PowerActivationPhase.LoopEnding; }
        public bool IsEnding { get => _activationPhase == PowerActivationPhase.MinTimeEnding || _activationPhase == PowerActivationPhase.LoopEnding; }
        public bool IsCharging { get => _activationPhase == PowerActivationPhase.Charging; }
        public bool IsActive { get => IsInActivation || IsToggledOn() || IsChanneling || IsCharging || IsEnding || _activationPhase == PowerActivationPhase.ChannelStarting; }

        public Power(Game game, PrototypeId prototypeDataRef)
        {
            Game = game;
            PrototypeDataRef = prototypeDataRef;
            Prototype = prototypeDataRef.As<PowerPrototype>();

            TargetingStylePrototype = Prototype.TargetingStyle.As<TargetingStylePrototype>();
            GamepadSettingsPrototype = Prototype.GamepadSettings.As<GamepadSettingsPrototype>();
        }

        public override string ToString()
        {
            return $"powerProtoRef={GameDatabase.GetPrototypeName(PrototypeDataRef)}, owner={Owner}";
        }

        public bool Initialize(WorldEntity owner, bool isTeamUpPassivePowerWhileAway, PropertyCollection initializeProperties)
        {
            Owner = owner;
            _isTeamUpPassivePowerWhileAway = isTeamUpPassivePowerWhileAway;

            if (Prototype == null)
                return Logger.WarnReturn(false, $"Initialize(): Prototype == null");

            GeneratePowerProperties(Properties, Prototype, initializeProperties, Owner);
            CreateSituationalComponent();

            return true;
        }

        public bool OnAssign()
        {
            // Initialize situational component
            if (_situationalComponent != null)
            {
                _situationalComponent.Initialize();
                _situationalComponent.OnPowerAssigned();
            }

            // Initialize keywords
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "OnAssign(): powerProto == null");

            _keywordsMask = Prototype.KeywordsMask.Copy<KeywordsMask>();

            // Apply keyword changes from the owner avatar
            WorldEntity owner = Owner;
            if (owner is not Avatar)
            {
                owner = GetUltimateOwner();
                if (owner == null || owner is not Avatar)
                    return true;
            }

            foreach (var kvp in owner.Properties.IteratePropertyRange(PropertyEnum.PowerKeywordChange, powerProto.DataRef))
            {
                Property.FromParam(kvp.Key, 1, out PrototypeId keywordProtoRef);

                if (kvp.Value == (int)TriBool.True)
                    AddKeyword(keywordProtoRef);
                else
                    RemoveKeyword(keywordProtoRef);
            }

            return true;
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

        // Keywords

        public bool AddKeyword(PrototypeId keywordProtoRef)
        {
            var powerKeywordProto = GameDatabase.GetPrototype<PowerKeywordPrototype>(keywordProtoRef);
            if (powerKeywordProto == null) return Logger.WarnReturn(false, "AddKeyword(): powerKeywordProto == null");

            powerKeywordProto.GetBitMask(ref _keywordsMask);
            return true;
        }

        public bool RemoveKeyword(PrototypeId keywordProtoRef)
        {
            var powerKeywordProto = GameDatabase.GetPrototype<PowerKeywordPrototype>(keywordProtoRef);
            if (powerKeywordProto == null) return Logger.WarnReturn(false, "RemoveKeyword(): powerKeywordProto == null");

            _keywordsMask.Reset(powerKeywordProto.GetBitIndex());
            return true;
        }

        public bool HasKeyword(KeywordPrototype keywordProto)
        {
            return keywordProto != null && KeywordPrototype.TestKeywordBit(_keywordsMask, keywordProto);
        }

        public WorldEntity GetUltimateOwner()
        {
            if (Owner == null) return Logger.WarnReturn<WorldEntity>(null, "GetUltimateOwner(): Owner == null");

            if (Owner.HasPowerUserOverride == false)
                return Owner;

            ulong powerUserOverrideId = Owner.Properties[PropertyEnum.PowerUserOverrideID];
            if (powerUserOverrideId == Entity.InvalidId)
                return Owner;

            WorldEntity ultimateOwner = Game.EntityManager.GetEntity<WorldEntity>(powerUserOverrideId);
            if (ultimateOwner == null || ultimateOwner.IsInWorld == false)
                return null;

            return ultimateOwner;
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

        public bool IsInRange(WorldEntity target, RangeCheckType checkType)
        {
            throw new NotImplementedException();
        }

        public bool IsInRange(Vector3 position, RangeCheckType activation)
        {
            throw new NotImplementedException();
        }

        #region State Accessors

        public bool PreventsNewMovementWhileActive()
        {
            if (Prototype == null) return false;

            if (Prototype.MovementPreventWhileActive)
                return true;

            return _activationPhase switch
            {
                PowerActivationPhase.ChannelStarting => Prototype.MovementPreventChannelStart,
                PowerActivationPhase.Channeling => Prototype.MovementPreventChannelLoop,
                PowerActivationPhase.LoopEnding => Prototype.MovementPreventChannelEnd,
                _ => false,
            };
        }

        public bool IsToggledOn()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsToggledOn(): powerProto == null");
            if (Owner == null) return Logger.WarnReturn(false, "IsToggledOn(): Owner == null");
            return IsToggledOn(powerProto, Owner);
        }

        public static bool IsToggledOn(PowerPrototype powerProto, WorldEntity owner)
        {
            return owner.Properties[PropertyEnum.PowerToggleOn, powerProto.DataRef];
        }

        public TimeSpan GetCooldownTimeRemaining()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Data Accessors

        // NOTE: We have to use methods instead of properties here because we can't have static methods and properties share the same name.
        // NOTE: Do we actually need all these prototype null checks in instance methods?

        public PowerCategoryType GetPowerCategory()
        {
            return Prototype != null ? Prototype.PowerCategory : PowerCategoryType.None;
        }

        public static PowerCategoryType GetPowerCategory(PowerPrototype powerProto)
        {
            return powerProto.PowerCategory;
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

        public bool IsMissileEffect()
        {
            return GetPowerCategory() == PowerCategoryType.MissileEffect;
        }

        public static bool IsMissileEffect(PowerPrototype powerProto)
        {
            return GetPowerCategory(powerProto) == PowerCategoryType.MissileEffect;
        }

        public bool IsProcEffect()
        {
            return GetPowerCategory() == PowerCategoryType.ProcEffect;
        }

        public static bool IsProcEffect(PowerPrototype powerProto)
        {
            return GetPowerCategory(powerProto) == PowerCategoryType.ProcEffect;
        }

        public bool IsItemPower()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsItemPower(): powerProto == null");
            return IsItemPower(powerProto);
        }

        public static bool IsItemPower(PowerPrototype powerProto)
        {
            return GetPowerCategory(powerProto) == PowerCategoryType.ItemPower;
        }

        public bool IsRecurring()
        {
            return Prototype != null && Prototype.IsRecurring;
        }

        public PowerActivationType GetActivationType()
        {
            return Prototype != null ? Prototype.Activation : PowerActivationType.None;
        }

        public static PowerActivationType GetActivationType(PowerPrototype powerProto)
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

        public bool IsPartOfAMovementPower()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsPartOfAMovementPower(): powerProto == null");
            return IsPartOfAMovementPower(powerProto);
        }

        public static bool IsPartOfAMovementPower(PowerPrototype powerProto)
        {
            return powerProto is MovementPowerPrototype;
        }

        public bool IsChannelingPower()
        {
            return GetTotalChannelingTime() != TimeSpan.Zero && IsRecurring() == false;
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

        public bool AlwaysTargetsMousePosition()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "AlwaysTargetsMousePosition(): powerProto == null");
            return AlwaysTargetsMousePosition(powerProto);
        }

        public static bool AlwaysTargetsMousePosition(PowerPrototype powerProto)
        {
            TargetingStylePrototype stylePrototype = powerProto.GetTargetingStyle();
            if (stylePrototype == null) return Logger.WarnReturn(false, "AlwaysTargetsMousePosition(): stylePrototype == null");
            return stylePrototype.AlwaysTargetMousePos;
        }

        public bool ShouldOrientToTarget()
        {
            TargetingStylePrototype stylePrototype = TargetingStylePrototype;
            if (stylePrototype == null) return Logger.WarnReturn(false, "ShouldOrientToTarget(): stylePrototype == null");
            return stylePrototype.TurnsToFaceTarget;
        }

        public static bool ShouldOrientToTarget(PowerPrototype powerProto)
        {
            TargetingStylePrototype stylePrototype = powerProto.GetTargetingStyle();
            if (stylePrototype == null) return Logger.WarnReturn(false, "ShouldOrientToTarget(): stylePrototype == null");
            return stylePrototype.TurnsToFaceTarget;
        }

        public bool DisableOrientationWhileActive()
        {
            TargetingStylePrototype stylePrototype = TargetingStylePrototype;
            if (stylePrototype == null) return Logger.WarnReturn(false, "DisableOrientationWhileActive(): stylePrototype == null");
            return stylePrototype.DisableOrientationDuringPower;
        }

        public static bool DisableOrientationWhileActive(PowerPrototype powerProto)
        {
            TargetingStylePrototype stylePrototype = powerProto.GetTargetingStyle();
            if (stylePrototype == null) return Logger.WarnReturn(false, "DisableOrientationWhileActive(): stylePrototype == null");
            return stylePrototype.DisableOrientationDuringPower;
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
            TargetingReachPrototype reachProto = powerProto.GetTargetingReach();
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

        public float GetApplicationRange()
        {
            if (Owner == null) return Logger.WarnReturn(0f, "GetApplicationRange(): Owner == null");
            return TargetsAOE() ? GetAOERadius() : GetRange();
        }

        public bool RequiresLineOfSight()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "RequiresLineOfSight(): powerProto == null");
            return RequiresLineOfSight(powerProto);
        }

        public static bool RequiresLineOfSight(PowerPrototype powerProto)
        {
            TargetingReachPrototype targetingReachProto = powerProto.GetTargetingReach();
            if (targetingReachProto == null) return Logger.WarnReturn(false, "RequiresLineOfSight(): targetingReachProto == null");
            return targetingReachProto.RequiresLineOfSight;
        }

        public bool LOSCheckAlongGround()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "LOSCheckAlongGround(): powerProto == null");

            TargetingReachPrototype targetingReachProto = powerProto.GetTargetingReach();
            if (targetingReachProto == null) return Logger.WarnReturn(false, "LostCheckAlongGround(): targetingReachProto == null");

            return targetingReachProto.LOSCheckAlongGround;
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
            {
                Logger.Warn($"GetAnimationTime(): The following power has a non-zero animation time, but bonuses on the character are such" +
                    $" that the time is being reduced to 0, which will cause Bad Things to happen...\n[{powerProto}]");
            }

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
                TimeSpan channelStartTime = GetChannelStartTime(powerProto, owner, power);
                TimeSpan channelLoopTime = GetChannelLoopTime(powerProto, owner, powerProperties, power);
                TimeSpan channelMinTime = powerProto.ChannelMinTime;
                return Clock.Max(channelStartTime + channelLoopTime, channelMinTime);
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

        public TimeSpan GetFullExecutionTime()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(TimeSpan.Zero, "GetFullExecutionTime(): powerProto == null");
            return GetFullExecutionTime(powerProto, Owner, Properties, this);
        }

        public static TimeSpan GetFullExecutionTime(PowerPrototype powerProto, WorldEntity owner, PropertyCollection powerProperties, Power power)
        {
            TimeSpan chargingTime = GetChargingTime(powerProto, owner, power);
            TimeSpan animationTime = GetAnimationTime(powerProto, owner, power);
            TimeSpan standardExecutionTime = chargingTime + animationTime;

            TimeSpan totalChannelingTime = GetTotalChannelingTime(powerProto, owner, powerProperties, power);
            if (totalChannelingTime > TimeSpan.Zero)
            {
                if (standardExecutionTime > TimeSpan.Zero)
                {
                    Logger.Warn($"GetFullExecutionTime(): The following power has non-zero charging/standard-anim time AND non-zero channel time," +
                        $" which are incompatible! Using the channel time only.\nPower: [{powerProto}]");
                }

                return totalChannelingTime;
            }

            return standardExecutionTime;
        }

        public TimeSpan GetCooldownDuration()
        {
            if (Owner == null) return Logger.WarnReturn(TimeSpan.Zero, "GetCooldownDuration(): Owner == null");

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(TimeSpan.Zero, "GetCooldownDuration(): powerProto == null");

            return GetCooldownDuration(powerProto, Owner, Properties);
        }

        public static TimeSpan GetCooldownDuration(PowerPrototype powerProto, WorldEntity owner, PropertyCollection powerProperties)
        {
            // First check if the power is already on cooldown and return that if it is
            TimeSpan cooldownTimeElapsed = owner.GetAbilityCooldownTimeElapsed(powerProto);
            TimeSpan cooldownDurationForLastActivation = owner.GetAbilityCooldownDurationUsedForLastActivation(powerProto);

            if (cooldownTimeElapsed <= cooldownDurationForLastActivation)
                return cooldownDurationForLastActivation;

            // Calculate new cooldown duration
            return CalcCooldownDuration(powerProto, owner, powerProperties);
        }

        public static TimeSpan CalcCooldownDuration(PowerPrototype powerProto, WorldEntity owner, PropertyCollection powerProperties, TimeSpan baseCooldown = default)
        {
            if (baseCooldown == TimeSpan.Zero)
                baseCooldown = powerProto.GetCooldownDuration(powerProperties, owner.Properties);

            // TODO: apply modifiers

            return baseCooldown;
        }

        public static bool IsCooldownOnPlayer(PowerPrototype powerProto)
        {
            return powerProto.CooldownOnPlayer;
        }

        public bool TriggersComboPowerOnEvent(PowerEventType onPowerEnd)
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "TriggersComboPowerOnEvent(): powerProto == null");
            return powerProto.ExtraActivation != null && powerProto.ExtraActivation is SecondaryActivateOnReleasePrototype;
        }

        public bool IsOnExtraActivation()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsOnExtraActivation(): powerProto == null");
            return IsOnExtraActivation(powerProto, Owner);
        }

        public static bool IsOnExtraActivation(PowerPrototype powerProto, WorldEntity owner)
        {
            if (owner == null) return Logger.WarnReturn(false, "IsOnExtraActivation(): owner == null");
            if (powerProto.DataRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "IsOnExtraActivation(): powerProto.DataRef == PrototypeId.Invalid");

            if (powerProto.ExtraActivation == null || powerProto.ExtraActivation is not ExtraActivateOnSubsequentPrototype extraActivate)
                return false;

            if (extraActivate.ExtraActivateEffect == SubsequentActivateType.RepeatActivation)
                return false;

            int powerActivationCount = owner.Properties[PropertyEnum.PowerActivationCount, powerProto.DataRef];

            if (extraActivate.ExtraActivateEffect != SubsequentActivateType.DestroySummonedEntity || powerActivationCount % 2 != 1)
                return false;

            return true;
        }

        public bool IsToggled()
        {
            return Prototype != null && Prototype.IsToggled;
        }

        public bool IsCancelledOnDamage()
        {
            return Prototype != null && Prototype.CancelledOnDamage;
        }

        public bool IsCancelledOnMove()
        {
            return Prototype != null && Prototype.CancelledOnMove;
        }

        public bool IsCancelledOnRelease()
        {
            return Prototype != null && Prototype.CancelledOnButtonRelease;
        }

        public bool IsCancelledOnTargetKilled()
        {
            return Prototype != null && Prototype.CancelledOnTargetKilled;
        }

        public bool IsNonCancellableChannelPower()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsNonCancellableChannelPower(): powerProto == null");

            return powerProto.CanBeInterrupted == false && IsCancelledOnRelease() == false && IsCancelledOnMove() == false && IsChannelingPower();
        }

        public bool IsExclusiveActivation()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsExclusiveActivation(): powerProto == null");
            return IsExclusiveActivation(powerProto, Owner, Properties, this);
        }

        public static bool IsExclusiveActivation(PowerPrototype powerProto, WorldEntity owner, PropertyCollection powerProperties, Power power)
        {
            if (owner == null) return Logger.WarnReturn(false, "IsExclusiveActivation(): owner == null");

            if (GetActivationType(powerProto) == PowerActivationType.Passive)
                return false;

            if (powerProto.ForceNonExclusive)
                return false;

            if (IsProcEffect(powerProto) || IsComboEffect(powerProto) || IsMissileEffect(powerProto) || powerProto.IsToggled || IsItemPower(powerProto))
            {
                if (GetFullExecutionTime(powerProto, owner, powerProperties, power) == TimeSpan.Zero)
                    return powerProto is MovementPowerPrototype movementPowerProto && movementPowerProto.ConstantMoveTime == false;
            }

            return IsOnExtraActivation(powerProto, owner) == false;
        }

        public bool IsSecondActivateOnRelease()
        {
            return false;
        }

        public bool IsContinuous()
        {
            if (IsToggled())
                return false;

            // <= 50 ms is too fast to be a continuous power - is this related to game fixed time update time?
            if (GetFullExecutionTime().TotalMilliseconds <= 50)
                return false;

            if (GetCooldownDuration() > TimeSpan.Zero)
                return false;

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsContinuous(): powerProto == null");

            if (powerProto.DisableContinuous)
                return false;

            if (powerProto.PowerCategory != PowerCategoryType.NormalPower)
                return false;

            if (powerProto.ExtraActivation != null)
                return false;

            if (powerProto.Activation == PowerActivationType.Passive || powerProto.Activation == PowerActivationType.TwoStageTargeted)
                return false;

            if (IsCancelledOnRelease())
                return false;

            if (IsSecondActivateOnRelease())
                return false;

            // After facing many challenges, we have reached the end and earned our right to be a continuous power
            return true;
        }

        #endregion

        private bool CreateSituationalComponent()
        {
            if (Game == null) return Logger.WarnReturn(false, "CreateSituationalComponent(): Game == null");
            
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "CreateSituationalComponent(): powerProto == null");

            if (powerProto?.SituationalComponent?.SituationalTrigger == null)
                return true;

            _situationalComponent = new(Game, powerProto.SituationalComponent, this);
            return true;
        }
    }
}
