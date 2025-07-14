using MHServerEmu.Core.Config;

namespace MHServerEmu.Games.Network.InstanceManagement
{
    public class GameInstanceConfig : ConfigContainer
    {
        public int NumWorkerThreads { get; private set; } = 1;
    }
}
