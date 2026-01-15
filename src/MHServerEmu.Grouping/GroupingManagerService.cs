using MHServerEmu.Core.Config;
using MHServerEmu.Core.Network;
using MHServerEmu.Grouping.Chat;

namespace MHServerEmu.Grouping
{
    public class GroupingManagerService : IGameService
    {
        private readonly GroupingServiceMailbox _serviceMailbox;

        public GroupingClientManager ClientManager { get; }
        public GroupingChatManager ChatManager { get; }
        public ChatTipManager ChatTipManager { get; }

        public GroupingManagerConfig Config { get; }

        public GameServiceState State { get; private set; } = GameServiceState.Created;

        public GroupingManagerService()
        {
            Config = ConfigManager.Instance.GetConfig<GroupingManagerConfig>();

            _serviceMailbox = new(this);

            ClientManager = new();
            ChatManager = new(this);
            ChatTipManager = new(this);
        }

        #region IGameService Implementation

        public void Run()
        {
            State = GameServiceState.Starting;

            ChatTipManager.Initialize();

            State = GameServiceState.Running;
            while (State == GameServiceState.Running)
            {
                _serviceMailbox.ProcessMessages();

                ChatTipManager.Update();

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
