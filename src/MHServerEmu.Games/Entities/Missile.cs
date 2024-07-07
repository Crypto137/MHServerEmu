using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Entities
{
    public class Missile : Agent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public MissilePrototype MissilePrototype { get => Prototype as MissilePrototype; }

        private Bounds _entityCollideBounds;
        public override Bounds EntityCollideBounds { get => _entityCollideBounds; set => _entityCollideBounds = value; }
        public override bool CanRepulseOthers => false;
        public PrototypeId MissilePowerPrototypeRef { get => Properties[PropertyEnum.CreatorPowerPrototype]; }
        public MissilePowerPrototype MissilePowerPrototype { get => GameDatabase.GetPrototype<MissilePowerPrototype>(MissilePowerPrototypeRef); }

        private MissileCreationContextPrototype _contextPrototype;

        public MissileCreationContextPrototype MissileCreationContextPrototype { get => _contextPrototype; }
        public GravitatedMissileContextPrototype GravitatedContext { get => _contextPrototype?.GravitatedContext; }
        public bool IsReturningMissile { get => _contextPrototype != null && _contextPrototype.IsReturningMissile; }
        public bool IsMovedIndependentlyOnClient { get => _contextPrototype != null && _contextPrototype.IndependentClientMovement; }
        public bool IsKilledOnOverlappingCollision { get => _contextPrototype != null && _contextPrototype.KilledOnOverlappingCollision; }
        public Random Random { get; private set; }
        public Action ReturnTargetMissingEvent { get; private set; }
        public Action SeekTargetMissingEvent { get; private set; }

        private EventPointer<PendingKillCallback> _pendingKillEvent = new();

        public Missile(Game game) : base(game) 
        {
            _flags |= EntityFlags.IsNeverAffectedByPowers;
            _contextPrototype = null;
            ReturnTargetMissingEvent = OnReturnTargetMissing;
            SeekTargetMissingEvent = OnSeekTargetMissing;
            Random = new();
            _entityCollideBounds = new();
        }

        public override void OnEnteredWorld(EntitySettings settings)
        {
            base.OnEnteredWorld(settings);
            if (_contextPrototype == null) return;

            InitializeEntityCollideBounds(_contextPrototype);
            if (IsSimulated)
            {
                if (ApplyMissileCreationContext(_contextPrototype) == false) return;
                StartMovement();
            }            
        }

        private void StartMovement()
        {
            throw new NotImplementedException();
        }

        private bool ApplyMissileCreationContext(MissileCreationContextPrototype creationContext)
        {
            if (creationContext.PowerList.HasValue())
                foreach (var powerContext in creationContext.PowerList)
                    if (powerContext == null || powerContext.Power == PrototypeId.Invalid 
                        || CreateMissilePower(powerContext.Power) == null)
                        return false;
            return true;
        }

        private Power CreateMissilePower(PrototypeId powerRef)
        {
            if (powerRef == PrototypeId.Invalid) return null;
            Power power = GetPower(powerRef);
            if (power != null)
            {
                PowerIndexProperties indexProps = new(Properties[PropertyEnum.PowerRank], CharacterLevel, CombatLevel, 
                    Properties[PropertyEnum.ItemLevel], Properties[PropertyEnum.ItemVariation]);
                power = AssignPower(powerRef, indexProps, false);
            }
            return power;
        }

        private void InitializeEntityCollideBounds(MissileCreationContextPrototype creationContext)
        {
            float radius = creationContext.RadiusEffectOverride > 0 ? creationContext.RadiusEffectOverride : Bounds.GetRadius();
            var location = RegionLocation;
            float height = Math.Max(radius, location.Position.Z - location.ProjectToFloor().Z);
            _entityCollideBounds.InitializeCapsule(radius, height, BoundsCollisionType.Overlapping, BoundsFlags.None);
        }

        public override void OnDeallocate()
        {
            // TODO
            base.OnDeallocate();
        }

        public override bool CanCollideWith(WorldEntity other)
        {
            if (base.CanCollideWith(other) == false) return false;
            if (other.Properties[PropertyEnum.NoMissileCollide] == true) return false;
            return true;
        }

        internal bool OnBounce(Vector3 position)
        {
            throw new NotImplementedException();
        }

        private void OnSeekTargetMissing() 
        {
            ResetLifespan(TimeSpan.FromMilliseconds(1));
        }

        private void OnReturnTargetMissing()
        {
            if  (_pendingKillEvent.IsValid)
            {
                Logger.Warn($"A Missile attempting to schedule a kill event with one already active [{ToString}]");
                return;
            }
            ScheduleEntityEvent(_pendingKillEvent, TimeSpan.Zero);
        }

        private class PendingKillCallback : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => ((Missile)t).Kill();
        }
    }
}
