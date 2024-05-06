using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
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
        HasEntityPrototypeId    = 1 << 11
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

    public class LocomotionState // TODO: Change to struct? Consider how this is used before doing it
    {
        // NOTE: Due to how LocomotionState serialization is implemented, we should be able to
        // get away with using C# auto properties instead of private fields.
        public LocomotionFlags LocomotionFlags { get; set; }
        public LocomotorMethod Method { get; set; }
        public float BaseMoveSpeed { get; set; }
        public int Height { get; set; }
        public ulong FollowEntityId { get; set; }
        public float FollowEntityRangeStart { get; set; }
        public float FollowEntityRangeEnd { get; set; }
        public int PathGoalNodeIndex { get; set; }
        public List<NaviPathNode> PathNodes { get; set; } = new();

        public LocomotionState()
        {
            Method = LocomotorMethod.Default;
            PathNodes = new();
        }

        public LocomotionState(float baseMoveSpeed)
        {
            BaseMoveSpeed = baseMoveSpeed;
            PathNodes = new();
        }

        public LocomotionState(LocomotionState other)
        {
            Set(other);
        }

        // NOTE: LocomotionState serialization implementation is similar to what PowerCollection is doing
        // (i.e. separate static serialization methods for serialization and deserialization rather than
        // combined ISerialize implementention we have seen everywhere else).

        public static bool SerializeTo(Archive archive, NaviPathNode pathNode, Vector3 previousVertex)
        {
            throw new NotImplementedException();
        }

        public static bool SerializeFrom(Archive archive, NaviPathNode pathNode, Vector3 previousVertex)
        {
            throw new NotImplementedException();
        }

        public static bool SerializeTo(Archive archive, LocomotionState state, LocomotionMessageFlags flags)
        {
            throw new NotImplementedException();
        }

        public static bool SerializeFrom(Archive archive, LocomotionState state, LocomotionMessageFlags flags)
        {
            throw new NotImplementedException();
        }

        public void Decode(CodedInputStream stream, LocomotionMessageFlags flags)
        {
            PathNodes.Clear();

            if (flags.HasFlag(LocomotionMessageFlags.HasLocomotionFlags))
                LocomotionFlags = (LocomotionFlags)stream.ReadRawVarint64();

            if (flags.HasFlag(LocomotionMessageFlags.HasMethod))
                Method = (LocomotorMethod)stream.ReadRawVarint32();

            if (flags.HasFlag(LocomotionMessageFlags.HasMoveSpeed))
                BaseMoveSpeed = stream.ReadRawZigZagFloat(0);

            if (flags.HasFlag(LocomotionMessageFlags.HasHeight))
                Height = (int)stream.ReadRawVarint32();

            if (flags.HasFlag(LocomotionMessageFlags.HasFollowEntityId))
                FollowEntityId = stream.ReadRawVarint64();

            if (flags.HasFlag(LocomotionMessageFlags.HasFollowEntityRange))
            {
                FollowEntityRangeStart = stream.ReadRawZigZagFloat(0);
                FollowEntityRangeEnd = stream.ReadRawZigZagFloat(0);
            }

            if (flags.HasFlag(LocomotionMessageFlags.UpdatePathNodes))
            {
                PathGoalNodeIndex = (int)stream.ReadRawVarint32();
                int count = (int)stream.ReadRawVarint64();

                Vector3 previousVertex = Vector3.Zero;
                for (int i = 0; i < count; i++)
                {
                    NaviPathNode pathNode = new();
                    pathNode.Decode(stream, previousVertex);
                    previousVertex = pathNode.Vertex;
                    PathNodes.Add(pathNode);
                }
            }
        }

        public void Encode(CodedOutputStream stream, LocomotionMessageFlags flags)
        {
            if (flags.HasFlag(LocomotionMessageFlags.HasLocomotionFlags))
                stream.WriteRawVarint64((ulong)LocomotionFlags);

            if (flags.HasFlag(LocomotionMessageFlags.HasMethod))
                stream.WriteRawVarint32((uint)Method);

            if (flags.HasFlag(LocomotionMessageFlags.HasMoveSpeed))
                stream.WriteRawZigZagFloat(BaseMoveSpeed, 0);

            if (flags.HasFlag(LocomotionMessageFlags.HasHeight))
                stream.WriteRawVarint32((uint)Height);

            if (flags.HasFlag(LocomotionMessageFlags.HasFollowEntityId))
                stream.WriteRawVarint64(FollowEntityId);

            if (flags.HasFlag(LocomotionMessageFlags.HasFollowEntityRange))
            {
                stream.WriteRawZigZagFloat(FollowEntityRangeStart, 0);
                stream.WriteRawZigZagFloat(FollowEntityRangeEnd, 0);
            }

            if (flags.HasFlag(LocomotionMessageFlags.UpdatePathNodes))
            {
                stream.WriteRawVarint32((uint)PathGoalNodeIndex);
                stream.WriteRawVarint64((ulong)PathNodes.Count);

                Vector3 previousVertex = Vector3.Zero;
                foreach (NaviPathNode naviVector in PathNodes)
                {
                    naviVector.Encode(stream, previousVertex);
                    previousVertex = naviVector.Vertex;
                }
            }
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

        public void StateFrom(LocomotionState locomotionState)
        {
            // TODO Replace with SerializeFrom()
            Set(locomotionState);
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
            //PathNodes = new(other.PathNodes);
            PathNodes.Clear();
            PathNodes.AddRange(other.PathNodes);
        }
    }
}
