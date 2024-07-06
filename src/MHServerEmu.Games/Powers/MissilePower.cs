using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Powers
{
    public class MissilePower : Power
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public MissilePowerPrototype MissilePowerPrototype { get => Prototype as MissilePowerPrototype; }       
        public int TotalNumberOfMissilesCreated { get; private set; }
        public int MissileCountPerCreationEvent { get => Properties[PropertyEnum.MissileCountPerCreationEvent]; }
        public int MaxNumberOfMissilesToCreateTotal { get => Properties[PropertyEnum.MissileCreationCountTotal]; }
        protected TimeSpan CreationDelay { get => TimeSpan.FromMilliseconds((long)Properties[PropertyEnum.MissileCreationDelay]); }

        private readonly GRandom _random;
        private readonly EventPointer<CreateMissileDelayedEvent> _createMissileEvent = new();

        public MissilePower(Game game, PrototypeId prototypeDataRef) : base(game, prototypeDataRef)
        {
            _random = new();
            TotalNumberOfMissilesCreated = 0;
        }

        public override void OnDeallocate()
        {
            Game?.GameEventScheduler.CancelEvent(_createMissileEvent);
            base.OnDeallocate();
        }

        public override PowerUseResult Activate(ref PowerActivationSettings settings)
        {
            // TODO Get and Sort clients for AOI

            CancelCreationDelayEvent();
            return base.Activate(ref settings);
        }

        protected override PowerUseResult ActivateInternal(in PowerActivationSettings settings)
        {
            if (settings.PowerRandomSeed != 0)
                _random.Seed((int)settings.PowerRandomSeed);
            return base.ActivateInternal(settings);
        }

        protected override bool ApplyInternal(PowerApplication powerApplication)
        {
            // TODO check Interested clients from AOI

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

                    EvalContextData contextData = new(Game);
                    contextData.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, prototype.Properties);
                    contextData.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, owner.Properties);
                    contextData.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, target.Properties);
                    contextData.SetReadOnlyVar_EntityPtr(EvalContext.Var1, owner);

                    contextIndex = Eval.RunInt(prototype.EvalSelectMissileContextIndex, contextData);
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

            var creationSettings = new EntitySettings
            {
                EntityRef = missileContext.Entity,
                RegionId = region.Id,
                IgnoreNavi = missileContext.Ghost
            };

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

            var extraProperties = new PropertyCollection();
            SetExtraProperties(extraProperties, creationSettings, powerApplication, missileContext, contextIndex, missileProto);

            creationSettings.Properties = extraProperties;

            return CreateMissileInternal(creationSettings, missileContext, powerApplication, entityManager);
        }

        private void SetExtraProperties(PropertyCollection extraProperties, EntitySettings creationSettings, PowerApplication powerApplication, MissileCreationContextPrototype missileContext, int contextIndex, MissilePrototype missilePrototype)
        {
            throw new NotImplementedException();
        }

        private bool CreateMissileInternal(EntitySettings creationSettings, MissileCreationContextPrototype missileContext, PowerApplication powerApplication, EntityManager entityManager)
        {
            throw new NotImplementedException();
        }

        private static Vector3 CreationOffset(Vector3 direction, MissileCreationContextPrototype missileContext)
        {
            if (missileContext.CreationOffset == null) return Vector3.Zero;
            return Transform3.BuildTransform(Vector3.Zero, Orientation.FromDeltaVector(direction)) * missileContext.CreationOffset.ToVector3();
        }

        private bool ScheduleCreationDelayEvent(TimeSpan delay, PowerApplication powerApplication)
        {
            var sheduler = Game?.GameEventScheduler;
            if (sheduler == null) return false;
            if (_createMissileEvent.IsValid) return Logger.WarnReturn(false, $"ScheduleCreationDelayEvent called when event already scheduled. POWER={ToString()}");
            sheduler.ScheduleEvent(_createMissileEvent, delay, _pendingEvents);
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
