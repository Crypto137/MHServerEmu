using MHServerEmu.Common;
using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer.GameData.Gpak.FileFormats;

namespace MHServerEmu.GameServer.GameData.Gpak
{
    public class ResourceStorage : GpakStorage
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public Dictionary<ulong, string> DirectoryDict { get; } = new();

        public Dictionary<string, CellPrototype> CellDict { get; } = new();
        public Dictionary<string, District> DistrictDict { get; } = new();
        public Dictionary<string, Encounter> EncounterDict { get; } = new();
        public Dictionary<string, PropSet> PropSetDict { get; } = new();
        public Dictionary<string, Prop> PropDict { get; } = new();
        public Dictionary<string, FileFormats.UI> UIDict { get; } = new();

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
                    case ".ui":
                        UIDict.Add(entry.FilePath, new(entry.Data));
                        break;
                }
            }

            Logger.Info($"Parsed {CellDict.Count} cells");
            Logger.Info($"Parsed {DistrictDict.Count} districts");
            Logger.Info($"Parsed {EncounterDict.Count} encounters");
            Logger.Info($"Parsed {PropSetDict.Count} prop sets");
            Logger.Info($"Parsed {PropDict.Count} props");
            Logger.Info($"Parsed {UIDict.Count} UIs");
        }

        public override bool Verify()
        {
            return CellDict.Count > 0
                && DistrictDict.Count > 0
                && EncounterDict.Count > 0
                && PropSetDict.Count > 0
                && PropDict.Count > 0
                && UIDict.Count > 0;
        }

        public override void Export()
        {
            // Set up json serializer
            _jsonSerializerOptions.Converters.Add(new MarkerPrototypeConverter());
            _jsonSerializerOptions.Converters.Add(new NaviPatchPrototypeConverter());
            _jsonSerializerOptions.Converters.Add(new UIPanelPrototypeConverter());

            SerializeDictAsJson(CellDict);
            SerializeDictAsJson(DistrictDict);
            SerializeDictAsJson(EncounterDict);
            SerializeDictAsJson(PropSetDict);
            SerializeDictAsJson(PropDict);
            SerializeDictAsJson(UIDict);
        }
    }
}
