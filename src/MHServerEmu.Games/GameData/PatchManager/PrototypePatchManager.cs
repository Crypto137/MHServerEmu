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
                if (entry.InsertValue)
                {
                    InsertValue(prototype, fieldInfo, entry.Value);
                }
                else
                {
                    object convertedValue = ConvertValue(entry.Value, fieldType);
                    fieldInfo.SetValue(prototype, convertedValue);
                }
            }
            catch (Exception ex)
            {
                Logger.WarnException(ex, $"Failed UpdateValue: [{entry.Prototype}] [{entry.Path}] {ex.Message}");
            }
        }

        private static void InsertValue(Prototype prototype, PropertyInfo fieldInfo, string valueStruct)
        {
            Type fieldType = fieldInfo.PropertyType; 
            if (fieldType.IsArray == false)
                throw new InvalidOperationException($"Field {fieldInfo.Name} is not array.");     

            var values = ParseValueStruct(valueStruct);

            if (values.ContainsKey("Type") == false)
                throw new InvalidOperationException($"Type not found");

            string typeName = values["Type"];
            Type elementType = fieldType.GetElementType(); 

            if (elementType == null || IsTypeCompatible(elementType, typeName) == false)
                throw new InvalidOperationException($"Type {typeName} is not assignable for {elementType?.Name}.");

            var currentArray = (Array)fieldInfo.GetValue(prototype);

            int newLength = CalcNewLength(currentArray, values);
            var newArray = Array.CreateInstance(elementType, newLength);

            if (currentArray != null)
                Array.Copy(currentArray, newArray, currentArray.Length);

            AddElements(newArray, elementType, values, currentArray.Length);

            fieldInfo.SetValue(prototype, newArray);
        }

        private static int CalcNewLength(Array currentArray, Dictionary<string, string> values)
        {
            int currentLength = currentArray?.Length ?? 0;
            int valuesCount = 1;
            if (values.TryGetValue("Values", out var strArray))
            {
                int length = strArray.Split(';').Length;
                if (length > 1) valuesCount = length;
            }
            return currentLength + valuesCount;
        }

        private static bool IsTypeCompatible(Type baseType, string typeName)
        {
            Type derivedType = Type.GetType($"{baseType.Namespace}.{typeName}");
            return derivedType == null
                ? throw new InvalidOperationException($"Type {typeName} not found.")
                : baseType.IsAssignableFrom(derivedType);
        }

        private static void AddElements(Array newArray, Type elementType, Dictionary<string, string> values, int lastIndex)
        {
            if (elementType.IsClass)
            {
                // TODO
            }
            else if (values.TryGetValue("Values", out string strArray))
            {
                var valueElements = strArray.Split(';');
                foreach (var valueString in valueElements)
                {
                    object elementValue = ConvertValue(valueString, elementType);
                    newArray.SetValue(elementValue, lastIndex++);
                }
            }
            else if (values.TryGetValue("Value", out string valueString))
            {
                object elementValue = ConvertValue(valueString, elementType);
                newArray.SetValue(elementValue, lastIndex);
            }
        }

        private static Dictionary<string, string> ParseValueStruct(string valueStruct)
        {
            valueStruct = valueStruct.Trim('{', '}');
            var keyValuePairs = valueStruct.Split(',');
            var result = new Dictionary<string, string>();

            foreach (var pair in keyValuePairs)
            {
                var parts = pair.Split(':');
                if (parts.Length != 2)
                    throw new FormatException($"Wrong value format: {pair}");

                string key = parts[0].Trim();
                string value = parts[1].Trim();

                result[key] = value;
            }

            return result;
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
