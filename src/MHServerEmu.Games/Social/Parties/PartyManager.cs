using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;

namespace MHServerEmu.Games.Social.Parties
{
    public class PartyManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public Game Game { get; }

        public PartyManager(Game game)
        {
            Game = game;
        }

        public void OnClientPartyOperationRequest(Player player, PartyOperationPayload operation)
        {
            Logger.Debug($"OnClientPartyOperationRequest():\n{operation}");
        }
    }
}
