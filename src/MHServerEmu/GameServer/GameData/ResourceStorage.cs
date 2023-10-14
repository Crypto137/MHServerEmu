using System.Text.Json;
using MHServerEmu.Common;
using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer.GameData.Gpak;
using MHServerEmu.GameServer.GameData.JsonOutput;
using MHServerEmu.GameServer.GameData.Prototypes;

namespace MHServerEmu.GameServer.GameData
{
    public class ResourceStorage
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public Dictionary<string, CellPrototype> CellDict { get; } = new();
        public Dictionary<string, DistrictPrototype> DistrictDict { get; } = new();
        public Dictionary<string, EncounterPrototype> EncounterDict { get; } = new();
        public Dictionary<string, PropSetPrototype> PropSetDict { get; } = new();
        public Dictionary<string, PropPrototype> PropDict { get; } = new();
        public Dictionary<string, UIPrototype> UIDict { get; } = new();

        public ResourceStorage(GpakFile gpakFile)
        {
            foreach (GpakEntry entry in gpakFile.Entries)
            {
                ulong dataId = HashHelper.HashPath($"&{entry.FilePath.ToLower()}");
                GameDatabase.PrototypeRefManager.AddDataRef(dataId, entry.FilePath);

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

        public bool Verify()
        {
            return CellDict.Count > 0
                && DistrictDict.Count > 0
                && EncounterDict.Count > 0
                && PropSetDict.Count > 0
                && PropDict.Count > 0
                && UIDict.Count > 0;
        }

        public void Export()
        {
            // Set up json serializer
            JsonSerializerOptions options = new() { WriteIndented = true };
            options.Converters.Add(new MarkerPrototypeConverter());
            options.Converters.Add(new NaviPatchPrototypeConverter());
            options.Converters.Add(new UIPanelPrototypeConverter());

            SerializeDictAsJson(CellDict, options);
            SerializeDictAsJson(DistrictDict, options);
            SerializeDictAsJson(EncounterDict, options);
            SerializeDictAsJson(PropSetDict, options);
            SerializeDictAsJson(PropDict, options);
            SerializeDictAsJson(UIDict, options);
        }

        private void SerializeDictAsJson<T>(Dictionary<string, T> dict, JsonSerializerOptions options)
        {
            foreach (var kvp in dict)
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "GPAK", "Export", $"{kvp.Key}.json");
                string dir = Path.GetDirectoryName(path);
                if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);

                File.WriteAllText(path, JsonSerializer.Serialize((object)kvp.Value, options));
            }
        }
    }
}
