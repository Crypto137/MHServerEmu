using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.Network
{
    // NOTE: The client uses C++ templates (RepVar<T>) for these to avoid repetition.
    // We have to do a bunch of copying and pasting to achieve the same result without losing performance.

    // NOTE: This per-variable replication system was much more heavily used before version 1.25.
    // By version 1.52 only three types (int, ulong, string) are still in use,
    // although ReplicatedPropertyCollection is also part of the same overall system.

    public struct RepInt : IArchiveMessageHandler, ISerialize
    {
        public ulong ReplicationId { get; set; } = 0;
        public int Value { get; set; } = 0;

        public RepInt() { }

        public RepInt(ulong replicationId, int value)
        {
            ReplicationId = replicationId;
            Value = value;
        }

        public override string ToString() => $"[{ReplicationId}] {Value}";

        public bool Serialize(Archive archive)
        {
            bool success = true;

            ulong replicationId = ReplicationId;
            success &= Serializer.Transfer(archive, ref replicationId);

            int value = Value;
            success &= Serializer.Transfer(archive, ref value);

            return success;
        }
    }

    public struct RepULong : IArchiveMessageHandler, ISerialize
    {
        public ulong ReplicationId { get; set; } = 0;
        public ulong Value { get; set; } = 0;

        public RepULong() { }

        public RepULong(ulong replicationId, ulong value)
        {
            ReplicationId = replicationId;
            Value = value;
        }

        public override string ToString() => $"[{ReplicationId}] {Value}";

        public bool Serialize(Archive archive)
        {
            bool success = true;

            ulong replicationId = ReplicationId;
            success &= Serializer.Transfer(archive, ref replicationId);

            ulong value = Value;
            success &= Serializer.Transfer(archive, ref value);

            return success;
        }
    }

    public struct RepString : IArchiveMessageHandler, ISerialize
    {
        public ulong ReplicationId { get; set; } = 0;
        public string Value { get; set; } = string.Empty;

        public RepString() { }

        public RepString(ulong replicationId, string value)
        {
            ReplicationId = replicationId;
            Value = value;
        }

        public override string ToString() => $"[{ReplicationId}] {Value}";

        public bool Serialize(Archive archive)
        {
            bool success = true;

            ulong replicationId = ReplicationId;
            success &= Serializer.Transfer(archive, ref replicationId);

            string value = Value;
            success &= Serializer.Transfer(archive, ref value);

            return success;
        }
    }
}
