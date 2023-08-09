using System.Text.Json;
using MHServerEmu.Common;
using MHServerEmu.GameServer.Data.Gpak.FileFormats;

namespace MHServerEmu.GameServer.Data.Gpak
{
    public static class Resource
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static readonly Dictionary<string, District> DistrictDict = new();

        public static void Initialize(GpakFile gpakFile)
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

        public static void Export()
        {
            File.WriteAllText($"{Directory.GetCurrentDirectory()}\\Assets\\GPAK\\District.json", JsonSerializer.Serialize(DistrictDict));
        }
    }
}
