using MHServerEmu.Common;
using MHServerEmu.GameServer.GameData.Gpak.FileFormats;

namespace MHServerEmu.GameServer.GameData.Gpak
{
    public class ResourceStorage : GpakStorage
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public Dictionary<string, District> DistrictDict { get; } = new();

        public ResourceStorage(GpakFile gpakFile)
        {
            foreach (GpakEntry entry in gpakFile.Entries)
            {
                switch (Path.GetExtension(entry.FilePath))
                {
                    case ".district":
                        DistrictDict.Add(entry.FilePath, new(entry.Data));
                        break;
                }
            }

            Logger.Info($"Parsed {DistrictDict.Count} districts");
        }

        public override bool Verify()
        {
            return DistrictDict.Count > 0;
        }

        public override void Export()
        {
            SerializeDictAsJson(DistrictDict);
        }
    }
}
