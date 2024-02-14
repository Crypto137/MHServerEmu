using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Properties
{
    public class PropertyInfo
    {
        public PropertyId PropertyId { get; }
        public string PropertyName { get; }

        public string PropertyInfoName { get; }
        public PrototypeId PropertyInfoPrototypeRef { get; }
        public PropertyInfoPrototype PropertyInfoPrototype { get; set; }

        public BlueprintId PropertyMixinBlueprintRef { get; set; } = BlueprintId.Invalid;

        public PropertyDataType DataType { get => PropertyInfoPrototype.Type; }

        public PropertyInfo(PropertyEnum @enum, string propertyInfoName, PrototypeId propertyInfoPrototypeRef)
        {
            PropertyId = new(@enum);
            PropertyInfoName = propertyInfoName;
            PropertyName = $"{PropertyInfoName}Prop";
            PropertyInfoPrototypeRef = propertyInfoPrototypeRef;
        }

        public BlueprintId GetParamPrototypeBlueprint(int paramIndex)
        {
            // NYI
            return BlueprintId.Invalid;
        }
    }
}
