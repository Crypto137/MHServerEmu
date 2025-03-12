using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Prototypes;
using System.ComponentModel;
using System.Reflection;

namespace MHServerEmu.Games.GameData.HardTuning
{
    public class HardTuningManager
    {

        private static readonly Logger Logger = LogManager.CreateLogger();
        private PrototypeId _currentProtoRef;
        private readonly Dictionary<PrototypeId, List<HardTuningUpdateValue>> _hardTuningDict = new();
        private Dictionary<Prototype, string> _pathDict = new ();

        public static HardTuningManager Instance { get; } = new();

        public void Initialize()
        {
            LoadHardTuningDataFromDisk();
        }

        private bool LoadHardTuningDataFromDisk()
        {
            string hardTuningDirectory = Path.Combine(FileHelper.DataDirectory, "Game");
            if (Directory.Exists(hardTuningDirectory) == false)
                return Logger.WarnReturn(false, "LoadHardTuningDataFromDisk(): Game data directory not found");

            int count = 0;

            // Read all .json files that start with HardTuningData
            foreach (string filePath in FileHelper.GetFilesWithPrefix(hardTuningDirectory, "HardTuningData", "json"))
            {
                string fileName = Path.GetFileName(filePath);

                HardTuningUpdateValue[] updateValues = FileHelper.DeserializeJson<HardTuningUpdateValue[]>(filePath);
                if (updateValues == null)
                {
                    Logger.Warn($"LoadHardTuningDataFromDisk(): Failed to parse {fileName}, skipping");
                    continue;
                }

                foreach (HardTuningUpdateValue value in updateValues)
                {
                    PrototypeId prototypeId = GameDatabase.GetPrototypeRefByName(value.Prototype);
                    if (prototypeId == PrototypeId.Invalid) continue;
                    AddHardTuningValue(prototypeId, value);
                    count++;
                }

                Logger.Trace($"Parsed hard tuning data from {fileName}");
            }

            return Logger.InfoReturn(true, $"Loaded {count} hard tuning values");
        }

        private void AddHardTuningValue(PrototypeId prototypeId, in HardTuningUpdateValue value)
        {
            if (_hardTuningDict.TryGetValue(prototypeId, out var hardTuningList) == false)
            {
                hardTuningList = [];
                _hardTuningDict[prototypeId] = hardTuningList;
            }
            hardTuningList.Add(value);
        }

        public bool PreCheck(PrototypeId protoRef)
        {
            if (protoRef == PrototypeId.Invalid) 
                return _currentProtoRef != PrototypeId.Invalid;

            if (_hardTuningDict.ContainsKey(protoRef))
                _currentProtoRef = protoRef;
            else
                _currentProtoRef = PrototypeId.Invalid;

            _pathDict.Clear();

            return _currentProtoRef != PrototypeId.Invalid;
        }

        public void PostOverride(Prototype prototype)
        {
            if (_hardTuningDict.TryGetValue(_currentProtoRef, out var list) == false) return; 
            if (_pathDict.TryGetValue(prototype, out var currentPath) == false) return;

            foreach (var entry in list)
                CheckAndUpdate(entry, prototype, currentPath);
        }

        private static bool CheckAndUpdate(HardTuningUpdateValue entry, Prototype prototype, string currentPath)
        {
            if (currentPath.StartsWith('.')) currentPath = currentPath[1..];

            if (entry.СlearPath != currentPath) return false;

            var fieldInfo = prototype.GetType().GetProperty(entry.FieldName);
            if (fieldInfo == null) return false;

            UpdateValue(prototype, fieldInfo, entry);

            return true;
        }

        private static object ConvertValue(string stringValue, Type targetType)
        {
            TypeConverter converter = TypeDescriptor.GetConverter(targetType);

            if (converter != null && converter.CanConvertFrom(typeof(string)))
                return converter.ConvertFrom(stringValue);

            return Convert.ChangeType(stringValue, targetType);
        }

        private static void UpdateValue(Prototype prototype, PropertyInfo fieldInfo, HardTuningUpdateValue entry)
        {
            try
            {
                Type fieldType = fieldInfo.PropertyType;
                object convertedValue = ConvertValue(entry.Value, fieldType);
                fieldInfo.SetValue(prototype, convertedValue);
            }
            catch (Exception ex)
            {
                Logger.WarnException(ex, $"Failed UpdateValue: [{entry.Prototype}] [{entry.Path}] {ex.Message}");
            }
        }

        public void SetPath(Prototype parent, Prototype child, string fieldName)
        {
            string parentPath = _pathDict.TryGetValue(parent, out var path) ? path : string.Empty;
            _pathDict[child] = $"{parentPath}.{fieldName}";
        }

        public void SetPathIndex(Prototype parent, Prototype child, string fieldName, int index)
        {
            string parentPath = _pathDict.TryGetValue(parent, out var path) ? path : string.Empty;
            _pathDict[child] = $"{parentPath}.{fieldName}[{index}]";
        }
    }
}
