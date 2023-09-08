using Gazillion;
using MHServerEmu.Common.Logging;
using MHServerEmu.Networking;

namespace MHServerEmu.GameServer.Billing
{
    public class BillingService : IGameMessageHandler
    {
        private const int CurrencyBalance = 9000;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private GameServerManager _gameServerManager;

        public BillingService(GameServerManager gameServerManager)
        {
            _gameServerManager = gameServerManager;
        }

        public void Handle(FrontendClient client, ushort muxId, GameMessage message)
        {
            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageGetCatalog:
                    Logger.Info($"Received NetMessageGetCatalog");
                    var dumpedCatalog = NetMessageCatalogItems.ParseFrom(PacketHelper.LoadMessagesFromPacketFile("NetMessageCatalogItems.bin")[0].Content);

                    var catalog = NetMessageCatalogItems.CreateBuilder()
                        .MergeFrom(dumpedCatalog)
                        .SetTimestampSeconds(_gameServerManager.GetDateTime() / 1000000)
                        .SetTimestampMicroseconds(_gameServerManager.GetDateTime())
                        .SetClientmustdownloadimages(false)
                        .Build();

                    client.SendMessage(muxId, new(catalog));
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
