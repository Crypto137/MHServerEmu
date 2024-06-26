using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Powers
{
    // Just a stub for now

    public class SituationalPowerComponent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private Game _game;
        private SituationalPowerComponentPrototype _prototype;
        private Power _power;

        public SituationalPowerComponent(Game game, SituationalPowerComponentPrototype prototype, Power power)
        {
            _game = game;
            _prototype = prototype;
            _power = power;
        }

        public void Initialize()
        {
            Logger.Debug($"Initialize(): {_power.Prototype}");
        }

        public void OnPowerAssigned()
        {

        }
    }
}
