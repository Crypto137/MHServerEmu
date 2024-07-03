using Gazillion;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Behavior;
using MHServerEmu.Games.Behavior.StaticAI;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Powers
{
    public partial class Power
    {
        private const float PowerPositionSweepPadding = Locomotor.MovementSweepPadding;
        private const float PowerPositionSweepPaddingSquared = PowerPositionSweepPadding * PowerPositionSweepPadding;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private bool _isTeamUpPassivePowerWhileAway;
        private SituationalPowerComponent _situationalComponent;
        private KeywordsMask _keywordsMask = new();

        private PowerActivationPhase _activationPhase = PowerActivationPhase.Inactive;
        private PowerActivationSettings _lastActivationSettings;

        private readonly EventGroup _pendingEvents1 = new();
        private readonly EventGroup _pendingEvents2 = new();    // end power, channeling
        private readonly EventGroup _pendingPowerApplicationEvents = new();

        private readonly EventPointer<EndPowerEvent> _endPowerEvent = new();

        public Game Game { get; }
        public PrototypeId PrototypeDataRef { get; }
        public PowerPrototype Prototype { get; }
        public TargetingStylePrototype TargetingStylePrototype { get; }
        public GamepadSettingsPrototype GamepadSettingsPrototype { get; }

        public WorldEntity Owner { get; private set; }
        public PropertyCollection Properties { get; } = new();

        public float AnimSpeedCache { get; private set; } = -1f;
        public bool WasLastActivateInterrupted { get; private set; }
        public TimeSpan LastActivateGameTime { get; private set; }
        public PowerActivationSettings LastActivationSettings { get => _lastActivationSettings; }

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

        public void OnUnassign()
        {
            _situationalComponent?.Shutdown();

            EndPowerFlags endPowerFlags = EndPowerFlags.ExplicitCancel | EndPowerFlags.Unassign;
            if (Owner.TestStatus(EntityStatus.ExitingWorld))
                endPowerFlags |= EndPowerFlags.ExitWorld;

            EndPower(endPowerFlags);

            Owner?.Properties.RemoveProperty(new(PropertyEnum.PowerActivationCount, PrototypeDataRef));

            // TODO: call this from PowerCollection
            OnDeallocate();
        }

        public void OnOwnerEnteredWorld()
        {
            _situationalComponent?.Initialize();
        }

        public void OnOwnerExitedWorld()
        {
            _situationalComponent?.Shutdown();
        }

        public void OnOwnerCastSpeedChange()
        {
            // Reset animation speed cache when owner cast speed changes
            AnimSpeedCache = -1f;
        }

        public void OnDeallocate()
        {
            Game.GameEventScheduler.CancelAllEvents(_pendingEvents1);
            Game.GameEventScheduler.CancelAllEvents(_pendingEvents2);
            Game.GameEventScheduler.CancelAllEvents(_pendingPowerApplicationEvents);
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
            return PowerUseResult.Success;
            //throw new NotImplementedException();
        }

        public PowerUseResult Activate(ref PowerActivationSettings settings)
        {
            Logger.Trace($"Activate(): {Prototype}");

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(PowerUseResult.GenericError, "Activate(): powerProto == null");

            WorldEntity target = Game.EntityManager.GetEntity<WorldEntity>(settings.TargetEntityId);

            // Charging (variable activation time) powers
            if (powerProto.ExtraActivation != null)
            {
                if (powerProto.ExtraActivation is SecondaryActivateOnReleasePrototype secondaryActivation)
                {
                    // If this is not a release yet, send pre-activation message to clients
                    if (settings.VariableActivationRelease == false)
                    {
                        var preActivatePower = NetMessagePreActivatePower.CreateBuilder()
                            .SetIdUserEntity(Owner.Id)
                            .SetPowerPrototypeId((ulong)PrototypeDataRef)
                            .SetIdTargetEntity(settings.TargetEntityId)
                            .SetTargetPosition(settings.TargetPosition.ToNetStructPoint3())
                            .Build();

                        Game.NetworkManager.SendMessageToInterested(preActivatePower, Owner, AOINetworkPolicyValues.AOIChannelProximity, true);
                        return PowerUseResult.Success;
                    }

                    if (secondaryActivation.MaxReleaseTimeMS > 0)
                    {
                        int variableActivationTimeMS = Math.Min((int)settings.VariableActivationTime.TotalMilliseconds, secondaryActivation.MaxReleaseTimeMS);
                        float variableActivationTimePct = variableActivationTimeMS / (float)Math.Max(secondaryActivation.MinReleaseTimeMS, secondaryActivation.MaxReleaseTimeMS);

                        Properties[PropertyEnum.VariableActivationTimeMS] = TimeSpan.FromMilliseconds(variableActivationTimeMS);
                        Properties[PropertyEnum.VariableActivationTimePct] = variableActivationTimePct;
                    }
                }
            }
            else if (powerProto.PowerCategory == PowerCategoryType.ComboEffect && settings.VariableActivationTime > TimeSpan.Zero
                && settings.TriggeringPowerPrototypeRef != PrototypeId.Invalid && Owner != null)
            {
                Power triggeringPower = Owner.GetPower(settings.TriggeringPowerPrototypeRef);
                if (triggeringPower != null)
                {
                    Properties.CopyProperty(triggeringPower.Properties, PropertyEnum.VariableActivationTimeMS);
                    Properties.CopyProperty(triggeringPower.Properties, PropertyEnum.VariableActivationTimeMS);
                }
            }

            // Run all defined activation evals
            if (powerProto.EvalOnActivate.HasValue())
            {
                // Initialize context data
                EvalContextData contextData = new(Game);
                contextData.SetVar_PropertyCollectionPtr(EvalContext.Default, Properties);
                contextData.SetVar_PropertyCollectionPtr(EvalContext.Entity, Owner.Properties);
                contextData.SetReadOnlyVar_ConditionCollectionPtr(EvalContext.Var1, Owner.ConditionCollection);
                contextData.SetReadOnlyVar_EntityPtr(EvalContext.Var2, Owner);
                contextData.SetReadOnlyVar_EntityPtr(EvalContext.Var3, target);

                if (Owner is Agent agent)
                {
                    AIController aiController = agent.AIController;
                    if (aiController != null)
                        contextData.SetVar_PropertyCollectionPtr(EvalContext.EntityBehaviorBlackboard, aiController.Blackboard.PropertyCollection);
                }

                Eval.InitTeamUpEvalContext(contextData, Owner);
                contextData.SetVar_PropertyCollectionPtr(EvalContext.Other, target != null ? target.Properties : new PropertyCollection());

                if (RunActivateEval(contextData) == false)
                    return Logger.WarnReturn(PowerUseResult.GenericError, $"Activate(): EvalOnActivate failed for Power: {this}.");
            }

            // Run power synergy eval if defined
            if (powerProto.EvalPowerSynergies != null)
            {
                EvalContextData contextData = new(Game);
                contextData.SetVar_PropertyCollectionPtr(EvalContext.Default, Properties);
                contextData.SetVar_PropertyCollectionPtr(EvalContext.Entity, Owner.Properties);
                contextData.SetVar_PropertyCollectionPtr(EvalContext.Other, target?.Properties);
                contextData.SetReadOnlyVar_ConditionCollectionPtr(EvalContext.Var1, Owner.ConditionCollection);
                contextData.SetReadOnlyVar_EntityPtr(EvalContext.Var2, Owner);
                Eval.InitTeamUpEvalContext(contextData, Owner);

                if (Eval.RunBool(powerProto.EvalPowerSynergies, contextData) == false)
                    return Logger.WarnReturn(PowerUseResult.GenericError, $"Activate(): The EvalPowerSynergies Eval in a power failed:\nPower: [{this}]");
            }

            if (StopsMovementOnActivation())
            {
                if (Owner is Agent agent)
                {
                    Locomotor locomotor = agent.Locomotor;
                    if (locomotor != null && (Owner.IsMovementAuthoritative || locomotor.IsSyncMoving))
                        locomotor.Stop();
                }
            }

            if (OrientsTowardsTargetWhileActive())
            {
                if (Owner is Agent agent)
                    agent.OrientForPower(this, settings.TargetPosition, settings.UserPosition);
            }

            if (GetTargetingShape() == TargetingShapeType.Self)
            {
                settings.TargetEntityId = Owner.Id;
            }
            else if (GetTargetingShape() == TargetingShapeType.TeamUp)
            {
                if (Owner is Avatar avatar)
                {
                    Agent currentTeamUpAgent = avatar.CurrentTeamUpAgent;
                    if (currentTeamUpAgent != null)
                    {
                        settings.TargetEntityId = currentTeamUpAgent.Id;
                        settings.TargetPosition = currentTeamUpAgent.RegionLocation.Position;
                    }
                }
            }

            settings.InitialTargetPosition = settings.TargetPosition;
            GenerateActualTargetPosition(settings.TargetEntityId, settings.InitialTargetPosition, out settings.TargetPosition, in settings);

            MovementPowerPrototype movementPowerProto = FindPowerPrototype<MovementPowerPrototype>(powerProto);
            if (movementPowerProto == null || movementPowerProto.TeleportMethod != TeleportMethodType.Teleport)
                ComputePowerMovementSettings(movementPowerProto, ref settings);

            if (Properties.HasProperty(PropertyEnum.PowerPeriodicActivation))
            {
                int activateAtCount = Properties[PropertyEnum.PowerPeriodicActivation];
                int activationCount = Properties[PropertyEnum.PowerPeriodicActivationCount];

                if (activateAtCount <= 0)
                {
                    return Logger.WarnReturn(PowerUseResult.GenericError,
                        $"Activate(): Tried to activate Periodic Activation Power [{this}] but it has no periodic activation value specified!");
                }

                activationCount++;
                if (activationCount < activateAtCount)
                {
                    Properties[PropertyEnum.PowerPeriodicActivationCount] = activateAtCount;
                    return PowerUseResult.Success;
                }
                else
                {
                    Properties[PropertyEnum.PowerPeriodicActivationCount] = 0;
                }
            }

            PowerUseResult result = ActivateInternal(in settings);
            if (result != PowerUseResult.Success)
                return result;

            WasLastActivateInterrupted = false;

            if (Owner != null && (Owner.IsDestroyed || Owner.IsInWorld == false))
                return result;

            if (Game == null) return Logger.WarnReturn(PowerUseResult.GenericError, "Activate(): Game == null");
            LastActivateGameTime = Game.CurrentTime;

            // We need to get target here again because activation settings may have changed since the beginning of this method
            target = Game.EntityManager.GetEntity<WorldEntity>(settings.TargetEntityId);
            if (target != null && Owner.IsHostileTo(target))
                Owner.Properties[PropertyEnum.LastHostileTargetID] = settings.TargetEntityId;

            _activationPhase = PowerActivationPhase.Active;

            if (GetChargingTime() > TimeSpan.Zero)
                StartCharging();

            if (GetTotalChannelingTime() > TimeSpan.Zero)
                ScheduleChannelStart();

            if (GetActivationType() != PowerActivationType.Passive && powerProto.IsRecurring == false)
                SchedulePowerEnd(in settings);

            _situationalComponent?.OnPowerActivated(target);

            return result;
        }

        public void ReleaseVariableActivation(ref PowerActivationSettings settings)
        {
            Logger.Debug($"ReleaseVariableActivation(): {Prototype}");
            settings.VariableActivationRelease = true;  // Mark power as release
            Activate(ref settings);
        }

        public bool EndPower(EndPowerFlags flags)
        {
            Logger.Trace($"EndPower(): {Prototype} (flags={flags})");

            // Validate client cancel requests
            if (flags.HasFlag(EndPowerFlags.ExplicitCancel) && flags.HasFlag(EndPowerFlags.ClientRequest)
                && flags.HasFlag(EndPowerFlags.ExitWorld) == false && flags.HasFlag(EndPowerFlags.Unassign) == false
                && flags.HasFlag(EndPowerFlags.Interrupting) == false && flags.HasFlag(EndPowerFlags.Force) == false)
            {
                _lastActivationSettings.Flags |= PowerActivationSettingsFlags.Cancel;
                if (CanBeUserCanceledNow() == false)
                    return false;
            }

            if (CanEndPower(flags) == false)
                return false;

            if (OnEndPowerCheckTooEarly(flags))
            {
                _activationPhase = PowerActivationPhase.MinTimeEnding;
                return false;
            }

            if (OnEndPowerRemoveApplications(flags))
                return false;

            OnEndPowerCancelEvents(flags);
            OnEndPowerCancelConditions();
            OnEndPowerSendCancel(flags);

            if (OnEndPowerCheckLoopEnd(flags))
            {
                HandleTriggerPowerEventOnPowerLoopEnd();
                _activationPhase = PowerActivationPhase.LoopEnding;
                return false;
            }

            EndPowerInternal(flags);

            if (flags.HasFlag(EndPowerFlags.Interrupting))
                WasLastActivateInterrupted = true;

            bool wasActive = _activationPhase != PowerActivationPhase.Inactive;
            _activationPhase = PowerActivationPhase.Inactive;

            if (IsToggledOn() && Owner?.IsInWorld == true)
            {
                bool exitWorld = flags.HasFlag(EndPowerFlags.ExitWorld);
                bool unassign = flags.HasFlag(EndPowerFlags.Unassign);
                bool notEnoughEndurance = flags.HasFlag(EndPowerFlags.NotEnoughEndurance);

                if ((exitWorld == false && unassign) || (exitWorld && HasEnduranceCostRecurring()) || notEnoughEndurance)
                    SetToggleState(false, unassign);
            }

            if (Owner == null) return Logger.WarnReturn(false, "EndPower(): Owner == null");
            Owner.OnPowerEnded(this, flags);

            if (wasActive)
                HandleTriggerPowerEventOnPowerStopped(flags);

            if (flags.HasFlag(EndPowerFlags.ExplicitCancel) == false && flags.HasFlag(EndPowerFlags.ExitWorld) == false
                && flags.HasFlag(EndPowerFlags.Unassign) == false)
            {
                HandleTriggerPowerEventOnPowerEnd();
            }

            _lastActivationSettings = new();

            Owner.ActivatePostPowerAction(this, flags);

            OnEndPowerConditionalRemove(flags);     // Remove one-offs, like throwables

            return true;
        }

        public void StartCooldown()
        {
            // TODO
        }

        public PowerPositionSweepResult PowerPositionSweep(RegionLocation regionLocation, Vector3 targetPosition, ulong targetId,
            ref Vector3? resultPosition, bool forceDoNotMoveToExactTargetLocation = false, float rangeOverride = 0f)
        {
            if (Owner == null) return Logger.WarnReturn(PowerPositionSweepResult.Error, "PowerPositionSweep(): Owner == null");

            Region region = regionLocation.Region;
            if (region == null) return Logger.WarnReturn(PowerPositionSweepResult.Error, "PowerPositionSweep(): region == null");

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(PowerPositionSweepResult.Error, "PowerPositionSweep(): powerProto == null");

            resultPosition = targetPosition;
            
            if (powerProto is MovementPowerPrototype movementPowerProto)
            {
                if (movementPowerProto.PowerMovementPathPct > 0f || movementPowerProto.TeleportMethod != TeleportMethodType.None)
                {
                    Locomotor locomotor = Owner.Locomotor;
                    if (locomotor == null) return Logger.WarnReturn(PowerPositionSweepResult.Error, "PowerPositionSweep(): locomotor == null");

                    bool doNotMoveToExactTargetLocation = forceDoNotMoveToExactTargetLocation || movementPowerProto.MoveToExactTargetLocation == false;
                    float range = Segment.IsNearZero(rangeOverride) ? GetRange() : rangeOverride;

                    Vector3 findPoint = resultPosition.Value;
                    PointOnLineResult result = region.NaviMesh.FindPointOnLineToOccupy(ref findPoint, regionLocation.Position, targetPosition, range,
                        Owner.Bounds, locomotor.PathFlags, movementPowerProto.BlockingCheckFlags, doNotMoveToExactTargetLocation);
                    resultPosition = findPoint;

                    return result switch
                    {
                        PointOnLineResult.Failed    => PowerPositionSweepResult.TargetPositionInvalid,
                        PointOnLineResult.Clipped   => PowerPositionSweepResult.Clipped,
                        PointOnLineResult.Success   => PowerPositionSweepResult.Success,
                        _                           => PowerPositionSweepResult.Error,
                    };
                }
            }

            return PowerPositionSweepInternal(regionLocation, targetPosition, targetId, ref resultPosition, false, false);
        }

        public bool PowerLOSCheck(RegionLocation regionLocation, Vector3 targetPosition, ulong targetId, ref Vector3? resultPosition, bool losCheckAlongGround)
        {
            PowerPositionSweepResult result = PowerPositionSweepInternal(regionLocation, targetPosition, targetId, ref resultPosition, true, losCheckAlongGround);

            if (result == PowerPositionSweepResult.Clipped)
                return Vector3.DistanceSquared(targetPosition, resultPosition.Value) <= PowerPositionSweepPaddingSquared;

            return result == PowerPositionSweepResult.Success;
        }

        public static int ComputeNearbyPlayers(Region region, Vector3 position, int min, bool combatActive, HashSet<ulong> nearbyPlayers = null)
        {
            throw new NotImplementedException();
        }

        public static bool IsValidTarget(PowerPrototype powerProto, WorldEntity worldEntity1, AlliancePrototype alliance, WorldEntity worldEntity2)
        {
            return true;
        }

        public bool IsInRange(WorldEntity target, RangeCheckType checkType)
        {
            if (target == null) return Logger.WarnReturn(false, "IsInRange(): target == null");

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsInRange(): powerProto == null");
            if (Owner == null) return Logger.WarnReturn(false, "IsInRange(): powerProto == null");

            float range = GetRange();
            Vector3 userPosition = Owner.RegionLocation.Position;
            float userRadius = Owner.Bounds.Radius;
            Vector3 targetPosition = target.RegionLocation.Position;
            float targetRadius = target.Bounds.Radius;

            return IsInRangeInternal(powerProto, range, userPosition, userRadius, targetPosition, checkType, targetRadius);
        }

        public bool IsInRange(Vector3 targetPosition, RangeCheckType checkType)
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsInRange(): powerProto == null");
            if (Owner == null) return Logger.WarnReturn(false, "IsInRange(): powerProto == null");

            float range = GetRange();
            Vector3 userPosition = Owner.RegionLocation.Position;
            float userRadius = Owner.Bounds.Radius;

            return IsInRangeInternal(powerProto, range, userPosition, userRadius, targetPosition, checkType, 0f);
        }

        public static bool ValidateAOETarget(WorldEntity target, PowerPrototype powerProto, WorldEntity user, Vector3 powerUserPosition,
            AlliancePrototype userAllianceProto, bool needsLineOfSight)
        {
            return true;
        }

        public static bool IsTargetInAOE(WorldEntity target, WorldEntity owner, Vector3 ownerPosition, Vector3 targetPosition, float radius,
            int beamSlice, TimeSpan totalSweepTime, PowerPrototype powerProto, PropertyCollection properties)
        {
            Logger.Debug("IsTargetInAOE()");
            var styleProto = powerProto.GetTargetingStyle();
            if (styleProto == null) return Logger.WarnReturn(false, $"IsTargetInAOE(): Unable to get the prototype for power. Prototype:{powerProto} ");
            Vector3 position = targetPosition;
            if (styleProto.AOESelfCentered && styleProto.RandomPositionRadius == 0)
                position = ownerPosition + styleProto.GetOwnerOrientedPositionOffset(owner);

            return styleProto.TargetingShape switch
            {
                TargetingShapeType.ArcArea      => IsTargetInArc(target, owner, radius, position, targetPosition, powerProto, styleProto, properties),
                TargetingShapeType.BeamSweep    => IsTargetInBeamSlice(target, owner, radius, position, targetPosition, beamSlice, totalSweepTime, powerProto, styleProto),
                TargetingShapeType.CapsuleArea  => IsTargetInCapsule(target, owner, position, targetPosition, powerProto, styleProto, properties),
                TargetingShapeType.CircleArea   => IsTargetInCircle(target, radius, position),
                TargetingShapeType.RingArea     => IsTargetInRing(target, radius, position, powerProto, properties),
                TargetingShapeType.WedgeArea    => IsTargetInWedge(target, owner, radius, position, targetPosition, powerProto, styleProto),
                _ => Logger.WarnReturn(false, $"IsTargetInAOE(): Targeting shape ({styleProto.TargetingShape}) for this power hasn't been implemented! Prototype: {powerProto}"),
            };
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

        public bool StopsMovementOnActivation()
        {
            if (Owner is Avatar avatar && IsGamepadMeleeMoveIntoRangePower() && avatar.PendingActionState == PendingActionState.MovingToRange)
                return false;

            return Prototype != null && Prototype.MovementOrientToTargetOnActivate;
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

        public bool HasEnduranceCostRecurring()
        {
            // TODO
            return false;
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

        public bool OrientsTowardsTargetWhileActive()
        {
            return Prototype != null && Prototype.MovementOrientToTargetOnActivate;
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
            return GetAOERadius(powerProto, Owner.Properties);
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

        public float GetAOEAngle()
        {
            var powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(0f, "GetAOEAngle(): powerProto == null");
            return GetAOEAngle(powerProto);
        }

        public static float GetAOEAngle(PowerPrototype powerProto)
        {
            var styleProto = powerProto.GetTargetingStyle();
            if (styleProto == null) return Logger.WarnReturn(0f, "GetAOEAngle(): styleProto == null");

            if (styleProto.TargetingShape == TargetingShapeType.CircleArea)
                return 360.0f;

            return styleProto.AOEAngle switch
            {
                AOEAngleType._0     => 0.0f,
                AOEAngleType._1     => 1.0f,
                AOEAngleType._10    => 10.0f,
                AOEAngleType._30    => 30.0f,
                AOEAngleType._45    => 45.0f,
                AOEAngleType._60    => 60.0f,
                AOEAngleType._90    => 90.0f,
                AOEAngleType._120   => 120.0f,
                AOEAngleType._180   => 180.0f,
                AOEAngleType._240   => 240.0f,
                AOEAngleType._300   => 300.0f,
                AOEAngleType._360   => 360.0f,
                _                   => 0.0f
            };
        }

        public float GetTargetingWidth()
        {
            if (Owner == null) return Logger.WarnReturn(0f, "GetTargetingWidth(): Owner == null");
            var powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(0f, "GetTargetingWidth(): powerProto == null");
            return GetTargetingWidth(powerProto, Owner.Properties);
        }

        public static float GetTargetingWidth(PowerPrototype powerProto, PropertyCollection ownerProperties)
        {
            var styleProto = powerProto.GetTargetingStyle();
            if (styleProto == null) return Logger.WarnReturn(0f, "GetTargetingWidth(): styleProto == null");
            return styleProto.Width * GetAOESizePctModifier(powerProto, ownerProperties);
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

        public bool IsGamepadMeleeMoveIntoRangePower()
        {
            return GamepadSettingsPrototype != null && GamepadSettingsPrototype.MeleeMoveIntoRange;
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

        public float GetProjectileSpeed(float distance)
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(0f, "GetProjectileSpeed(): powerProto == null");
            return GetProjectileSpeed(powerProto, Properties, Owner.Properties, distance);
        }

        public float GetProjectileSpeed(Vector3 userPosition, Vector3 targetPosition)
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(0f, "GetProjectileSpeed(): powerProto == null");
            return GetProjectileSpeed(powerProto, Properties, Owner.Properties, userPosition, targetPosition);
        }

        public static float GetProjectileSpeed(PowerPrototype powerProto, PropertyCollection powerProperties, PropertyCollection ownerProperties,
            Vector3 userPosition, Vector3 targetPosition)
        {
            float distance = 0f;

            if (powerProto.ProjectileTimeToImpactOverride > 0f)
                distance = Vector3.Distance(userPosition, targetPosition);

            return GetProjectileSpeed(powerProto, powerProperties, ownerProperties, distance);
        }

        public static float GetProjectileSpeed(PowerPrototype powerProto, PropertyCollection powerProperties, PropertyCollection ownerProperties, float distance)
        {
            float speed;

            if (powerProto.ProjectileTimeToImpactOverride > 0f)
                speed = distance / powerProto.ProjectileTimeToImpactOverride;
            else
                speed = powerProto.GetProjectilesSpeed(powerProperties, ownerProperties);

            if (ownerProperties != null)
                speed *= 1f + powerProperties[PropertyEnum.MissileSpeedBonus];

            return speed;
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

        public float GetAnimContactTimePercent()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(0f, "GetAnimContactTimePercent(): powerProto == null");
            return GetAnimContactTimePercent(powerProto, Owner);
        }

        public static float GetAnimContactTimePercent(PowerPrototype powerProto, WorldEntity owner)
        {
            if (owner == null) return Logger.WarnReturn(0f, "GetAnimContactTimePercent(): powerProto == null");

            float powerContactPctWhenMoving = powerProto.Properties != null ? powerProto.Properties[PropertyEnum.PowerContactPctWhenMoving] : -1f;

            if (powerContactPctWhenMoving >= 0f)
            {
                if (owner.Locomotor != null && owner.Locomotor.IsLocomoting)
                    return powerContactPctWhenMoving;
            }

            return powerProto.GetContactTimePercent(owner.GetOriginalWorldAsset(), owner.GetEntityWorldAsset());
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

        public TimeSpan GetActivationTime()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(TimeSpan.Zero, "GetActivationTime(): powerProto == null");

            // Channeled powers
            if (GetChannelLoopTime() > TimeSpan.Zero)
            {
                if (powerProto.IsRecurring == false)
                    return GetChannelStartTime() * GetAnimContactTimePercent(powerProto, Owner);

                return GetChannelStartTime() + (GetChannelLoopTime() * GetAnimContactTimePercent(powerProto, Owner));
            }

            // Non-channeled powers
            float animSpeed = GetAnimSpeed();
            float timeMult = animSpeed > 0f ? 1f / animSpeed : 0f;     // Avoid division by 0 / negative
            TimeSpan oneOffAnimContactTime = powerProto.GetOneOffAnimContactTime(Owner.GetOriginalWorldAsset(), Owner.GetEntityWorldAsset());
            return GetChargingTime() + (oneOffAnimContactTime * timeMult);
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
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsSecondActivateOnRelease(): powerProto == null");

            if (powerProto.ExtraActivation == null)
                return false;

            return Prototype.ExtraActivation is SecondaryActivateOnReleasePrototype;
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

        public bool CanBeUsedInRegion(Region region)
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "CanBeUsedInRegino(): powerProto == null");
            return CanBeUsedInRegion(powerProto, Properties, region);
        }

        public static bool CanBeUsedInRegion(PowerPrototype powerProto, PropertyCollection powerProperties, Region region)
        {
            if (region == null) return false;
            RegionPrototype regionPrototype = region.RegionPrototype;
            if (regionPrototype == null) return Logger.WarnReturn(false, "CanBeUsedInRegion(): regionPrototype == null");

            PropertyCollection properties = powerProperties ?? powerProto.Properties;

            // Check power properties
            if (powerProto.Activation != PowerActivationType.Passive && properties != null)
            {
                // Check if we can use the power in the current region type (town / public / private / etc)
                if (properties[PropertyEnum.PowerUsePreventIn, (int)regionPrototype.Behavior])
                    return false;

                // Check keywords that prevent powers from being used in regions
                foreach (var kvp in properties.IteratePropertyRange(PropertyEnum.PowerUsePreventInRegionKwd))
                {
                    if (kvp.Value == false)
                        continue;

                    Property.FromParam(kvp.Key, 0, out PrototypeId regionKeywordRef);
                    if (regionKeywordRef == PrototypeId.Invalid)
                        Logger.Warn($"CanBeUsedInRegion(): Power has invalid PowerUsePreventInRegionKwd!\n Power Prototype: {powerProto}");

                    if (regionPrototype.HasKeyword(regionKeywordRef.As<KeywordPrototype>()))
                        return false;
                }

                // Check keywords that are required for a power to be used in a region
                foreach (var kvp in properties.IteratePropertyRange(PropertyEnum.PowerUseRequiresRegionKwd))
                {
                    if (kvp.Value == false)
                        continue;

                    Property.FromParam(kvp.Key, 0, out PrototypeId regionKeywordRef);
                    if (regionKeywordRef == PrototypeId.Invalid)
                        Logger.Warn($"CanBeUsedInRegion(): Power has invalid PowerUseRequiresRegionKwd!\n Power Prototype: {powerProto}");

                    if (regionPrototype.HasKeyword(regionKeywordRef.As<KeywordPrototype>()) == false)
                        return false;
                }
            }

            // Check region keyword blacklist
            if (regionPrototype.PowerKeywordBlacklist.HasValue() && powerProto.Keywords.HasValue())
            {
                foreach (PrototypeId powerKeywordRef in regionPrototype.PowerKeywordBlacklist)
                {
                    if (powerProto.HasKeyword(powerKeywordRef.As<KeywordPrototype>()))
                        return false;
                }
            }

            return true;
        }

        public static T FindPowerPrototype<T>(PowerPrototype powerProto) where T: PowerPrototype
        {
            if (powerProto == null) return Logger.WarnReturn<T>(null, "FindPowerPrototype(): powerProto == null");

            if (powerProto is T typedPowerProto)
                return typedPowerProto;

            if (powerProto.ActionsTriggeredOnPowerEvent.HasValue())
            {
                foreach (PowerEventActionPrototype triggeredPowerEventProto in powerProto.ActionsTriggeredOnPowerEvent)
                {
                    if (triggeredPowerEventProto.EventAction != PowerEventActionType.UsePower)
                        continue;

                    if (triggeredPowerEventProto.Power == PrototypeId.Invalid)
                        return Logger.WarnReturn<T>(null, $"FindPowerPrototype(): Infinite loop detected in {powerProto}!");

                    typedPowerProto = FindPowerPrototype<T>(triggeredPowerEventProto.Power.As<PowerPrototype>());
                    if (typedPowerProto != null)
                        return typedPowerProto;
                }
            }

            return null;
        }

        #endregion

        protected PowerUseResult ActivateInternal(in PowerActivationSettings settings)
        {
            // Send non-combo activations and combos triggered by the server
            if (IsComboEffect() == false || settings.Flags.HasFlag(PowerActivationSettingsFlags.ServerCombo))
            {
                // Send message if there are any interested clients in proximity
                PlayerConnectionManager networkManager = Owner.Game.NetworkManager;

                // Owner is excluded from power activation messages unless explicitly flagged or this is a combo power triggered by the server (therefore the client is not aware of it)
                bool skipOwner = settings.Flags.HasFlag(PowerActivationSettingsFlags.NotifyOwner) == false && settings.Flags.HasFlag(PowerActivationSettingsFlags.ServerCombo) == false;
                IEnumerable<PlayerConnection> interestedClients = networkManager.GetInterestedClients(Owner, AOINetworkPolicyValues.AOIChannelProximity, skipOwner);
                if (interestedClients.Any())
                {
                    NetMessageActivatePower activatePowerMessage = ArchiveMessageBuilder.BuildActivatePowerMessage(this, in settings);
                    networkManager.SendMessageToMultiple(interestedClients, activatePowerMessage);
                }
            }

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(PowerUseResult.GenericError, "ActivateInternal(): powerProto == null");

            // Make a copy of activation settings
            _lastActivationSettings = settings;

            // Trigger events (this relies on the copy of setting we just made)
            HandleTriggerPowerEventOnPowerStart();

            return PowerUseResult.Success;
        }

        protected virtual bool EndPowerInternal(EndPowerFlags flags)
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "EndPowerInternal(): powerProto == null");
            powerProto.OnEndPower(this, Owner);
            return true;
        }

        protected virtual bool OnEndPowerCheckTooEarly(EndPowerFlags flags)
        {
            return false;
        }

        protected virtual bool OnEndPowerRemoveApplications(EndPowerFlags flags)
        {
            return false;
        }

        protected virtual void OnEndPowerCancelEvents(EndPowerFlags flags)
        {

        }

        protected virtual void OnEndPowerCancelConditions()
        {

        }

        protected virtual void OnEndPowerSendCancel(EndPowerFlags flags)
        {
            // Do not send updates for powers that were not interrupted
            if (flags.HasFlag(EndPowerFlags.ExplicitCancel) == false && flags.HasFlag(EndPowerFlags.ClientRequest) == false)
                return;

            // Do not send updates when cleaning up
            if (flags.HasFlag(EndPowerFlags.ExitWorld) || flags.HasFlag(EndPowerFlags.Unassign))
                return;

            // Send message if there are any interested clients in proximity
            PlayerConnectionManager networkManager = Owner.Game.NetworkManager;

            // The owner's client should have canceled the power it requested on its own
            bool skipOwner = flags.HasFlag(EndPowerFlags.ClientRequest);
            IEnumerable<PlayerConnection> interestedClients = networkManager.GetInterestedClients(Owner, AOINetworkPolicyValues.AOIChannelProximity, skipOwner);
            if (interestedClients.Any() == false) return;

            // NOTE: Although NetMessageCancelPower is not an archive, it uses power prototype enums
            ulong powerPrototypeEnum = (ulong)DataDirectory.Instance.GetPrototypeEnumValue<PowerPrototype>(PrototypeDataRef);
            var cancelPowerMessage = NetMessageCancelPower.CreateBuilder()
                .SetIdAgent(Owner.Id)
                .SetPowerPrototypeId(powerPrototypeEnum)
                .SetEndPowerFlags((uint)flags)
                .Build();

            networkManager.SendMessageToMultiple(interestedClients, cancelPowerMessage);
        }

        protected virtual bool OnEndPowerCheckLoopEnd(EndPowerFlags flags)
        {
            return false;
        }

        protected virtual void OnEndPowerConditionalRemove(EndPowerFlags flags)
        {
            // Unassign one-off powers (e.g. throwables)
            if (Prototype.RemovedOnUse && flags.HasFlag(EndPowerFlags.Unassign) == false)
                Owner.UnassignPower(PrototypeDataRef);
        }

        protected virtual void GenerateActualTargetPosition(ulong targetId, Vector3 initialTargetPosition, out Vector3 actualTargetPosition,
            in PowerActivationSettings settings)
        {
            actualTargetPosition = initialTargetPosition;
            //TODO
        }

        protected virtual bool SetToggleState(bool value, bool doNotStartCooldown = false)
        {
            if (IsToggled() == false) return Logger.WarnReturn(false, "SetToggleState(): Trying to toggle a power that isn't togglable!");

            if (value)
            {
                HandleTriggerPowerEventOnPowerToggleOn();
            }
            else
            {
                HandleTriggerPowerEventOnPowerToggleOff();

                if (doNotStartCooldown == false)
                    StartCooldown();
            }

            return true;
        }

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

        private static bool IsInRangeInternal(PowerPrototype powerProto, float range, Vector3 userPosition, float userRadius,
            Vector3 targetPosition, RangeCheckType checkType, float targetRadius)
        {
            if (powerProto.Activation == PowerActivationType.Passive)
                return true;

            TargetingStylePrototype targetingPrototype = powerProto.GetTargetingStyle();
            if (targetingPrototype == null) return Logger.WarnReturn(false, "IsInRangeInternal(): targetingPrototype == null");

            if (targetingPrototype.TargetingShape == TargetingShapeType.Self)
                return true;

            if (powerProto.PowerCategory == PowerCategoryType.ProcEffect)
                return true;

            if (powerProto is MovementPowerPrototype movementPowerProto)
            {
                if (movementPowerProto.MoveToExactTargetLocation == false && targetingPrototype.NeedsTarget == false)
                    return true;
            }

            // Distance to the edge of the target
            float distance = Vector3.Distance2D(userPosition, targetPosition) - targetRadius;

            // RangeActivationReduction is not used in GetRange(), and according to PowerPrototype::validateTargetingSettings(),
            // it has something to do with client-server synchronization. It's probably used to have the power activate on the
            // client later to account for latency, so we do not need it on the server I think.
            //if (checkType == RangeCheckType.Activation)
            //    range -= powerProto.RangeActivationReduction;

            // Range cannot be less than user radius. 5 appears to be additional padding
            range = MathF.Max(userRadius, range) + 5f;

            return (distance - range) <= 0f;
        }

        private PowerPositionSweepResult PowerPositionSweepInternal(RegionLocation regionLocation, Vector3 targetPosition,
            ulong targetId, ref Vector3? resultPosition, bool losCheck, bool losCheckAlongGround)
        {
            if (Owner == null) return Logger.WarnReturn(PowerPositionSweepResult.Error, "PowerPositionSweepInternal(): Owner == null");

            Region region = regionLocation.Region;
            if (region == null) return Logger.WarnReturn(PowerPositionSweepResult.Error, "PowerPositionSweepInternal(): region == null");

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(PowerPositionSweepResult.Error, "PowerPositionSweepInternal(): powerProto == null");

            // This is used multiple times, so we do a cast for it now
            MovementPowerPrototype movementPowerProto = powerProto as MovementPowerPrototype;

            NaviMesh naviMesh = region.NaviMesh;

            // Sweep settings
            Vector3 fromPosition = regionLocation.Position;
            Vector3 toPosition = targetPosition;
            float sweepRadius = 0f;
            PathFlags pathFlags = PathFlags.Power;
            Vector3? resultNormal = null;
            float padding = PowerPositionSweepPadding;
            HeightSweepType heightSweepType = HeightSweepType.None;
            int maximumHeight = short.MaxValue;
            int minimumHeight = short.MinValue;

            bool clipped = false;

            // Determine sweep settings based for the power
            if (losCheck == false && movementPowerProto != null)
            {
                Locomotor locomotor = Owner.Locomotor;
                if (locomotor == null) return Logger.WarnReturn(PowerPositionSweepResult.Error, "PowerPositionSweepInternal(): locomotor == null");

                bool doNotMoveToExactTargetLocation = movementPowerProto.MoveToExactTargetLocation == false;

                PointOnLineResult pointOnLineResult = naviMesh.FindPointOnLineToOccupy(ref toPosition, fromPosition, toPosition, GetRange(),
                    Owner.Bounds, locomotor.PathFlags, movementPowerProto.BlockingCheckFlags, doNotMoveToExactTargetLocation);

                if (pointOnLineResult == PointOnLineResult.Failed)
                    return PowerPositionSweepResult.TargetPositionInvalid;

                clipped = pointOnLineResult == PointOnLineResult.Clipped;
                pathFlags = PathFlags.Walk;
                int movementHeightBonus = movementPowerProto.MovementHeightBonus;

                if (movementHeightBonus != 0 || locomotor.PathFlags.HasFlag(PathFlags.Fly))
                {
                    if (movementHeightBonus > 0)
                        maximumHeight = (int)regionLocation.ProjectToFloor().Z + movementHeightBonus;
                    else
                        minimumHeight = (int)regionLocation.ProjectToFloor().Z + movementHeightBonus;

                    pathFlags |= PathFlags.Fly;
                    heightSweepType = HeightSweepType.Constraint;
                }
            }
            else if (losCheckAlongGround)
            {
                pathFlags = PathFlags.Walk;
            }
            else if (losCheck)
            {
                maximumHeight = (int)MathF.Max(fromPosition.Z + Owner.Bounds.EyeHeight, targetPosition.Z);
                heightSweepType = HeightSweepType.Constraint;
            }

            if (powerProto is MissilePowerPrototype missilePowerProto)
            {
                sweepRadius = missilePowerProto.MaximumMissileBoundsSphereRadius;
            }
            else if (powerProto is SummonPowerPrototype summonPowerProto && losCheck == false)
            {
                if (summonPowerProto.SummonEntityContexts == null)
                    return Logger.WarnReturn(PowerPositionSweepResult.Error, "PowerPositionSweepInternal(): summonPowerProto.SummonEntityContexts == null");

                WorldEntityPrototype nonHotspotSummonEntityPrototype = null;
                float maximumSphereRadius = 0f;

                for (int i = 0; i < summonPowerProto.SummonEntityContexts.Length; i++)
                {
                    WorldEntityPrototype summonedPrototype = summonPowerProto.GetSummonEntity(i, Owner.GetOriginalWorldAsset());
                    if (summonedPrototype == null)
                        return Logger.WarnReturn(PowerPositionSweepResult.Error, "PowerPositionSweepInternal(): summonedPrototype == null");

                    if (summonedPrototype is not HotspotPrototype)
                    {
                        if (summonedPrototype.Bounds == null)
                            return Logger.WarnReturn(PowerPositionSweepResult.Error, "PowerPositionSweepInternal(): summonedPrototype.Bounds == null");

                        float sphereRadius = summonedPrototype.Bounds.GetSphereRadius();
                        if (sphereRadius > maximumSphereRadius)
                        {
                            maximumSphereRadius = sphereRadius;
                            nonHotspotSummonEntityPrototype = summonedPrototype;
                        }
                    }
                }

                if (nonHotspotSummonEntityPrototype != null)
                {
                    Bounds bounds = new(nonHotspotSummonEntityPrototype.Bounds, targetPosition);
                    pathFlags = Region.GetPathFlagsForEntity(nonHotspotSummonEntityPrototype);
                    sweepRadius = bounds.Radius;
                }
            }

            if (powerProto.HeightCheckPadding != 0f)
            {
                if (powerProto.HeightCheckPadding > 0f)
                    maximumHeight = (int)(regionLocation.ProjectToFloor().Z + powerProto.HeightCheckPadding);
                else
                    minimumHeight = (int)(regionLocation.ProjectToFloor().Z + powerProto.HeightCheckPadding);

                pathFlags |= PathFlags.Fly;
                heightSweepType = HeightSweepType.Constraint;
            }

            // Do the first sweep
            SweepResult sweepResult = naviMesh.Sweep(fromPosition, toPosition, sweepRadius, pathFlags, ref resultPosition,
                ref resultNormal, padding, heightSweepType, maximumHeight, minimumHeight, Owner);

            if (sweepResult == SweepResult.Failed)
                return PowerPositionSweepResult.Error;

            if (sweepResult == SweepResult.Success || sweepResult == SweepResult.Clipped)
            {
                if (losCheck)
                {
                    WorldEntity firstHitEntity = region.SweepToFirstHitEntity(fromPosition, toPosition, Owner, targetId,
                        losCheck, sweepRadius + padding, ref resultPosition);

                    if (firstHitEntity != null)
                        sweepResult = SweepResult.Clipped;
                }
                else if (movementPowerProto != null && movementPowerProto.UserNoEntityCollide)
                {
                    int blockFlags = 1 << (int)BoundsMovementPowerBlockType.All;

                    if (movementPowerProto.IsHighFlyingPower == false && movementPowerProto.MovementHeightBonus == 0)
                        blockFlags |= 1 << (int)BoundsMovementPowerBlockType.Ground;

                    WorldEntity firstHitEntity = region.SweepToFirstHitEntity(Owner.Bounds, resultPosition.Value - fromPosition,
                        ref resultPosition, new MovementPowerEntityCollideFunc(blockFlags));

                    if (firstHitEntity != null)
                        sweepResult = SweepResult.Clipped;
                }
            }

            clipped |= sweepResult == SweepResult.Clipped;

            // Do a second sweep if we need more than just LOS
            if (losCheck == false && sweepResult == SweepResult.HeightMap && pathFlags.HasFlag(PathFlags.Fly))
            {
                pathFlags &= ~PathFlags.Fly;
                sweepResult = naviMesh.Sweep(fromPosition, toPosition, sweepRadius, pathFlags, ref resultPosition,
                    ref resultNormal, padding, heightSweepType, maximumHeight, minimumHeight, Owner);

                if (sweepResult == SweepResult.Failed)
                    return PowerPositionSweepResult.Error;

                if (sweepResult == SweepResult.Clipped)
                    return PowerPositionSweepResult.Clipped;
            }

            return clipped ? PowerPositionSweepResult.Clipped : PowerPositionSweepResult.Success;
        }

        private static bool IsTargetInArc(WorldEntity target, WorldEntity owner, float radius, Vector3 position, Vector3 targetPosition,
            PowerPrototype powerProto, TargetingStylePrototype styleProto, PropertyCollection properties)
        {
            return IsTargetInWedge(target, owner, radius, position, targetPosition, powerProto, styleProto)
                && IsTargetInRing(target, radius, position, powerProto, properties);
        }

        private static bool IsTargetInBeamSlice(WorldEntity target, WorldEntity owner, float radius, Vector3 position, Vector3 targetPosition,
            int beamSlice, TimeSpan beamTime, PowerPrototype powerProto, TargetingStylePrototype styleProto)
        {
            float aoeAngle = GetAOEAngle(powerProto);
            if (beamSlice >= 0)
                GetBeamSweepSliceCheckData(powerProto, targetPosition, position, beamSlice, aoeAngle, beamTime, ref aoeAngle, ref targetPosition);
            return IsTargetInWedge(target, owner, radius, position, targetPosition, powerProto, styleProto, aoeAngle);
        }

        private static bool IsTargetInCapsule(WorldEntity target, WorldEntity owner, Vector3 position, Vector3 targetPosition,
            PowerPrototype powerProto, TargetingStylePrototype styleProto, PropertyCollection properties)
        {
            float radius = GetTargetingWidth(powerProto, properties);
            float length = GetAOERadius(powerProto, properties);
            Vector3 direction = GetDirectionCheckData(styleProto, owner, position, targetPosition);
            Vector3 endPosition = position + direction * length;
            var capsule = new Capsule(position, endPosition, radius);
            return target.Bounds.Intersects(capsule);
        }

        private static bool IsTargetInCircle(WorldEntity target, float radius, Vector3 position)
        {
            var sphere = new Sphere(position, radius);
            return target.Bounds.Intersects(sphere);
        }

        private static bool IsTargetInRing(WorldEntity target, float radius, Vector3 position, PowerPrototype powerProto, PropertyCollection properties)
        {
            if (IsTargetInCircle(target, radius, position))
            {
                float targetRadius = target.Bounds.Radius;
                float width = GetTargetingWidth(powerProto, properties);
                float ringRadius = radius - width;

                Vector3 targetPosition = target.RegionLocation.Position;
                Vector3 distance = position - targetPosition;
                float targetDistance = Vector3.Length(distance);
                return targetDistance + targetRadius > ringRadius;
            }

            return false;
        }

        private static bool IsTargetInWedge(WorldEntity target, WorldEntity owner, float radius, Vector3 position, Vector3 targetPosition,
            PowerPrototype powerProto, TargetingStylePrototype styleProto, float aoeAngle = 0.0f)
        {
            if (aoeAngle == 0.0f) aoeAngle = GetAOEAngle(powerProto);
            if (aoeAngle <= 0.0f)
                return Logger.WarnReturn(false, $"IsTargetInWedge(): Trying to use a power with an invalid unsupported obtuse wedge angle! Prototype: {powerProto}");

            float targetRadius = target.Bounds.Radius;
            Vector3 targetPos = target.RegionLocation.Position;
            Vector3 direction = GetDirectionCheckData(styleProto, owner, position, targetPosition);
            Vector3 distance = targetPos - position;
            float lengthSq = Vector3.LengthSquared2D(distance);
            float radiusSq = MathHelper.Square(radius + targetRadius);
            if (lengthSq > radiusSq) return false;

            float halfAngle = MathHelper.ToRadians(aoeAngle / 2.0f);
            float angle = Vector3.Angle2D(distance, direction);
            if (angle < halfAngle) return true;

            Vector3 vectorSide = Vector3.SafeNormalize2D(Vector3.Perp2D(distance)) * targetRadius;

            float angleRight = Vector3.Angle2D(vectorSide + distance, direction);
            if (angleRight < halfAngle) return true;

            float angleLeft = Vector3.Angle2D(-vectorSide + distance, direction);
            if (angleLeft < halfAngle) return true;

            return false;
        }

        private static void GetBeamSweepSliceCheckData(PowerPrototype powerProto, Vector3 targetPosition, Vector3 position, int beamSlice,
            float aoeAngle, TimeSpan totalSweepTime, ref float angleResult, ref Vector3 positionResult)
        {
            TimeSpan sweepUpdateRate = TimeSpan.FromMilliseconds((long)powerProto.Properties[PropertyEnum.AOESweepRateMS]);
            if (sweepUpdateRate >= totalSweepTime)
            {
                Logger.Warn($"GetBeamSweepSliceCheckData(): Trying to get targets for a BeamSweep power whose update rate is slower than the total sweep time!\n[{powerProto}]");
                return;
            }

            float angleTime = Math.Min(aoeAngle, aoeAngle * (float)(sweepUpdateRate.TotalSeconds / totalSweepTime.TotalSeconds));
            float totalAngle = angleTime * (beamSlice + 1);
            float angleSliceCenter = -0.5f * aoeAngle;

            if (totalAngle <= aoeAngle)
            {
                angleResult = angleTime;
                angleSliceCenter += (angleTime / 2.0f) * ((2 * beamSlice) + 1);
            }
            else
            {
                float finalAngle = angleTime - (totalAngle - aoeAngle);
                angleResult = finalAngle;
                angleSliceCenter += (finalAngle / 2.0f) + (angleTime / 2.0f) * (2 * beamSlice);
            }

            float sweepDirection = powerProto.Properties[PropertyEnum.AOESweepDirectionCW] ? 1.0f : -1.0f;
            angleSliceCenter *= sweepDirection;

            Matrix3 rotMat = Matrix3.RotationZ(MathHelper.ToRadians(angleSliceCenter));
            Vector3 toTargetPosition = targetPosition - position;

            positionResult = position + rotMat * toTargetPosition;
        }

        private static Vector3 GetDirectionCheckData(TargetingStylePrototype styleProto, WorldEntity owner, Vector3 position, Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - position).To2D();

            if (owner != null && owner.IsInWorld && Vector3.LengthSqr(direction) < Segment.Epsilon)
                direction = owner.Forward.To2D();

            if (styleProto.OrientationOffset != 0.0f)
            {
                Transform3 transform = Transform3.BuildTransform(Vector3.Zero, new Orientation(MathHelper.ToRadians(styleProto.OrientationOffset), 0.0f, 0.0f));
                direction = transform * direction;
            }

            return Vector3.Normalize(direction);
        }

        private void ComputePowerMovementSettings(MovementPowerPrototype movementPowerProto, ref PowerActivationSettings settings)
        {
            if (movementPowerProto != null && movementPowerProto.TeleportMethod == TeleportMethodType.None)
                GenerateMovementPathToTarget(movementPowerProto, ref settings);

            ComputeTimeForPowerMovement(movementPowerProto, ref settings);
        }

        private void GenerateMovementPathToTarget(MovementPowerPrototype movementPowerProto, ref PowerActivationSettings settings)
        {
            // TODO
            Logger.Debug("GenerateMovementPathToTarget()");
        }

        private void ComputeTimeForPowerMovement(MovementPowerPrototype movementPowerProto, ref PowerActivationSettings settings)
        {
            // TODO
            //Logger.Debug("ComputeTimeForPowerMovement()");
        }

        private void StartCharging()
        {
            // TODO
            Logger.Debug("StartCharging()");
        }

        private void StopCharging()
        {
            // TODO
            Logger.Debug("StopCharging()");
        }

        private void ScheduleChannelStart()
        {
            // TODO
            Logger.Debug("ScheduleChannelStart()");
        }

        private bool CanBeUserCanceledNow()
        {
            // TODO
            Logger.Debug("CanBeUserCanceledNow()");
            return true;
        }

        private bool CanEndPower(EndPowerFlags flags)
        {
            // TODO
            return true;
        }

        private bool RunActivateEval(EvalContextData contextData)
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "RunActivateEval(): powerProto == null");

            bool success = true;

            foreach (EvalPrototype evalProto in powerProto.EvalOnActivate)
            {
                bool evalSuccess = Eval.RunBool(evalProto, contextData);
                success &= evalSuccess;
                if (evalSuccess == false)
                    Logger.Warn($"RunActivateEval(): The following EvalOnActivate Eval in a power failed:\nEval: [{evalProto.ExpressionString()}]\nPower: [{powerProto}]");
            }

            return success;
        }

        private bool SchedulePowerEnd(in PowerActivationSettings settings)
        {
            if (Owner == null) return Logger.WarnReturn(false, "SchedulePowerEnd(): Owner == null");

            EndPowerFlags flags = EndPowerFlags.None;

            if (Properties[PropertyEnum.PowerActiveUntilProjExpire])
            {
                if (Prototype is MissilePowerPrototype)
                    return true;

                float speed = GetProjectileSpeed(GetRange());
                if (speed <= 0f) return Logger.WarnReturn(false, "SchedulePowerEnd(): speed <= 0f");

                float distance = 2 * GetRange() * (1 + Properties[PropertyEnum.BounceCount]);
                TimeSpan delay = TimeSpan.FromSeconds(distance / speed);

                return SchedulePowerEnd(delay);
            }

            TimeSpan executionTime = GetFullExecutionTime() - GetChannelEndTime();

            if (Prototype is MovementPowerPrototype movementPowerProto)
            {
                if (movementPowerProto.ConstantMoveTime == false && movementPowerProto.ChanneledMoveTime == false)
                    executionTime += settings.MovementTime;
            }

            if (settings.Flags.HasFlag(PowerActivationSettingsFlags.Cancel) && CanBeUserCanceledNow())
            {
                TimeSpan activationTime = GetActivationTime();

                float animSpeed = GetAnimSpeed();
                float timeMult = animSpeed > 0f ? 1f / animSpeed : 0f;

                TimeSpan adjustedTime = activationTime + (Prototype.NoInterruptPostWindowTime * timeMult);
                if (adjustedTime < executionTime)
                {
                    flags |= EndPowerFlags.ExplicitCancel;
                    executionTime = adjustedTime;
                }
            }

            return SchedulePowerEnd(executionTime, flags);
        }

        private bool SchedulePowerEnd(TimeSpan delay, EndPowerFlags flags = EndPowerFlags.None, bool doNotReschedule = false)
        {
            PowerPrototype powerProto = Prototype;

            if (powerProto.ActiveUntilCancelled == false || flags.HasFlag(EndPowerFlags.ChanneledLoopEnd) || flags.HasFlag(EndPowerFlags.ChanneledMinTime))
            {
                EventScheduler scheduler = Game.GameEventScheduler;

                if (_endPowerEvent.IsValid)
                {
                    if (doNotReschedule == false)
                    {
                        scheduler.RescheduleEvent(_endPowerEvent, delay);
                        _endPowerEvent.Get().Initialize(this, flags);
                    }

                    return true;
                }

                scheduler.ScheduleEvent(_endPowerEvent, delay > TimeSpan.Zero ? delay : TimeSpan.FromMilliseconds(1), _pendingEvents2);
                _endPowerEvent.Get().Initialize(this, flags);
            }

            return true;
        }

        private void TEMP_SendActivatePowerMessage(in PowerActivationSettings settings)
        {
            ActivatePowerMessageFlags flags = ActivatePowerMessageFlags.None;
            if (settings.TargetEntityId == Owner.Id)
                flags |= ActivatePowerMessageFlags.TargetIsUser;

            if (settings.TriggeringPowerPrototypeRef != PrototypeId.Invalid)
                flags |= ActivatePowerMessageFlags.HasTriggeringPowerPrototypeRef;

            if (settings.TargetPosition == settings.UserPosition)
                flags |= ActivatePowerMessageFlags.TargetPositionIsUserPosition;
            else if (settings.TargetPosition != Vector3.Zero)
                flags |= ActivatePowerMessageFlags.HasTargetPosition;

            if (settings.MovementTime != TimeSpan.Zero)
                flags |= ActivatePowerMessageFlags.HasMovementTime;

            if (settings.VariableActivationTime != TimeSpan.Zero)
                flags |= ActivatePowerMessageFlags.HasVariableActivationTime;

            if (settings.PowerRandomSeed != 0)
                flags |= ActivatePowerMessageFlags.HasPowerRandomSeed;

            uint fxRandomSeed = settings.FXRandomSeed != 0 ? settings.FXRandomSeed : (uint)Game.Random.Next(1, 10000);
            flags |= ActivatePowerMessageFlags.HasFXRandomSeed;

            ActivatePowerArchive activatePower = new()
            {
                ReplicationPolicy = AOINetworkPolicyValues.AOIChannelProximity,
                Flags = flags,
                UserEntityId = Owner.Id,
                PowerPrototypeRef = PrototypeDataRef,
                UserPosition = settings.UserPosition,
                FXRandomSeed = fxRandomSeed
            };

            if (flags.HasFlag(ActivatePowerMessageFlags.HasTriggeringPowerPrototypeRef))
                activatePower.TriggeringPowerPrototypeRef = settings.TriggeringPowerPrototypeRef;

            if (flags.HasFlag(ActivatePowerMessageFlags.HasTargetPosition))
                activatePower.TargetPosition = settings.TargetPosition;

            if (flags.HasFlag(ActivatePowerMessageFlags.HasMovementTime))
                activatePower.MovementTime = settings.MovementTime;

            if (flags.HasFlag(ActivatePowerMessageFlags.HasVariableActivationTime))
                activatePower.VariableActivationTime = settings.VariableActivationTime;

            if (flags.HasFlag(ActivatePowerMessageFlags.HasPowerRandomSeed))
                activatePower.PowerRandomSeed = settings.PowerRandomSeed;

            Game.NetworkManager.SendMessageToInterested(activatePower.ToProtobuf(), Owner, AOINetworkPolicyValues.AOIChannelProximity, false);
        }

        private class EndPowerEvent : CallMethodEventParam1<Power, EndPowerFlags>
        {
            protected override CallbackDelegate GetCallback() => (t, p1) => t.EndPower(p1);
        }
    }
}
