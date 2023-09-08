using Gazillion;
using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer.Billing.Catalogs;
using MHServerEmu.Networking;
using System.Text.Json;

namespace MHServerEmu.GameServer.Billing
{
    public class BillingService : IGameMessageHandler
    {
        private const int CurrencyBalance = 9000;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly GameServerManager _gameServerManager;
        private readonly Catalog _catalog;

        public BillingService(GameServerManager gameServerManager)
        {
            _gameServerManager = gameServerManager;
            _catalog = JsonSerializer.Deserialize<Catalog>(File.ReadAllText($"{Directory.GetCurrentDirectory()}\\Assets\\Catalog.json"));
            Logger.Info($"Initialized store catalog with {_catalog.Entries.Length} entries");
        }

        public void Handle(FrontendClient client, ushort muxId, GameMessage message)
        {
            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageGetCatalog:
                    Logger.Info($"Received NetMessageGetCatalog");
                    client.SendMessage(muxId, new(_catalog.ToNetMessageCatalogItems(false)));
                    break;

                case ClientToGameServerMessage.NetMessageGetCurrencyBalance:
                    Logger.Info($"Received NetMessageGetCurrencyBalance");

                    client.SendMessage(muxId, new(NetMessageGetCurrencyBalanceResponse.CreateBuilder()
                        .SetCurrencyBalance(CurrencyBalance)
                        .Build()));

                    break;

                case ClientToGameServerMessage.NetMessageBuyItemFromCatalog:
                    Logger.Info($"Received NetMessageBuyItemFromCatalog");
                    var buyItemMessage = NetMessageBuyItemFromCatalog.ParseFrom(message.Content);
                    Logger.Trace(buyItemMessage.ToString());

                    client.SendMessage(muxId, new(NetMessageBuyItemFromCatalogResponse.CreateBuilder()
                        .SetDidSucceed(true)
                        .SetCurrentCurrencyBalance(CurrencyBalance)
                        .SetErrorcode(BuyItemResultErrorCodes.BUY_RESULT_ERROR_SUCCESS)
                        .SetSkuId(buyItemMessage.SkuId)
                        .Build()));

                    break;

                default:
                    Logger.Warn($"Received unhandled message {(ClientToGameServerMessage)message.Id} (id {message.Id})");
                    break;
            }
        }

        public void Handle(FrontendClient client, ushort muxId, GameMessage[] messages)
        {
            foreach (GameMessage message in messages) Handle(client, muxId, message);
        }
    }
}
