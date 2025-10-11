using MHServerEmu.Core.Network.Web;

namespace MHServerEmu.Auth.Handlers
{
    /// <summary>
    /// Empty <see cref="WebHandler"/> that responds with status code 404 to incoming requests.
    /// </summary>
    public class NotFoundWebHandler : WebHandler
    {
        protected override Task Get(WebRequestContext context)
        {
            context.StatusCode = 404;
            return Task.CompletedTask;
        }

        protected override Task Post(WebRequestContext context)
        {
            context.StatusCode = 404;
            return Task.CompletedTask;
        }

        protected override Task Delete(WebRequestContext context)
        {
            context.StatusCode = 404;
            return Task.CompletedTask;
        }
    }
}
