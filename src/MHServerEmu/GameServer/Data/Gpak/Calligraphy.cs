using System.Text.Json;
using MHServerEmu.Common;
using MHServerEmu.GameServer.Data.Gpak.FileFormats;

namespace MHServerEmu.GameServer.Data.Gpak
{
    public static class Calligraphy
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static readonly Dictionary<string, GDirectory> GDirectoryDict = new();
        public static readonly Dictionary<string, GType> GTypeDict = new();
        public static readonly Dictionary<string, Curve> CurveDict = new();

        public static void Initialize(GpakFile gpakFile)
        {
            foreach (GpakEntry entry in gpakFile.Entries)
            {
                switch (Path.GetExtension(entry.FilePath))
                {
                    case ".directory":
                        GDirectoryDict.Add(entry.FilePath, new(entry.Data));
                        break;
                    case ".type":
                        GTypeDict.Add(entry.FilePath, new(entry.Data));
                        break;
                    case ".curve":
                        CurveDict.Add(entry.FilePath, new(entry.Data));
                        break;
                }
            }

            Logger.Info($"Parsed {GDirectoryDict.Count} directories, {GTypeDict.Count} types, {CurveDict.Count} curves");
        }

        public static void Export()
        {
            SerializeDictAsJson(GDirectoryDict);
            SerializeDictAsJson(GTypeDict);

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

        private static void SerializeDictAsJson<T>(Dictionary<string, T> dict)
        {
            JsonSerializerOptions jsonOptions = new();
            jsonOptions.WriteIndented = true;

            jsonOptions.Converters.Add(new GDirectoryEntryConverter());

            foreach (var kvp in dict)
            {
                string path = $"{Directory.GetCurrentDirectory()}\\Assets\\GPAK\\Export\\{kvp.Key}.json";
                string dir = Path.GetDirectoryName(path);
                if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);

                File.WriteAllText(path, JsonSerializer.Serialize((object)kvp.Value, jsonOptions));
            }
        }
    }
}
