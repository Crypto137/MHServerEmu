using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;

namespace MHServerEmu.Billing
{
    public class BillingService : IGameService
    {
        // All the message handling / order fulfillment functionality has been moved to Games.MTXStore.CatalogManager.
        // I'm keeping this service stub because eventually it will be used for converting ES to G via the store interface.

        private static readonly Logger Logger = LogManager.CreateLogger();

        public GameServiceState State { get; private set; } = GameServiceState.Created;

        public BillingService() { }

        #region IGameService Implementation

        public void Run()
        {
            State = GameServiceState.Running;
        }

        public void Shutdown()
        {
            State = GameServiceState.Shutdown;
        }

        public void ReceiveServiceMessage<T>(in T message) where T : struct, IGameServiceMessage
        {
            switch (message)
            {
                default:
                    Logger.Warn($"ReceiveServiceMessage(): Unhandled service message type {typeof(T).Name}");
                    break;
            }
        }

        public string GetStatus()
        {
            return "Running";
        }

        #endregion
    }
}
