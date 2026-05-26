using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Powers
{
    public class MissilePower : Power
    {
        private readonly GRandom _random = new();
        private readonly HashSet<Player> _interestedPlayers = new();

        private readonly EventPointer<CreateMissileDelayedEvent> _createMissileEvent = new();

        public int TotalNumberOfMissilesCreated { get; private set; } = 0;

        protected TimeSpan CreationDelay { get => TimeSpan.FromMilliseconds((long)Properties[PropertyEnum.MissileCreationDelay]); }

        public MissilePowerPrototype MissilePowerPrototype { get => Prototype as MissilePowerPrototype; }
        public int MissileCountPerCreationEvent { get => Properties[PropertyEnum.MissileCountPerCreationEvent]; }
        public int MaxNumberOfMissilesToCreateTotal { get => Properties[PropertyEnum.MissileCreationCountTotal]; }

        public MissilePower(Game game, PrototypeId prototypeDataRef) : base(game, prototypeDataRef)
        {
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

            using var interestedPlayerListHandle = ListPool<Player>.Instance.Get(out List<Player> interestedPlayerList);
            Game.NetworkManager.GetInterestedPlayers(interestedPlayerList, Owner, AOINetworkPolicyValues.AOIChannelProximity);
            foreach (Player player in interestedPlayerList)
                _interestedPlayers.Add(player);             

            CancelCreationDelayEvent();

            return base.Activate(ref settings);
        }

        protected override PowerUseResult ActivateInternal(ref PowerActivationSettings settings)
        {
            if (settings.PowerRandomSeed != 0)
                _random.Seed(settings.PowerRandomSeed);

            return base.ActivateInternal(ref settings);
        }

        protected override bool ApplyInternal(PowerApplication powerApplication)
        {
            // Remove interested players who became no longer interested between activation and application of this power
            using var interestedPlayerSetHandle = HashSetPool<Player>.Instance.Get(out HashSet<Player> interestedPlayerSet);
            using var interestedPlayerListHandle = ListPool<Player>.Instance.Get(out List<Player> interestedPlayerList);

            Game.NetworkManager.GetInterestedPlayers(interestedPlayerList, Owner, AOINetworkPolicyValues.AOIChannelProximity);

            foreach (Player player in interestedPlayerList)
                interestedPlayerSet.Add(player);

            foreach (Player player in _interestedPlayers)
            {
                if (interestedPlayerSet.Contains(player) == false)
                    _interestedPlayers.Remove(player);
            }

            // Do the application
            if (base.ApplyInternal(powerApplication) == false)
                return false;

            MissilePowerPrototype prototype = MissilePowerPrototype;
            if (!Verify.IsNotNull(prototype)) return false;

            TotalNumberOfMissilesCreated = 0;

            if (prototype.MissileCreationContexts.HasValue())
            {
                MissileCreateResult result = CreateMissileLooper(powerApplication);
                if (result == MissileCreateResult.Failure)
                {
                    if (Properties[PropertyEnum.PowerActiveUntilProjExpire])
                        SchedulePowerEnd(TimeSpan.Zero, EndPowerFlags.Force, false);

                    return false;
                }

                if (result == MissileCreateResult.Success && ShouldScheduleMoreMissilesForCreation(prototype))
                {
                    if (!Verify.IsTrue(ScheduleCreationDelayEvent(CreationDelay, powerApplication), $"Failed to schedule a missile creation event Power:{this}"))
                        return false;
                }
            }

            return true;
        }

        protected override void EndPowerInternal(EndPowerFlags flags)
        {
            base.EndPowerInternal(flags);

            MissilePowerPrototype prototype = MissilePowerPrototype;
            if (!Verify.IsNotNull(prototype)) return;

            if (prototype.MissileAllowCreationAfterPwrEnds == false)
                CancelCreationDelayEvent();
        }

        protected override void OnEndPowerCancelEvents(EndPowerFlags flags)
        {
            base.OnEndPowerCancelEvents(flags);

            if (flags.HasFlag(EndPowerFlags.ExplicitCancel))
            {
                MissilePowerPrototype prototype = MissilePowerPrototype;
                if (!Verify.IsNotNull(prototype)) return;

                if (prototype.MissileAllowCreationAfterPwrEnds == false)
                    CancelCreationDelayEvent();
            }
        }

        private MissileCreateResult CreateMissileLooper(PowerApplication powerApplication)
        {
            MissilePowerPrototype prototype = MissilePowerPrototype;
            if (!Verify.IsNotNull(prototype)) return MissileCreateResult.Failure;

            if (!Verify.IsNotNull(Game)) return MissileCreateResult.Failure;

            EntityManager entityManager = Game.EntityManager;
            if (!Verify.IsNotNull(entityManager)) return MissileCreateResult.Failure;

            int maxMissileCountPerCreationEvent = Math.Max(1, MissileCountPerCreationEvent);
            int maxMissilesToCreate = Math.Max(1, MaxNumberOfMissilesToCreateTotal);

            MissileCreationContextPrototype[] contexts = prototype.MissileCreationContexts;
            int vectorSize = contexts.Length;

            for (int i = 0; i < maxMissileCountPerCreationEvent && TotalNumberOfMissilesCreated < maxMissilesToCreate; i++)
            {
                int contextIndex;
                MissileCreationContextPrototype contextPrototype;

                if (prototype.MissileSelectRandomContext)
                {
                    Picker<MissileCreationContextPrototype> picker = new(_random);
                    foreach (MissileCreationContextPrototype context in contexts)
                        picker.Add(context, context.RandomPickWeight);

                    picker.Pick(out contextPrototype, out contextIndex);
                }
                else if (prototype.EvalSelectMissileContextIndex != null)
                {
                    WorldEntity owner = entityManager.GetEntity<WorldEntity>(powerApplication.UserEntityId);
                    WorldEntity target = entityManager.GetEntity<WorldEntity>(powerApplication.TargetEntityId);

                    using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                    evalContext.Game = Game;
                    evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, prototype.Properties);
                    evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, owner?.Properties);
                    evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, target?.Properties);
                    evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Var1, owner);

                    contextIndex = Eval.RunInt(prototype.EvalSelectMissileContextIndex, evalContext);
                    if (!Verify.IsTrue(contextIndex >= 0 && contextIndex < vectorSize, $"Eval returned an out-of-range missile context index (max {vectorSize - 1}, index {contextIndex}). POWER={this}"))
                        return MissileCreateResult.Failure;

                    contextPrototype = contexts[contextIndex];
                }
                else
                {
                    contextIndex = TotalNumberOfMissilesCreated % vectorSize;
                    if (!Verify.IsTrue(contextIndex >= 0 && contextIndex < vectorSize)) return MissileCreateResult.Failure;

                    contextPrototype = contexts[contextIndex];
                }

                if (!Verify.IsNotNull(contextPrototype)) return MissileCreateResult.Failure;

                if (CreateMissile(entityManager, contextPrototype, powerApplication, contextIndex, i) == false)
                    return (i > 0) ? MissileCreateResult.PartialSuccess : MissileCreateResult.Failure;

                TotalNumberOfMissilesCreated++;
            }

            return MissileCreateResult.Success;
        }

        private bool CreateMissile(EntityManager entityManager, MissileCreationContextPrototype missileContext, PowerApplication powerApplication, int contextIndex, int missileIndex)
        {
            if (!Verify.IsNotNull(Owner)) return false;
            if (!Verify.IsNotNull(Game)) return false;
            if (!Verify.IsNotNull(missileContext)) return false;

            Region region = Owner.Region;
            if (!Verify.IsNotNull(region)) return false;

            MissilePrototype missilePrototype = missileContext.Entity.As<MissilePrototype>();
            if (!Verify.IsNotNull(missilePrototype)) return false;

            using EntitySettings creationSettings = ObjectPoolManager.Instance.Get<EntitySettings>();
            creationSettings.EntityRef = missileContext.Entity;
            creationSettings.RegionId = region.Id;
            creationSettings.IgnoreNavi = missileContext.Ghost;

            Vector3 ownerPosition = Owner.RegionLocation.Position;
            Vector3 targetPosition = powerApplication.TargetPosition;

            Vector3 toTarget = targetPosition - ownerPosition;
            Vector3 direction = Vector3.Zero;

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

            if (missileContext.IgnoresPitch)
                direction.Z = 0.0f;

            direction = Vector3.Normalize(direction);

            if (!Verify.IsNotNull(missileContext.InitialDirectionAxisRotation)) return false;
            if (!Verify.IsNotNull(missileContext.InitialDirectionRandomVariance)) return false;

            GRandom random = new(powerApplication.PowerRandomSeed + missileIndex * 10);
            Vector3 angleVector = Vector3.NextVector3(random, missileContext.InitialDirectionAxisRotation.ToVector3(), missileContext.InitialDirectionRandomVariance.ToVector3());

            if (Vector3.IsNearZero(angleVector) == false)
            {                
                angleVector = Vector3.ToRadians(angleVector);

                Vector3 axisX = Vector3.Normalize(toTarget);
                Vector3 axisZ = Vector3.ZAxis;
                Vector3 axisY = Vector3.Normalize(Vector3.Cross(axisX, axisZ));

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

            creationSettings.Position += GetCreationOffset(direction, missileContext);  // applyCreationOffset() in the client

            Bounds bounds = new();
            bounds.InitializeSphere(missileContext.Radius, BoundsCollisionType.Overlapping);
            bounds.Center = creationSettings.Position;

            if (missileContext.Ghost == false &&
                region.IsLocationClear(ref bounds, Region.GetPathFlagsForEntity(missilePrototype), PositionCheckFlags.None) == false)
                return false;

            if (region.GetCellAtPosition(creationSettings.Position) == null && region.ProjectBoundsIntoRegion(ref bounds, direction))
                creationSettings.Position = bounds.Center;

            if (missileContext.CreationOffsetCheckLOS && missileContext.CreationOffset != null && missileContext.CreationOffset.IsZero() == false)
            {
                float offsetSq = Vector3.DistanceSquared2D(ownerPosition, creationSettings.Position);
                float boundsSq = MathHelper.Square(Owner.Bounds.Radius + missileContext.Radius);
                if (offsetSq > boundsSq && Owner.LineOfSightTo(creationSettings.Position) == false)
                    return false;
            }

            creationSettings.VariationSeed = powerApplication.FXRandomSeed;
            creationSettings.LocomotorHeightOverride = Math.Max(missileContext.Radius, creationSettings.Position.Z - RegionLocation.ProjectToFloor(region, creationSettings.Position).Z);

            using PropertyCollection extraProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            SetExtraProperties(extraProperties, creationSettings, powerApplication, missileContext, contextIndex, missilePrototype);

            creationSettings.Properties = extraProperties;

            return Verify.IsTrue(CreateMissileInternal(creationSettings, missileContext, powerApplication, entityManager));
        }

        private static Vector3 GetCreationOffset(Vector3 direction, MissileCreationContextPrototype missileContext)
        {
            if (!Verify.IsNotNull(missileContext.CreationOffset)) return Vector3.Zero;
            return Transform3.BuildTransform(Vector3.Zero, Orientation.FromDeltaVector(direction)) * missileContext.CreationOffset.ToVector3();
        }

        private void SetExtraProperties(PropertyCollection extraProperties, EntitySettings creationSettings, PowerApplication powerApplication, MissileCreationContextPrototype missileContext, int contextIndex, MissilePrototype missilePrototype)
        {
            if (!Verify.IsNotNull(Owner)) return;
            if (!Verify.IsNotNull(missileContext)) return;

            Vector3 ownerPosition = Owner.RegionLocation.Position;
            Vector3 targetPosition = powerApplication.TargetPosition;

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

            MissilePowerPrototype powerPrototype = MissilePowerPrototype;
            if (!Verify.IsNotNull(powerPrototype)) return;

            TransferMissilePierceChance(extraProperties);

            if (powerPrototype.ExtraActivation != null &&
                powerPrototype.ExtraActivation is SecondaryActivateOnReleasePrototype extraActivateOnRelease)
            {
                if (extraActivateOnRelease.RangeIncreasePerSecond != CurveId.Invalid)
                {
                    Curve curve = extraActivateOnRelease.RangeIncreasePerSecond.AsCurve();
                    if (!Verify.IsNotNull(curve)) return;

                    float rangeIncreasePerSecond = curve.GetAt(Rank);
                    float activationTime = MathF.Min(
                        (float)powerApplication.VariableActivationTime.TotalSeconds,
                        (float)TimeSpan.FromMilliseconds(extraActivateOnRelease.MaxReleaseTimeMS).TotalSeconds);

                    range += rangeIncreasePerSecond * activationTime;
                }
            }

            extraProperties[PropertyEnum.MissileBaseMoveSpeed] = projectileSpeed;
            extraProperties[PropertyEnum.MissileRange] = range;

            if (missileContext.InfiniteLifespan == false)
            {
                int lifespanOverride = missileContext.LifespanOverrideMS;
                if (lifespanOverride != 0)
                {
                    creationSettings.Lifespan = TimeSpan.FromMilliseconds(lifespanOverride);
                }
                else
                {
                    // Projectile speed appears to be zero in some cases, need to figure out if this is okay.
                    // When range / projectileSpeed = NaN, TimeSpan.FromSeconds() throws an exception.
                    TimeSpan lifespan = TimeSpan.Zero;
                    if (Verify.IsTrue(projectileSpeed != 0f, $"Projectile speed is zero! power=[{powerPrototype}], owner=[{Owner}]"))
                        lifespan = TimeSpan.FromSeconds(range / projectileSpeed);

                    float lifespanMult = missilePrototype.Locomotion.RotationSpeed > 0 ? 1.5f : 1.0f;
                    creationSettings.Lifespan = lifespan * lifespanMult + missilePrototype.GetSeekDelayTime();
                }
            }

            if (missilePrototype.NaviMethod == LocomotorMethod.MissileSeeking)
                extraProperties[PropertyEnum.MissileSeekTargetId] = powerApplication.TargetEntityId;

            // CreatorPowerPrototype needs to be set after SerializeEntityPropertiesForPowerPayload so that it doesn't get overriden
            //extraProperties[PropertyEnum.CreatorPowerPrototype] = PrototypeDataRef;
            
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
                int seed = powerApplication.PowerRandomSeed * (contextIndex + 1) * 10;
                extraProperties[PropertyEnum.MissileSeed] = seed;
            }

            extraProperties.CopyProperty(Properties, PropertyEnum.DamagePctBonus);
            extraProperties.CopyProperty(Properties, PropertyEnum.DamageRating);
            extraProperties.CopyProperty(Properties, PropertyEnum.DamageMult);
            extraProperties.CopyPropertyRange(Properties, PropertyEnum.DamageMultForPower);

            WorldEntity propertySourceEntity = GetPayloadPropertySourceEntity(GetUltimateOwner());
            if (!Verify.IsNotNull(propertySourceEntity)) return;

            SerializeEntityPropertiesForPowerPayload(propertySourceEntity, extraProperties);

            extraProperties[PropertyEnum.CreatorPowerPrototype] = PrototypeDataRef;
        }

        private void TransferMissilePierceChance(PropertyCollection extraProperties)
        {
            foreach (var kvp in Owner.Properties.IteratePropertyRange(PropertyEnum.MissilePierceChance))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId keywordRef);
                PropertyInfo propInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(PropertyEnum.MissilePierceChance);

                if (keywordRef == (PrototypeId)propInfo.GetParamPrototypeBlueprint(0) || HasKeyword(keywordRef.As<KeywordPrototype>()))
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
                        locomotor.FollowEntity(targetId, 0.0f, ref locomotionOptions);
                        locomotor.FollowEntityMissingEvent.AddActionBack(missile.SeekTargetMissingAction);
                    }
                    else
                    {
                        locomotor.MoveTo(powerApplication.TargetPosition, ref locomotionOptions);
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

                    locomotor.MoveForward(ref locomotionOptions);
                    var missileProto = missile.MissilePrototype;
                    if (missileProto.GetSeekDelayTime() > TimeSpan.Zero)
                        locomotor.SetMethod(LocomotorMethod.Default, missileProto.GetSeekDelaySpeed());
                }
            }

            return true;
        }

        private bool ShouldScheduleMoreMissilesForCreation(MissilePowerPrototype missilePowerProto)
        {
            return CreationDelay > TimeSpan.Zero &&
                (missilePowerProto.MissileAllowCreationAfterPwrEnds == false ||
                TotalNumberOfMissilesCreated < MaxNumberOfMissilesToCreateTotal ||
                MaxNumberOfMissilesToCreateTotal < 0);
        }

        private bool ScheduleCreationDelayEvent(TimeSpan delay, PowerApplication powerApplication)
        {
            EventScheduler scheduler = Game?.GameEventScheduler;
            if (!Verify.IsNotNull(scheduler)) return false;

            if (!Verify.IsTrue(_createMissileEvent.IsValid == false, $"ScheduleCreationDelayEvent called when event already scheduled. POWER={this}"))
                return false;

            scheduler.ScheduleEvent(_createMissileEvent, delay, _pendingEvents);
            _createMissileEvent.Get().Initialize(this, powerApplication);

            return true;
        }

        private void CancelCreationDelayEvent()
        {
            if (_createMissileEvent.IsValid == false)
                return;

            EventScheduler scheduler = Game?.GameEventScheduler;
            if (!Verify.IsNotNull(scheduler)) return;

            scheduler.CancelEvent(_createMissileEvent);
        }

        private enum MissileCreateResult
        {
            Success,
            Failure,
            PartialSuccess,
        }

        private class CreateMissileDelayedEvent : ScheduledEvent
        {
            private MissilePower _missilePower;
            private PowerApplication _powerApplication;

            public void Initialize(MissilePower missilePower, PowerApplication powerApplication)
            {
                _missilePower = missilePower;
                _powerApplication = powerApplication;
            }

            public override bool OnTriggered()
            {
                if (!Verify.IsNotNull(_missilePower)) return false;

                WorldEntity owner = _missilePower.Owner;
                if (owner == null || (owner is Agent && owner.IsDead) || owner.IsInWorld == false)
                    return false;

                MissilePowerPrototype missilePowerProto = _missilePower.MissilePowerPrototype;
                if (!Verify.IsNotNull(missilePowerProto)) return false;

                MissileCreateResult result = _missilePower.CreateMissileLooper(_powerApplication);
                if (!Verify.IsTrue(result == MissileCreateResult.Success, $"CreateMissileDelayedEvent failed to create all its missiles! Result: {result}  Power: {_missilePower}  Owner: {owner}"))
                    return false;

                if (_missilePower.ShouldScheduleMoreMissilesForCreation(missilePowerProto))
                {
                    if (!Verify.IsTrue(_missilePower.ScheduleCreationDelayEvent(_missilePower.CreationDelay, _powerApplication),
                        $"Failed to schedule a missile creation event Power:{_missilePower}"))
                        return false;
                }

                return true;
            }

            public override void Clear()
            {
                _missilePower = default;
                _powerApplication = default;
            }
        }
    }
}
