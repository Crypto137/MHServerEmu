using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Properties
{
    public class PropertyTicker
    {
        public const ulong InvalidId = 0;

        private static readonly Logger Logger = LogManager.CreateLogger();

        public ulong Id { get; private set; }

        public PropertyTicker()
        {
        }

        public void Initialize(ulong id, PropertyCollection properties, ulong targetId, ulong creatorId, ulong ultimateCreatorId,
            TimeSpan updateInterval, ulong conditionId, PowerPrototype powerProto, bool targetsCreator)
        {
            Id = id;
            // TODO
        }

        public void Start(TimeSpan duration, bool isPaused)
        {
            Logger.Debug($"Start(): Id={Id}");
        }

        public void Stop(bool tickOnStop)
        {
            Logger.Debug($"Stop(): Id={Id}");
        }

        public void Update(TimeSpan duration, bool isPaused)
        {
            Logger.Debug($"Update(): Id={Id}");
        }
    }
}
