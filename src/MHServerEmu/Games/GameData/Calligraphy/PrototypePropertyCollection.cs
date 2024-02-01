using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    public class PrototypePropertyCollection : PropertyCollection
    {
        // super hacky placeholder implementation to get things working for now
        // nothing to see here

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<(BlueprintId, byte), TempPrototypePropertyContainer> _propertyContainer = new();

        public void AddPropertyFieldValue(BlueprintId blueprintId, byte blueprintCopyNum, string fieldName, ulong value)
        {
            if (_propertyContainer.TryGetValue((blueprintId, blueprintCopyNum), out var container) == false)
            {
                container = new();
                _propertyContainer.Add((blueprintId, blueprintCopyNum), container);
            }

            var fieldInfo = typeof(TempPrototypePropertyContainer).GetProperty(fieldName);
            fieldInfo.SetValue(container, value);
        }

        public void AddPropertyContainer(BlueprintId blueprintId, byte blueprintCopyNum, TempPrototypePropertyContainer container)
        {
            _propertyContainer.Add((blueprintId, blueprintCopyNum), container);
        }

        public TempPrototypePropertyContainer GetPropertyContainer(BlueprintId blueprintId, byte blueprintCopyNum = 0)
        {
            if (_propertyContainer.TryGetValue((blueprintId, blueprintCopyNum), out var container) == false)
                Logger.WarnReturn<TempPrototypePropertyContainer>(null, $"Failed to get property container for blueprint {GameDatabase.GetBlueprintName(blueprintId)}");
            return container;
        }

        // ShallowCopy() is part of the real API that PrototypePropertyCollection is supposed to have
        // It is used for prototype field copying
        public PrototypePropertyCollection ShallowCopy()
        {
            PrototypePropertyCollection newCollection = new();

            foreach (var kvp in _propertyContainer)
            {
                var newContainer = kvp.Value.Clone();
                newCollection.AddPropertyContainer(kvp.Key.Item1, kvp.Key.Item2, newContainer);
            }

            return newCollection;
        }
    }

    public class TempPrototypePropertyContainer
    {
        public ulong Value { get; protected set; }
        public ulong CurveIndex { get; protected set; }
        public ulong Param0 { get; protected set; }
        public ulong Param1 { get; protected set; }
        public ulong Param2 { get; protected set; }
        public ulong Param3 { get; protected set; }

        public TempPrototypePropertyContainer Clone()
        {
            return new()
            {
                Value = Value,
                CurveIndex = CurveIndex,
                Param0 = Param0,
                Param1 = Param1,
                Param2 = Param2,
                Param3 = Param3
            };
        }
    }
}
