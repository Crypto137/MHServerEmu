using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.Regions
{
    public partial class Cell
    {
        public uint Id { get; }
        public ulong PrototypeId { get; }
        public Vector3 PositionInArea { get; }
        public List<ReservedSpawn> EncounterList { get; } = new();

        public Cell(uint id, ulong prototypeId, Vector3 positionInArea)
        {
            Id = id;
            PrototypeId = prototypeId;
            PositionInArea = positionInArea;
        }

        public void AddEncounter(ulong asset, uint id, bool useMarkerOrientation) => EncounterList.Add(new(asset, id, useMarkerOrientation));
        public void AddEncounter(ReservedSpawn reservedSpawn) => EncounterList.Add(reservedSpawn);

        [Flags]
        public enum Type
        {
            None = 0,        // 0000
            N = 1,           // 0001
            E = 2,           // 0010
            S = 4,           // 0100
            W = 8,           // 1000
            NS = 5,          // 0101
            EW = 10,         // 1010
            NE = 3,          // 0011
            NW = 9,          // 1001
            ES = 6,          // 0110
            SW = 12,         // 1100
            ESW = 14,        // 1110
            NSW = 13,        // 1101
            NEW = 11,        // 1011
            NES = 7,         // 0111
            NESW = 15,       // 1111
            // Dot
            dN = 128,        // 1000 0000
            dE = 64,         // 0100 0000
            dS = 32,         // 0010 0000
            dW = 16,         // 0001 0000
            dNE = 192,       // 1100 0000
            dSE = 96,        // 0110 0000
            dSW = 48,        // 0011 0000
            dNW = 144,       // 1001 0000
            NESWdNW = 159,   // 1001 1111
            NESWdNE = 207,   // 1100 1111
            NESWdSW = 63,    // 0011 1111
            NESWdSE = 111,   // 0110 1111
            dNESW = 240,     // 1111 0000
            DotMask = 480,   // 1 1110 0000 !!! Error Mask
            // c
            NESWcN = 351,    // 1 0101 1111
            NESWcE = 303,    // 1 0010 1111
            NESWcS = 159,    // 0 0111 1111
            NESWcW = 207,    // 0 1100 1111
        }

        [Flags]
        public enum Walls
        {
            None = 0,   // 000000000
            N = 1,      // 000000001
            NE = 2,     // 000000010
            E = 4,      // 000000100
            SE = 8,     // 000001000
            S = 16,     // 000010000
            SW = 32,    // 000100000
            W = 64,     // 001000000
            NW = 128,   // 010000000
            C = 256,    // 100000000
            All = 511,  // 111111111
        }

        [Flags]
        public enum WallGroup
        {
            N = 254,
            E = 251,
            S = 239,
            W = 191,
            NE = 250,
            ES = 235,
            SW = 175,
            NW = 190,
            NS = 238,
            EW = 187,
            NES = 234,
            ESW = 171,
            NSW = 174,
            NEW = 186,
            NESW = 170,
            WideNE = 248,
            WideES = 227,
            WideSW = 143,
            WideNW = 62,
            WideNES = 224,
            WideESW = 131,
            WideNSW = 14,
            WideNEW = 56,
            WideNESW = 0,
            WideNESWcN = 130,
            WideNESWcE = 10,
            WideNESWcS = 40,
            WideNESWcW = 160,
        }

        [Flags]
        public enum Filler
        {
            N = 1,
            NE = 2,
            E = 4,
            SE = 8,
            S = 16,
            SW = 32,
            W = 64,
            NW = 128,
            C = 256,
            None = 0,
        }
    }
}
