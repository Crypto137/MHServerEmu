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

        public TempPrototypePropertyContainer GetPropertyContainer(BlueprintId blueprintId, byte blueprintCopyNum = 0)
        {
            if (_propertyContainer.TryGetValue((blueprintId, blueprintCopyNum), out var container) == false)
                Logger.WarnReturn<TempPrototypePropertyContainer>(null, $"Failed to get property container for blueprint {GameDatabase.GetBlueprintName(blueprintId)}");
            return container;
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
    }
}
