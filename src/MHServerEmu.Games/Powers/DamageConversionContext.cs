using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Powers
{
    public struct DamageConversionContext
    {
        public float DamageBase { get; }
        public DamageType DamageType { get; }
        public PowerPrototype PowerPrototype { get; }

        public PropertyCollection SourceProperties { get; private set; }
        public WorldEntity Target { get; private set; }
        public float DifficultyMultiplier { get; private set; }

        public PropertyEnum ConversionProperty { get; private set; }
        public PropertyEnum ConversionRatioProperty { get; private set; }
        public PropertyEnum ConversionMaxProperty { get; private set; }

        public float DamageConverted { get; set; }

        public DamageConversionContext(float damageBase, DamageType damageType, PowerPrototype powerProto)
        {
            DamageBase = damageBase;
            DamageType = damageType;
            PowerPrototype = powerProto;

            DamageConverted = damageBase;
        }

        public void SetIncoming(PropertyCollection source, WorldEntity target)
        {
            SourceProperties = source;
            Target = target;
            DifficultyMultiplier = 1f;

            ConversionProperty = PropertyEnum.DamageConversionIncoming;
            ConversionRatioProperty = PropertyEnum.DamageConversionRatio;
            ConversionMaxProperty = PropertyEnum.DamageConversionMax;
        }

        public void SetOutgoing(PropertyCollection source, WorldEntity target, float difficultyMultiplier)
        {
            SourceProperties = source;
            Target = target;
            DifficultyMultiplier = difficultyMultiplier;

            ConversionProperty = PropertyEnum.DamageConversionOutgoing;
            ConversionRatioProperty = PropertyEnum.DamageConversionRatio;
            ConversionMaxProperty = PropertyEnum.DamageConversionMax;
        }

        public void SetForPower(PropertyCollection source, WorldEntity target)
        {
            SourceProperties = source;
            Target = target;
            DifficultyMultiplier = 1f;

            ConversionProperty = PropertyEnum.DamageConversionForPower;
            ConversionRatioProperty = PropertyEnum.DamageConversionRatioForPower;
            ConversionMaxProperty = PropertyEnum.DamageConversionMaxForPower;
        }
    }
}
