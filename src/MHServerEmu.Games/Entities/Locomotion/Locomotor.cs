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
        public WorldEntity Owner { get; private set; }
        public LocomotionState LocomotionState { get; private set; }
        public bool MovementImpeded { get; set; }
        public bool IsMoving { get; private set; }
        public float DefaultRunSpeed { get; private set; }
        public bool WalkEnabled { get; private set; }
        public float WalkSpeed { get; private set; }
        public float RotationSpeed { get; private set; }
        public float Height { get; private set; }
        public LocomotorMethod Method { get; private set; }
        public float GiveupDistanceThreshold { get; private set; }
        public TimeSpan GiveUpTime { get; private set; }

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
            GiveupDistanceThreshold = distanceThreshold;
            GiveUpTime = time;
        }

        internal void Locomote()
        {
            throw new NotImplementedException();
        }
    }
}
