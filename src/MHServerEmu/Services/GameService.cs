using Gazillion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu.Services
{
    public partial class GameService
    {
        public PubSubServerTypes ServerType { get; protected set; }

        public virtual void Handle(FrontendClient client, byte messageId, byte[] message)
        {
            Console.WriteLine($"[{ServerType}] Received message id {messageId}");
        }
    }
}
