using MHServerEmu.Common;
using MHServerEmu.GameServer.GameData.Gpak.FileFormats;

namespace MHServerEmu.GameServer.GameData.Gpak
{
    public class CalligraphyStorage : GpakStorage
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public Dictionary<string, DataDirectory> DataDirectoryDict { get; } = new();
        public Dictionary<string, GType> GTypeDict { get; } = new();
        public Dictionary<string, Curve> CurveDict { get; } = new();
        public Dictionary<string, Blueprint> BlueprintDict { get; } = new();

        public CalligraphyStorage(GpakFile gpakFile)
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

            foreach (GpakEntry entry in blueprintList)
                BlueprintDict.Add(entry.FilePath, new(entry.Data));

            // TODO: defaults

            // TODO: prototypes

            Logger.Info($"Parsed {DataDirectoryDict.Count} directories, {GTypeDict.Count} types, {CurveDict.Count} curves, {BlueprintDict.Count} blueprints");
        }

        public override bool Verify()
        {
            return DataDirectoryDict.Count > 0
                && GTypeDict.Count > 0
                && CurveDict.Count > 0
                && BlueprintDict.Count > 0;
        }

        public override void Export()
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

            SerializeDictAsJson(BlueprintDict);
        }
    }
}
