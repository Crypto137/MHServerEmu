using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities.Locomotion
{
    public class Locomotor
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public Event FollowEntityGiveUpEvent { get; private set; }
        public Event FollowEntityMissingEvent { get; private set; }

        public const float ReachedPathPointEpsilon = 2.0f;
        public const float MovementSweepPadding = 0.5f;
        public const float GiveUpGoalDistance = 16.0f;

        public TimeSpan SyncPathInterval = TimeSpan.FromMilliseconds(400);
        public TimeSpan DefaultUpdateNavigationInfluenceFreq = TimeSpan.FromMilliseconds(500);
        public float OutOfSyncAdjustDistanceSq = MathHelper.Square(16.0f);

        private WorldEntity _owner;
        public LocomotionState LocomotionState { get; private set; }
        public bool MovementImpeded { get; set; }
        public bool IsMoving { get; private set; }
        public bool IsEnabled { get; private set; }
        public float DefaultRunSpeed { get => _runSpeed; }        
        public float Height { get; private set; }
        public LocomotorMethod Method { get => LocomotionState.Method; }

        private GeneratedPath _generatedPath;

        public bool IsLocomoting { get => LocomotionState.LocomotionFlags.HasFlag(LocomotionFlags.IsLocomoting); }
        public bool IsWalking { get => LocomotionState.LocomotionFlags.HasFlag(LocomotionFlags.IsWalking); }
        public bool IsLooking { get => LocomotionState.LocomotionFlags.HasFlag(LocomotionFlags.IsLooking); }
        public bool HasLocomotionNoEntityCollide { get => LocomotionState.LocomotionFlags.HasFlag(LocomotionFlags.LocomotionNoEntityCollide); }
        public bool IsMovementPower { get => LocomotionState.LocomotionFlags.HasFlag(LocomotionFlags.IsMovementPower); }
        public bool IsDrivingMovementMode { get => LocomotionState.LocomotionFlags.HasFlag(LocomotionFlags.IsDrivingMovementMode); }
        public bool IsHighFlying { get => LocomotionState.Method == LocomotorMethod.HighFlying; }
        public bool IsSyncMoving { get => LocomotionState.LocomotionFlags.HasFlag(LocomotionFlags.IsSyncMoving); }
        public bool IgnoresWorldCollision { get => LocomotionState.LocomotionFlags.HasFlag(LocomotionFlags.IgnoresWorldCollision); }
        public int CurrentMoveHeight { get => LocomotionState.Height; }
        public bool IsMissile { get => LocomotionState.Method == LocomotorMethod.Missile || LocomotionState.Method == LocomotorMethod.MissileSeeking; }
        public bool IsSeekingMissile { get => LocomotionState.Method == LocomotorMethod.MissileSeeking; }
        public PathFlags PathFlags { get => GetPathFlags(LocomotionState.Method); }
        public bool IsFollowingEntity { get => LocomotionState.FollowEntityId != 0; }
        public ulong FollowEntityId { get => LocomotionState.FollowEntityId; }
        public bool SupportsWalking { get; private set; }
        public LocomotionState LastSyncState { get; private set; }
        public bool HasSyncState { get => _syncStateTime != TimeSpan.Zero; }
        public bool HasPath { get => _generatedPath.Path.IsValid; }
        public bool IsFollowingSyncPath { get => _syncPathGoalNodeIndex > 0 && LocomotionState.PathGoalNodeIndex <= _syncPathGoalNodeIndex; }
        public bool IsSyncMovingNoCollision { get => IsSyncMoving || IsFollowingSyncPath; }
        public bool IsStuck { get => HasPath && !IsPathComplete() && !IsEnabled; }
        public NaviPathResult LastGeneratedPathResult { get => _generatedPath.PathResult; }
        public static LocomotionOptions DefaultFollowEntityLocomotionOptions => new(TimeSpan.FromSeconds(1.0), 0, 0.0f, 0.0f, 0, 0);
        private bool _hasOrientationSyncState;
        private TimeSpan _syncStateTime;
        private Orientation _syncOrientation;
        private Vector3 _syncPosition;
        private TimeSpan _syncNextRepathTime;
        private int _syncAttempts;
        private float _syncSpeed;
        private int _syncPathGoalNodeIndex;
        private int _syncAttemptsFailed;


        private int _giveUpRepathCount;
        private float _giveUpDistanceThreshold;
        private TimeSpan _giveUpTime;
        private TimeSpan _giveUpNextTime;
        private Vector3 _giveUpPosition;
        private float _giveUpDistance;

        private float _runSpeed;
        private float _walkSpeed;
        private float _rotationSpeed;
        private float _moveSpeedOverride;

        private int _repathCount;
        private TimeSpan _repathDelay;
        private TimeSpan _repathTime;

        private bool _initRotation;
        private LocomotorMethod _defaultMethod;
        private LocomotionState _lastLocomotionState;
        private PathGenerationFlags _pathGenerationFlags;
        private float _incompleteDistance;
        private TimeSpan _updateNavigationInfluenceTime;
        private float _rotationDirection;

        public Locomotor()
        {
            FollowEntityGiveUpEvent = new();
            FollowEntityMissingEvent = new();
            LocomotionState = new();
            _lastLocomotionState = new();
            _defaultMethod = LocomotorMethod.None;
            _generatedPath = new();
            _repathTime = TimeSpan.Zero;
            _repathDelay = TimeSpan.Zero;
            _giveUpTime = TimeSpan.Zero;
            _giveUpNextTime = new();
            LastSyncState = new();
            _syncStateTime = TimeSpan.Zero;
            _syncNextRepathTime = new ();
            _syncOrientation = new();
            _updateNavigationInfluenceTime = new();
            _giveUpPosition = new();
        }

        public void Initialize(LocomotorPrototype locomotorProto, WorldEntity entity, float heightOverride = 0.0f)
        {
            _owner = entity;
            if (entity != null && entity.Properties.HasProperty(PropertyEnum.MissileBaseMoveSpeed))
                _runSpeed = entity.Properties[PropertyEnum.MissileBaseMoveSpeed];
            else
                _runSpeed = locomotorProto.Speed;

            SupportsWalking = locomotorProto.WalkEnabled;
            _walkSpeed = locomotorProto.WalkSpeed;
            _rotationSpeed = locomotorProto.RotationSpeed;
            Height = heightOverride != 0.0f ? heightOverride : locomotorProto.Height;

            if (_owner != null)
            {
                var worldEntityProto = _owner.WorldEntityPrototype;
                if (worldEntityProto != null)
                    _defaultMethod = worldEntityProto.NaviMethod;
            }

            LocomotionState.Method = _defaultMethod;
            LocomotionState.BaseMoveSpeed = DefaultRunSpeed;
        }

        public static PathFlags GetPathFlags(LocomotorMethod naviMethod)
        {
            return naviMethod switch
            {
                LocomotorMethod.Ground or LocomotorMethod.Airborne => PathFlags.Walk,
                LocomotorMethod.TallGround => PathFlags.TallWalk,
                LocomotorMethod.Missile or LocomotorMethod.MissileSeeking => PathFlags.Power,
                LocomotorMethod.HighFlying => PathFlags.Fly,
                _ => PathFlags.None,
            };
        }

        public SweepResult SweepFromTo(Vector3 fromPosition, Vector3 toPosition, ref Vector3 resultPosition, ref Vector3? resultNormal, float padding = MovementSweepPadding)
        {
            if (_owner == null) return SweepResult.Failed;
            NaviMesh naviMesh = _owner.NaviMesh;
            if (naviMesh == null) return SweepResult.Failed;

            PathFlags pathFlags = PathFlags;
            HeightSweepType heightSweep = HeightSweepType.None;
            int maxHeight = short.MaxValue;
            int minHeight = short.MinValue;

            if (pathFlags.HasFlag(PathFlags.Fly))
            {
                float flyHeight = GetCurrentFlyingHeight();
                if (flyHeight != 0.0f)
                {
                    maxHeight = (int)(RegionLocation.ProjectToFloor(_owner.Region, _owner.Cell, fromPosition).Z + flyHeight);
                    heightSweep = HeightSweepType.Constraint;
                }
            }
            else
            {
                int moveHeight = CurrentMoveHeight;
                if (moveHeight != 0)
                {
                    if (moveHeight > 0)
                        maxHeight = (int)(RegionLocation.ProjectToFloor(_owner.Region, _owner.Cell, fromPosition).Z + moveHeight);
                    else
                        minHeight = (int)(RegionLocation.ProjectToFloor(_owner.Region, _owner.Cell, fromPosition).Z + moveHeight);
                    pathFlags |= PathFlags.Fly;
                    heightSweep = HeightSweepType.Constraint;
                }
            }

            float radius = _owner.Bounds.Radius;
            Vector3? sweepPosition = resultPosition;
            SweepResult sweepResult = naviMesh.Sweep(fromPosition, toPosition, radius, pathFlags, ref sweepPosition, ref resultNormal,
                                                     padding, heightSweep, maxHeight, minHeight, _owner);
            resultPosition = sweepPosition.Value;
            if (sweepResult != SweepResult.Failed)
            {
                if (sweepResult == SweepResult.HeightMap && Vector3.IsNearZero2D(fromPosition - resultPosition))
                { 
                    pathFlags &= ~PathFlags.Fly;
                    pathFlags |= PathFlags.Walk;
                    if (naviMesh.Contains(fromPosition, radius, new DefaultContainsPathFlagsCheck(pathFlags)))
                    {
                        sweepPosition = resultPosition;
                        sweepResult = naviMesh.Sweep(fromPosition, toPosition, radius, pathFlags, ref sweepPosition, ref resultNormal,
                                                     padding, HeightSweepType.None, 0, 0, _owner);
                        resultPosition = sweepPosition.Value;
                    }
                }

                if (IsMissile)
                {
                    Region region = _owner.Region;
                    if (region == null) return SweepResult.Failed;

                    Cell cell = region.GetCellAtPosition(resultPosition);
                    if (cell == null || RegionLocation.ProjectToFloor(cell, resultPosition).Z > toPosition.Z)
                        sweepResult = SweepResult.Clipped;
                }
                else
                {
                    resultPosition = RegionLocation.ProjectToFloor(_owner.Region, _owner.Cell, resultPosition);
                    resultPosition = _owner.FloorToCenter(resultPosition);
                }
            }
            return sweepResult;
        }

        public float GetCurrentFlyingHeight()
        {
            if (_owner == null) return 0.0f;
            if (IsHighFlying)
            {
                var globalsProto = GameDatabase.GlobalsPrototype;
                if (globalsProto == null) return 0.0f;
                return globalsProto.HighFlyingHeight;
            }
            else
            {
                var method = Method;
                if (method == LocomotorMethod.Airborne || method == LocomotorMethod.Missile || method == LocomotorMethod.MissileSeeking)
                    return Height;
                else
                    return 0.0f;
            }
        }

        public SweepResult SweepTo(Vector3 toPosition, ref Vector3 resultPosition, ref Vector3? resultNormal, float padding = MovementSweepPadding)
        {
            if (_owner == null)
                return SweepResult.Failed;

            if (IgnoresWorldCollision)
            {
                Region region = _owner.Region;
                if (region == null)
                    return SweepResult.Failed;

                if (region.GetCellAtPosition(toPosition) == null)
                {
                    resultPosition = _owner.RegionLocation.Position;
                    return SweepResult.Clipped;
                }

                resultPosition = toPosition;
                return SweepResult.Success;
            }

            return SweepFromTo(_owner.RegionLocation.Position, toPosition, ref resultPosition, ref resultNormal, padding);
        }

        public void SetGiveUpLimits(float distanceThreshold, TimeSpan time)
        {
            _giveUpDistanceThreshold = distanceThreshold;
            _giveUpTime = time;
        }

        public void Locomote()
        {
            if (_owner == null) return;
            Game game = _owner.Game;
            if (game == null || _owner.IsInWorld == false) return;
            float timeSeconds;

            if (_owner.Game.AdminCommandManager.TestAdminFlag(AdminFlags.LocomotionSync))
            {
                if (IsEnabled && _hasOrientationSyncState && IsDrivingMovementMode && _owner.IsExecutingPower)
                {                    
                    Vector3 syncDir = _syncOrientation.GetMatrix3() * Vector3.Forward;
                    timeSeconds = (float)game.FixedTimeBetweenUpdates.TotalSeconds;
                    Vector3 delta = RotateMaxTurnThisFrame3D(_owner.Forward, syncDir, 300.0f, timeSeconds);
                    var orientation = Orientation.FromDeltaVector(delta);
                    ChangePositionFlags changeFlags = ChangePositionFlags.PhysicsResolve | ChangePositionFlags.DoNotSendToServer | ChangePositionFlags.DoNotSendToClients;
                    _owner.ChangeRegionPosition(null, orientation, changeFlags);

                    if (_syncOrientation == _owner.Orientation)
                        ClearOrientationSyncState();
                }
                else if (HasSyncState && IsEnabled == false)
                {
                    if (_owner.ActivePowerPreventsMovement(PowerMovementPreventionFlags.Sync))
                    {
                        if (_hasOrientationSyncState)
                        {
                            Vector3 lookDir = _syncOrientation.GetMatrix3() * Vector3.Forward;
                            LookAt(_owner.RegionLocation.Position + lookDir);
                        }
                    }
                    else
                    {
                        TimeSpan timeNow = GameTimeNow();
                        if (_syncNextRepathTime < timeNow)
                        {
                            _syncNextRepathTime = timeNow + SyncPathInterval;
                            float desyncDistanceSq = Vector3.DistanceSquared2D(_syncPosition, _owner.RegionLocation.Position);
                            if (desyncDistanceSq > OutOfSyncAdjustDistanceSq)
                            {
                                bool syncTeleport = false;
                                if (++_syncAttempts < 5)
                                {
                                    var ownerProto = _owner.WorldEntityPrototype;
                                    if (ownerProto == null) return;
                                    var locomotorProto = ownerProto.Locomotor;
                                    if (locomotorProto == null) return;

                                    LocomotionOptions locomotionOptions = new ();
                                    locomotionOptions.Flags |= LocomotionFlags.IsSyncMoving;
                                    if (_owner.ActivePowerOrientsToTarget() || locomotorProto.DisableOrientationForSyncMove || _owner.ActivePowerDisablesOrientation())
                                        locomotionOptions.Flags |= LocomotionFlags.DisableOrientation;

                                    if (PathTo(_syncPosition, locomotionOptions))
                                        _syncSpeed = LocomotionState.BaseMoveSpeed * 1.5f;
                                    else
                                    {
                                        if (++_syncAttemptsFailed == 3)
                                            syncTeleport = true;
                                        else
                                        {
                                            Vector3 resultPosition = Vector3.Zero;
                                            Vector3? resultNormal = null;
                                            if (SweepTo(_syncPosition, ref resultPosition, ref resultNormal) == SweepResult.Success)
                                                if (MoveTo(_syncPosition, locomotionOptions))
                                                    _syncSpeed = LocomotionState.BaseMoveSpeed * 1.5f;
                                        }
                                    }
                                }
                                else
                                    syncTeleport = true;

                                if (syncTeleport)
                                {
                                    ChangePositionFlags changeFlags = ChangePositionFlags.Force | ChangePositionFlags.PhysicsResolve | ChangePositionFlags.DoNotSendToServer | ChangePositionFlags.DoNotSendToClients;
                                    _owner.ChangeRegionPosition(_syncPosition, _syncOrientation, changeFlags);
                                    UpdateNavigationInfluence(true);
                                    ClearSyncState();
                                }
                            }
                            else
                            {
                                if (_hasOrientationSyncState)
                                {
                                    Vector3 lookDir = _syncOrientation.GetMatrix3() * Vector3.Forward;
                                    LookAt(_owner.RegionLocation.Position + lookDir);
                                }
                                ClearSyncState();
                            }
                        }
                    }
                }
            }

            if (IsEnabled == false) return;

            timeSeconds = (float)game.FixedTimeBetweenUpdates.TotalSeconds;
            if (IsLooking && _owner.CanRotate())
            {
                bool rotated = DoRotationInPlace(timeSeconds, GetLookingGoalDir());
                PushLocomotionStateChanges();
                if (rotated)
                {
                    ClearSyncState();
                    Stop();
                }
                return;
            }

            IsMoving = false;
            if (_owner.CanMove() && GetNextLocomotePosition(timeSeconds, out Vector3 movePosition))
            {
                Vector3 dir = movePosition - _owner.RegionLocation.Position;
                if (!Vector3.IsNearZero(dir))
                {
                    Vector3 dirTo2d = dir.To2D();
                    if (!_owner.ActivePowerDisablesOrientation() && !Vector3.IsNearZero(dirTo2d))
                        if (DoRotationInPlace(timeSeconds, dir) == false)
                        {
                            _giveUpNextTime = GameTimeNow() + _giveUpTime;
                            return;
                        }

                    _owner.Physics.ApplyInternalForce(dir);

                    if (!IsMissile && !_owner.ActivePowerDisablesOrientation())
                        if (Vector3.LengthSqr(dirTo2d) > Segment.Epsilon)
                        {
                            var orientation = Orientation.FromDeltaVector(Vector3.Normalize(dirTo2d));
                            SetOrientation(orientation);
                        }
                    IsMoving = true;
                }
            }

            if (!IsFollowingEntity && Method != LocomotorMethod.Missile && (HasPath && IsPathComplete()) && !IsDrivingMovementMode && !IsMoving)
                SetEnabled(false, false);
            else
                UpdateNavigationInfluence(false);

            if (_owner.IsMovementAuthoritative && _giveUpDistanceThreshold > 0.0f && !IsMissile && !(IsFollowingSyncPath && IsDrivingMovementMode))
            {
                _giveUpDistance += Vector3.Distance2D(_owner.RegionLocation.Position, _giveUpPosition);
                _giveUpPosition = _owner.RegionLocation.Position;

                TimeSpan timeNow = GameTimeNow();
                if (_giveUpNextTime != TimeSpan.Zero && _giveUpNextTime < timeNow)
                {
                    bool giveUp = false;
                    if (_giveUpDistance < _giveUpDistanceThreshold)
                    {
                        if (_giveUpRepathCount < 0) _giveUpRepathCount = _repathCount;

                        if (_repathDelay == TimeSpan.Zero || (_giveUpRepathCount < _repathCount))
                            giveUp = true;
                        else
                            if (!IsFollowingEntity && GetPathGoal(out Vector3 goalPosition))
                            {
                                float goalDistance = Vector3.Distance2D(goalPosition, _owner.RegionLocation.Position) - _owner.Bounds.GetRadius();
                                giveUp |= (goalDistance < GiveUpGoalDistance);
                            }
                    }

                    if (giveUp)
                    {
                        if (IsFollowingEntity) FollowEntityGiveUpEvent.Invoke();
                        SetEnabled(false, false);
                    }
                    else
                    {
                        _giveUpNextTime = timeNow + _giveUpTime;
                        _giveUpDistance = 0.0f;
                    }
                }
            }
            PushLocomotionStateChanges();
        }

        public bool GetPathGoal(out Vector3 goalPosition)
        {
            if (HasPath)
            {
                goalPosition = _generatedPath.Path.GetFinalPosition();
                return true;
            }
            goalPosition = default;
            return false;
        }

        public bool GetPathStart(out Vector3 startPosition)
        {
            if (HasPath)
            {
                startPosition = _generatedPath.Path.GetStartPosition();
                return true;
            }
            startPosition = default;
            return false;
        }

        public bool IsPathComplete()
        {
            if (_owner == null) return false;
            if (_generatedPath.Path.IsComplete == false)
            {
                if (IsFollowingEntity)
                {
                    WorldEntity followEntity = GetFollowEntity();
                    if (followEntity != null)
                    {
                        float distanceSq = Vector3.DistanceSquared2D(_owner.RegionLocation.Position, followEntity.RegionLocation.Position);
                        return distanceSq <= MathHelper.Square(LocomotionState.FollowEntityRangeEnd + ReachedPathPointEpsilon);
                    }
                    else
                        return true;
                }
                return false;
            }
            return true;
        }

        public WorldEntity GetFollowEntity()
        {
            if (_owner == null) return null;
            var followEntity = _owner.Game.EntityManager.GetEntity<WorldEntity>(LocomotionState.FollowEntityId);
            if (followEntity != null && followEntity.IsInWorld) return followEntity;
            return null;
        }

        private void SetEnabled(bool enabled, bool clearPath = true)
        {
            if (_owner == null) return;

            if (enabled && (!_owner.IsInWorld || _owner.TestStatus(EntityStatus.ExitingWorld)))
            {
                ResetState();
                IsEnabled = false;
                return;
            }

            if (enabled != IsEnabled)
            {
                if (enabled)
                {
                    if (_owner.Region != null)
                        IsEnabled = true;
                    else
                    {
                        Logger.Warn($"Trying to enable locomotor, but entity it belongs to is not currently in the world, disabling locomotor. Entity={_owner}, RegionLoc={_owner.RegionLocation}");
                        ResetState();
                    }
                }
                else
                {
                    IsEnabled = false;
                    UpdateNavigationInfluence(true);
                }
            }

            if (enabled && (_giveUpTime != TimeSpan.Zero))
            {
                _giveUpNextTime = GameTimeNow() + _giveUpTime;
                _giveUpPosition = _owner.RegionLocation.Position;
            }

            if (IsEnabled == false)
            {
                ResetState(clearPath);
                PushLocomotionStateChanges();
            }
        }

        private void SetOrientation(Orientation orientation)
        {
            if (_owner != null)
            {
                if (LocomotionState.LocomotionFlags.HasFlag(LocomotionFlags.DisableOrientation) == false)
                {
                    AdminCommandManager adminCommandManager = _owner.Game.AdminCommandManager;
                    ChangePositionFlags changeFlags;
                    if (adminCommandManager != null && adminCommandManager.TestAdminFlag(AdminFlags.LocomotionSync))
                        changeFlags = ChangePositionFlags.PhysicsResolve | ChangePositionFlags.DoNotSendToClients;
                    else
                        changeFlags = ChangePositionFlags.None;
                    _owner.ChangeRegionPosition(null, orientation, changeFlags);
                }
            }
            else
                Logger.Debug($"Locomotor owner was invalid when setting orientation. Loco: {ToString()}");
        }

        private bool DoRotationInPlace(float timeSeconds, Vector3 goalDir)
        {
            if (_owner == null) return false;
            if (LocomotionState.LocomotionFlags.HasFlag(LocomotionFlags.DisableOrientation)) return true;
            float currentRotationSpeed = GetCurrentRotationSpeed();
            if (IsMissile && IsSeekingMissile && currentRotationSpeed > 0.0f) return true;

            bool setRotation = true;
            if (!_initRotation)
            {
                if (currentRotationSpeed > 0.0f)
                {
                    Vector3 goalDir2d = goalDir.To2D();
                    if (Vector3.LengthSqr(goalDir2d) > 0.0f)
                    {
                        goalDir2d = Vector3.Normalize(goalDir2d);
                        Vector3 dir2d = Vector3.Normalize(_owner.Forward.To2D());
                        if (!Vector3.IsNearZero(dir2d - goalDir2d))
                        {
                            float deg = MathHelper.ToDegrees(MathF.Acos(Vector3.Dot2D(dir2d, goalDir2d)));
                            float rotDeg = currentRotationSpeed * timeSeconds;
                            if (rotDeg < deg)
                            {
                                if (Segment.Cross2D(dir2d, goalDir2d) < 0.0f) rotDeg = -rotDeg;
                                _rotationDirection = rotDeg;
                                Matrix3 dirRot = Matrix3.RotationZ(MathHelper.ToRadians(rotDeg));
                                goalDir2d = dirRot * dir2d;
                                setRotation = false;
                            }
                            SetOrientation(Orientation.FromDeltaVector(goalDir2d));
                        }
                    }
                }
                else
                {
                    Orientation orientation;
                    if (IsMissile)
                        orientation = Orientation.FromDeltaVector(goalDir);
                    else
                        orientation = Orientation.FromDeltaVector2D(goalDir);
                    SetOrientation(orientation);
                    setRotation = true;
                }

                if (setRotation && _rotationDirection != 0.0f) _rotationDirection = 0.0f;
            }

            if (setRotation) _initRotation = true;
            return setRotation;
        }

        public float GetCurrentRotationSpeed()
        {
            if (_owner == null) return 0.0f;
            float result = _rotationSpeed;
            float speedOverride = _owner.Properties[PropertyEnum.RotationSpeedOverride];
            if (speedOverride > 0.0f) result = speedOverride;
            return result;
        }

        public TimeSpan GetCurrentETA()
        {
            if (_owner == null) return TimeSpan.Zero;
            float currentDistance = _generatedPath.Path.ApproxCurrentDistance(_owner.RegionLocation.Position);
            float currentSpeed = GetCurrentSpeed();
            return TimeSpan.FromSeconds(currentDistance / currentSpeed);
        }

        private Vector3 GetLookingGoalDir()
        {
            if (_owner == null || !IsLooking) return Vector3.XAxis;
            if (GetPathStart(out Vector3 startPosition) && GetPathGoal(out Vector3 goalPosition))
                return goalPosition - startPosition;
            else
                return _owner.Forward;
        }

        public void Stop()
        {
            if (_owner != null && _owner.IsInWorld)
                SetEnabled(false);
        }

        private bool GetNextLocomotePosition(float timeSeconds, out Vector3 resultMovePosition)
        {
            resultMovePosition = default;
            if (_owner == null || _owner.Region == null || !IsEnabled) return false;
            if (RefreshCurrentPath() == false) return false;
            Vector3 currentPosition = _owner.RegionLocation.Position;

            switch (Method)
            {
                case LocomotorMethod.TallGround:
                case LocomotorMethod.Airborne:
                case LocomotorMethod.Ground:
                case LocomotorMethod.HighFlying:
                    {
                        if (IsFollowingEntity)
                        {
                            if (_generatedPath.Path.IsValid == false) return false;
                            if (LocomotionState.FollowEntityRangeStart > 0.0f)
                            {
                                Vector3 finalPosition = _generatedPath.Path.GetFinalPosition();
                                float distanceSq = Vector3.DistanceSquared(finalPosition, currentPosition);
                                if (distanceSq <= (LocomotionState.FollowEntityRangeEnd * LocomotionState.FollowEntityRangeEnd))
                                    return false;
                            }
                        }

                        float moveDistance = GetCurrentSpeed() * timeSeconds;
                        if (LocomotionState.LocomotionFlags.HasFlag(LocomotionFlags.MoveForward))
                            resultMovePosition = currentPosition + _owner.Forward * moveDistance;
                        else
                        {
                            if (_generatedPath.Path.IsValid == false || _generatedPath.Path.IsComplete) return false;
                            _generatedPath.Path.GetNextMovePosition(currentPosition, moveDistance, out resultMovePosition, out _);
                            LocomotionState.PathGoalNodeIndex = _generatedPath.Path.GetCurrentGoalNodeIndex();
                        }

                        if (IgnoresWorldCollision == false)
                        {
                            resultMovePosition = _owner.FloorToCenter(RegionLocation.ProjectToFloor(_owner.Region, _owner.Cell, resultMovePosition));
                            if (!Vector3.IsFinite(resultMovePosition)) return false;
                        }
                        return true;
                    }

                case LocomotorMethod.Missile:
                case LocomotorMethod.MissileSeeking:
                    {
                        if (_owner.Game == null) return false;
                        if (_owner is not Missile ownerAsMissile) return false;
                        var missileContext = ownerAsMissile.MissileCreationContextPrototype;
                        if (missileContext == null) return false;
                        WorldEntity followEntity = _owner.Game.EntityManager.GetEntity<WorldEntity>(LocomotionState.FollowEntityId);

                        var gravitatedContext = missileContext.GravitatedContext;
                        if (gravitatedContext != null)
                        {
                            Vector3 currentDirection = _owner.Forward;
                            Vector3 currentVelocity = currentDirection * GetCurrentSpeed();
                            Vector3 nextVelocity = currentVelocity + (Vector3.Up * gravitatedContext.Gravity);
                            Vector3 nextDirection = Vector3.SafeNormalize(nextVelocity);
                            Vector3 nextPosition = currentPosition + nextVelocity * timeSeconds;
                            Vector3 floorPosition = RegionLocation.ProjectToFloor(_owner.Region, _owner.Cell, nextPosition);

                            Orientation newOrientation;
                            if (nextPosition.Z <= floorPosition.Z)
                            {
                                if (!ownerAsMissile.OnBounce(floorPosition)) return false;
                                float bounceSpeed = Vector3.Length(nextVelocity) * gravitatedContext.OnBounceCoefficientOfRestitution;
                                int randomDegree = gravitatedContext.OnBounceRandomDegreeFromForward;
                                if (randomDegree != 0)
                                {
                                    var random = ownerAsMissile.Random;
                                    nextDirection = Vector3.AxisAngleRotate(nextDirection, Vector3.ZAxis, MathHelper.ToRadians(random.Next(-randomDegree, randomDegree)));
                                }
                                Vector3 axisVector = Vector3.SafeNormalize(Vector3.Cross(Vector3.Up, nextDirection));
                                Vector3 normalDirection = Vector3.AxisAngleRotate(Vector3.Up, axisVector, MathHelper.ToRadians(90.0f));
                                float angle = MathF.Acos(Vector3.Dot(normalDirection, nextDirection));
                                newOrientation = Orientation.FromDeltaVector(Vector3.AxisAngleRotate(nextDirection, -axisVector, 2 * angle));
                                _owner.Properties[PropertyEnum.MovementSpeedOverride] = bounceSpeed;
                                resultMovePosition = floorPosition + new Vector3(0.0f, 0.0f, _owner.Bounds.HalfHeight);
                            }
                            else
                            {
                                newOrientation = Orientation.FromDeltaVector(nextDirection);
                                float newSpeed = Vector3.Length(currentVelocity);
                                _owner.Properties[PropertyEnum.MovementSpeedOverride] = newSpeed;
                                resultMovePosition = nextPosition;
                            }
                            _owner.ChangeRegionPosition(null, newOrientation);
                        }
                        else
                        {
                            if (GetPathGoal(out Vector3 goalPosition) && (followEntity == null || !followEntity.IsDead))
                            {
                                if (GetCurrentRotationSpeed() > 0f)
                                {
                                    Vector3 currentDirection = _owner.Forward;
                                    Vector3 nextDirection = goalPosition - currentPosition;
                                    float currentRotationSpeed = GetCurrentRotationSpeed();

                                    if (missileContext.InterpolateRotationSpeed)
                                    {
                                        float interpolateRotMultByDist = missileContext.InterpolateRotMultByDist;
                                        if (Segment.EpsilonTest(interpolateRotMultByDist, 1.0f) == false)
                                        {
                                            float originalRotationSpeed = GetOriginalRotationSpeed();
                                            float spawnDistanceToTargetSqr = _owner.Properties[PropertyEnum.SpawnDistanceToTargetSqr];
                                            float nextDistanceToTargetSqr = MathF.Min(Vector3.LengthSquared(nextDirection), spawnDistanceToTargetSqr);
                                            float minSpeed = MathF.Min(currentRotationSpeed, originalRotationSpeed);
                                            currentRotationSpeed = Segment.Lerp(minSpeed, originalRotationSpeed * interpolateRotMultByDist, 
                                                (spawnDistanceToTargetSqr - nextDistanceToTargetSqr) / spawnDistanceToTargetSqr);
                                        }
                                        else
                                        {
                                            float interpolateOvershotAccel = missileContext.InterpolateOvershotAccel;
                                            if (Segment.IsNearZero(interpolateOvershotAccel) == false)
                                            {
                                                WorldEntity powerUser = _owner.Game.EntityManager.GetEntity<WorldEntity>(_owner.Properties[PropertyEnum.PowerUserOverrideID]);
                                                if (powerUser != null && powerUser.IsInWorld)
                                                    if (Vector3.Dot(powerUser.RegionLocation.Position - goalPosition, currentPosition - goalPosition) < 0)
                                                        currentRotationSpeed += currentRotationSpeed * interpolateOvershotAccel * timeSeconds;
                                            }
                                        }
                                        _rotationSpeed = currentRotationSpeed;
                                    }

                                    if (missileContext.IgnoresPitch)
                                    {
                                        currentDirection.Z = 0.0f;
                                        nextDirection.Z = 0.0f;
                                    }

                                    currentDirection = Vector3.Normalize(currentDirection);
                                    nextDirection = Vector3.Normalize(nextDirection);
                                    SetRotateMaxTurnThisFrame3D(currentDirection, nextDirection, currentRotationSpeed, timeSeconds);
                                }
                                else
                                    _owner.OrientToward(goalPosition);
                            }

                            Vector3 moveDelta = _owner.Forward * (GetCurrentSpeed() * timeSeconds);
                            resultMovePosition = currentPosition + moveDelta;
                            if (Vector3.IsFinite(resultMovePosition) == false) return false;
                        }

                        return true;
                    }

                default:
                    resultMovePosition = currentPosition;
                    return false;
            }
        }

        public float GetOriginalRotationSpeed()
        {
            var locomotorProto = _owner?.WorldEntityPrototype?.Locomotor;
            return locomotorProto != null ? locomotorProto.RotationSpeed : 0.0f;
        }

        private bool RefreshCurrentPath()
        {
            if (_owner == null) return false;

            bool repath = false;
            Vector3 finalPosition = Vector3.Zero;
            bool updateEnd = false;
            Vector3 updateEndPosition = Vector3.Zero;

            if (IsFollowingEntity)
            {
                WorldEntity followEntity = _owner.Game.EntityManager.GetEntity<WorldEntity>(LocomotionState.FollowEntityId);
                if (_owner.IsMovementAuthoritative)
                {
                    if (followEntity != null && followEntity.IsInWorld)
                    {
                        if (!_generatedPath.Path.IsValid || (_repathDelay != TimeSpan.Zero && _repathTime < GameTimeNow()))
                        {
                            repath = true;
                            NaviPoint naviInfluencePoint = followEntity.NavigationInfluencePoint;
                            if (naviInfluencePoint != null)
                            {
                                finalPosition = naviInfluencePoint.Pos;
                                updateEnd = true;
                                updateEndPosition = followEntity.RegionLocation.Position;
                            }
                            else
                                finalPosition = followEntity.RegionLocation.Position;
                        }
                        else
                            _generatedPath.Path.UpdateEndPosition(followEntity.RegionLocation.Position);
                    }
                    else
                    {
                        FollowEntityMissingEvent.Invoke();
                        Stop();
                        if (IsSeekingMissile && _owner.IsInWorld)
                        {
                            SetEnabled(true);
                            return true;
                        }
                        else
                            return false;
                    }
                }
                else
                {
                    if (followEntity != null && followEntity.IsInWorld && _generatedPath.Path.IsValid)
                        _generatedPath.Path.UpdateEndPosition(followEntity.RegionLocation.Position);
                }
            }
            else
            {
                if (_generatedPath.Path.IsValid && !_generatedPath.Path.IsComplete 
                    && (_repathDelay != TimeSpan.Zero && _repathTime < GameTimeNow()))
                {
                    repath = true;
                    finalPosition = _generatedPath.Path.GetFinalPosition();
                }
            }

            if (repath)
            {
                float finalDistance = Vector3.Distance2D(finalPosition, _owner.RegionLocation.Position) - _owner.Bounds.Radius;
                if (finalDistance < 16.0f && _generatedPath.Path.IsValid && !_generatedPath.Path.IsComplete)
                    repath = false;
            }

            if (repath)
            {
                _repathCount++;
                GeneratedPath newPath = new ();
                GeneratePath(newPath, finalPosition, _pathGenerationFlags, _incompleteDistance);
                bool pathSuccess = newPath.PathResult == NaviPathResult.Success || newPath.PathResult == NaviPathResult.IncompletedPath;
                float approxCurrentDistance = _generatedPath.Path.ApproxCurrentDistance(_owner.RegionLocation.Position);
                bool validDistance = pathSuccess && (!_generatedPath.Path.IsValid || newPath.Path.ApproxTotalDistance() < approxCurrentDistance);

                bool movementImpeded = MovementImpeded;
                if (pathSuccess && IsFollowingEntity && _generatedPath.Path.IsCurrentGoalNodeLastNode)
                {
                    Vector3? resultPosition = new(); 
                    Vector3? normalResult = null;
                    SweepResult sweepResult = _owner.NaviMesh.Sweep(_owner.RegionLocation.Position, _generatedPath.Path.GetFinalPosition(), _owner.Bounds.Radius, PathFlags, 
                        ref resultPosition, ref normalResult, MovementSweepPadding);
                    if (sweepResult != SweepResult.Success)
                        movementImpeded = true;
                }

                if (validDistance || (pathSuccess && movementImpeded))
                {
                    _generatedPath.Set(newPath);
                    if (updateEnd && _generatedPath.Path.IsValid)
                        _generatedPath.Path.UpdateEndPosition(updateEndPosition);
                    LocomotionState.PathNodes.Set(_generatedPath.Path.PathNodeList);
                    LocomotionState.PathGoalNodeIndex = _generatedPath.Path.GetCurrentGoalNodeIndex();
                    _syncPathGoalNodeIndex = 0;
                    _initRotation = false;
                }
                _repathTime = GameTimeNow() + _repathDelay;
            }
            return true;
        }

        public bool PathTo(Vector3 position, LocomotionOptions options)
        {
            if (!Vector3.IsFinite(position)) return false;
            ResetState(); 
            GeneratePath(_generatedPath, position, options.PathGenerationFlags, options.IncompleteDistance);
            bool success = _generatedPath.PathResult == NaviPathResult.Success || _generatedPath.PathResult == NaviPathResult.IncompletedPath;
            if (success)
            {
                _pathGenerationFlags = options.PathGenerationFlags;
                _incompleteDistance = options.IncompleteDistance;
                _repathDelay = options.RepathDelay;
                if (_repathDelay != TimeSpan.Zero)
                    _repathTime = GameTimeNow() + _repathDelay;
                LocomotionState.BaseMoveSpeed = CalcBaseMoveSpeedForLocomotion(options);
                LocomotionState.Height = options.MoveHeight;
                LocomotionState.LocomotionFlags |= options.Flags | LocomotionFlags.IsLocomoting;
                LocomotionState.PathNodes.Set(_generatedPath.Path.PathNodeList);
            }
            SetEnabled(success);
            return success;
        }

        public bool PathToWaypoints(List<Waypoint> waypoints)
        {
            ResetState();
            Region region = _owner?.Region;
            if (region == null || waypoints.Count == 0 || waypoints.Last().Side != NaviSide.Point) return false;
            bool hasNaviInfluence = _owner.HasNavigationInfluence;
            if (hasNaviInfluence) _owner.DisableNavigationInfluence();

            _generatedPath.PathResult = _generatedPath.Path.GenerateWaypointPath(region.NaviMesh, _owner.RegionLocation.Position, waypoints, _owner.Bounds.Radius, PathFlags);
            bool success = (_generatedPath.PathResult == NaviPathResult.Success);
            if (success)
            {
                LocomotionOptions options = new();
                _pathGenerationFlags = options.PathGenerationFlags;
                _incompleteDistance = options.IncompleteDistance;
                _repathDelay = options.RepathDelay;
                if (_repathDelay != TimeSpan.Zero)
                    _repathTime = GameTimeNow() + _repathDelay;
                LocomotionState.BaseMoveSpeed = CalcBaseMoveSpeedForLocomotion(options);
                LocomotionState.Height = options.MoveHeight;
                LocomotionState.LocomotionFlags |= options.Flags | LocomotionFlags.IsLocomoting;
                LocomotionState.PathNodes.Set(_generatedPath.Path.PathNodeList);
            }

            if (hasNaviInfluence) _owner.EnableNavigationInfluence();
            SetEnabled(success);
            return success;
        }

        public bool FollowEntity(ulong targetId, float range = 0.0f, LocomotionOptions options = null, bool clearPath = true)
        {
            options ??= DefaultFollowEntityLocomotionOptions;
            return FollowEntity(targetId, range, range, options, clearPath);
        }

        public bool FollowEntity(ulong targetId, float rangeStart, float rangeEnd, LocomotionOptions options, bool clearPath = true)
        {
            if (targetId != 0 && _owner.IsInWorld == false) return false;
            if (FollowEntityId != targetId) ResetState(clearPath);
            LocomotionState.FollowEntityId = targetId;
            LocomotionState.FollowEntityRangeStart = rangeStart;
            LocomotionState.FollowEntityRangeEnd = rangeEnd;
            _repathDelay = options.RepathDelay;
            if (_repathDelay != TimeSpan.Zero)
                _repathTime = GameTimeNow() + _repathDelay;
            UnregisterFollowEvents();
            _pathGenerationFlags = options.PathGenerationFlags | PathGenerationFlags.IncompletedPath;
            _incompleteDistance = options.IncompleteDistance;
            LocomotionState.BaseMoveSpeed = CalcBaseMoveSpeedForLocomotion(options);
            LocomotionState.Height = options.MoveHeight;
            LocomotionState.LocomotionFlags |= options.Flags | LocomotionFlags.IsLocomoting;

            if (clearPath && IsFollowingEntity) RefreshCurrentPath();
            SetEnabled(IsFollowingEntity);
            return _generatedPath.PathResult == NaviPathResult.Success || _generatedPath.PathResult == NaviPathResult.IncompletedPath;
        }

        public bool FollowPath(GeneratedPath followPath, LocomotionOptions options)
        {
            bool success = followPath.PathResult == NaviPathResult.Success || 
                (options.PathGenerationFlags.HasFlag(PathGenerationFlags.IncompletedPath) && followPath.PathResult == NaviPathResult.IncompletedPath);
            if (success)
            {
                ResetState();
                _generatedPath.Set(followPath);
                _pathGenerationFlags = options.PathGenerationFlags;
                _incompleteDistance = options.IncompleteDistance;
                _repathDelay = options.RepathDelay;
                if (_repathDelay != TimeSpan.Zero) 
                    _repathTime = GameTimeNow() + _repathDelay;
                LocomotionState.BaseMoveSpeed = CalcBaseMoveSpeedForLocomotion(options);
                LocomotionState.Height = options.MoveHeight;
                LocomotionState.LocomotionFlags |= options.Flags | LocomotionFlags.IsLocomoting;
                LocomotionState.PathNodes.Set(_generatedPath.Path.PathNodeList);
                SetEnabled(true);
                return true;
            }
            else
                return false;
        }

        private float CalcBaseMoveSpeedForLocomotion(LocomotionOptions options)
        {
            if (options.BaseMoveSpeed > 0.0f)
                return options.BaseMoveSpeed;
            else
            {
                if (_moveSpeedOverride > 0.0f)
                    return _moveSpeedOverride;
                else
                {
                    float baseMoveSpeed = options.Flags.HasFlag(LocomotionFlags.IsWalking) ? _walkSpeed : _runSpeed;
                    baseMoveSpeed += GetBaseMovementSpeedBonus();
                    return baseMoveSpeed;
                }
            }
        }

        private float GetBaseMovementSpeedBonus()
        {
            if (_owner == null) return 0.0f;
            float speedBonus = 0.0f;
            foreach (var kvp in _owner.Properties.IteratePropertyRange(PropertyEnum.MovementSpeedFromEndurance))
            {
                Property.FromParam(kvp.Key, 0, out int manaType);
                float speedFromEndurance = _owner.Properties[PropertyEnum.MovementSpeedFromEndurance, manaType];
                float enduranceMax = _owner.Properties[PropertyEnum.EnduranceMax, manaType];
                if (speedFromEndurance > 0.0f && enduranceMax > 0.0f)
                    speedBonus += speedFromEndurance * (_owner.Properties[PropertyEnum.Endurance, manaType] / enduranceMax);
            }
            return speedBonus;
        }

        private void ResetState(bool clearPath = true)
        {
            IsMoving = false;
            MovementImpeded = false;
            UnregisterFollowEvents();
            LocomotionState.FollowEntityId = 0;
            LocomotionState.FollowEntityRangeStart = 0.0f;
            LocomotionState.FollowEntityRangeEnd = 0.0f;

            if (clearPath)
            {
                _generatedPath.Path.Clear();
                LocomotionState.PathNodes.Clear();
                LocomotionState.PathGoalNodeIndex = 0;
                LocomotionState.LocomotionFlags = 0;
                _syncPathGoalNodeIndex = 0;
            }
            else
                LocomotionState.LocomotionFlags &= LocomotionFlags.IsDrivingMovementMode;

            _giveUpDistance = 0.0f;
            _giveUpNextTime = TimeSpan.Zero;
            _giveUpRepathCount = -1;
            _pathGenerationFlags = 0;
            _incompleteDistance = 0.0f;
            _repathDelay = TimeSpan.Zero;
            _repathCount = 0;
            _initRotation = false;
            LocomotionState.BaseMoveSpeed = DefaultRunSpeed;
            LocomotionState.Height = 0;
        }

        private void UnregisterFollowEvents()
        {
            FollowEntityGiveUpEvent.UnregisterCallbacks();
            FollowEntityMissingEvent.UnregisterCallbacks();
        }

        public bool MoveTo(Vector3 position, LocomotionOptions options)
        {
            if (!Vector3.IsFinite(position)) return false;
            ResetState();

            _generatedPath.PathResult = _generatedPath.Path.GenerateSimpleMove(_owner.RegionLocation.Position, position, _owner.Bounds.GetRadius(), PathFlags);
            bool success = _generatedPath.PathResult == NaviPathResult.Success;
            if (success)
            {
                LocomotionState.BaseMoveSpeed = CalcBaseMoveSpeedForLocomotion(options);
                LocomotionState.Height = options.MoveHeight;
                LocomotionState.LocomotionFlags |= options.Flags | LocomotionFlags.IsLocomoting | LocomotionFlags.MoveTo;
                LocomotionState.PathNodes.Set(_generatedPath.Path.PathNodeList);
            }
            SetEnabled(success);
            return success;
        }

        public bool MoveForward(LocomotionOptions options)
        {
            ResetState();
            LocomotionState.BaseMoveSpeed = CalcBaseMoveSpeedForLocomotion(options);
            LocomotionState.Height = options.MoveHeight;
            LocomotionState.LocomotionFlags |= options.Flags | LocomotionFlags.IsLocomoting | LocomotionFlags.MoveForward;
            SetEnabled(true);
            return true;
        }

        private TimeSpan GameTimeNow()
        {
            var game = _owner?.Game;
            if (game == null) return new();
            return game.CurrentTime;
        }

        public void LookAt(Vector3 position)
        {
            if (_owner == null || !Vector3.IsFinite(position)) return;
            if (IsLooking)
            {
                _generatedPath.Path.UpdateEndPosition(position);
                LocomotionState.PathNodes.Set(_generatedPath.Path.PathNodeList);
            }
            else
            {
                ResetState();
                Vector3 ownerPosition = _owner.RegionLocation.Position;
                Vector3 direction = Vector3.Normalize(position - ownerPosition);
                if (!Vector3.IsNearZero(direction - _owner.Forward))
                {
                    _generatedPath.PathResult = _generatedPath.Path.GenerateSimpleMove(ownerPosition, position, _owner.Bounds.GetRadius(), PathFlags);
                    bool success = (_generatedPath.PathResult == NaviPathResult.Success);
                    if (success)
                    {
                        LocomotionState.LocomotionFlags |= LocomotionFlags.IsLooking;
                        LocomotionState.PathNodes.Set(_generatedPath.Path.PathNodeList);
                    }
                    SetEnabled(success);
                }
            }
        }

        private void SetRotateMaxTurnThisFrame3D(Vector3 currentDirection, Vector3 goalDirection, float rotationSpeed, float timeSeconds) 
        {
            if (_owner == null) return;
            var vector = RotateMaxTurnThisFrame3D(currentDirection, goalDirection, rotationSpeed, timeSeconds);
            SetOrientation(Orientation.FromDeltaVector(vector));
        }

        public static Vector3 RotateMaxTurnThisFrame3D(Vector3 currentDirection, Vector3 goalDirection, float rotationSpeed, float timeSeconds)
        {
            if (Vector3.IsNearZero(currentDirection - goalDirection))
                return currentDirection;
            else
            {
                float maxDeg = MathHelper.ToDegrees(Vector3.Angle(currentDirection, goalDirection));
                float deg = rotationSpeed * timeSeconds;
                if (deg < maxDeg)
                {
                    Vector3 axis = Vector3.Normalize(Vector3.Cross(currentDirection, goalDirection));
                    return Vector3.AxisAngleRotate(currentDirection, axis, MathHelper.ToRadians(deg));
                }
                else
                    return goalDirection;
            }
        }

        private void UpdateNavigationInfluence(bool forceUpdate = false)
        {
            if (!_owner.HasNavigationInfluence) return;
            TimeSpan timeNow = GameTimeNow();
            NaviPoint influencePoint = _owner.NavigationInfluencePoint;

            if (forceUpdate || (_updateNavigationInfluenceTime < timeNow) 
                || (influencePoint != null && Vector3.DistanceSquared2D(influencePoint.Pos, _owner.RegionLocation.Position) > 576.0f))
            {
                _owner.UpdateNavigationInfluence();
                _updateNavigationInfluenceTime = timeNow + DefaultUpdateNavigationInfluenceFreq;
            }
        }

        private void PushLocomotionStateChanges()
        {
            if (LocomotionState.LocomotionFlags.HasFlag(LocomotionFlags.IsSyncMoving) == false)
            {
                _owner.OnLocomotionStateChanged(_lastLocomotionState, LocomotionState);
                _lastLocomotionState.Set(LocomotionState);
            }
        }

        public void ClearOrientationSyncState()
        {
            _hasOrientationSyncState = false;
        }

        public void ClearSyncState()
        {
            _syncStateTime = TimeSpan.Zero;
        }

        public void SetMethod(LocomotorMethod method, float moveSpeedOverride = 0.0f)
        {
            LocomotionState.Method = (method == LocomotorMethod.Default) ? _defaultMethod : method;
            if (_moveSpeedOverride != moveSpeedOverride)
            {
                _moveSpeedOverride = moveSpeedOverride;
                LocomotionState.BaseMoveSpeed = _moveSpeedOverride > 0.0f ? _moveSpeedOverride : _runSpeed;
            }
            PushLocomotionStateChanges();
        }

        public float GetBonusMovementSpeed(bool skipOverride = true)
        {
            float defaultRunSpeed = DefaultRunSpeed;
            float moveSpeedOverride = _owner.MovementSpeedOverride;
            if (moveSpeedOverride > 0.0f && skipOverride == false)
                return moveSpeedOverride - defaultRunSpeed;
            return (defaultRunSpeed * GetCurrentSpeedRate()) - defaultRunSpeed;
        }

        public void SetSyncState(LocomotionState locomotionState, Vector3 syncPosition, Orientation syncOrientation)
        {
            if (_owner == null) return;
         
            LastSyncState.Set(locomotionState);
            _syncStateTime = TimeSpan.Zero;
            _syncNextRepathTime = TimeSpan.Zero;
            _syncPosition = syncPosition;
            _syncOrientation = syncOrientation;
            _syncAttempts = 0;
            _syncAttemptsFailed = 0;
            _syncSpeed = 0.0f;
            _syncPathGoalNodeIndex = 0;
            _hasOrientationSyncState = false;
            LocomotionState.Set(locomotionState);
            LocomotionState.Method = (LocomotionState.Method == LocomotorMethod.Default) ? _defaultMethod : LocomotionState.Method;
            
            if (_owner.Game.AdminCommandManager.TestAdminFlag(AdminFlags.LocomotionSync))
            {
                if (_owner.IsInWorld && _owner.CanRotate() && LocomotionState.LocomotionFlags.HasFlag(LocomotionFlags.IsLooking))
                {
                    _generatedPath.Path.Init(_owner.Bounds.Radius, PathFlags, LocomotionState.PathNodes);
                    _generatedPath.PathResult = NaviPathResult.Success;
                    Vector3 lookingDir = GetLookingGoalDir();
                    SetEnabled(false);
                    _syncStateTime = GameTimeNow();
                    _hasOrientationSyncState = true;
                    _syncOrientation = Orientation.FromDeltaVector(lookingDir);
                }
                else if (_owner.IsInWorld && _owner.CanMove() && LocomotionState.LocomotionFlags.HasFlag(LocomotionFlags.IsLocomoting))
                {
                    if (LocomotionState.LocomotionFlags.HasFlag(LocomotionFlags.MoveForward))
                    {
                        ChangePositionFlags changeFlags = ChangePositionFlags.Force | ChangePositionFlags.PhysicsResolve | ChangePositionFlags.DoNotSendToServer | ChangePositionFlags.DoNotSendToClients;
                        _owner.ChangeRegionPosition(syncPosition, syncOrientation, changeFlags);
                        _generatedPath.Path.Init(_owner.Bounds.Radius, PathFlags, LocomotionState.PathNodes);
                        _generatedPath.PathResult = NaviPathResult.Success;
                        SetEnabled(true);
                    }
                    else if (LocomotionState.PathNodes.Count == 0 || LocomotionState.PathGoalNodeIndex == LocomotionState.PathNodes.Count)
                    {
                        SetEnabled(false);
                        _syncStateTime = GameTimeNow();
                        _hasOrientationSyncState = true;
                    }
                    else
                    {
                        bool syncTeleport = false;
                        float desyncDistanceSq = Vector3.DistanceSquared2D(_owner.RegionLocation.Position, syncPosition);
                        if (desyncDistanceSq > 16.0f)
                        {
                            int numPathNodes = LocomotionState.PathNodes.Count;
                            if (numPathNodes >= 2 && LocomotionState.PathGoalNodeIndex < numPathNodes)
                            {
                                _generatedPath.Path.Init(_owner.Bounds.Radius, PathFlags, LocomotionState.PathNodes);
                                _generatedPath.PathResult = NaviPathResult.Success;

                                int goalNodeIndex = LocomotionState.PathGoalNodeIndex;
                                var currentGoalPosition = _generatedPath.Path.GetCurrentGoalPosition(_owner.RegionLocation.Position);

                                if (Vector3.Dot2D(currentGoalPosition - syncPosition, _owner.RegionLocation.Position - syncPosition) > 0.0f)
                                {
                                    GeneratePath(_generatedPath, currentGoalPosition, PathGenerationFlags.Default);
                                    _generatedPath.Path.PopGoal();
                                }
                                else
                                {
                                    GeneratePath(_generatedPath, syncPosition, PathGenerationFlags.Default);

                                    if (_generatedPath.PathResult != NaviPathResult.Success)
                                    {
                                        Vector3 resultPosition = default;
                                        Vector3? resultNormal = null;
                                        if (SweepTo(syncPosition, ref resultPosition, ref resultNormal) == SweepResult.Success)
                                            _generatedPath.PathResult = _generatedPath.Path.GenerateSimpleMove(_owner.RegionLocation.Position, syncPosition, _owner.Bounds.Radius, PathFlags);
                                    }
                                }

                                if (_generatedPath.PathResult == NaviPathResult.Success)
                                {
                                    int numPathNodeList = _generatedPath.Path.PathNodeList.Count;
                                    _generatedPath.Path.Append(LocomotionState.PathNodes, goalNodeIndex);

                                    float originalPathLength = NaviPath.CalcAccurateDistance(LocomotionState.PathNodes);

                                    if (LocomotionState.BaseMoveSpeed > Segment.Epsilon && originalPathLength > Segment.Epsilon)
                                    {
                                        float fullPathLength = _generatedPath.Path.AccurateTotalDistance();
                                        _syncSpeed = fullPathLength / (originalPathLength / LocomotionState.BaseMoveSpeed);
                                        _syncSpeed = Math.Max(_syncSpeed, LocomotionState.BaseMoveSpeed);
                                        if (_syncSpeed > LocomotionState.BaseMoveSpeed * 3.0f)
                                        {
                                            _syncSpeed = 0f;
                                            syncTeleport = true;
                                        }
                                        _syncSpeed = Math.Min(_syncSpeed, LocomotionState.BaseMoveSpeed * 2.0f);
                                    }

                                    if (syncTeleport == false)
                                    {
                                        LocomotionState.PathNodes.Set(_generatedPath.Path.PathNodeList);
                                        LocomotionState.PathGoalNodeIndex = _generatedPath.Path.GetCurrentGoalNodeIndex();
                                        _syncPathGoalNodeIndex = Math.Max(0, numPathNodeList - 1);
                                    }
                                }
                                else
                                    syncTeleport = true;
                            }
                        }
                        else
                            syncTeleport = true;

                        if (syncTeleport)
                        {
                            ChangePositionFlags changeFlags = ChangePositionFlags.Force | ChangePositionFlags.PhysicsResolve | ChangePositionFlags.DoNotSendToServer | ChangePositionFlags.DoNotSendToClients;
                            _owner.ChangeRegionPosition(syncPosition, syncOrientation, changeFlags);
                            _generatedPath.Path.Init(_owner.Bounds.Radius, PathFlags, LocomotionState.PathNodes);
                            _generatedPath.PathResult = NaviPathResult.Success;
                        }
                        else if (IsDrivingMovementMode && _owner.IsExecutingPower && _owner.Orientation != syncOrientation)
                            _hasOrientationSyncState = true;

                        SetEnabled(true);
                    }
                }
                else
                {
                    SetEnabled(false);
                    _syncStateTime = GameTimeNow();
                    _hasOrientationSyncState = true;
                }
            }

            PushLocomotionStateChanges();
        }

        public NaviPathResult GeneratePath(GeneratedPath generatedPath, Vector3 goalPosition, PathGenerationFlags pathGenerationFlags,
            float incompleteDistance = 0.0f, WorldEntity other = null)
        {
            generatedPath.Clear();
            Region region = _owner?.Region;
            if (region == null) return NaviPathResult.FailedRegion;

            bool hasNaviInfluence = _owner.HasNavigationInfluence;
            if (hasNaviInfluence) _owner.DisableNavigationInfluence();

            bool otherHasNaviInfluence = false;
            if (other != null)
            {
                otherHasNaviInfluence = other.HasNavigationInfluence;
                if (otherHasNaviInfluence) other.DisableNavigationInfluence();
            }

            List<WorldEntity> entities = new ();
            _owner.OnPreGeneratePath(_owner.RegionLocation.Position, goalPosition, entities);
            generatedPath.PathResult = generatedPath.Path.GeneratePath(region.NaviMesh, _owner.RegionLocation.Position, goalPosition,
                _owner.Bounds.GetRadius(), PathFlags, pathGenerationFlags, incompleteDistance);

            foreach (WorldEntity entity in entities)
                entity?.EnableNavigationInfluence();

            if (hasNaviInfluence) _owner.EnableNavigationInfluence();
            if (otherHasNaviInfluence) other.EnableNavigationInfluence();

            return generatedPath.PathResult;
        }

        public float GetCurrentSpeed()
        {
            if (_owner == null) return 0.0f;
            float currentSpeed;
            float speedOverride = _owner.MovementSpeedOverride;
            if (speedOverride > 0.0f)
                currentSpeed = speedOverride;
            else
            {
                currentSpeed = LocomotionState.BaseMoveSpeed;
                if (LocomotionState.LocomotionFlags.HasFlag(LocomotionFlags.SkipCurrentSpeedRate) == false)
                    currentSpeed *= GetCurrentSpeedRate();
            }
            Power activePower = _owner.ActivePower;
            if (activePower != null && activePower.IsTravelPower())
            {
                var combatGlobals = GameDatabase.CombatGlobalsPrototype;
                if (combatGlobals != null)
                    currentSpeed = Math.Min(combatGlobals.TravelPowerMaxSpeed, currentSpeed);
            }
            currentSpeed = Math.Max(0.0f, currentSpeed);

            if (IsSyncMoving || IsFollowingSyncPath)
                currentSpeed = Math.Max(_syncSpeed, currentSpeed);

            return currentSpeed;
        }

        public float GetCurrentSpeedRate()
        {
            return _owner != null ? _owner.MovementSpeedRate : 0.0f;
        }

        public override string ToString()
        {
            return $"Locomotor m_owner:({_owner})";
        }
    }

    public struct Waypoint
    {
        public Vector3 Point;
        public NaviSide Side;
        public float Radius;

        public Waypoint(Vector3 point, NaviSide side, float radius)
        {
            Point = point;
            Side = side;
            Radius = radius;
        }
    }

    public class GeneratedPath
    {
        public NaviPath Path { get; private set; }
        public NaviPathResult PathResult { get; set; }

        public GeneratedPath()
        {
            Path = new();
            Clear();
        }

        public void Set(GeneratedPath other)
        {
            Path.Copy(other.Path);
            PathResult = other.PathResult;
        }

        public void Clear()
        {
            Path.Clear();
            PathResult = NaviPathResult.Failed;
        }
    }

    public class LocomotionOptions
    {
        public TimeSpan RepathDelay;
        public PathGenerationFlags PathGenerationFlags;
        public float IncompleteDistance;
        public float BaseMoveSpeed;
        public int MoveHeight;
        public LocomotionFlags Flags;

        public LocomotionOptions()
        {
            RepathDelay = TimeSpan.Zero;
        }

        public LocomotionOptions(TimeSpan repathDelay, PathGenerationFlags pathGenerationFlags, float incompleteDistance, 
            float baseMoveSpeed, int moveHeight, LocomotionFlags flags)
        {
            RepathDelay = repathDelay;
            PathGenerationFlags = pathGenerationFlags;
            IncompleteDistance = incompleteDistance;
            BaseMoveSpeed = baseMoveSpeed;
            MoveHeight = moveHeight;
            Flags = flags;
        }
    }
}
