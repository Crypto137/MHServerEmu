using Gazillion;
using Google.ProtocolBuffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu.Services.Implementations
{
    public class PlayerMgrServerSiteMgrControlService : GameService
    {
        public PlayerMgrServerSiteMgrControlService()
        {
            ServerType = Gazillion.PubSubServerTypes.PLAYERMGR_SERVER_SITEMGR_CONTROL;
        }
    }
}
