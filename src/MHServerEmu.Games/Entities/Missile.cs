using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
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
        public GRandom Random { get; private set; }
        public Action ReturnTargetMissingEvent { get; private set; }
        public Action SeekTargetMissingEvent { get; private set; }
        private TimeSpan _lastSizeUpdateTime;

        private EventPointer<PendingKillCallback> _pendingKillEvent = new();

        public override AOINetworkPolicyValues CompatibleReplicationChannels
        {
            get => base.CompatibleReplicationChannels | (IsMovedIndependentlyOnClient ? AOINetworkPolicyValues.AOIChannelClientIndependent : 0);
        }

        public Missile(Game game) : base(game) 
        {
            SetFlag(EntityFlags.IsNeverAffectedByPowers, true);
            _contextPrototype = null;
            ReturnTargetMissingEvent = OnReturnTargetMissing;
            SeekTargetMissingEvent = OnSeekTargetMissing;
            Random = new();
            _entityCollideBounds = new();
            _lastSizeUpdateTime = TimeSpan.Zero;
        }

        public override string ToString()
        {
            return $"{base.ToString()}, missile.power={GameDatabase.GetFormattedPrototypeName(MissilePowerPrototypeRef)}";
        }

        public override bool ApplyInitialReplicationState(ref EntitySettings settings)
        {
            if (base.ApplyInitialReplicationState(ref settings) == false) return false;

            _contextPrototype = GetMissileCreationContextPrototype();
            if (_contextPrototype == null) return false;

            if (GameDatabase.DataDirectory.PrototypeIsAbstract(PrototypeDataRef))
                Bounds.InitializeSphere(_contextPrototype.Radius, BoundsCollisionType.Overlapping);

            Random.Seed(Properties[PropertyEnum.MissileSeed]);
            return true;
        }

        private MissileCreationContextPrototype GetMissileCreationContextPrototype()
        {
            MissilePowerPrototype powerProto = MissilePowerPrototype;
            if (powerProto == null || powerProto.MissileCreationContexts.IsNullOrEmpty())
                return null;

            int contextIndex = Properties[PropertyEnum.MissileContextIndex];
            if (contextIndex < 0 || contextIndex >= powerProto.MissileCreationContexts.Length)
                return null;

            return powerProto.MissileCreationContexts[contextIndex];
        }

        public override ChangePositionResult ChangeRegionPosition(Vector3? position, Orientation? orientation, ChangePositionFlags flags = ChangePositionFlags.None)
        {
            if (IsMovedIndependentlyOnClient)
                flags |= ChangePositionFlags.DoNotSendToClients;

            if (position.HasValue)
            {
                var region = Region;
                if (region != null && region.GetCellAtPosition(position.Value) == null)
                {
                    OnOutOfWorld();
                    return ChangePositionResult.InvalidPosition;
                }
            }

            if (_contextPrototype == null) return ChangePositionResult.InvalidPosition;

            float sizeIncreasePerSec = _contextPrototype.SizeIncreasePerSec;
            if (sizeIncreasePerSec > 0.0f)
            {
                var game = Game;
                if (game == null) return ChangePositionResult.NotChanged;

                var currentTime = game.CurrentTime;
                if (_lastSizeUpdateTime != TimeSpan.Zero)
                {
                    float seconds = (float)(currentTime - _lastSizeUpdateTime).TotalSeconds;
                    float sizeIncrease = sizeIncreasePerSec * seconds;
                    if (sizeIncrease != 0.0f)
                    {
                        Bounds.Radius += sizeIncrease;
                        EntityCollideBounds.Radius += sizeIncrease;
                    }
                }
                _lastSizeUpdateTime = currentTime;
            }

            ChangePositionResult result = base.ChangeRegionPosition(position, orientation, flags);
            EntityCollideBounds.Center = Bounds.Center;
            return result;
        }

        public override void OnPropertyChange(PropertyId id, PropertyValue newValue, PropertyValue oldValue, SetPropertyFlags flags)
        {
            base.OnPropertyChange(id, newValue, oldValue, flags);
            if (flags.HasFlag(SetPropertyFlags.Refresh)) return;

            if (id.Enum == PropertyEnum.MovementSpeedRate && !Segment.IsNearZero(oldValue.RawFloat))
            {
                float ratio = newValue.RawFloat / oldValue.RawFloat;
                if (!Segment.IsNearZero(ratio)) // OnMoveSpeedChanged
                    ScaleRemainingLifespan(1.0f / ratio);
            }
        }

        private void OnOutOfWorld()
        {
            List<Power> powerList = new();
            GetMissilePowersWithActivationEvent(powerList, null, MissilePowerActivationEventType.OnOutOfWorld);
            ActivateMissilePowers(powerList, null, RegionLocation.Position);
            Kill();
        }

        public override void OnKilled(WorldEntity killer, KillFlags killFlags, WorldEntity directKiller)
        {
            NotifyCreatorPowerEnd();
            // TODO PropertyEnum.CreatorPowerPrototype HandleTriggerPowerEvent PowerEventType.OnMissileKilled
            base.OnKilled(killer, killFlags, directKiller);
        }

        public override void OnLifespanExpired()
        {
            if (Game == null) return;
            if (IsInWorld)
            {
                List<Power> powerList = new();
                GetMissilePowersWithActivationEvent(powerList, null, MissilePowerActivationEventType.OnLifespanExpired);
                ActivateMissilePowers(powerList, null, RegionLocation.Position);
            }
            if (IsReturningMissile && IsSimulated)
                ReturnMissile();
            else
                Kill();
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

            // TODO set PropertyEnum.WeaponMissing to owner if PropertyEnum.PowerUsesReturningWeapon
        }

        private void StartMovement()
        {           
            if (_contextPrototype == null) return;

            var locomotionOptions = new LocomotionOptions();
            if (_contextPrototype.OneShot == false)
                locomotionOptions.Flags |= LocomotionFlags.LocomotionNoEntityCollide;
            if (_contextPrototype.Ghost)
                locomotionOptions.Flags |= LocomotionFlags.IgnoresWorldCollision;

            var locomotor = Locomotor;
            if (locomotor == null) return;

            switch (locomotor.Method)
            {
                case LocomotorMethod.Ground:
                case LocomotorMethod.Missile:
                    if (!Segment.IsNearZero(Properties[PropertyEnum.MissileBaseMoveSpeed]))
                        locomotor.MoveForward(locomotionOptions);
                    break;

                case LocomotorMethod.MissileSeeking:
                    var missilePowerProto = MissilePowerPrototype;
                    if (missilePowerProto == null) return;
                    if (_contextPrototype.IndependentClientMovement)
                    {
                        Logger.Warn($"Seeking Missiles should not have IndependentClientMovement set to true {ToString()} on {missilePowerProto}");
                        return;
                    }
                    break;

                default:
                    Logger.Warn($"Invalid Locomotor type {locomotor.Method} on {ToString()}");
                    return;
            }
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
            if (power == null)
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
            // TODO set PropertyEnum.WeaponMissing to owner
            NotifyCreatorPowerEnd();
            base.OnDeallocate();
        }

        public override bool CanCollideWith(WorldEntity collidedWith)
        {
            if (base.CanCollideWith(collidedWith) == false) return false;
            if (collidedWith.Properties[PropertyEnum.NoMissileCollide] == true) return false;
            if (IsSimulated == false || collidedWith.IsSimulated == false) return false;
            return true;
        }

        public override void OnOverlapBegin(WorldEntity whom, Vector3 whoPos, Vector3 whomPos)
        {
            if (whom != null) OnCollide(whom, whoPos);
        }

        public override void OnCollide(WorldEntity collidedWith, Vector3 position)
        {
            if (_contextPrototype == null) return;

            bool missileAlwaysCollides = false;
            if (collidedWith != null && collidedWith.Properties[PropertyEnum.MissileAlwaysCollides])
                missileAlwaysCollides = true;

            if (_contextPrototype.NoCollide) return;
            if (IsSimulated == false) return;

            if (collidedWith != null && collidedWith.IsHotspot)
            {
                if (collidedWith.IsReflectingHotspot && IsHostileTo(collidedWith.Alliance))
                    CheckAndApplyMissileReflection(collidedWith, position);
                return;
            }

            bool collideWithWhom = false;
            List<Power> collisionPowers = GetCollisionPowers(collidedWith);

            if (collisionPowers.Count > 0 || missileAlwaysCollides)
            {
                collideWithWhom = true;
                if (collidedWith != null)
                {
                    if (CheckAndApplyMissileReflection(collidedWith, position)) return;
                    OnValidTargetHit(collidedWith);
                }
            }

            bool kill = false;
            if (collidedWith != null)
            {
                if (missileAlwaysCollides)
                {
                    ActivateMissilePowers(collisionPowers, collidedWith, position);
                    kill |= OnCollideWithEntity(collidedWith);
                }
                else
                {
                    if (CheckMissileAbsorbedBy(collidedWith))
                    {
                        Properties.AdjustProperty(0.5f, PropertyEnum.DamagePctWeaken);
                        kill |= true;
                    }

                    if (collidedWith.Id == Properties[PropertyEnum.PowerUserOverrideID])
                    {
                        if (collideWithWhom)
                            ActivateMissilePowers(collisionPowers, collidedWith, position);
                        kill |= OnCollideWithOwner(collidedWith);
                    }
                    else if (collideWithWhom)
                    {
                        ActivateMissilePowers(collisionPowers, collidedWith, position);
                        kill |= OnCollideWithEntity(collidedWith);
                    }
                }
            }
            else
            {
                ActivateMissilePowers(collisionPowers, null, position);
                kill |= OnCollideWithWorldGeometry();
            }

            if (kill) Kill(collidedWith);
        }

        private bool OnCollideWithWorldGeometry()
        {
            bool kill = true;
            if (IsReturningMissile && Properties[PropertyEnum.MissileReturning] == false)
            {
                ReturnMissile();
                kill = false;
            }
            return kill;
        }

        private void ReturnMissile()
        {
            ulong ownerId = Properties[PropertyEnum.PowerUserOverrideID];
            var manager = Game.EntityManager;
            var ownerEntity = manager.GetEntity<WorldEntity>(ownerId);
            if (ownerEntity != null && ownerEntity.IsInWorld)
            {
                CancelScheduledLifespanExpireEvent();
                Properties[PropertyEnum.MissileReturning] = true;

                if (Physics.IsOverlappingEntity(ownerId))
                {
                    WorldEntity overlappingEntity = manager.GetEntity<WorldEntity>(ownerId);
                    if (overlappingEntity == null) return;
                    OnCollide(overlappingEntity, RegionLocation.Position);
                    return;
                }

                var locomotor = Locomotor;
                if (locomotor == null || _contextPrototype == null) return;

                var locomotionOptions = new LocomotionOptions { RepathDelay = TimeSpan.FromSeconds(0.5) };
                if (_contextPrototype.OneShot == false)
                    locomotionOptions.Flags |= LocomotionFlags.LocomotionNoEntityCollide;

                locomotor.FollowEntity(ownerId, 0f, locomotionOptions);
                locomotor.FollowEntityMissingEvent.AddActionBack(ReturnTargetMissingEvent);
            }
            else Kill();
        }

        private bool OnCollideWithOwner(WorldEntity collidedWith)
        {
            ulong ownerId = Properties[PropertyEnum.PowerUserOverrideID];
            if (ownerId != collidedWith.Id) return false;
            return IsReturningMissile && Properties[PropertyEnum.MissileReturning];
        }

        private bool CheckMissileAbsorbedBy(WorldEntity collidedWith)
        {
            if (Properties[PropertyEnum.MissileAbsorbImmunity]) return false;
            float absorbChancePct = collidedWith.Properties[PropertyEnum.MissileAbsorbChancePct];
            if (absorbChancePct > 0.0f && collidedWith.IsHostileTo(this))
            {
                if (Game == null) return false;
                if (Random.NextFloat() < absorbChancePct)
                {
                    // TODO checkAbsorb for collidedWith
                    return true; 
                }
            }
            return false;
        }

        private bool OnCollideWithEntity(WorldEntity collidedWith)
        {
            if (_contextPrototype == null) return false;

            bool isBlocking = collidedWith.Bounds.CollisionType == BoundsCollisionType.Blocking 
                || (collidedWith.Bounds.CollisionType == BoundsCollisionType.Overlapping && IsKilledOnOverlappingCollision);
            bool isOneShot = _contextPrototype.OneShot;
            bool explodeOnCollision = _contextPrototype.ReturningMissileExplodeOnCollide;
            bool missileAlwaysCollides = collidedWith.Properties[PropertyEnum.MissileAlwaysCollides];

            bool returnMissile = false;
            bool kill = false;

            if (isBlocking && (CheckMissilePierce() == false || missileAlwaysCollides))
            {
                if (IsReturningMissile)
                {
                    if (Properties[PropertyEnum.MissileReturning])
                    {
                        if (explodeOnCollision)
                            kill = true;
                    }
                    else
                    {
                        if (explodeOnCollision)
                            kill = true;
                        else if (isOneShot || missileAlwaysCollides)
                            returnMissile = true;
                    }
                }
                else if (isOneShot || missileAlwaysCollides)
                    kill = true;
            }

            if (returnMissile) ReturnMissile();
            return kill;
        }

        private bool CheckMissilePierce()
        {
            return Random.NextFloat() <= Properties[PropertyEnum.MissilePierceChance];
        }

        private List<Power> GetCollisionPowers(WorldEntity collidedWith)
        {
            List<Power> powerList = new();
            bool isOwner = collidedWith != null && collidedWith.Id == Properties[PropertyEnum.PowerUserOverrideID];

            if (IsReturningMissile && Properties[PropertyEnum.MissileReturning])
            {
                if (isOwner)
                    GetMissilePowersWithActivationEvent(powerList, collidedWith, MissilePowerActivationEventType.OnReturned);
                else
                    GetMissilePowersWithActivationEvent(powerList, collidedWith, MissilePowerActivationEventType.OnReturning);
            }
            else if (isOwner == false)
            {
                if (collidedWith == null)
                {
                    GetMissilePowersWithActivationEvent(powerList, collidedWith, MissilePowerActivationEventType.OnCollideWithWorld);
                    if (powerList.Count == 0)
                        GetMissilePowersWithActivationEvent(powerList, collidedWith, MissilePowerActivationEventType.OnCollide);
                }
                else
                    GetMissilePowersWithActivationEvent(powerList, collidedWith, MissilePowerActivationEventType.OnCollide);
            }

            return powerList;
        }

        private void OnValidTargetHit(WorldEntity collidedWith)
        {
            // TODO PropertyEnum.CreatorPowerPrototype HandleTriggerPowerEvent PowerEventType.OnMissileHit
        }

        private bool CheckAndApplyMissileReflection(WorldEntity collidedWith, Vector3 position)
        {
            if (CheckReflection(collidedWith, position, out bool outBounds, out bool reflectWithinHotspot))
            {
                if (Game == null) return false;

                if (IsReturningMissile)
                {
                    if (Properties[PropertyEnum.MissileReturning])
                        Kill();
                    else
                        ReturnMissile();
                }
                else
                {
                    NotifyCreatorPowerEnd();
                    Locomotor locomotor = Locomotor;
                    if (locomotor == null) return false;

                    if (locomotor.Method == LocomotorMethod.MissileSeeking)
                    {
                        locomotor.Stop();
                        locomotor.SetMethod(LocomotorMethod.Missile);
                        Properties.RemoveProperty(PropertyEnum.MissileSeekTargetId);
                    }

                    ulong ownerId = collidedWith.Properties[PropertyEnum.PowerUserOverrideID];
                    if (ownerId == InvalidId) ownerId = collidedWith.Id;

                    Vector3 direction = Forward;
                    bool inForwardDirection = collidedWith.Properties[PropertyEnum.ReflectInForwardDirection];
                    float angleVariance = collidedWith.Properties[PropertyEnum.SkillshotReflectAngVariance];

                    if (outBounds == false && reflectWithinHotspot)
                    {
                        if (inForwardDirection)
                            direction = AddRandomMissileAngleVariance(Random, -angleVariance, angleVariance, collidedWith.Forward);
                        else
                            direction = AddRandomMissileAngleVariance(Random, -angleVariance, angleVariance, direction);
                    }
                    else
                    {
                        if (inForwardDirection)
                            direction = AddRandomMissileAngleVariance(Random, -angleVariance, angleVariance, collidedWith.Forward);
                        else
                            direction = ReflectDirectionAlongMovementPlane(collidedWith, position);
                    }

                    Properties[PropertyEnum.Reflected] = collidedWith.Id;
                    Properties[PropertyEnum.PowerUserOverrideID] = ownerId;
                    Properties[PropertyEnum.AllianceOverride] = collidedWith.Alliance.DataRef;

                    ChangeRegionPosition(position, Orientation.FromDeltaVector(direction));

                    ResetLifespan(TotalLifespan);
                    StartMovement();
                }

                collidedWith.OnSkillshotReflected(this); // TODO check all virtual for this
                return true;
            }

            return false;
        }

        private bool CheckReflection(WorldEntity collidedWith, Vector3 position, out bool outBounds, out bool reflectWithinHotspot)
        {
            outBounds = false;
            reflectWithinHotspot = false;

            if (Properties[PropertyEnum.MissileReflectionImmunity]) return false;

            float skillshotReflectChancePct = collidedWith.Properties[PropertyEnum.SkillshotReflectChancePct];
            if (skillshotReflectChancePct > 0f)
            {
                bool reflectAngle = true;
                float reflectFromAngle = collidedWith.Properties[PropertyEnum.SkillshotReflectFromAngle];
                if (reflectFromAngle > 0f)
                    reflectAngle = CheckWithinAngle(collidedWith.RegionLocation.Position, collidedWith.Forward, position, reflectFromAngle);

                outBounds = collidedWith.Bounds.Contains(position) == false;
                reflectWithinHotspot = collidedWith.Properties[PropertyEnum.SkillshotReflectInHotspot];

                bool reflect = false;
                if (outBounds && collidedWith.Properties[PropertyEnum.SkillshotReflectOutHotspot])
                    reflect = true;
                else if (outBounds == false && reflectWithinHotspot)
                    reflect = true;

                if (reflect && reflectAngle && Random.NextFloat() < skillshotReflectChancePct)
                    return true;
            }

            return false;
        }

        private Vector3 AddRandomMissileAngleVariance(GRandom random, float minAngle, float maxAngle, in Vector3 direction)
        {
            if (Game == null) return new Vector3(0.0f, 0.0f, 1.0f);
            float randomAngle = random.NextFloat(minAngle, maxAngle);
            Transform3 transform = Transform3.BuildTransform(Vector3.Zero, new (MathHelper.ToRadians(randomAngle), 0.0f, 0.0f));
            return transform * direction;
        }

        private Vector3 ReflectDirectionAlongMovementPlane(WorldEntity collidedWith, Vector3 position)
        {
            Vector3 forward2d = Forward.To2D();
            Vector3 direction2d = forward2d;

            if (Vector3.IsNearZero(direction2d))
                return -forward2d;
            else
                direction2d = Vector3.Normalize(direction2d);

            Vector3 distance2dNorm;
            Vector3 distance2d = (position - collidedWith.RegionLocation.Position).To2D();

            if (Vector3.IsNearZero(distance2d))
                return -forward2d;
            else
                distance2dNorm = Vector3.Normalize(distance2d);

            Vector3 projectNormal2d = distance2dNorm * Vector3.Dot(distance2dNorm, direction2d);
            Vector3 reflectDirection2D = direction2d - projectNormal2d * 2;

            Vector3 planeNormal = Vector3.Normalize(GetUp);
            float reflectedDirectionZ = 0.0f;
            if (!Segment.IsNearZero(planeNormal.Z))
            {
                reflectedDirectionZ = (-planeNormal.X * (reflectDirection2D.X - direction2d.X) 
                    - planeNormal.Y * (reflectDirection2D.Y - direction2d.Y)) 
                    / planeNormal.Z + direction2d.Z;
            }
            Vector3 reflectDirection = new (reflectDirection2D.X, reflectDirection2D.Y, reflectedDirectionZ);

            if (Game == null) return new Vector3(0.0f, 0.0f, 1.0f);

            float range = MathHelper.Pi * 0.25f;
            float angle = Random.NextFloat(-range, range);
            Matrix3 rotMat = Matrix3.Rotation(angle, planeNormal);
            reflectDirection = rotMat * reflectDirection;

            return reflectDirection;
        }

        private void NotifyCreatorPowerEnd()
        {
            Game game = Game;
            if (game != null)
            {
                var creatorPowerProtoRef = Properties[PropertyEnum.CreatorPowerPrototype];
                if (creatorPowerProtoRef != PrototypeId.Invalid)
                {
                    var creatorPowerProto = GameDatabase.GetPrototype<PowerPrototype>(creatorPowerProtoRef);
                    if (creatorPowerProto != null
                        && creatorPowerProto.Properties != null
                        && creatorPowerProto.Properties[PropertyEnum.PowerActiveUntilProjExpire])
                    {
                        var owner = game.EntityManager.GetEntity<WorldEntity>(Properties[PropertyEnum.PowerUserOverrideID]);
                        if (owner != null)
                        {
                            var creatorPower = owner.GetPower(creatorPowerProtoRef);
                            if (creatorPower != null && creatorPower.IsActive)
                                creatorPower.EndPower(EndPowerFlags.None);
                        }
                    }
                }
            }
        }

        public bool OnBounce(Vector3 position)
        {
            var gravitatedContext = GravitatedContext;
            if (gravitatedContext == null) return false;

            List<Power> powerList = new();
            GetMissilePowersWithActivationEvent(powerList, null, MissilePowerActivationEventType.OnBounce);
            ActivateMissilePowers(powerList, null, position);

            int numBounces = Properties[PropertyEnum.NumMissileBounces];
            if (++numBounces >= gravitatedContext.NumBounces)
            {
                Kill();
                return false;
            }

            Properties[PropertyEnum.NumMissileBounces] = numBounces;
            return true;
        }

        private void ActivateMissilePowers(List<Power> powerList, WorldEntity target, Vector3 position)
        {
            if (powerList.Count == 0) return;
            foreach (var power in powerList)
            {
                if (power == null) continue;
                if (power.CanTrigger() == PowerUseResult.Success)
                {
                    ulong targetId = InvalidId;
                    var targetPos = position;

                    if (target != null)
                    {
                        targetId = target.Id;
                        targetPos = target.RegionLocation.Position;
                    }

                    var powerSettings = new PowerActivationSettings(targetId, targetPos, position)
                    {
                        VariableActivationTime = Properties[PropertyEnum.VariableActivationTimeMS],
                        FXRandomSeed = (uint)Properties[PropertyEnum.VariationSeed]
                    };

                    // EntityHelper.CrateOrb(EntityHelper.TestOrb.Blue, RegionLocation.Position, Region);
                    // EntityHelper.CrateOrb(EntityHelper.TestOrb.Red, position, Region);
                    ActivateMissilePower(power, ref powerSettings, target);
                }
            }
        }

        private PowerUseResult ActivateMissilePower(Power power, ref PowerActivationSettings powerSettings, WorldEntity target)
        {
            return ActivatePower(power, ref powerSettings);
        }

        private void GetMissilePowersWithActivationEvent(List<Power> powerList, WorldEntity target, MissilePowerActivationEventType eventType)
        {
            var missileContext = _contextPrototype;
            if (missileContext == null) return;
            var missilePowerList = missileContext.PowerList;
            if (missilePowerList == null) return;

            foreach (var missilePowerContext in missilePowerList)
            {
                if (missilePowerContext == null) return;
                if (missilePowerContext.MissilePowerActivationEvent == eventType)
                    if (missilePowerContext.GetPercentChanceToActivate(Properties) >= Random.NextFloat())
                    {
                        if (missilePowerContext.Power == PrototypeId.Invalid) return;
                        var missilePower = GetPower(missilePowerContext.Power);
                        if (missilePower == null)
                        {
                            Logger.Warn($"GetMissilePowersWithActivationEvent attempting to get power {missilePowerContext.Power} from the missile but the power instance was not found.\n  Missile: {this}");
                            return;
                        }

                        if (target == null || missilePower.IsValidTarget(target))
                            powerList.Add(missilePower);
                    }
            }
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
