using Gazillion;
using Google.ProtocolBuffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu.Services.Implementations
{
    public class PlayerMgrServerSocialCommonService : GameService
    {
        public PlayerMgrServerSocialCommonService()
        {
            ServerType = Gazillion.PubSubServerTypes.PLAYERMGR_SERVER_SOCIAL_COMMON;
        }
    }
}
