using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.Regions
{
    public class Cell
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

        public enum Type {
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
	}
}
