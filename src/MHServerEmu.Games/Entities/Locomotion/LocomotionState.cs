using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
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
        Flag2                   = 1 << 2,
        HasLocomotionFlags      = 1 << 3,
        HasMethod               = 1 << 4,
        UpdatePathNodes         = 1 << 5,
        Flag6                   = 1 << 6,
        HasMoveSpeed            = 1 << 7,
        HasHeight               = 1 << 8,
        HasFollowEntityId       = 1 << 9,
        HasFollowEntityRange    = 1 << 10,
        HasEntityPrototypeId    = 1 << 11
    }

    [Flags]
    public enum LocomotionFlags : ulong
    {
        None = 0,
        IsLocomoting = 1ul << 0,
        IsWalking = 1ul << 1,
        IsLooking = 1ul << 2,
        SkipCurrentSpeedRate = 1ul << 3,
        LocomotionNoEntityCollide = 1ul << 4,
        IsMovementPower = 1ul << 5,
        DisableOrientation = 1ul << 6,
        IsDrivingMovementMode = 1ul << 7,
        MoveForward = 1ul << 8,
        MoveTo = 1ul << 9,
        IsSyncMoving = 1ul << 10,
        IgnoresWorldCollision = 1ul << 11,
    }

    public class LocomotionState // TODO replace to struct
    {
        public LocomotionFlags LocomotionFlags { get; set; }
        public LocomotorMethod Method { get; set; }
        public float BaseMoveSpeed { get; set; }
        public int Height { get; set; }
        public ulong FollowEntityId { get; set; }
        public float FollowEntityRangeStart { get; set; }
        public float FollowEntityRangeEnd { get; set; }
        public int PathGoalNodeIndex { get; set; }     // This was signed in old protocols
        public List<NaviPathNode> PathNodes { get; set; }

        public LocomotionState()
        {
            Method = LocomotorMethod.Default;
            PathNodes = new();
        }

        public LocomotionState(CodedInputStream stream, LocomotionMessageFlags flags)
        {
            PathNodes = new();

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
                for (int i = 0; i < count; i++)
                    PathNodes.Add(new(stream));
            }
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
                foreach (NaviPathNode naviVector in PathNodes) naviVector.Encode(stream);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"LocomotionFlags: {LocomotionFlags}");
            sb.AppendLine($"Method: 0x{Method:X}");
            sb.AppendLine($"BaseMoveSpeed: {BaseMoveSpeed}");
            sb.AppendLine($"Height: 0x{Height:X}");
            sb.AppendLine($"FollowEntityId: {FollowEntityId}");
            sb.AppendLine($"FollowEntityRangeStart: {FollowEntityRangeStart}");
            sb.AppendLine($"FollowEntityRangeEnd: {FollowEntityRangeEnd}");
            sb.AppendLine($"PathGoalNodeIndex: {PathGoalNodeIndex}");
            for (int i = 0; i < PathNodes.Count; i++) sb.AppendLine($"PathNode{i}: {PathNodes[i]}");
            return sb.ToString();
        }

        public void StateFrom(LocomotionState locomotionState)
        {
            // TODO Replace to SerializeFrom
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
            PathNodes = new(other.PathNodes);
        }
    }
}
