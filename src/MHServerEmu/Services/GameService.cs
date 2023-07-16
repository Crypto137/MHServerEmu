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
        private static readonly Logger Logger = LogManager.CreateLogger();

        public PubSubServerTypes ServerType { get; protected set; }

        public virtual void Handle(FrontendClient client, byte messageId, byte[] message)
        {
            Logger.Warn($"Unimplemented server type {ServerType} received message id {messageId}");
        }
    }
}
