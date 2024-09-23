using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Regions
{
    public static class RegionHelper
    {
        private static readonly RegionPrototypeId[] PatrolRegions = new RegionPrototypeId[]
        {
            RegionPrototypeId.XManhattanRegion1to60,
            RegionPrototypeId.XManhattanRegion60Cosmic,
            RegionPrototypeId.BrooklynPatrolRegionL60,
            RegionPrototypeId.BrooklynPatrolRegionL60Cosmic,
            RegionPrototypeId.UpperMadripoorRegionL60,
            RegionPrototypeId.UpperMadripoorRegionL60Cosmic,
        };

        public static bool TEMP_IsPatrolRegion(PrototypeId regionProtoRef)
        {
            return PatrolRegions.Contains((RegionPrototypeId)regionProtoRef);
        }

        public static void DumpRegionToJson(Region region)
        {
            RegionDump regionDump = new();

            foreach (var areaKvp in region.Areas)
            {
                AreaDump areaDump = new(areaKvp.Value.PrototypeDataRef, areaKvp.Value.Origin);
                regionDump.Add(areaKvp.Key, areaDump);

                foreach (var cellKvp in areaKvp.Value.Cells)
                    areaDump.Cells.Add(cellKvp.Key, new(cellKvp.Value.Prototype.ToString(), cellKvp.Value.AreaPosition));
            }

            FileHelper.SerializeJson(Path.Combine(FileHelper.ServerRoot, "RegionDumps", $"{region.PrototypeName}_{region.RandomSeed}.json"),
                regionDump, new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });
        }

        private class RegionDump : Dictionary<uint, AreaDump> { }

        private class AreaDump
        {
            public ulong PrototypeDataRef { get; set; }
            public Vector3 Origin { get; set; }
            public SortedDictionary<uint, CellDump> Cells { get; set; } = new();

            public AreaDump(PrototypeId areaProtoRef, Vector3 origin)
            {
                PrototypeDataRef = (ulong)areaProtoRef;
                Origin = origin;
            }
        }

        private class CellDump
        {
            public string PrototypeName { get; set; }
            public Vector3 Position { get; set; }

            public CellDump(string prototypeName, Vector3 position)
            {
                PrototypeName = prototypeName;
                Position = position;
            }
        }
    }
}
