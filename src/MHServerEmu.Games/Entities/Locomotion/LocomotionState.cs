using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData.Prototypes;

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
        ForwardMove = 1ul << 8,
        MoveTo = 1ul << 9,
        IsSyncMoving = 1ul << 10,
        IgnoresWorldCollision = 1ul << 11,
    }

    public class LocomotionState
    {
        public LocomotionFlags LocomotionFlags { get; set; }
        public LocomotorMethod Method { get; set; }
        public float BaseMoveSpeed { get; set; }
        public int Height { get; set; }
        public ulong FollowEntityId { get; set; }
        public Vector2 FollowEntityRange { get; set; }
        public uint PathGoalNodeIndex { get; set; }     // This was signed in old protocols
        public List<LocomotionPathNode> LocomotionPathNodes { get; set; }

        public LocomotionState()
        {
            Method = LocomotorMethod.Default;
            LocomotionPathNodes = new();
        }

        public LocomotionState(CodedInputStream stream, LocomotionMessageFlags flags)
        {
            LocomotionPathNodes = new();

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
                FollowEntityRange = new(stream.ReadRawZigZagFloat(0), stream.ReadRawZigZagFloat(0));

            if (flags.HasFlag(LocomotionMessageFlags.UpdatePathNodes))
            {
                PathGoalNodeIndex = stream.ReadRawVarint32();
                int count = (int)stream.ReadRawVarint64();
                for (int i = 0; i < count; i++)
                    LocomotionPathNodes.Add(new(stream));
            }
        }

        public LocomotionState(float baseMoveSpeed)
        {
            BaseMoveSpeed = baseMoveSpeed;
            LocomotionPathNodes = new();
        }

        public LocomotionState(LocomotionFlags locomotionFlags, LocomotorMethod method, float baseMoveSpeed, int height,
            ulong followEntityId, Vector2 followEntityRange, uint pathGoalNodeIndex, List<LocomotionPathNode> locomotionPathNodes)
        {
            LocomotionFlags = locomotionFlags;
            Method = method;
            BaseMoveSpeed = baseMoveSpeed;
            Height = height;
            FollowEntityId = followEntityId;
            FollowEntityRange = followEntityRange;
            PathGoalNodeIndex = pathGoalNodeIndex;
            LocomotionPathNodes = locomotionPathNodes;
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
                FollowEntityRange.Encode(stream, 0);

            if (flags.HasFlag(LocomotionMessageFlags.UpdatePathNodes))
            {
                stream.WriteRawVarint32(PathGoalNodeIndex);
                stream.WriteRawVarint64((ulong)LocomotionPathNodes.Count);
                foreach (LocomotionPathNode naviVector in LocomotionPathNodes) naviVector.Encode(stream);
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
            sb.AppendLine($"FollowEntityRange: {FollowEntityRange}");
            sb.AppendLine($"PathGoalNodeIndex: {PathGoalNodeIndex}");
            for (int i = 0; i < LocomotionPathNodes.Count; i++) sb.AppendLine($"LocomotionPathNode{i}: {LocomotionPathNodes[i]}");
            return sb.ToString();
        }

        internal void StateFrom(LocomotionState locomotionState)
        {
            throw new NotImplementedException();
        }
    }
}
