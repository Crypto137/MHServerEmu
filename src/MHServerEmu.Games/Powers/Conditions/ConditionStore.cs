using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Powers.Conditions
{
    /// <summary>
    /// Contains data required to persistently store a <see cref="Condition"/>.
    /// </summary>
    public ref struct ConditionStore
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public PrototypeId ConditionProtoRef;
        public PrototypeId CreatorPowerPrototypeRef;
        public bool IsPaused;
        public ulong TimeRemaining;
        public ulong SerializeGameTime;
        public PropertyCollection Properties;

        public ConditionStore(PropertyCollection properties)
        {
            if (properties == null) throw new ArgumentNullException(nameof(properties));
            Properties = properties;
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            success &= Serializer.Transfer(archive, ref ConditionProtoRef);
            success &= Serializer.Transfer(archive, ref CreatorPowerPrototypeRef);
            success &= Serializer.Transfer(archive, ref TimeRemaining);
            success &= Serializer.Transfer(archive, ref SerializeGameTime);
            success &= Serializer.Transfer(archive, ref IsPaused);
            success &= Serializer.Transfer(archive, ref Properties);

            return success;
        }
    }
}
