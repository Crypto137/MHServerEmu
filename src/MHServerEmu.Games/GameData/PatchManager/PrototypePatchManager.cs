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
        
        private readonly Dictionary<PrototypeId, List<PrototypePatchEntry>> _patchDict = new();
        private PatchContext _context = new();
        private bool _initialized = false;

        public static PrototypePatchManager Instance { get; } = new();

        /// <summary>
        /// Loads patches after Globals are loaded.
        /// </summary>
        public void Initialize(bool enablePatchManager)
        {
            if (enablePatchManager) _initialized |= LoadPatchDataFromDisk("PatchData");
        }

        private bool LoadPatchDataFromDisk(string prefix)
        {
            string patchDirectory = Path.Combine(FileHelper.DataDirectory, "Game", "Patches");
            if (Directory.Exists(patchDirectory) == false)
            {
                Logger.Warn("LoadPatchDataFromDisk(): Game data directory not found");
                return false;
            }

            int count = 0;
            var options = new JsonSerializerOptions { Converters = { new PatchEntryConverter() } };

            // Read all .json files that start with the specified prefix
            foreach (string filePath in FileHelper.GetFilesWithPrefix(patchDirectory, prefix, "json"))
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

            if (count == 0)
                return false;

            Logger.Info($"Loaded {count} {prefix} patches");
            return true;
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
                    _context.ProtoStack.Push(protoRef);
            }

            return _context.ProtoStack.Count > 0;
        }

        private static bool NotPatched(List<PrototypePatchEntry> list)
        {
            foreach (var entry in list)
                if (entry.Patched == false) return true;
            return false;
        }

        public void PostOverride(Prototype prototype)
        {
            if (_context.ProtoStack.Count == 0) return;

            string currentPath = string.Empty;
            if (prototype.DataRef == PrototypeId.Invalid 
                && _context.PathDict.TryGetValue(prototype, out currentPath) == false) return;

            PrototypeId patchProtoRef = _context.ProtoStack.Peek();
            if (prototype.DataRef != PrototypeId.Invalid)
            {
                if (prototype.DataRef != patchProtoRef) return;
                if (_patchDict.ContainsKey(prototype.DataRef))
                    patchProtoRef = _context.ProtoStack.Pop();
            }

            if (_patchDict.TryGetValue(patchProtoRef, out var list) == false) return;

            foreach (var entry in list)
                if (entry.Patched == false)
                    CheckAndUpdate(entry, prototype, currentPath);

            if (_context.ProtoStack.Count == 0)
                _context.PathDict.Clear();
        }

        private static bool CheckAndUpdate(PrototypePatchEntry entry, Prototype prototype, string currentPath)
        {
            if (currentPath.StartsWith('.')) currentPath = currentPath[1..];
            if (entry.СlearPath != currentPath) return false;

            var fieldInfo = prototype.GetType().GetProperty(entry.FieldName);
            if (fieldInfo == null)
            {
                Logger.Warn($"CheckAndUpdate: {entry.FieldName} not found for {entry.Prototype}");
                return false;
            }

            UpdateValue(prototype, fieldInfo, entry);
            Logger.Trace($"Patch Prototype: {entry.Prototype} {entry.Path} = {entry.Value.GetValue()}");

            return true;
        }

        public static object ConvertValue(object rawValue, Type targetType)
        {
            if (targetType.IsInstanceOfType(rawValue))
                return rawValue;

            if (targetType.IsSubclassOf(typeof(Prototype)))
            {
                PrototypeId? protoRef = null;
                switch (rawValue)
                {
                    case PrototypeId protoId:   protoRef = protoId; break;
                    case ulong dataId:          protoRef = (PrototypeId)dataId; break;
                }

                if (protoRef.HasValue)
                {
                    PatchContext contextBefore = Instance.CreateSubContext();

                    Prototype proto = GameDatabase.GetPrototype<Prototype>(protoRef.Value);

                    Instance.RestoreContext(contextBefore);

                    return proto;
                }
            }

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
                if (entry.ArrayValue)
                {
                    if (entry.ArrayIndex != -1)
                        SetIndexValue(prototype, fieldInfo, entry.ArrayIndex, entry.Value);
                    else
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

        private static void SetIndexValue(Prototype prototype, PropertyInfo fieldInfo, int index, ValueBase value)
        {
            Type fieldType = fieldInfo.PropertyType;
            if (fieldType.IsArray == false)
                throw new InvalidOperationException($"Field {fieldInfo.Name} is not array.");

            Array array = (Array)fieldInfo.GetValue(prototype);
            if (array == null || index < 0 || index >= array.Length)
                throw new IndexOutOfRangeException($"Invalid index {index} for array {fieldInfo.Name}.");

            object valueEntry = value.GetValue();

            var entryType = valueEntry.GetType();
            Type elementType = fieldType.GetElementType();

            if (elementType == null || IsTypeCompatible(elementType, entryType, value.ValueType) == false)
                throw new InvalidOperationException($"Type {value.ValueType} is not assignable to {elementType?.Name}.");

            object converted = ConvertValue(valueEntry, elementType);
            array.SetValue(converted, index);
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
            string parentPath = _context.PathDict.TryGetValue(parent, out var path) ? path : string.Empty;
            if (parent.DataRef != PrototypeId.Invalid && _patchDict.ContainsKey(parent.DataRef)) 
                parentPath = string.Empty;
            _context.PathDict[child] = $"{parentPath}.{fieldName}";
        }

        public void SetPathIndex(Prototype parent, Prototype child, string fieldName, int index)
        {
            string parentPath = _context.PathDict.TryGetValue(parent, out var path) ? path : string.Empty;
            if (parent.DataRef != PrototypeId.Invalid && _patchDict.ContainsKey(parent.DataRef)) 
                parentPath = string.Empty;
            _context.PathDict[child] = $"{parentPath}.{fieldName}[{index}]";
        }

        private PatchContext CreateSubContext()
        {
            PatchContext oldContext = _context;
            _context = new();
            return oldContext;
        }

        private void RestoreContext(in PatchContext context)
        {
            _context = context;
        }

        private readonly struct PatchContext
        {
            public readonly Stack<PrototypeId> ProtoStack;
            public readonly Dictionary<Prototype, string> PathDict;

            public PatchContext()
            {
                ProtoStack = new();
                PathDict = new();
            }
        }
    }
}
