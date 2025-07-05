using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Network;

namespace MHServerEmu.PlayerManagement
{
    public class LoginQueueManager
    {
        private const ushort MuxChannel = 1;

        private readonly DoubleBufferQueue<IFrontendClient> _pendingQueue = new();

        private PlayerManagerService _playerManagerService;

        public LoginQueueManager(PlayerManagerService playerManagerService)
        {
            _playerManagerService = playerManagerService;
        }

        public void Update()
        {
            _pendingQueue.Swap();

            while (_pendingQueue.CurrentCount > 0)
            {
                IFrontendClient client = _pendingQueue.Dequeue();

                // TODO: LoginQueueStatus
                /*
                client.SendMessage(MuxChannel, LoginQueueStatus.CreateBuilder()
                    .SetPlaceInLine(Config.QueuePlaceInLine)
                    .SetNumberOfPlayersInLine(Config.QueueNumberOfPlayersInLine)
                    .Build());
                */

                client.SendMessage(MuxChannel, SessionEncryptionChanged.CreateBuilder()
                    .SetRandomNumberIndex(0)
                    .SetEncryptedRandomNumber(ByteString.Empty)
                    .Build());
            }
        }

        public void AcceptClient(IFrontendClient client)
        {
            _pendingQueue.Enqueue(client);
        }
    }
}
