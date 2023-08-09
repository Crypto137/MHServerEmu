using System.Text.Json;
using MHServerEmu.Common;
using MHServerEmu.GameServer.Data.Gpak.FileFormats;

namespace MHServerEmu.GameServer.Data.Gpak
{
    public static class Calligraphy
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static readonly Dictionary<string, GType> GTypeDict = new();

        public static void Initialize(GpakFile gpakFile)
        {
            foreach (GpakEntry entry in gpakFile.Entries)
            {
                switch (Path.GetExtension(entry.FilePath))
                {
                    case ".type":
                        GTypeDict.Add(entry.FilePath, new(entry.Data));
                        break;
                }
            }

            Logger.Info($"Parsed {GTypeDict.Count} types");
        }

        public static void Export()
        {
            File.WriteAllText($"{Directory.GetCurrentDirectory()}\\Assets\\GPAK\\GType.json", JsonSerializer.Serialize(GTypeDict));
        }
    }
}
