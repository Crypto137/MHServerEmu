using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Powers
{
    public class MissilePower : Power
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly GRandom _random = new();
        private readonly HashSet<Player> _interestedPlayers = new();
        private readonly EventPointer<CreateMissileDelayedEvent> _createMissileEvent = new();

        protected TimeSpan CreationDelay { get => TimeSpan.FromMilliseconds((long)Properties[PropertyEnum.MissileCreationDelay]); }

        public MissilePowerPrototype MissilePowerPrototype { get => Prototype as MissilePowerPrototype; }       
        public int TotalNumberOfMissilesCreated { get; private set; }
        public int MissileCountPerCreationEvent { get => Properties[PropertyEnum.MissileCountPerCreationEvent]; }
        public int MaxNumberOfMissilesToCreateTotal { get => Properties[PropertyEnum.MissileCreationCountTotal]; }

        public MissilePower(Game game, PrototypeId prototypeDataRef) : base(game, prototypeDataRef)
        {
            TotalNumberOfMissilesCreated = 0;
        }

        public override void OnDeallocate()
        {
            Game?.GameEventScheduler.CancelEvent(_createMissileEvent);
            base.OnDeallocate();
        }

        public override PowerUseResult Activate(ref PowerActivationSettings settings)
        {
            // Remember players that were aware of the owner when we activated this power.
            // These are the players that receive NetMessageActivatePower and create the
            // missile on their own, including the player who used the power in the first place.
            _interestedPlayers.Clear();
            foreach (Player player in Game.NetworkManager.GetInterestedPlayers(Owner, AOINetworkPolicyValues.AOIChannelProximity))
                _interestedPlayers.Add(player);

            CancelCreationDelayEvent();
            return base.Activate(ref settings);
        }

        protected override PowerUseResult ActivateInternal(ref PowerActivationSettings settings)
        {
            if (settings.PowerRandomSeed != 0)
                _random.Seed((int)settings.PowerRandomSeed);
            return base.ActivateInternal(ref settings);
        }

        protected override bool ApplyInternal(PowerApplication powerApplication)
        {
            // Remove interested players who became no longer interested between activation and application of this power
            HashSet<Player> currentlyInterestedPlayers = new();

            foreach (Player player in Game.NetworkManager.GetInterestedPlayers(Owner, AOINetworkPolicyValues.AOIChannelProximity))
                currentlyInterestedPlayers.Add(player);

            // NOTE: It should be safe to remove from a HashSet<T> during iteration as of .NET 6
            foreach (Player player in _interestedPlayers)
            {
                if (currentlyInterestedPlayers.Contains(player) == false)
                    _interestedPlayers.Remove(player);
            }

            // Do the application
            if (base.ApplyInternal(powerApplication) == false) return false;

            var prototype = MissilePowerPrototype;
            if (prototype == null) return false;

            TotalNumberOfMissilesCreated = 0;
            if (prototype.MissileCreationContexts != null)
            {
                MissileCreateResult result = CreateMissileLooper(powerApplication);
                if (result == MissileCreateResult.Failure)
                {
                    if (Properties[PropertyEnum.PowerActiveUntilProjExpire])
                        SchedulePowerEnd(TimeSpan.Zero, EndPowerFlags.Force, false);
                    return false;
                }

                if (result == MissileCreateResult.Success && ShouldScheduleMoreMissilesForCreation(prototype))
                    if (ScheduleCreationDelayEvent(CreationDelay, powerApplication) == false)
                        return Logger.WarnReturn(false, $"Failed to schedule a missile creation event Power:{ToString()}");
            }

            return true;
        }

        protected override bool EndPowerInternal(EndPowerFlags flags)
        {
            base.EndPowerInternal(flags);
            var prototype = MissilePowerPrototype;
            if (prototype == null) return false;
            if (prototype.MissileAllowCreationAfterPwrEnds == false)
                CancelCreationDelayEvent();
            return true;
        }

        protected override bool OnEndPowerCancelEvents(EndPowerFlags flags)
        {
            base.OnEndPowerCancelEvents(flags);

            if (flags.HasFlag(EndPowerFlags.ExplicitCancel))
            {
                var prototype = MissilePowerPrototype;
                if (prototype == null) return false;
                if (prototype.MissileAllowCreationAfterPwrEnds == false)
                    CancelCreationDelayEvent();
            }

            return true;
        }

        private MissileCreateResult CreateMissileLooper(PowerApplication powerApplication)
        {
            var prototype = MissilePowerPrototype;
            if (prototype == null || Game == null) return MissileCreateResult.Failure;
            var entityManager = Game.EntityManager;
            if (entityManager == null) return MissileCreateResult.Failure;

            int maxMissileCountPerCreationEvent = Math.Max(1, MissileCountPerCreationEvent);
            int maxMissilesToCreate = Math.Max(1, MaxNumberOfMissilesToCreateTotal);

            var contexts = prototype.MissileCreationContexts;
            int contextsLength = contexts.Length;

            for (int i = 0; i < maxMissileCountPerCreationEvent && TotalNumberOfMissilesCreated < maxMissilesToCreate; i++)
            {
                int contextIndex;
                MissileCreationContextPrototype contextProto;

                if (prototype.MissileSelectRandomContext)
                {
                    Picker<MissileCreationContextPrototype> picker = new(_random);
                    foreach (var context in contexts)
                        picker.Add(context, context.RandomPickWeight);
                    picker.Pick(out contextProto, out contextIndex);
                }
                else if (prototype.EvalSelectMissileContextIndex != null)
                {
                    var owner = entityManager.GetEntity<WorldEntity>(powerApplication.UserEntityId);
                    var target = entityManager.GetEntity<WorldEntity>(powerApplication.TargetEntityId);

                    using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                    evalContext.Game = Game;
                    evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, prototype.Properties);
                    evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, owner.Properties);
                    evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, target?.Properties);
                    evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Var1, owner);

                    contextIndex = Eval.RunInt(prototype.EvalSelectMissileContextIndex, evalContext);
                    if (contextIndex < 0 || contextIndex >= contextsLength)
                        return Logger.WarnReturn(MissileCreateResult.Failure, $"Eval returned an out-of-range missile context index (max {contextsLength - 1}, index {contextIndex}). POWER={this}");

                    contextProto = contexts[contextIndex];
                }
                else
                {
                    contextIndex = TotalNumberOfMissilesCreated % contextsLength;
                    if (contextIndex < 0 || contextIndex >= contextsLength) return MissileCreateResult.Failure;
                    contextProto = contexts[contextIndex];
                }

                if (contextProto == null) return MissileCreateResult.Failure;
                if (CreateMissile(entityManager, contextProto, powerApplication, contextIndex, i) == false)
                    return (i > 0) ? MissileCreateResult.SuccessFirst : MissileCreateResult.Failure;

                TotalNumberOfMissilesCreated++;
            }

            return MissileCreateResult.Success;
        }

        private bool CreateMissile(EntityManager entityManager, MissileCreationContextPrototype missileContext, PowerApplication powerApplication, int contextIndex, int missileIndex)
        {
            if (Owner == null || Game == null || missileContext == null) return false;

            var region = Owner.Region;
            if (region == null) return false;

            var missileProto = missileContext.Entity.As<MissilePrototype>();
            if (missileProto == null) return false;

            using EntitySettings creationSettings = ObjectPoolManager.Instance.Get<EntitySettings>();
            creationSettings.EntityRef = missileContext.Entity;
            creationSettings.RegionId = region.Id;
            creationSettings.IgnoreNavi = missileContext.Ghost;

            var ownerPosition = Owner.RegionLocation.Position;
            var targetPosition = powerApplication.TargetPosition;

            var toTarget = targetPosition - ownerPosition;
            var direction = Vector3.Zero;

            if (Vector3.LengthSqr(toTarget) < Segment.Epsilon)
            {
                toTarget = Owner.Forward.To2D();
                direction = toTarget;
            }
            else
            {
                switch (missileContext.InitialDirection)
                {
                    case MissileInitialDirectionType.Forward:
                        direction = toTarget;
                        break;
                    case MissileInitialDirectionType.Backward:
                        direction = -toTarget;
                        break;
                    case MissileInitialDirectionType.Left:
                        direction = Vector3.Cross(toTarget, Vector3.ZAxis);
                        break;
                    case MissileInitialDirectionType.Right:
                        direction = -Vector3.Cross(toTarget, Vector3.ZAxis);
                        break;
                    case MissileInitialDirectionType.Up:
                        direction = Vector3.ZAxis;
                        break;
                    case MissileInitialDirectionType.OwnersForward:
                        direction = Owner.Forward;
                        break;
                }
            }

            if (missileContext.IgnoresPitch) direction.Z = 0.0f;

            direction = Vector3.Normalize(direction);

            if (missileContext.InitialDirectionAxisRotation == null) return false;
            if (missileContext.InitialDirectionRandomVariance == null) return false;

            var random = new GRandom((int)powerApplication.PowerRandomSeed + missileIndex * 10);
            var angleVector = Vector3.NextVector3(random, missileContext.InitialDirectionAxisRotation.ToVector3(), missileContext.InitialDirectionRandomVariance.ToVector3());

            if (!Vector3.IsNearZero(angleVector))
            {                
                angleVector = Vector3.ToRadians(angleVector);

                var axisX = Vector3.Normalize(toTarget);
                var axisZ = Vector3.ZAxis;
                var axisY = Vector3.Normalize(Vector3.Cross(axisX, axisZ));

                direction = Vector3.AxisAngleRotate(direction, axisZ, angleVector.X);
                direction = Vector3.AxisAngleRotate(direction, -axisY, angleVector.Y);
                direction = Vector3.AxisAngleRotate(direction, -axisX, angleVector.Z);
            }

            creationSettings.Orientation = Orientation.FromDeltaVector(direction);

            switch (missileContext.SpawnLocation)
            {
                case MissileSpawnLocationType.CenteredOnOwner:
                    creationSettings.Position = ownerPosition;
                    break;
                case MissileSpawnLocationType.InFrontOfOwner:
                    creationSettings.Position = ownerPosition + (direction * Owner.Bounds.Radius);
                    break;
            }

            creationSettings.Position += CreationOffset(direction, missileContext);

            Bounds bounds = new ();
            bounds.InitializeSphere(missileContext.Radius, BoundsCollisionType.Overlapping);
            bounds.Center = creationSettings.Position;

            if (missileContext.Ghost == false 
                && region.IsLocationClear(bounds, Region.GetPathFlagsForEntity(missileProto), PositionCheckFlags.None) == false)
                return false;

            if (region.GetCellAtPosition(creationSettings.Position) == null && region.ProjectBoundsIntoRegion(ref bounds, direction))
                creationSettings.Position = bounds.Center;

            if (missileContext.CreationOffsetCheckLOS && missileContext.CreationOffset != null && missileContext.CreationOffset.IsZero() == false)
            {
                var offsetSq = Vector3.DistanceSquared2D(ownerPosition, creationSettings.Position);
                var boundsSq = MathHelper.Square(Owner.Bounds.Radius + missileContext.Radius);
                if (offsetSq > boundsSq && Owner.LineOfSightTo(creationSettings.Position) == false)
                    return false;
            }

            creationSettings.VariationSeed = powerApplication.FXRandomSeed;
            creationSettings.LocomotorHeightOverride = Math.Max(missileContext.Radius, creationSettings.Position.Z - RegionLocation.ProjectToFloor(region, creationSettings.Position).Z);

            using var extraProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            SetExtraProperties(extraProperties, creationSettings, powerApplication, missileContext, contextIndex, missileProto);

            creationSettings.Properties = extraProperties;

            return CreateMissileInternal(creationSettings, missileContext, powerApplication, entityManager);
        }

        private void SetExtraProperties(PropertyCollection extraProperties, EntitySettings creationSettings, PowerApplication powerApplication, MissileCreationContextPrototype missileContext, int contextIndex, MissilePrototype missilePrototype)
        {
            if (Owner == null || missileContext == null) return;
            var ownerPosition = Owner.RegionLocation.Position;
            var targetPosition = powerApplication.TargetPosition;

            if (Owner is Missile)
            {
                extraProperties.CopyProperty(Owner.Properties, PropertyEnum.MissileCreatorId);
                extraProperties.CopyProperty(Owner.Properties, PropertyEnum.PowerUserOverrideID);
            }
            else
            {
                extraProperties[PropertyEnum.MissileCreatorId] = Owner.Id;
                extraProperties[PropertyEnum.MissileOwnedByPlayer] = Owner.CanBePlayerOwned();
                extraProperties[PropertyEnum.PowerUserOverrideID] = Owner.Id;
            }

            float projectileSpeed = GetProjectileSpeed(ownerPosition, targetPosition);
            float range = GetRange();

            var powerProto = MissilePowerPrototype;
            if (powerProto == null) return;

            TransferMissilePierceChance(extraProperties);

            if (powerProto.ExtraActivation != null)
                if (powerProto.ExtraActivation is SecondaryActivateOnReleasePrototype secondary)
                    if (secondary.RangeIncreasePerSecond != CurveId.Invalid)
                    {
                        var curve = GameDatabase.GetCurve(secondary.RangeIncreasePerSecond);
                        if (curve == null) return;
                        float timeRange = curve.GetAt(Rank);
                        float activationTime = MathF.Min(
                            (float)powerApplication.VariableActivationTime.TotalSeconds,
                            (float)TimeSpan.FromMilliseconds(secondary.MaxReleaseTimeMS).TotalSeconds);
                        range += timeRange * activationTime;
                    }

            extraProperties[PropertyEnum.MissileBaseMoveSpeed] = projectileSpeed;
            extraProperties[PropertyEnum.MissileRange] = range;

            if (missileContext.InfiniteLifespan == false)
            {
                int lifespanOverride = missileContext.LifespanOverrideMS;
                if (lifespanOverride != 0)
                    creationSettings.Lifespan = TimeSpan.FromMilliseconds(lifespanOverride);
                else
                {
                    if (projectileSpeed == 0f)
                    {
                        // Projectile speed appears to be zero in some cases, need to figure out if this is intended.
                        // When range / projectileSpeed = NaN, TimeSpan.FromSeconds() throws an exception.
                        Logger.Warn($"SetExtraProperties(): projectileSpeed is 0 in [{powerProto}] belonging to [{Owner}].");
                        projectileSpeed = 1f;
                    }

                    float lifespanMult = missilePrototype.Locomotion.RotationSpeed > 0 ? 1.5f : 1.0f;
                    creationSettings.Lifespan = TimeSpan.FromSeconds(range / projectileSpeed) * lifespanMult + missilePrototype.GetSeekDelayTime();
                }
            }

            if (missilePrototype.NaviMethod == LocomotorMethod.MissileSeeking)
                extraProperties[PropertyEnum.MissileSeekTargetId] = powerApplication.TargetEntityId;

            extraProperties[PropertyEnum.CreatorPowerPrototype] = PrototypeDataRef;
            extraProperties[PropertyEnum.PowerRank] = Rank;

            extraProperties.CopyProperty(Properties, PropertyEnum.CharacterLevel);
            extraProperties.CopyProperty(Properties, PropertyEnum.CombatLevel);
            extraProperties.CopyProperty(Properties, PropertyEnum.ItemLevel);

            extraProperties[PropertyEnum.MissileAbsorbImmunity] = Properties[PropertyEnum.MissileAbsorbImmunity];
            extraProperties[PropertyEnum.MissileBlockingHotspotImmunity] = Properties[PropertyEnum.MissileBlockingHotspotImmunity];
            extraProperties[PropertyEnum.MissileReflectionImmunity] = Properties[PropertyEnum.MissileReflectionImmunity];
            extraProperties[PropertyEnum.MovementSpeedChangeImmunity] = Properties[PropertyEnum.MovementSpeedChangeImmunity];

            extraProperties.CopyProperty(Properties, PropertyEnum.PowerUsesReturningWeapon);

            extraProperties[PropertyEnum.AllianceOverride] = Owner.Alliance.DataRef;

            if (missileContext.InterpolateRotationSpeed) 
                extraProperties[PropertyEnum.SpawnDistanceToTargetSqr] = Vector3.LengthSqr(targetPosition - ownerPosition);

            extraProperties[PropertyEnum.DamagePctBonusDistanceFar] = Properties[PropertyEnum.DamagePctBonusDistanceFar];
            extraProperties[PropertyEnum.DamagePctBonusDistanceClose] = Properties[PropertyEnum.DamagePctBonusDistanceClose];
            extraProperties.CopyPropertyRange(Properties, PropertyEnum.DamageMultPowerCdKwd);

            extraProperties[PropertyEnum.CreatorEntityAssetRefBase] = Owner.GetOriginalWorldAsset();
            extraProperties[PropertyEnum.CreatorEntityAssetRefCurrent] = Owner.GetEntityWorldAsset();
            extraProperties[PropertyEnum.CreatorRank] = Owner.Properties[PropertyEnum.Rank];

            extraProperties[PropertyEnum.VariableActivationTimeMS] = powerApplication.VariableActivationTime;
            extraProperties.CopyProperty(Properties, PropertyEnum.VariableActivationTimePct);

            extraProperties.CopyProperty(Properties, PropertyEnum.CritDamageMult);
            extraProperties.CopyProperty(Properties, PropertyEnum.CritDamageRating);
            extraProperties.CopyProperty(Properties, PropertyEnum.CritDamagePowerMultBonus);
            extraProperties.CopyProperty(Properties, PropertyEnum.CritRatingBonusAdd);
            extraProperties.CopyProperty(Properties, PropertyEnum.CritRatingBonusMult);
            extraProperties.CopyPropertyRange(Properties, PropertyEnum.CritRatingBonusMultPowerKeyword);

            extraProperties.CopyProperty(Properties, PropertyEnum.SuperCritDamageMult);
            extraProperties.CopyProperty(Properties, PropertyEnum.SuperCritDamageRating);
            extraProperties.CopyProperty(Properties, PropertyEnum.SuperCritDamagePowerMultBonus);
            extraProperties.CopyProperty(Properties, PropertyEnum.SuperCritRatingBonusAdd);
            extraProperties.CopyProperty(Properties, PropertyEnum.SuperCritRatingBonusMult);
            extraProperties.CopyPropertyRange(Properties, PropertyEnum.SuperCritRatingBonusMultPowerKeyword);

            extraProperties[PropertyEnum.MissileContextIndex] = contextIndex;

            if (powerApplication.PowerRandomSeed != 0)
            {
                int seed = (int)powerApplication.PowerRandomSeed * (contextIndex + 1) * 10;
                extraProperties[PropertyEnum.MissileSeed] = seed;
            }

            extraProperties.CopyProperty(Properties, PropertyEnum.DamagePctBonus);
            extraProperties.CopyProperty(Properties, PropertyEnum.DamageRating);
            extraProperties.CopyProperty(Properties, PropertyEnum.DamageMult);
            extraProperties.CopyPropertyRange(Properties, PropertyEnum.DamageMultForPower);

            SerializeEntityPropertiesForPowerPayload(GetPayloadPropertySourceEntity(), Properties);    // todo: team-up away powers
        }

        private void TransferMissilePierceChance(PropertyCollection extraProperties)
        {
            foreach (var kvp in Owner.Properties.IteratePropertyRange(PropertyEnum.MissilePierceChance))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId keywordRef);
                var propInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(PropertyEnum.MissilePierceChance);
                if (keywordRef == (PrototypeId)propInfo.GetParamPrototypeBlueprint(0) || HasKeyword(GameDatabase.GetPrototype<KeywordPrototype>(keywordRef)))
                {
                    extraProperties[PropertyEnum.MissilePierceChance] = kvp.Value;
                    break;
                }
            }
        }

        private bool CreateMissileInternal(EntitySettings creationSettings, MissileCreationContextPrototype missileContext, PowerApplication powerApplication, EntityManager entityManager)
        {
            ulong regionId = creationSettings.RegionId;
            if (missileContext == null) return false;

            // Clear region id so that client-independent missiles don't enter the world automatically
            // before we get the chance to set their replication channel to client-independent.
            if (missileContext.IndependentClientMovement) 
                creationSettings.RegionId = 0;

            if (entityManager.CreateEntity(creationSettings) is not Missile missile) return false;

            if (missileContext.IndependentClientMovement)
            {
                // Add client independent replication channel to the missile manually for all players who
                // were interested in the owner during the activation.
                foreach (Player player in _interestedPlayers)
                {
                    AreaOfInterest aoi = player.AOI;
                    aoi.AddClientIndependentEntity(missile);
                }

                // Restore regionId
                creationSettings.RegionId = regionId;
                var region = Game.RegionManager.GetRegion(regionId);

                // Add the missile to the world
                missile.EnterWorld(region, creationSettings.Position, creationSettings.Orientation, creationSettings);
            }

            var locomotor = missile.Locomotor;
            if (locomotor == null) return false;

            if (locomotor.Method == LocomotorMethod.MissileSeeking)
            {
                if (missileContext.IndependentClientMovement) return false;

                var locomotionOptions = new LocomotionOptions { RepathDelay = TimeSpan.FromSeconds(0.5) };
                if (missileContext.OneShot == false)
                    locomotionOptions.Flags |= LocomotionFlags.LocomotionNoEntityCollide;

                if (missile.AIController == null)
                {
                    ulong targetId = missile.Properties[PropertyEnum.MissileSeekTargetId];
                    var target = entityManager.GetEntity<WorldEntity>(targetId);
                    if (target != null && target.IsDead == false)
                    {
                        locomotor.FollowEntity(targetId, 0.0f, locomotionOptions);
                        locomotor.FollowEntityMissingEvent.AddActionBack(missile.SeekTargetMissingAction);
                    }
                    else
                    {
                        locomotor.MoveTo(powerApplication.TargetPosition, locomotionOptions);
                    }
                } 
                else
                {
                    var style = TargetingStylePrototype;
                    ulong targetId = powerApplication.TargetEntityId;
                    if (targetId != Entity.InvalidId && (style.TargetingShape == TargetingShapeType.SingleTarget 
                        || style.TargetingShape == TargetingShapeType.SingleTargetRandom))
                    {
                        var target = entityManager.GetEntity<WorldEntity>(targetId);
                        if (target != null) 
                            missile.AIController?.SetTargetEntity(target);
                    }

                    locomotor.MoveForward(locomotionOptions);
                    var missileProto = missile.MissilePrototype;
                    if (missileProto.GetSeekDelayTime() > TimeSpan.Zero)
                        locomotor.SetMethod(LocomotorMethod.Default, missileProto.GetSeekDelaySpeed());
                }
            }

            return true;
        }

        private static Vector3 CreationOffset(Vector3 direction, MissileCreationContextPrototype missileContext)
        {
            if (missileContext.CreationOffset == null) return Vector3.Zero;
            return Transform3.BuildTransform(Vector3.Zero, Orientation.FromDeltaVector(direction)) * missileContext.CreationOffset.ToVector3();
        }

        private bool ScheduleCreationDelayEvent(TimeSpan delay, PowerApplication powerApplication)
        {
            var scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return false;
            if (_createMissileEvent.IsValid) return Logger.WarnReturn(false, $"ScheduleCreationDelayEvent called when event already scheduled. POWER={ToString()}");
            scheduler.ScheduleEvent(_createMissileEvent, delay, _pendingEvents);
            _createMissileEvent.Get().Initialize(this, powerApplication);

            return true;
        }

        private bool ShouldScheduleMoreMissilesForCreation(MissilePowerPrototype missilePowerProto)
        {
            return CreationDelay > TimeSpan.Zero
                && (missilePowerProto.MissileAllowCreationAfterPwrEnds == false 
                || TotalNumberOfMissilesCreated < MaxNumberOfMissilesToCreateTotal
                || MaxNumberOfMissilesToCreateTotal < 0);
        }

        public enum MissileCreateResult
        {
            Success,
            Failure,
            SuccessFirst,
        }

        private void CancelCreationDelayEvent()
        {
            if (_createMissileEvent.IsValid)
                Game?.GameEventScheduler?.CancelEvent(_createMissileEvent);
        }

        private class CreateMissileDelayedEvent : ScheduledEvent
        {
            private static readonly Logger Logger = LogManager.CreateLogger();

            private MissilePower _missilePower;
            private PowerApplication _powerApplication;

            public void Initialize(MissilePower missilePower, PowerApplication powerApplication)
            {
                _missilePower = missilePower;
                _powerApplication = powerApplication;
            }

            public override bool OnTriggered()
            {
                if (_missilePower == null) return Logger.WarnReturn(false, "OnTriggered(): _missilePower == null");

                var owner = _missilePower.Owner;
                if (owner == null || (owner is Agent && owner.IsDead) || owner.IsInWorld == false) return false;

                var missilePowerProto = _missilePower.MissilePowerPrototype;
                if (missilePowerProto == null) return false;

                MissileCreateResult result = _missilePower.CreateMissileLooper(_powerApplication);

                if (result != MissileCreateResult.Success) return Logger.WarnReturn(false, $"CreateMissileDelayedEvent failed to create all its missiles! " +
                    $"Result: {result}  Power: {_missilePower}  Owner: {owner}");

                if (_missilePower.ShouldScheduleMoreMissilesForCreation(missilePowerProto) 
                    && _missilePower.ScheduleCreationDelayEvent(_missilePower.CreationDelay, _powerApplication) == false)
                        return Logger.WarnReturn(false, $"Failed to schedule a missile creation event Power:{_missilePower}");

                return true;
            }
        }
    }
}
