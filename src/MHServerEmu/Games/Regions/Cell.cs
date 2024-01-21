using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.Regions
{
    public class Cell
    {
        public uint Id { get; }
        public PrototypeId PrototypeId { get; }
        public Vector3 PositionInArea { get; }
        public List<ReservedSpawn> EncounterList { get; } = new();

        public Cell(uint id, PrototypeId prototypeId, Vector3 positionInArea)
        {
            Id = id;
            PrototypeId = prototypeId;
            PositionInArea = positionInArea;
        }

        public void AddEncounter(ulong asset, uint id, bool useMarkerOrientation) => EncounterList.Add(new(asset, id, useMarkerOrientation));
        public void AddEncounter(ReservedSpawn reservedSpawn) => EncounterList.Add(reservedSpawn);

        #region Enums

        [AssetEnum((int)None)]      // DRAG/RegionGenerators/Edges.type
        public enum Type
        {
            None = 0,
            N = 1,
            E = 2,
            S = 4,
            W = 8,
            NS = 5,
            EW = 10,
            NE = 3,
            NW = 9,
            ES = 6,
            SW = 12,
            ESW = 14,
            NSW = 13,
            NEW = 11,
            NES = 7,
            NESW = 15,
            NESWdNW = 159,
            NESWdNE = 207,
            NESWdSW = 63,
            NESWdSE = 111,
            NESWcN = 351,
            NESWcE = 303,
            NESWcS = 159,
            NESWcW = 207,
        }

        [AssetEnum((int)WideNESW)]      // DRAG/CellWallTypes.type
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
        }

        #endregion
    }
}
