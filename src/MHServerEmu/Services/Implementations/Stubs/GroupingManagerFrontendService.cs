using Gazillion;
using Google.ProtocolBuffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu.Services.Implementations
{
    public class GroupingManagerFrontendService : GameService
    {
        public GroupingManagerFrontendService()
        {
            ServerType = Gazillion.PubSubServerTypes.GROUPING_MANAGER_FRONTEND;
        }
    }
}
