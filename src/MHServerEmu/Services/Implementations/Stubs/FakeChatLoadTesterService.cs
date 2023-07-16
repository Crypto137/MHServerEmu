using Gazillion;
using Google.ProtocolBuffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu.Services.Implementations
{
    public class FakeChatLoadTesterService : GameService
    {
        public FakeChatLoadTesterService()
        {
            ServerType = Gazillion.PubSubServerTypes.FAKE_CHAT_LOAD_TESTER;
        }
    }
}
