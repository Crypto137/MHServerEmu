using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.Common;

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

    public class LocomotionState
    {
        public UInt64Flags LocomotionFlags { get; set; }
        public uint Method { get; set; }
        public float MoveSpeed { get; set; }
        public uint Height { get; set; }
        public ulong FollowEntityId { get; set; }
        public Vector2 FollowEntityRange { get; set; }
        public uint PathGoalNodeIndex { get; set; }     // This was signed in old protocols
        public LocomotionPathNode[] LocomotionPathNodes { get; set; } = Array.Empty<LocomotionPathNode>();

        public LocomotionState(CodedInputStream stream, LocomotionMessageFlags flags)
        {
            if (flags.HasFlag(LocomotionMessageFlags.HasLocomotionFlags))
                LocomotionFlags = (UInt64Flags)stream.ReadRawVarint64();

            if (flags.HasFlag(LocomotionMessageFlags.HasMethod))
                Method = stream.ReadRawVarint32();

            if (flags.HasFlag(LocomotionMessageFlags.HasMoveSpeed))
                MoveSpeed = stream.ReadRawZigZagFloat(0);

            if (flags.HasFlag(LocomotionMessageFlags.HasHeight))
                Height = stream.ReadRawVarint32();

            if (flags.HasFlag(LocomotionMessageFlags.HasFollowEntityId))
                FollowEntityId = stream.ReadRawVarint64();

            if (flags.HasFlag(LocomotionMessageFlags.HasFollowEntityRange))
                FollowEntityRange = new(stream.ReadRawZigZagFloat(0), stream.ReadRawZigZagFloat(0));

            if (flags.HasFlag(LocomotionMessageFlags.UpdatePathNodes))
            {
                PathGoalNodeIndex = stream.ReadRawVarint32();
                LocomotionPathNodes = new LocomotionPathNode[stream.ReadRawVarint64()];
                for (int i = 0; i < LocomotionPathNodes.Length; i++)
                    LocomotionPathNodes[i] = new(stream);
            }
        }

        public LocomotionState(float moveSpeed)
        {
            MoveSpeed = moveSpeed;
        }

        public LocomotionState(UInt64Flags locomotionFlags, uint method, float moveSpeed, uint height,
            ulong followEntityId, Vector2 followEntityRange, uint pathGoalNodeIndex, LocomotionPathNode[] locomotionPathNodes)
        {
            LocomotionFlags = locomotionFlags;
            Method = method;
            MoveSpeed = moveSpeed;
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
                stream.WriteRawVarint32(Method);

            if (flags.HasFlag(LocomotionMessageFlags.HasMoveSpeed))
                stream.WriteRawZigZagFloat(MoveSpeed, 0);

            if (flags.HasFlag(LocomotionMessageFlags.HasHeight))
                stream.WriteRawVarint32(Height);

            if (flags.HasFlag(LocomotionMessageFlags.HasFollowEntityId))
                stream.WriteRawVarint64(FollowEntityId);

            if (flags.HasFlag(LocomotionMessageFlags.HasFollowEntityRange))
                FollowEntityRange.Encode(stream, 0);

            if (flags.HasFlag(LocomotionMessageFlags.UpdatePathNodes))
            {
                stream.WriteRawVarint32(PathGoalNodeIndex);
                stream.WriteRawVarint64((ulong)LocomotionPathNodes.Length);
                foreach (LocomotionPathNode naviVector in LocomotionPathNodes) naviVector.Encode(stream);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"LocomotionFlags: {LocomotionFlags}");
            sb.AppendLine($"Method: 0x{Method:X}");
            sb.AppendLine($"MoveSpeed: {MoveSpeed}");
            sb.AppendLine($"Height: 0x{Height:X}");
            sb.AppendLine($"FollowEntityId: {FollowEntityId}");
            sb.AppendLine($"FollowEntityRange: {FollowEntityRange}");
            sb.AppendLine($"PathGoalNodeIndex: {PathGoalNodeIndex}");
            for (int i = 0; i < LocomotionPathNodes.Length; i++) sb.AppendLine($"LocomotionPathNode{i}: {LocomotionPathNodes[i]}");
            return sb.ToString();
        }
    }
}
