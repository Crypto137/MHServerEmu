using Gazillion;
using Google.ProtocolBuffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu.Services.Implementations
{
    public class GameInstanceServerMatchService : GameService
    {
        public GameInstanceServerMatchService()
        {
            ServerType = Gazillion.PubSubServerTypes.GAME_INSTANCE_SERVER_MATCH;
        }
    }
}
