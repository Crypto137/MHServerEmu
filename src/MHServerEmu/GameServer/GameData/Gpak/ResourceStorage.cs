using MHServerEmu.Common;
using MHServerEmu.GameServer.GameData.Gpak.FileFormats;

namespace MHServerEmu.GameServer.GameData.Gpak
{
    public class ResourceStorage : GpakStorage
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public Dictionary<string, Cell> CellDict { get; } = new();
        public Dictionary<string, District> DistrictDict { get; } = new();

        public ResourceStorage(GpakFile gpakFile)
        {
            foreach (GpakEntry entry in gpakFile.Entries)
            {
                switch (Path.GetExtension(entry.FilePath))
                {
                    case ".cell":
                        CellDict.Add(entry.FilePath, new(entry.Data));
                        break;                        

                    case ".district":
                        DistrictDict.Add(entry.FilePath, new(entry.Data));
                        break;
                }
            }

            Logger.Info($"Parsed {CellDict.Count} cells");
            Logger.Info($"Parsed {DistrictDict.Count} districts");
        }

        public override bool Verify()
        {
            return CellDict.Count > 0
                && DistrictDict.Count > 0;
        }

        public override void Export()
        {
            SerializeDictAsJson(CellDict);
            SerializeDictAsJson(DistrictDict);
        }
    }
}
