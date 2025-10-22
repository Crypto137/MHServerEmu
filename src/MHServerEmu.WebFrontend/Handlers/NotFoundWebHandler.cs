using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network.Web;

namespace MHServerEmu.WebFrontend.Handlers
{
    /// <summary>
    /// Empty <see cref="WebHandler"/> that responds with status code 404 to incoming requests.
    /// </summary>
    public class NotFoundWebHandler : WebHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        protected override Task Get(WebRequestContext context)
        {
            Logger.Trace($"Get(): {context.LocalPath}");
            context.StatusCode = 404;
            return Task.CompletedTask;
        }

        protected override Task Post(WebRequestContext context)
        {
            Logger.Trace($"Post(): {context.LocalPath}");
            context.StatusCode = 404;
            return Task.CompletedTask;
        }

        protected override Task Delete(WebRequestContext context)
        {
            Logger.Trace($"Delete(): {context.LocalPath}");
            context.StatusCode = 404;
            return Task.CompletedTask;
        }
    }
}
