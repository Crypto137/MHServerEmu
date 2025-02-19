using System.Text;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Navi;

namespace MHServerEmu.Games.Entities.Locomotion
{
    [Flags]
    public enum LocomotionMessageFlags : uint
    {
        None                    = 0,
        HasFullOrientation      = 1 << 0,
        NoLocomotionState       = 1 << 1,
        RelativeToPreviousState = 1 << 2,   // See LocomotionState::GetFieldFlags()
        HasLocomotionFlags      = 1 << 3,
        HasMethod               = 1 << 4,
        UpdatePathNodes         = 1 << 5,
        LocomotionFinished      = 1 << 6,
        HasMoveSpeed            = 1 << 7,
        HasHeight               = 1 << 8,
        HasFollowEntityId       = 1 << 9,
        HasFollowEntityRange    = 1 << 10,
        HasEntityPrototypeRef   = 1 << 11
    }

    [Flags]
    public enum LocomotionFlags : ulong
    {
        None                        = 0,
        IsLocomoting                = 1 << 0,
        IsWalking                   = 1 << 1,
        IsLooking                   = 1 << 2,
        SkipCurrentSpeedRate        = 1 << 3,
        LocomotionNoEntityCollide   = 1 << 4,
        IsMovementPower             = 1 << 5,
        DisableOrientation          = 1 << 6,
        IsDrivingMovementMode       = 1 << 7,
        MoveForward                 = 1 << 8,
        MoveTo                      = 1 << 9,
        IsSyncMoving                = 1 << 10,
        IgnoresWorldCollision       = 1 << 11,
    }

    /// <summary>
    /// Represents state of a <see cref="Locomotor"/>.
    /// </summary>
    public class LocomotionState : IPoolable, IDisposable
    {
        // TODO: For optimization reasons it may be a good idea to change this to a struct.
        // However, it is going to be large + mutable + have a reference type as one of its
        // fields (List<NaviPathNode>), so this should be done with care and only if actually needed.

        private static readonly Logger Logger = LogManager.CreateLogger();

        // NOTE: Due to how LocomotionState serialization is implemented, we should be able to
        // get away with using C# auto properties instead of private fields.
        public LocomotionFlags LocomotionFlags { get; set; }
        public LocomotorMethod Method { get; set; } = LocomotorMethod.Default;
        public float BaseMoveSpeed { get; set; }
        public int Height { get; set; }
        public ulong FollowEntityId { get; set; }
        public float FollowEntityRangeStart { get; set; }
        public float FollowEntityRangeEnd { get; set; }
        public int PathGoalNodeIndex { get; set; }
        public List<NaviPathNode> PathNodes { get; set; } = new();

        public bool IsInPool { get; set; }

        public LocomotionState() { }

        public LocomotionState(LocomotionState other)
        {
            Set(other);
        }

        public void Set(LocomotionState other)
        {
            LocomotionFlags = other.LocomotionFlags;
            Method = other.Method;
            BaseMoveSpeed = other.BaseMoveSpeed;
            Height = other.Height;
            FollowEntityId = other.FollowEntityId;
            FollowEntityRangeStart = other.FollowEntityRangeStart;
            FollowEntityRangeEnd = other.FollowEntityRangeEnd;
            PathGoalNodeIndex = other.PathGoalNodeIndex;

            // NOTE: Is it okay to add path nodes here by reference? Do we need a copy?
            // Review this if/when we change NaviPathNode to struct.
            PathNodes.Set(other.PathNodes);
        }

        public void ResetForPool()
        {
            LocomotionFlags = default;
            Method = LocomotorMethod.Default;
            BaseMoveSpeed = default;
            Height = default;
            FollowEntityId = default;
            FollowEntityRangeStart = default;
            FollowEntityRangeEnd = default;
            PathGoalNodeIndex = default;
            PathNodes.Clear();
        }

        public void Dispose()
        {
            ObjectPoolManager.Instance.Return(this);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(LocomotionFlags)}: {LocomotionFlags}");
            sb.AppendLine($"{nameof(Method)}: {Method}");
            sb.AppendLine($"{nameof(BaseMoveSpeed)}: {BaseMoveSpeed}");
            sb.AppendLine($"{nameof(Height)}: {Height}");
            sb.AppendLine($"{nameof(FollowEntityId)}: {FollowEntityId}");
            sb.AppendLine($"{nameof(FollowEntityRangeStart)}: {FollowEntityRangeStart}");
            sb.AppendLine($"{nameof(FollowEntityRangeEnd)}: {FollowEntityRangeEnd}");
            sb.AppendLine($"{nameof(PathGoalNodeIndex)}: {PathGoalNodeIndex}");
            for (int i = 0; i < PathNodes.Count; i++)
                sb.AppendLine($"{nameof(PathNodes)}[{i}]: {PathNodes[i]}");
            return sb.ToString();
        }

        // NOTE: LocomotionState serialization implementation is similar to what PowerCollection is doing
        // (i.e. separate static serialization methods for serialization and deserialization rather than
        // combined ISerialize implementation we have seen in most other cases).

        /// <summary>
        /// Serializes a <see cref="NaviPathNode"/> to the provided <see cref="Archive"/>.
        /// </summary>
        public static bool SerializeTo(Archive archive, NaviPathNode pathNode, Vector3 previousVertex)
        {
            bool success = true;

            // Encode offset from the previous vertex
            Vector3 offset = pathNode.Vertex - previousVertex;
            success &= Serializer.TransferVectorFixed(archive, ref offset, 3);

            // Pack vertex side + radius into a single value
            int vertexSideRadius = MathHelper.RoundUpToInt(pathNode.Radius);
            if (pathNode.VertexSide == NaviSide.Left) vertexSideRadius = -vertexSideRadius;
            success &= Serializer.Transfer(archive, ref vertexSideRadius);

            return success;
        }

        /// <summary>
        /// Deserializes a <see cref="NaviPathNode"/> from the provided <see cref="Archive"/>.
        /// </summary>
        public static bool SerializeFrom(Archive archive, NaviPathNode pathNode, Vector3 previousVertex)
        {
            bool success = true;

            // Decode offset and combine it with the previous vertex
            Vector3 offset = Vector3.Zero;
            success &= Serializer.TransferVectorFixed(archive, ref offset, 3);
            pathNode.Vertex = offset + previousVertex;

            // Vertex side and radius are encoded together in the same value
            int vertexSideRadius = 0;
            success &= Serializer.Transfer(archive, ref vertexSideRadius);
            if (vertexSideRadius < 0)
            {
                pathNode.VertexSide = NaviSide.Left;
                pathNode.Radius = -vertexSideRadius;
            }
            else if (vertexSideRadius > 0)
            {
                pathNode.VertexSide = NaviSide.Right;
                pathNode.Radius = vertexSideRadius;
            }
            else /* if (vertexSideRadius == 0) */
            {
                pathNode.VertexSide = NaviSide.Point;
                pathNode.Radius = 0f;
            }

            return success;
        }

        /// <summary>
        /// Serializes <see cref="LocomotionState"/> to the provided <see cref="Archive"/>
        /// given the specified <see cref="LocomotionMessageFlags"/>.
        /// </summary>
        public static bool SerializeTo(Archive archive, LocomotionState state, LocomotionMessageFlags flags)
        {
            bool success = true;

            if (flags.HasFlag(LocomotionMessageFlags.HasLocomotionFlags))
            {
                ulong locomotionFlags = (ulong)state.LocomotionFlags;
                success &= Serializer.Transfer(archive, ref locomotionFlags);
            }

            if (flags.HasFlag(LocomotionMessageFlags.HasMethod))
            {
                uint method = (uint)state.Method;
                success &= Serializer.Transfer(archive, ref method);
            }

            if (flags.HasFlag(LocomotionMessageFlags.HasMoveSpeed))
            {
                float moveSpeed = state.BaseMoveSpeed;
                success &= Serializer.TransferFloatFixed(archive, ref moveSpeed, 0);
            }

            if (flags.HasFlag(LocomotionMessageFlags.HasHeight))
            {
                uint height = (uint)state.Height;
                success &= Serializer.Transfer(archive, ref height);
            }

            if (flags.HasFlag(LocomotionMessageFlags.HasFollowEntityId))
            {
                ulong followEntityId = state.FollowEntityId;
                success &= Serializer.Transfer(archive, ref followEntityId);
            }

            if (flags.HasFlag(LocomotionMessageFlags.HasFollowEntityRange))
            {
                float rangeStart = state.FollowEntityRangeStart;
                float rangeEnd = state.FollowEntityRangeEnd;
                success &= Serializer.TransferFloatFixed(archive, ref rangeStart, 0);
                success &= Serializer.TransferFloatFixed(archive, ref rangeEnd, 0);
            }

            if (flags.HasFlag(LocomotionMessageFlags.UpdatePathNodes))
            {
                if (state.PathGoalNodeIndex < 0) Logger.Warn("SerializeTo(): state.PathGoalNodeIndex < 0");

                uint pathGoalNodeIndex = (uint)state.PathGoalNodeIndex;
                success &= Serializer.Transfer(archive, ref pathGoalNodeIndex);

                uint pathNodeCount = (uint)state.PathNodes.Count;
                success &= Serializer.Transfer(archive, ref pathNodeCount);

                if (pathNodeCount > 0)
                {
                    Vector3 previousVertex = Vector3.Zero;
                    foreach (NaviPathNode pathNode in state.PathNodes)
                    {
                        success &= SerializeTo(archive, pathNode, previousVertex);
                        previousVertex = pathNode.Vertex;
                    }
                }
            }

            return success;
        }

        /// <summary>
        /// Deserializes <see cref="LocomotionState"/> from the provided <see cref="Archive"/>
        /// given the specified <see cref="LocomotionMessageFlags"/>.
        /// </summary>
        public static bool SerializeFrom(Archive archive, LocomotionState state, LocomotionMessageFlags flags)
        {
            bool success = true;

            // NOTE: when the RelativeToPreviousState flag is not set and the value is not serialized,
            // it means the value is zero. When the flag IS set and the value is not serialized,
            // it means that the value has not changed relative to some previous locomotion state.

            if (flags.HasFlag(LocomotionMessageFlags.HasLocomotionFlags))
            {
                ulong locomotionFlags = 0;
                success &= Serializer.Transfer(archive, ref locomotionFlags);
                state.LocomotionFlags = (LocomotionFlags)locomotionFlags;       // NOTE: Is this correct? BitSet<12ul>::operator=
            }
            else if (flags.HasFlag(LocomotionMessageFlags.RelativeToPreviousState) == false)
                state.LocomotionFlags = LocomotionFlags.None;

            if (flags.HasFlag(LocomotionMessageFlags.HasMethod))
            {
                uint method = 0;
                success &= Serializer.Transfer(archive, ref method);
                state.Method = (LocomotorMethod)method;
            }
            else if (flags.HasFlag(LocomotionMessageFlags.RelativeToPreviousState) == false)
                state.Method = LocomotorMethod.Ground;

            if (flags.HasFlag(LocomotionMessageFlags.HasMoveSpeed))
            {
                float moveSpeed = 0f;
                success &= Serializer.TransferFloatFixed(archive, ref moveSpeed, 0);
                state.BaseMoveSpeed = moveSpeed;
            }
            else if (flags.HasFlag(LocomotionMessageFlags.RelativeToPreviousState) == false)
                state.BaseMoveSpeed = 0f;

            if (flags.HasFlag(LocomotionMessageFlags.HasHeight))
            {
                uint height = 0;
                success &= Serializer.Transfer(archive, ref height);
                state.Height = (int)height;
            }
            else if (flags.HasFlag(LocomotionMessageFlags.RelativeToPreviousState) == false)
                state.Height = 0;

            if (flags.HasFlag(LocomotionMessageFlags.HasFollowEntityId))
            {
                ulong followEntityId = 0;
                success &= Serializer.Transfer(archive, ref followEntityId);
                state.FollowEntityId = followEntityId;
            }
            else if (flags.HasFlag(LocomotionMessageFlags.RelativeToPreviousState) == false)
                state.FollowEntityId = 0;

            if (flags.HasFlag(LocomotionMessageFlags.HasFollowEntityRange))
            {
                float rangeStart = 0f;
                float rangeEnd = 0f;
                success &= Serializer.TransferFloatFixed(archive, ref rangeStart, 0);
                success &= Serializer.TransferFloatFixed(archive, ref rangeEnd, 0);
                state.FollowEntityRangeStart = rangeStart;
                state.FollowEntityRangeEnd = rangeEnd;
            }
            else if (flags.HasFlag(LocomotionMessageFlags.RelativeToPreviousState) == false)
            {
                state.FollowEntityRangeStart = 0f;
                state.FollowEntityRangeEnd = 0f;
            }

            if (flags.HasFlag(LocomotionMessageFlags.UpdatePathNodes))
            {
                uint pathGoalNodeIndex = 0;
                success &= Serializer.Transfer(archive, ref pathGoalNodeIndex);
                state.PathGoalNodeIndex = (int)pathGoalNodeIndex;

                state.PathNodes.Clear();
                uint pathNodeCount = 0;
                success &= Serializer.Transfer(archive, ref pathNodeCount);

                if (pathNodeCount > 0)
                {
                    Vector3 previousVertex = Vector3.Zero;
                    for (uint i = 0; i < pathNodeCount; i++)
                    {
                        NaviPathNode pathNode = new();
                        success &= SerializeFrom(archive, pathNode, previousVertex);
                        previousVertex = pathNode.Vertex;
                        state.PathNodes.Add(pathNode);
                    }
                }
            }
            else if (flags.HasFlag(LocomotionMessageFlags.RelativeToPreviousState) == false)
            {
                state.PathGoalNodeIndex = 0;
                state.PathNodes.Clear();
            }

            return success;
        }

        /// <summary>
        /// Compares two <see cref="LocomotionState"/> instances and returns <see cref="LocomotionMessageFlags"/> for serialization.
        /// </summary>
        public static LocomotionMessageFlags GetFieldFlags(LocomotionState currentState, LocomotionState previousState, bool withPathNodes)
        {
            if (currentState == null) return LocomotionMessageFlags.NoLocomotionState;

            LocomotionMessageFlags flags = LocomotionMessageFlags.None;

            if (previousState != null)
            {
                // If we have a previous state, it means we are sending a relative update that contains only what has changed
                flags |= LocomotionMessageFlags.RelativeToPreviousState;

                if (currentState.LocomotionFlags != previousState.LocomotionFlags)
                    flags |= LocomotionMessageFlags.HasLocomotionFlags;

                if (currentState.Method != previousState.Method)
                    flags |= LocomotionMessageFlags.HasMethod;

                if (currentState.BaseMoveSpeed != previousState.BaseMoveSpeed)
                    flags |= LocomotionMessageFlags.HasMoveSpeed;

                if (currentState.Height != previousState.Height)
                    flags |= LocomotionMessageFlags.HasHeight;

                if (currentState.FollowEntityId != previousState.FollowEntityId)
                    flags |= LocomotionMessageFlags.HasFollowEntityId;

                if (currentState.FollowEntityRangeStart != previousState.FollowEntityRangeStart)
                    flags |= LocomotionMessageFlags.HasFollowEntityRange;

                if (withPathNodes)
                {
                    bool isLocomoting = currentState.LocomotionFlags.HasFlag(LocomotionFlags.IsLocomoting);
                    bool isLooking = currentState.LocomotionFlags.HasFlag(LocomotionFlags.IsLooking);

                    if (isLocomoting || isLooking)
                        flags |= LocomotionMessageFlags.UpdatePathNodes;
                    else if ((previousState.LocomotionFlags.HasFlag(LocomotionFlags.IsLocomoting) && isLocomoting == false)
                        || (previousState.LocomotionFlags.HasFlag(LocomotionFlags.IsLooking) && isLooking == false))
                    {
                        // If we were locomoting or looking, and no longer are, flag the current locomotion state as finished
                        flags |= LocomotionMessageFlags.LocomotionFinished;
                    }
                }
            }
            else
            {
                // If no previous state is provided, it means we are sending a full locomotion state (we still omit default values)
                if (currentState.LocomotionFlags != LocomotionFlags.None)
                    flags |= LocomotionMessageFlags.HasLocomotionFlags;

                if (currentState.Method != LocomotorMethod.Ground)
                    flags |= LocomotionMessageFlags.HasMethod;

                if (currentState.BaseMoveSpeed != 0f)
                    flags |= LocomotionMessageFlags.HasMoveSpeed;

                if (currentState.Height != 0)
                    flags |= LocomotionMessageFlags.HasHeight;

                if (currentState.FollowEntityId != 0)
                    flags |= LocomotionMessageFlags.HasFollowEntityId;

                if (currentState.FollowEntityRangeStart != 0f)
                    flags |= LocomotionMessageFlags.HasFollowEntityRange;

                if (withPathNodes)
                    flags |= LocomotionMessageFlags.UpdatePathNodes;
            }

            return flags;
        }

        /// <summary>
        /// Compares two <see cref="LocomotionState"/> instances and returns whether or not sync is required.
        /// </summary>
        public static void CompareLocomotionStatesForSync(LocomotionState newState, LocomotionState oldState, out bool syncRequired, out bool pathNodeSyncRequired, bool skipGoalNode)
        {
            pathNodeSyncRequired = CompareLocomotionPathNodesForSync(newState, oldState, skipGoalNode);

            syncRequired = newState.LocomotionFlags != oldState.LocomotionFlags;
            syncRequired |= newState.BaseMoveSpeed != oldState.BaseMoveSpeed;
            syncRequired |= newState.Height != oldState.Height;
            syncRequired |= newState.Method != oldState.Method;
            syncRequired |= newState.FollowEntityId != oldState.FollowEntityId;
            syncRequired |= newState.FollowEntityRangeStart != oldState.FollowEntityRangeStart;
            syncRequired |= newState.FollowEntityRangeEnd != oldState.FollowEntityRangeEnd;
        }

        /// <summary>
        /// Compares <see cref="NaviPathNode"/> collections of two <see cref="LocomotionState"/> instances
        /// and returns <see langword="true"/> if an update is required.
        /// </summary>
        public static bool CompareLocomotionPathNodesForSync(LocomotionState newState, LocomotionState oldState, bool skipGoalNode)
        {
            if ((newState.LocomotionFlags.HasFlag(LocomotionFlags.IsLocomoting) ^ oldState.LocomotionFlags.HasFlag(LocomotionFlags.IsLocomoting))
                || (newState.LocomotionFlags.HasFlag(LocomotionFlags.IsLooking) ^ oldState.LocomotionFlags.HasFlag(LocomotionFlags.IsLooking)))
                return true;

            if (newState.LocomotionFlags.HasFlag(LocomotionFlags.IsLocomoting) == false
                && newState.LocomotionFlags.HasFlag(LocomotionFlags.IsLooking) == false)
                return false;

            if ((newState.PathNodes.Count > oldState.PathNodes.Count)
                || (newState.PathNodes.Count == 0 && oldState.PathNodes.Count > 0))
                return true;

            if (newState.PathNodes.Count > 0)
            {
                int nodesRemaining = newState.PathNodes.Count - newState.PathGoalNodeIndex;
                int newNodeIndex = newState.PathGoalNodeIndex;
                int oldNodeIndex = oldState.PathNodes.Count - nodesRemaining;
                if (skipGoalNode) --nodesRemaining;

                for (int i = 0; i < nodesRemaining; ++i)
                {
                    if (newState.PathNodes[newNodeIndex + i].VertexSide != oldState.PathNodes[oldNodeIndex + i].VertexSide
                    || newState.PathNodes[newNodeIndex + i].Radius != oldState.PathNodes[oldNodeIndex + i].Radius
                    || newState.PathNodes[newNodeIndex + i].Vertex != oldState.PathNodes[oldNodeIndex + i].Vertex)
                        return true;
                }
            }

            return false;
        }
    }
}
