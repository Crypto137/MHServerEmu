using MHServerEmu.Core.Network.Web;

namespace MHServerEmu.WebFrontend.Handlers
{
    /// <summary>
    /// Add trailing slashes to requests and redirects them if needed.
    /// </summary>
    public class TrailingSlashRedirectWebHandler : WebHandler
    {
        protected override Task Get(WebRequestContext context)
        {
            return RedirectIfNeeded(context);
        }

        protected override Task Post(WebRequestContext context)
        {
            return RedirectIfNeeded(context);
        }

        protected override Task Delete(WebRequestContext context)
        {
            return RedirectIfNeeded(context);
        }

        private static Task RedirectIfNeeded(WebRequestContext context)
        {
            string url = context.Url;

            if (url.EndsWith('/') == false)
            {
                url = $"{url}/";
                context.Redirect(url);
            }

            return Task.CompletedTask;
        }
    }
}
