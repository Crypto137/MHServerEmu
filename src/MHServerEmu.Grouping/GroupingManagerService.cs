using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;

namespace MHServerEmu.Grouping
{
    public class GroupingManagerService : IGameService
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly GroupingServiceMailbox _serviceMailbox;

        public GroupingClientManager ClientManager { get; }
        public GroupingChatManager ChatManager { get; }

        public GameServiceState State { get; private set; } = GameServiceState.Created;

        public GroupingManagerService()
        {
            _serviceMailbox = new(this);
            ClientManager = new();
            ChatManager = new(this);
        }

        #region IGameService Implementation

        public void Run()
        {
            State = GameServiceState.Running;
            while (State == GameServiceState.Running)
            {
                _serviceMailbox.ProcessMessages();
                Thread.Sleep(1);
            }
        }

        public void Shutdown()
        {
            State = GameServiceState.Shutdown;
        }

        public void ReceiveServiceMessage<T>(in T message) where T : struct, IGameServiceMessage
        {
            _serviceMailbox.PostMessage(message);
        }

        public void GetStatus(Dictionary<string, long> statusDict)
        {
            statusDict["GroupingManagerPlayers"] = ClientManager.Count;
        }

        #endregion
    }
}
