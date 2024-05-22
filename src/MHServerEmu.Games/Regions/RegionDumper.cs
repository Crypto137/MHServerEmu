using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Games.Regions
{
    /// <summary>
    /// Dumps region cell layout to JSON.
    /// </summary>
    public static class RegionDumper
    {
        public static void DumpRegion(Region region)
        {
            RegionDump regionDump = new();

            foreach (var areaKvp in region.Areas)
            {
                AreaDump areaDump = new();
                regionDump.Add(areaKvp.Key, areaDump);

                foreach (var cellKvp in areaKvp.Value.Cells)
                    areaDump.Add(cellKvp.Key, new(cellKvp.Value.CellProto.ToString(), new(cellKvp.Value.AreaPosition)));
            }

            FileHelper.SerializeJson(Path.Combine(FileHelper.ServerRoot, "RegionDumps", $"{region.PrototypeName}_{region.RandomSeed}.json"),
                regionDump, new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });
        }

        private class RegionDump : Dictionary<uint, AreaDump> { }
        private class AreaDump : Dictionary<uint, CellDump> { }

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
