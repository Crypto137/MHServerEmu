using MHServerEmu.Common;
using MHServerEmu.GameServer.GameData.Gpak.FileFormats;

namespace MHServerEmu.GameServer.GameData.Gpak
{
    public class ResourceStorage : GpakStorage
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public Dictionary<ulong, string> DirectoryDict { get; } = new();

        public Dictionary<string, Cell> CellDict { get; } = new();
        public Dictionary<string, District> DistrictDict { get; } = new();
        public Dictionary<string, Encounter> EncounterDict { get; } = new();
        public Dictionary<string, PropSet> PropSetDict { get; } = new();
        public Dictionary<string, Prop> PropDict { get; } = new();

        public ResourceStorage(GpakFile gpakFile)
        {
            foreach (GpakEntry entry in gpakFile.Entries)
            {
                DirectoryDict.Add(HashHelper.HashPath($"&{entry.FilePath.ToLower()}"), entry.FilePath);

                switch (Path.GetExtension(entry.FilePath))
                {
                    case ".cell":
                        CellDict.Add(entry.FilePath, new(entry.Data));
                        break;                        
                    case ".district":
                        DistrictDict.Add(entry.FilePath, new(entry.Data));
                        break;
                    case ".encounter":
                        EncounterDict.Add(entry.FilePath, new(entry.Data));
                        break;
                    case ".propset":
                        PropSetDict.Add(entry.FilePath, new(entry.Data));
                        break;
                    case ".prop":
                        PropDict.Add(entry.FilePath, new(entry.Data));
                        break;
                }
            }

            Logger.Info($"Parsed {CellDict.Count} cells");
            Logger.Info($"Parsed {DistrictDict.Count} districts");
            Logger.Info($"Parsed {EncounterDict.Count} encounters");
            Logger.Info($"Parsed {PropSetDict.Count} prop sets");
            Logger.Info($"Parsed {PropDict.Count} props");
        }

        public override bool Verify()
        {
            return CellDict.Count > 0
                && DistrictDict.Count > 0
                && EncounterDict.Count > 0
                && PropSetDict.Count > 0
                && PropDict.Count > 0;
        }

        public override void Export()
        {
            SerializeDictAsJson(CellDict);
            SerializeDictAsJson(DistrictDict);
            SerializeDictAsJson(EncounterDict);
            SerializeDictAsJson(PropSetDict);
            SerializeDictAsJson(PropDict);
        }
    }
}
