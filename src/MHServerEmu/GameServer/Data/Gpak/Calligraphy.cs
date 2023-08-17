using System.Text.Json;
using MHServerEmu.Common;
using MHServerEmu.GameServer.Data.Gpak.FileFormats;

namespace MHServerEmu.GameServer.Data.Gpak
{
    public static class Calligraphy
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static readonly Dictionary<string, DataDirectory> DataDirectoryDict = new();
        public static readonly Dictionary<string, GType> GTypeDict = new();
        public static readonly Dictionary<string, Curve> CurveDict = new();
        //public static readonly Dictionary<string, Blueprint> BlueprintDict = new();

        public static void Initialize(GpakFile gpakFile)
        {
            // Sort GpakEntries by type
            List<GpakEntry> directoryList = new();
            List<GpakEntry> typeList = new();
            List<GpakEntry> curveList = new();
            List<GpakEntry> blueprintList = new();
            List<GpakEntry> defaultsList = new();
            List<GpakEntry> prototypeList = new();

            foreach (GpakEntry entry in gpakFile.Entries)
            {
                switch (Path.GetExtension(entry.FilePath))
                {
                    case ".directory":
                        directoryList.Add(entry);
                        break;
                    case ".type":
                        typeList.Add(entry);
                        break;
                    case ".curve":
                        curveList.Add(entry);
                        break;
                    case ".blueprint":
                        blueprintList.Add(entry);
                        break;
                    case ".defaults":
                        defaultsList.Add(entry);
                        break;
                    case ".prototype":
                        prototypeList.Add(entry);
                        break;
                }
            }

            // Parse all entires in order by type
            foreach (GpakEntry entry in directoryList)
                DataDirectoryDict.Add(entry.FilePath, new(entry.Data));

            foreach (GpakEntry entry in typeList)
                GTypeDict.Add(entry.FilePath, new(entry.Data));

            foreach (GpakEntry entry in curveList)
                CurveDict.Add(entry.FilePath, new(entry.Data));

            //foreach (GpakEntry entry in blueprintList)
            //    BlueprintDict.Add(entry.FilePath, new(entry.Data));

            // TODO: defaults

            // TODO: prototypes

            Logger.Info($"Parsed {DataDirectoryDict.Count} directories, {GTypeDict.Count} types, {CurveDict.Count} curves");
        }

        public static void Export()
        {
            SerializeDictAsJson(DataDirectoryDict);
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

            //SerializeDictAsJson(BlueprintDict);
        }

        private static void SerializeDictAsJson<T>(Dictionary<string, T> dict)
        {
            JsonSerializerOptions jsonOptions = new();
            jsonOptions.WriteIndented = true;

            jsonOptions.Converters.Add(new DataDirectoryEntryConverter());

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
