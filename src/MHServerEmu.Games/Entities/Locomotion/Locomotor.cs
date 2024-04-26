using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities.Locomotion
{
    public class Locomotor
    {
        public const float MovementSweepPadding = 0.5f;
        public const float GiveUpGoalDistance = 16.0f;

        public TimeSpan SyncPathInterval = TimeSpan.FromMilliseconds(400);
        public TimeSpan DefaultUpdateNavigationInfluenceFreq = TimeSpan.FromMilliseconds(500);
        public float OutOfSyncAdjustDistanceSq = MathHelper.Square(16.0f);

        public WorldEntity Owner { get; private set; }
        public LocomotionState LocomotionState { get; private set; }
        public bool MovementImpeded { get; set; }
        public bool IsMoving { get; private set; }
        public bool IsEnabled { get; private set; }
        public float DefaultRunSpeed { get; private set; }
        public bool WalkEnabled { get; private set; }
        public float WalkSpeed { get; private set; }
        public float RotationSpeed { get; private set; }
        public float Height { get; private set; }
        public LocomotorMethod Method { get; private set; }
        public GeneratedPath GeneratedPath { get; private set; }

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

        public LocomotionState LastSyncState { get; internal set; }
        public bool HasSyncState { get => _syncStateTime != TimeSpan.Zero; }
        public bool HasPath { get => GeneratedPath.Path.IsValid; }
        public bool IsFollowingSyncPath { get; private set; }

        private bool _hasOrientationSyncState;
        private TimeSpan _syncStateTime;
        private Orientation _syncOrientation;
        private Vector3 _syncPosition;
        private TimeSpan _syncNextRepathTime;

        private int _syncAttempts;
        private float _giveUpDistanceThreshold;
        private TimeSpan _giveUpTime;
        private TimeSpan _giveUpNextTime;

        private float _syncSpeed;
        private int _syncAttemptsFailed;
        private Vector3 _giveUpPosition;
        private float _giveUpDistance;
        private TimeSpan _repathDelay;
        private int _giveUpRepathCount;
        private int _repathCount;

        public Locomotor()
        {
            LocomotionState = new();
            Method = LocomotorMethod.None;
        }

        public void Initialize(LocomotorPrototype locomotorProto, WorldEntity entity, float heightOverride = 0.0f)
        {
            Owner = entity;
            if (entity != null && entity.Properties.HasProperty(PropertyEnum.MissileBaseMoveSpeed))
                DefaultRunSpeed = entity.Properties[PropertyEnum.MissileBaseMoveSpeed];
            else
                DefaultRunSpeed = locomotorProto.Speed;

            WalkEnabled = locomotorProto.WalkEnabled;
            WalkSpeed = locomotorProto.WalkSpeed;
            RotationSpeed = locomotorProto.RotationSpeed;
            Height = heightOverride != 0.0f ? heightOverride : locomotorProto.Height;

            if (Owner != null)
            {
                var worldEntityProto = Owner.WorldEntityPrototype;
                if (worldEntityProto != null)
                    Method = worldEntityProto.NaviMethod;
            }

            LocomotionState.Method = Method;
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

        public SweepResult SweepFromTo(Vector3 fromPosition, Vector3 toPosition, ref Vector3 resultPosition, ref Vector3 resultNormal, float padding = MovementSweepPadding)
        {
            if (Owner == null) return SweepResult.Failed;
            NaviMesh naviMesh = Owner.NaviMesh;
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
                    maxHeight = (int)(RegionLocation.ProjectToFloor(Owner.Region, Owner.Cell, fromPosition).Z + flyHeight);
                    heightSweep = HeightSweepType.Constraint;
                }
            }
            else
            {
                int moveHeight = CurrentMoveHeight;
                if (moveHeight != 0)
                {
                    if (moveHeight > 0)
                        maxHeight = (int)(RegionLocation.ProjectToFloor(Owner.Region, Owner.Cell, fromPosition).Z + moveHeight);
                    else
                        minHeight = (int)(RegionLocation.ProjectToFloor(Owner.Region, Owner.Cell, fromPosition).Z + moveHeight);
                    pathFlags |= PathFlags.Fly;
                    heightSweep = HeightSweepType.Constraint;
                }
            }

            float radius = Owner.Bounds.Radius;
            SweepResult sweepResult = naviMesh.Sweep(fromPosition, toPosition, radius, pathFlags, ref resultPosition, ref resultNormal,
                                                     padding, heightSweep, maxHeight, minHeight, Owner);
            if (sweepResult != SweepResult.Failed)
            {
                if (sweepResult == SweepResult.HeightMap && Vector3.IsNearZero2D(fromPosition - resultPosition))
                { 
                    pathFlags &= ~PathFlags.Fly;
                    pathFlags |= PathFlags.Walk;
                    if (naviMesh.Contains(fromPosition, radius, new DefaultContainsPathFlagsCheck(pathFlags)))
                        sweepResult = naviMesh.Sweep(fromPosition, toPosition, radius, pathFlags, ref resultPosition, ref resultNormal,
                                                     padding, HeightSweepType.None, 0, 0, Owner);
                }

                if (IsMissile)
                {
                    Region region = Owner.Region;
                    if (region == null) return SweepResult.Failed;

                    Cell cell = region.GetCellAtPosition(resultPosition);
                    if (cell == null || RegionLocation.ProjectToFloor(cell, resultPosition).Z > toPosition.Z)
                        sweepResult = SweepResult.Clipped;
                }
                else
                {
                    resultPosition = RegionLocation.ProjectToFloor(Owner.Region, Owner.Cell, resultPosition);
                    resultPosition = Owner.FloorToCenter(resultPosition);
                }
            }
            return sweepResult;
        }

        public float GetCurrentFlyingHeight()
        {
            if (Owner == null) return 0.0f;
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

        public SweepResult SweepTo(Vector3 toPosition, ref Vector3 resultPosition, ref Vector3 resultNormal, float padding = MovementSweepPadding)
        {
            if (Owner == null)
            {
                resultPosition = Vector3.Zero;
                resultNormal = Vector3.Zero;
                return SweepResult.Failed;
            }

            if (IgnoresWorldCollision)
            {
                Region region = Owner.Region;
                if (region == null)
                {
                    resultPosition = Vector3.Zero;
                    resultNormal = Vector3.Zero;
                    return SweepResult.Failed;
                }

                if (region.GetCellAtPosition(toPosition) == null)
                {
                    resultPosition = Owner.RegionLocation.Position;
                    resultNormal = Vector3.Zero;
                    return SweepResult.Clipped;
                }

                resultPosition = toPosition;
                resultNormal = Vector3.Zero;
                return SweepResult.Success;
            }

            return SweepFromTo(Owner.RegionLocation.Position, toPosition, ref resultPosition, ref resultNormal, padding);
        }

        public void SetGiveUpLimits(float distanceThreshold, TimeSpan time)
        {
            _giveUpDistanceThreshold = distanceThreshold;
            _giveUpTime = time;
        }

        public void Locomote()
        {
            if (Owner == null) return;
            Game game = Owner.Game;
            if (game == null || Owner.IsInWorld == false) return;
            float timeSeconds;

            if (true) // Owner.Game.AdminCommandManager.Flag1
            {
                if (IsEnabled && _hasOrientationSyncState && IsDrivingMovementMode && Owner.IsExecutingPower)
                {                    
                    Vector3 syncDir = _syncOrientation.GetMatrix3() * Vector3.Forward;
                    timeSeconds = (float)game.FixedTimeBetweenUpdates.TotalSeconds;
                    var delta = RotateMaxTurnThisFrame3D(Owner.Forward, syncDir, 300.0f, timeSeconds);
                    var orientation = Orientation.FromDeltaVector(delta);
                    ChangePositionFlags changeFlags = ChangePositionFlags.PhysicsResolve | ChangePositionFlags.NoSendToServer | ChangePositionFlags.NoSendToClients;
                    Owner.ChangeRegionPosition(null, orientation, changeFlags);

                    if (_syncOrientation == Owner.Orientation)
                        ClearOrientationSyncState();
                }
                else if (HasSyncState && IsEnabled == false)
                {
                    if (Owner.ActivePowerPreventsMovement(PowerMovementPreventionFlags.Sync))
                    {
                        if (_hasOrientationSyncState)
                        {
                            Vector3 lookDir = _syncOrientation.GetMatrix3() * Vector3.Forward;
                            LookAt(Owner.RegionLocation.Position + lookDir);
                        }
                    }
                    else
                    {
                        TimeSpan timeNow = GameTimeNow();
                        if (_syncNextRepathTime < timeNow)
                        {
                            _syncNextRepathTime = timeNow + SyncPathInterval;
                            float desyncDistanceSq = Vector3.DistanceSquared2D(_syncPosition, Owner.RegionLocation.Position);
                            if (desyncDistanceSq > OutOfSyncAdjustDistanceSq)
                            {
                                bool syncTeleport = false;
                                if (++_syncAttempts < 5)
                                {
                                    var ownerProto = Owner.WorldEntityPrototype;
                                    if (ownerProto == null) return;
                                    var locomotorProto = ownerProto.Locomotor;
                                    if (locomotorProto == null) return;

                                    LocomotionOptions locomotionOptions = new ();
                                    locomotionOptions.Flags |= LocomotionFlags.IsSyncMoving;
                                    if (Owner.ActivePowerOrientsToTarget() || locomotorProto.DisableOrientationForSyncMove || Owner.ActivePowerDisablesOrientation())
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
                                            Vector3 resultNormal = null;
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
                                    ChangePositionFlags changeFlags = ChangePositionFlags.Force | ChangePositionFlags.PhysicsResolve | ChangePositionFlags.NoSendToServer | ChangePositionFlags.NoSendToClients;
                                    Owner.ChangeRegionPosition(_syncPosition, _syncOrientation, changeFlags);
                                    UpdateNavigationInfluence(true);
                                    ClearSyncState();
                                }
                            }
                            else
                            {
                                if (_hasOrientationSyncState)
                                {
                                    Vector3 lookDir = _syncOrientation.GetMatrix3() * Vector3.Forward;
                                    LookAt(Owner.RegionLocation.Position + lookDir);
                                }
                                ClearSyncState();
                            }
                        }
                    }
                }
            }

            if (IsEnabled == false) return;

            Vector3 currentPosition = Owner.RegionLocation.Position;
            timeSeconds = (float)game.FixedTimeBetweenUpdates.TotalSeconds;

            if (IsLooking && Owner.CanRotate)
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
            if (Owner.CanMove && GetNextLocomotePosition(timeSeconds, out Vector3 movePosition))
            {
                Vector3 dir = movePosition - Owner.RegionLocation.Position;
                if (!Vector3.IsNearZero(dir))
                {
                    Vector3 dirTo2d = dir.To2D();
                    if (!Owner.ActivePowerDisablesOrientation() && !Vector3.IsNearZero(dirTo2d))
                        if (DoRotationInPlace(timeSeconds, dir) == false)
                        {
                            _giveUpNextTime = GameTimeNow() + _giveUpTime;
                            return;
                        }

                    Owner.Physics.ApplyInternalForce(dir);

                    if (!IsMissile && !Owner.ActivePowerDisablesOrientation())
                        if (Vector3.LengthSquared(dirTo2d) > Segment.Epsilon)
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

            if (Owner.IsMovementAuthoritative && _giveUpDistanceThreshold > 0.0f && !IsMissile && !(IsFollowingSyncPath && IsDrivingMovementMode))
            {
                _giveUpDistance += Vector3.Distance2D(Owner.RegionLocation.Position, _giveUpPosition);
                _giveUpPosition = Owner.RegionLocation.Position;

                TimeSpan timeNow = GameTimeNow();
                if (_giveUpNextTime != TimeSpan.Zero && _giveUpNextTime < timeNow)
                {
                    bool giveUp = false;
                    if (_giveUpDistance < _giveUpDistanceThreshold)
                    {
                        if (_repathDelay == TimeSpan.Zero || (_giveUpRepathCount < _repathCount))
                            giveUp = true;
                        else
                            if (!IsFollowingEntity && GetPathGoal(out Vector3 goalPosition))
                            {
                                float goalDistance = Vector3.Distance2D(goalPosition, Owner.RegionLocation.Position) - Owner.Bounds.GetRadius();
                                giveUp |= (goalDistance < GiveUpGoalDistance);
                            }
                    }

                    if (giveUp)
                    {
                        if (IsFollowingEntity) FollowEntityGiveUpEvent();
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

        private void FollowEntityGiveUpEvent()
        {
            throw new NotImplementedException();
        }

        private bool GetPathGoal(out Vector3 goalPosition)
        {
            throw new NotImplementedException();
        }

        private bool IsPathComplete()
        {
            throw new NotImplementedException();
        }

        private void SetEnabled(bool v1, bool v2)
        {
            throw new NotImplementedException();
        }

        private void SetOrientation(Orientation movementOri)
        {
            throw new NotImplementedException();
        }

        private bool DoRotationInPlace(float time, Vector3 vector3)
        {
            throw new NotImplementedException();
        }

        private Vector3 GetLookingGoalDir()
        {
            throw new NotImplementedException();
        }

        private void Stop()
        {
            throw new NotImplementedException();
        }

        private bool GetNextLocomotePosition(float time, out Vector3 movePosition)
        {
            throw new NotImplementedException();
        }

        private bool PathTo(Vector3 position, LocomotionOptions locomotionOptions)
        {
            throw new NotImplementedException();
        }

        private bool MoveTo(Vector3 position, LocomotionOptions locomotionOptions, float padding = MovementSweepPadding)
        {
            throw new NotImplementedException();
        }

        private TimeSpan GameTimeNow()
        {
            throw new NotImplementedException();
        }

        private void LookAt(object value)
        {
            throw new NotImplementedException();
        }

        private Vector3 RotateMaxTurnThisFrame3D(Vector3 vector3, Vector3 dir, float v, float time)
        {
            throw new NotImplementedException();
        }

        private void UpdateNavigationInfluence(bool v)
        {
            throw new NotImplementedException();
        }

        private void PushLocomotionStateChanges()
        {
            throw new NotImplementedException();
        }

        public void ClearOrientationSyncState()
        {
            _hasOrientationSyncState = false;
        }

        public void ClearSyncState()
        {
            _syncStateTime = TimeSpan.Zero;
        }

        internal void SetSyncState(LocomotionState locomotionState, Vector3 position, Orientation orientation)
        {
            throw new NotImplementedException();
        }

        public float GetCurrentSpeed()
        {
            throw new NotImplementedException();
        }
    }

    public class GeneratedPath
    {
        public NaviPath Path { get; internal set; }
    }

    internal class LocomotionOptions
    {
        public LocomotionFlags Flags { get; internal set; }
    }
}
