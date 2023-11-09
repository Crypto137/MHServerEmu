using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;

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
    }
}
