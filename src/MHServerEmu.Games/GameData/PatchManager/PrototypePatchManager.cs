using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Prototypes;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;

namespace MHServerEmu.Games.GameData.PatchManager
{
    public class PrototypePatchManager
    {

        private static readonly Logger Logger = LogManager.CreateLogger();
        private Stack<PrototypeId> _protoStack = new();
        private readonly Dictionary<PrototypeId, List<PrototypePatchEntry>> _patchDict = new();
        private Dictionary<Prototype, string> _pathDict = new ();
        private bool _initialized = false;

        public static PrototypePatchManager Instance { get; } = new();

        public void Initialize(bool enablePatchManager)
        {
            if (enablePatchManager) _initialized = LoadPatchDataFromDisk();
        }

        private bool LoadPatchDataFromDisk()
        {
            string patchDirectory = Path.Combine(FileHelper.DataDirectory, "Game", "Patches");
            if (Directory.Exists(patchDirectory) == false)
                return Logger.WarnReturn(false, "LoadPatchDataFromDisk(): Game data directory not found");

            int count = 0;
            var options = new JsonSerializerOptions { Converters = { new PatchEntryConverter() } };

            // Read all .json files that start with PatchData
            foreach (string filePath in FileHelper.GetFilesWithPrefix(patchDirectory, "PatchData", "json"))
            {
                string fileName = Path.GetFileName(filePath);

                PrototypePatchEntry[] updateValues = FileHelper.DeserializeJson<PrototypePatchEntry[]>(filePath, options);
                if (updateValues == null)
                {
                    Logger.Warn($"LoadPatchDataFromDisk(): Failed to parse {fileName}, skipping");
                    continue;
                }

                foreach (PrototypePatchEntry value in updateValues)
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

        private void AddPatchValue(PrototypeId prototypeId, in PrototypePatchEntry value)
        {
            if (_patchDict.TryGetValue(prototypeId, out var patchList) == false)
            {
                patchList = [];
                _patchDict[prototypeId] = patchList;
            }
            patchList.Add(value);
        }

        public bool CheckProperties(PrototypeId protoRef, out Properties.PropertyCollection prop)
        {
            prop = null;
            if (_initialized == false) return false;

            if (protoRef != PrototypeId.Invalid && _patchDict.TryGetValue(protoRef, out var list))
                foreach (var entry in list)
                    if (entry.Value.ValueType == ValueType.Properties)
                    {
                        prop = entry.Value.GetValue() as Properties.PropertyCollection;
                        return prop != null;
                    }

            return false;
        }

        public bool PreCheck(PrototypeId protoRef)
        {
            if (_initialized == false) return false;

            if (protoRef != PrototypeId.Invalid && _patchDict.TryGetValue(protoRef, out var list))
            {
                if (NotPatched(list))
                    _protoStack.Push(protoRef);
            }

            return _protoStack.Count > 0;
        }

        private static bool NotPatched(List<PrototypePatchEntry> list)
        {
            foreach (var entry in list)
                if (entry.Patched == false) return true;
            return false;
        }

        public void PostOverride(Prototype prototype)
        {
            if (_protoStack.Count == 0) return;

            string currentPath = string.Empty;
            if (prototype.DataRef == PrototypeId.Invalid 
                && _pathDict.TryGetValue(prototype, out currentPath) == false) return;

            PrototypeId patchProtoRef = _protoStack.Peek();
            if (prototype.DataRef != PrototypeId.Invalid)
            {
                if (prototype.DataRef != patchProtoRef) return;
                if (_patchDict.ContainsKey(prototype.DataRef))
                    patchProtoRef = _protoStack.Pop();
            }

            if (_patchDict.TryGetValue(patchProtoRef, out var list) == false) return;

            foreach (var entry in list)
                if (entry.Patched == false)
                    CheckAndUpdate(entry, prototype, currentPath);

            if (_protoStack.Count == 0)
                _pathDict.Clear();
        }

        private static bool CheckAndUpdate(PrototypePatchEntry entry, Prototype prototype, string currentPath)
        {
            if (currentPath.StartsWith('.')) currentPath = currentPath[1..];
            if (entry.СlearPath != currentPath) return false;

            var fieldInfo = prototype.GetType().GetProperty(entry.FieldName);
            if (fieldInfo == null) return false;

            UpdateValue(prototype, fieldInfo, entry);
            Logger.Trace($"Patch Prototype: {entry.Prototype} {entry.Path} = {entry.Value.GetValue()}");

            return true;
        }

        public static object ConvertValue(object rawValue, Type targetType)
        {
            if (targetType.IsInstanceOfType(rawValue))
                return rawValue;

            TypeConverter converter = TypeDescriptor.GetConverter(targetType);
            if (converter != null && converter.CanConvertFrom(rawValue.GetType()))
                return converter.ConvertFrom(rawValue);

            return Convert.ChangeType(rawValue, targetType);
        }

        private static void UpdateValue(Prototype prototype, PropertyInfo fieldInfo, PrototypePatchEntry entry)
        {
            try
            {
                Type fieldType = fieldInfo.PropertyType;
                if (entry.InsertValue)
                {
                    InsertValue(prototype, fieldInfo, entry.Value);
                }
                else
                {
                    object convertedValue = ConvertValue(entry.Value.GetValue(), fieldType);
                    fieldInfo.SetValue(prototype, convertedValue);
                }
                entry.Patched = true;
            }
            catch (Exception ex)
            {
                Logger.WarnException(ex, $"Failed UpdateValue: [{entry.Prototype}] [{entry.Path}] {ex.Message}");
            }
        }

        private static void InsertValue(Prototype prototype, PropertyInfo fieldInfo, ValueBase value)
        {
            Type fieldType = fieldInfo.PropertyType; 
            if (fieldType.IsArray == false)
                throw new InvalidOperationException($"Field {fieldInfo.Name} is not array.");     

            var valueEntry = value.GetValue();

            var entryType = valueEntry.GetType();
            Type elementType = fieldType.GetElementType(); 

            if (elementType == null || IsTypeCompatible(elementType, entryType, value.ValueType) == false)
                throw new InvalidOperationException($"Type {value.ValueType} is not assignable for {elementType?.Name}.");

            var currentArray = (Array)fieldInfo.GetValue(prototype);

            int newLength = CalcNewLength(currentArray, valueEntry);
            var newArray = Array.CreateInstance(elementType, newLength);

            if (currentArray != null)
                Array.Copy(currentArray, newArray, currentArray.Length);

            AddElements(newArray, elementType, valueEntry, currentArray.Length);

            fieldInfo.SetValue(prototype, newArray);
        }

        private static int CalcNewLength(Array currentArray, object valueEntry)
        {
            int currentLength = currentArray?.Length ?? 0;
            int valuesCount = 1;
            if (valueEntry is Array array)
            {
                int length = array.Length;
                if (length > 1) valuesCount = length;
            }
            return currentLength + valuesCount;
        }

        private static bool IsTypeCompatible(Type baseType, Type entryType, ValueType valueType)
        {
            if (entryType.IsArray) entryType = entryType.GetElementType();
            if (valueType == ValueType.PrototypeDataRef || valueType == ValueType.PrototypeDataRefArray) 
                entryType = typeof(Prototype);
            return baseType.IsAssignableFrom(entryType) || entryType.IsAssignableFrom(baseType);
        }

        private static void AddElements(Array newArray, Type elementType, object valueEntry, int lastIndex)
        {
            if (valueEntry is Array array)
            {
                foreach (var entry in array)
                {
                    object elementValue = GetElementValue(entry, elementType);
                    newArray.SetValue(elementValue, lastIndex++);
                }
            }
            else
            {
                object elementValue = GetElementValue(valueEntry, elementType);
                newArray.SetValue(elementValue, lastIndex);
            }
        }

        private static object GetElementValue(object valueEntry, Type elementType)
        {
            if (elementType.IsClass && valueEntry is PrototypeId dataRef)
            {
                var prototype = GameDatabase.GetPrototype<Prototype>(dataRef) 
                    ?? throw new InvalidOperationException($"DataRef {dataRef} is not Prototype.");
                valueEntry = prototype;
            }

            return ConvertValue(valueEntry, elementType);            
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
