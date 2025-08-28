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
            switch (message)
            {
                // NOTE: We haven't really seen this, but there is a ClientToGroupingManager protocol
                // that includes a single message - GetPlayerInfoByName. If we ever receive it, it should end up here.

                // Handle everything in a dedicated worker thread
                case ServiceMessage.AddClient:
                case ServiceMessage.RemoveClient:
                case ServiceMessage.PlayerNameChanged:
                case ServiceMessage.GroupingManagerChat:
                case ServiceMessage.GroupingManagerTell:
                case ServiceMessage.GroupingManagerServerNotification:
                    _serviceMailbox.PostMessage(message);
                    break;

                default:
                    Logger.Warn($"ReceiveServiceMessage(): Unhandled service message type {typeof(T).Name}");
                    break;
            }
        }

        public string GetStatus()
        {
            return $"Players: {ClientManager.Count}";
        }

        #endregion
    }
}
