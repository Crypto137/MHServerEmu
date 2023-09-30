namespace MHServerEmu.Common
{
    public enum IdType : byte
    {
        Generic = 0,
        Account = 1,
        Session = 2,
        Game = 3,
        Region = 4
    }

    public static class IdGenerator
    {
        private static ushort _count = 0;   // Potential TODO: add separate counters for each id type to further reduce collisions

        /// <summary>
        /// Generate an id that can be used for various purposes.
        /// </summary>
        /// <param name="type">Id type enum.</param>
        /// <param name="reserved">Additional metadata (currently unused).</param>
        /// <returns>An id encoded as ulong.</returns>
        public static ulong Generate(IdType type, byte reserved = 0)
        {
            // The general idea is similar to Snowflake ids (see here: https://en.wikipedia.org/wiki/Snowflake_ID)
            // Currently our id is a ulong of 4 values spliced together:
            // byte type        - id type enum
            // byte reserved    - currently unused (can be useful for additional metadata if we need it)
            // uint timestamp   - unix timestamp in seconds
            // ushort count     - counter to avoid collisions (this allows us to generate up to 65535 unique ids per second)

            uint timestamp = (uint)((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();

            ulong id = 0;
            id |= (ulong)type << 56;
            id |= (ulong)reserved << 48;
            id |= (ulong)timestamp << 16;
            id |= _count++;

            return id;
        }

        /// <summary>
        /// Parse type, timestamp, and other data from an encoded ulong id.
        /// </summary>
        /// <param name="id">An id encoded as ulong.</param>
        /// <returns>A struct that contains parsed values.</returns>
        public static Id Parse(ulong id)
        {
            IdType type = (IdType)(id >> 56);
            byte reserved = (byte)((id >> 48) & 0xFF);
            uint timestamp = (uint)((id >> 16) & 0xFFFFFFFF);
            ushort count = (ushort)(id & 0xFFFF);

            return new(type, reserved, timestamp, count);
        }

        public readonly struct Id
        {
            public IdType Type { get; }
            public byte Reserved { get; }
            public uint Timestamp { get; }
            public ushort Count { get; }

            public Id(IdType type, byte reserved, uint timestamp, ushort count)
            {
                Type = type;
                Reserved = reserved;
                Timestamp = timestamp;
                Count = count;
            }

            public override string ToString()
            {
                return $"Type: {Type} Reserved: 0x{Reserved:X} Timestamp: {Timestamp} Count: {Count}";
            }
        }
    }
}
