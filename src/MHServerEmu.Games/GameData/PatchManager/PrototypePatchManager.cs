using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Prototypes;
using System.ComponentModel;
using System.Reflection;

namespace MHServerEmu.Games.GameData.PatchManager
{
    public class PrototypePatchManager
    {

        private static readonly Logger Logger = LogManager.CreateLogger();
        private Stack<PrototypeId> _protoStack = new();
        private readonly Dictionary<PrototypeId, List<PrototypePatchUpdateValue>> _patchDict = new();
        private Dictionary<Prototype, string> _pathDict = new ();

        public static PrototypePatchManager Instance { get; } = new();

        public void Initialize()
        {
            LoadPatchDataFromDisk();
        }

        private bool LoadPatchDataFromDisk()
        {
            string patchDirectory = Path.Combine(FileHelper.DataDirectory, "Game");
            if (Directory.Exists(patchDirectory) == false)
                return Logger.WarnReturn(false, "LoadPatchDataFromDisk(): Game data directory not found");

            int count = 0;

            // Read all .json files that start with PatchData
            foreach (string filePath in FileHelper.GetFilesWithPrefix(patchDirectory, "PatchData", "json"))
            {
                string fileName = Path.GetFileName(filePath);

                PrototypePatchUpdateValue[] updateValues = FileHelper.DeserializeJson<PrototypePatchUpdateValue[]>(filePath);
                if (updateValues == null)
                {
                    Logger.Warn($"LoadPatchDataFromDisk(): Failed to parse {fileName}, skipping");
                    continue;
                }

                foreach (PrototypePatchUpdateValue value in updateValues)
                {
                    if (value.Enabled == false) continue;
                    PrototypeId prototypeId = GameDatabase.GetPrototypeRefByName(value.Prototype);
                    if (prototypeId == PrototypeId.Invalid) continue;
                    AddPatchValue(prototypeId, value);
                    count++;
                }

                Logger.Trace($"Parsed patch data from {fileName}");
            }

            return Logger.InfoReturn(true, $"Loaded {count} patches");
        }

        private void AddPatchValue(PrototypeId prototypeId, in PrototypePatchUpdateValue value)
        {
            if (_patchDict.TryGetValue(prototypeId, out var patchList) == false)
            {
                patchList = [];
                _patchDict[prototypeId] = patchList;
            }
            patchList.Add(value);
        }

        public bool PreCheck(PrototypeId protoRef)
        {
            if (protoRef != PrototypeId.Invalid && _patchDict.ContainsKey(protoRef))
                _protoStack.Push(protoRef);

            return _protoStack.Count > 0;
        }

        public void PostOverride(Prototype prototype)
        {
            if (_protoStack.Count == 0) return;

            string currentPath = string.Empty;
            if (prototype.DataRef == PrototypeId.Invalid 
                && _pathDict.TryGetValue(prototype, out currentPath) == false) return;

            PrototypeId patchProtoRef;
            if (prototype.DataRef != PrototypeId.Invalid && _patchDict.ContainsKey(prototype.DataRef))
                patchProtoRef = _protoStack.Pop();
            else
                patchProtoRef = _protoStack.Peek();

            if (_patchDict.TryGetValue(patchProtoRef, out var list) == false) return;

            foreach (var entry in list)
                CheckAndUpdate(entry, prototype, currentPath);

            if (_protoStack.Count == 0)
                _pathDict.Clear();
        }

        private static bool CheckAndUpdate(PrototypePatchUpdateValue entry, Prototype prototype, string currentPath)
        {
            if (currentPath.StartsWith('.')) currentPath = currentPath[1..];
            if (entry.СlearPath != currentPath) return false;

            var fieldInfo = prototype.GetType().GetProperty(entry.FieldName);
            if (fieldInfo == null) return false;

            UpdateValue(prototype, fieldInfo, entry);
            Logger.Debug($"Update {entry.Prototype} {entry.Path} = {entry.Value}");

            return true;
        }

        private static object ConvertValue(string stringValue, Type targetType)
        {
            TypeConverter converter = TypeDescriptor.GetConverter(targetType);

            if (converter != null && converter.CanConvertFrom(typeof(string)))
                return converter.ConvertFrom(stringValue);

            return Convert.ChangeType(stringValue, targetType);
        }

        private static void UpdateValue(Prototype prototype, PropertyInfo fieldInfo, PrototypePatchUpdateValue entry)
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
            if (parent.DataRef != PrototypeId.Invalid) parentPath = string.Empty;
            _pathDict[child] = $"{parentPath}.{fieldName}";
        }

        public void SetPathIndex(Prototype parent, Prototype child, string fieldName, int index)
        {
            string parentPath = _pathDict.TryGetValue(parent, out var path) ? path : string.Empty;
            if (parent.DataRef != PrototypeId.Invalid) parentPath = string.Empty;
            _pathDict[child] = $"{parentPath}.{fieldName}[{index}]";
        }
    }
}
