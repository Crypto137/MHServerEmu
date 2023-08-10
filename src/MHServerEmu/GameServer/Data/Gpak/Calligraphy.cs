using System.Text.Json;
using MHServerEmu.Common;
using MHServerEmu.GameServer.Data.Gpak.FileFormats;

namespace MHServerEmu.GameServer.Data.Gpak
{
    public static class Calligraphy
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static readonly Dictionary<string, GType> GTypeDict = new();
        public static readonly Dictionary<string, Curve> CurveDict = new();

        public static void Initialize(GpakFile gpakFile)
        {
            foreach (GpakEntry entry in gpakFile.Entries)
            {
                switch (Path.GetExtension(entry.FilePath))
                {
                    case ".type":
                        GTypeDict.Add(entry.FilePath, new(entry.Data));
                        break;
                    case ".curve":
                        CurveDict.Add(entry.FilePath, new(entry.Data));
                        break;
                }
            }

            Logger.Info($"Parsed {GTypeDict.Count} types, {CurveDict.Count} curves");
        }

        public static void Export()
        { 
            JsonSerializerOptions jsonOptions = new();
            jsonOptions.WriteIndented = true;

            foreach (var kvp in GTypeDict)
            {
                string path = $"{Directory.GetCurrentDirectory()}\\Assets\\GPAK\\Export\\{kvp.Key}.json";
                string dir = Path.GetDirectoryName(path);
                if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);

                File.WriteAllText(path, JsonSerializer.Serialize(kvp.Value, jsonOptions));
            }

            foreach (var kvp in CurveDict)
            {
                string path = $"{Directory.GetCurrentDirectory()}\\Assets\\GPAK\\Export\\{kvp.Key}.tsv";
                string dir = Path.GetDirectoryName(path);
                if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);

                using (StreamWriter sw = new(path))
                {
                    foreach (double value in kvp.Value.Entries)
                        sw.WriteLine(value);
                }
            }     
        }
    }
}
