using Gazillion;
using Google.ProtocolBuffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu.Services.Implementations
{
    public class PlayerMgrServerFrontendService : GameService
    {
        private long _startTime;    // Used for calculating game time

        public PlayerMgrServerFrontendService()
        {
            ServerType = Gazillion.PubSubServerTypes.PLAYERMGR_SERVER_FRONTEND;
            _startTime = GetDateTime();
        }

        public override void Handle(FrontendClient client, byte messageId, byte[] message)
        {
            if (messageId == (byte)ClientToGameServerMessage.NetMessageReadyForGameJoin)
            {
                Console.WriteLine($"[PlayerMgrServerFrontendService] Received NetMessageReadyForGameJoin message");
                try
                {
                    var parsedMessage = NetMessageReadyForGameJoin.ParseFrom(message);
                    Console.Write(parsedMessage.ToString());
                }
                catch (InvalidProtocolBufferException e)
                {
                    Console.WriteLine($"[PlayerMgrServerFrontendService] Failed to parse NetMessageReadyForGameJoin message: {e.Message}");
                }

                /*
                Console.WriteLine("[PlayerMgrServerFrontendService] Responding with NetMessageReadyAndLoggedIn message");
                byte[] response = NetMessageReadyAndLoggedIn.CreateBuilder()
                    .Build().ToByteArray();
                client.SendGameServiceMessage(ServerType, (byte)GameServerToClientMessage.NetMessageReadyAndLoggedIn, response);
                */

                Console.WriteLine("[PlayerMgrServerFrontendService] Responding with NetMessageReadyForTimeSync message");
                byte[] response = NetMessageReadyForTimeSync.CreateBuilder()
                    .Build().ToByteArray();

                client.SendGameServiceMessage(ServerType, (byte)GameServerToClientMessage.NetMessageReadyForTimeSync, response);

                /*
                byte[] response = NetMessageSelectStartingAvatarForNewPlayer.CreateBuilder()
                    .Build().ToByteArray();
                client.SendGameServiceMessage(ServerType, (byte)GameServerToClientMessage.NetMessageSelectStartingAvatarForNewPlayer, response, GetGameTime());
                */
                
            }
            else if (messageId == (byte)ClientToGameServerMessage.NetMessageSyncTimeRequest)
            {
                Console.WriteLine($"[PlayerMgrServerFrontendService] Received NetMessageSyncTimeRequest message");
                var parsedMessage = NetMessageSyncTimeRequest.ParseFrom(message);
                Console.Write(parsedMessage.ToString());

                Console.WriteLine("[PlayerMgrServerFrontendService] Responding with NetMessageSyncTimeReply");

                byte[] response = NetMessageSyncTimeReply.CreateBuilder()
                    .SetGameTimeClientSent(parsedMessage.GameTimeClientSent)
                    .SetGameTimeServerReceived(GetGameTime())
                    .SetGameTimeServerSent(GetGameTime())

                    .SetDateTimeClientSent(parsedMessage.DateTimeClientSent)
                    .SetDateTimeServerReceived(GetDateTime())
                    .SetDateTimeServerSent(GetDateTime())

                    .SetDialation(0.0f)
                    .SetGametimeDialationStarted(GetGameTime())
                    .SetDatetimeDialationStarted(GetDateTime())
                    .Build().ToByteArray();

                client.SendGameServiceMessage(ServerType, (byte)GameServerToClientMessage.NetMessageSyncTimeReply, response);
            }
            else if (messageId == (byte)ClientToGameServerMessage.NetMessagePing)
            {
                Console.WriteLine($"[PlayerMgrServerFrontendService] Received NetMessagePing message");

                var parsedMessage = NetMessagePing.ParseFrom(message);
                Console.Write(parsedMessage.ToString());

                /*
                byte[] response = NetMessagePingResponse.CreateBuilder()
                    .SetDisplayOutput(false)
                    .SetRequestSentClientTime(parsedMessage.SendClientTime)
                    .SetRequestSentGameTime(parsedMessage.SendGameTime)
                    .Build().ToByteArray();

                client.SendGameServiceMessage(ServerType, (byte)GameServerToClientMessage.NetMessagePingResponse, response);
                */
            }
            else
            {
                Console.WriteLine($"[PlayerMgrServerFrontendService] Received unknown message id {messageId}");
            }
        }

        private long GetDateTime()
        {
            return ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds() * 1000;
        }

        private long GetGameTime()
        {
            return GetDateTime() - _startTime;
        }
    }
}
