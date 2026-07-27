using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using System.Text.Json;

namespace MHServerEmu.Games.GameData.PatchManager
{
    public class JsonPrototype : ValueBase
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly PrototypeId _parentRef;
        private readonly List<Field> _fields = new();

        private Prototype _instance;

        public override ValueType ValueType { get => ValueType.Prototype; }

        public JsonPrototype(JsonElement jsonElement)
        {
            _parentRef = (PrototypeId)jsonElement.GetProperty("ParentDataRef").GetUInt64();

            Type classType = GameDatabase.DataDirectory.GetPrototypeClassType(_parentRef);
            if (!Verify.IsNotNull(classType)) return;

            foreach (JsonProperty jsonProperty in jsonElement.EnumerateObject())
            {
                string fieldName = jsonProperty.Name;

                if (fieldName == "ParentDataRef")
                    continue;

                System.Reflection.PropertyInfo fieldInfo = classType.GetProperty(fieldName);
                if (!Verify.IsNotNull(fieldInfo))
                    continue;

                Type fieldType = fieldInfo.PropertyType;
                object fieldValue = PatchEntryConverter.ParseJsonElement(jsonProperty.Value, fieldType);

                Field field = new(fieldName, fieldValue, fieldType);
                _fields.Add(field);
            }
        }

        public override object GetValue()
        {
            if (!Verify.IsTrue(_parentRef != PrototypeId.Invalid)) return null;

            if (_instance == null)
            {
                Type classType = GameDatabase.DataDirectory.GetPrototypeClassType(_parentRef);
                if (!Verify.IsNotNull(classType)) return null;

                Prototype instance = GameDatabase.PrototypeClassManager.AllocatePrototype(classType);
                if (!Verify.IsNotNull(instance)) return null;

                CalligraphySerializer.CopyPrototypeDataRefFields(instance, _parentRef);

                foreach (Field field in _fields)
                {
                    System.Reflection.PropertyInfo fieldInfo = classType.GetProperty(field.Name);
                    if (!Verify.IsNotNull(fieldInfo))
                        continue;

                    try
                    {
                        object convertedValue = PrototypePatchManager.ConvertValue(field.Value, field.Type);
                        fieldInfo.SetValue(instance, convertedValue);
                    }
                    catch (Exception e)
                    {
                        Logger.Warn($"Can't convert {field.Name} in {classType.Name} - {e.Message}");
                    }
                }

                _instance = instance;
            }

            return _instance;
        }

        private readonly struct Field(string name, object value, Type type)
        {
            public readonly string Name = name;
            public readonly object Value = value;
            public readonly Type Type = type;
        }
    }

    public class JsonPrototypeArray : ValueBase
    {
        private readonly JsonPrototype[] _jsonPrototypes;
        private Prototype[] _instances;

        public override ValueType ValueType { get => ValueType.PrototypeArray; }

        public JsonPrototypeArray(JsonElement jsonElement)
        {
            if (jsonElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Json element is not array");

            JsonElement[] jsonArray = jsonElement.EnumerateArray().ToArray();
            if (jsonArray.Length == 0)
            {
                _jsonPrototypes = [];
                _instances = [];
                return;
            }

            _jsonPrototypes = new JsonPrototype[jsonArray.Length];
            for (int i = 0; i < jsonArray.Length; i++)
                _jsonPrototypes[i] = new(jsonArray[i]);
        }

        public override object GetValue()
        {
            if (_instances == null)
            {
                Prototype[] instances = new Prototype[_jsonPrototypes.Length];
                for (int i = 0; i < _jsonPrototypes.Length; i++)
                    instances[i] = (Prototype)_jsonPrototypes[i].GetValue();
                _instances = instances;
            }

            return _instances;
        }
    }
}
