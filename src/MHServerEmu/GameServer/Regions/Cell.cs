using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.Regions
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
    }
}
