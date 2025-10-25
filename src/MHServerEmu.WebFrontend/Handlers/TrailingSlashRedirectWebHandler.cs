using MHServerEmu.Core.Network.Web;

namespace MHServerEmu.WebFrontend.Handlers
{
    /// <summary>
    /// Add trailing slashes to requests and redirects them if needed.
    /// </summary>
    public class TrailingSlashRedirectWebHandler : WebHandler
    {
        private const string HtmlRedirect = @"<html><script>window.location.replace(window.location.href + '/');</script></html>";

        protected override async Task Get(WebRequestContext context)
        {
            await RedirectIfNeeded(context);
        }

        protected override async Task Post(WebRequestContext context)
        {
            await RedirectIfNeeded(context);
        }

        protected override async Task Delete(WebRequestContext context)
        {
            await RedirectIfNeeded(context);
        }

        private static async Task RedirectIfNeeded(WebRequestContext context)
        {
            if (context.LocalPath.EndsWith('/') == false)
            {
                // This is likely to be accessed from behind reverse proxy, in which case the client-side URL may be different from what we see server-side.
                // To make sure we add the trailing slash to the actual client URL, do the redirect client-side using JS for forwarded requests.
                if (string.IsNullOrWhiteSpace(context.XForwardedFor))
                    context.Redirect($"{context.LocalPath}/");
                else
                    await context.SendAsync(HtmlRedirect, "text/html"); 
            }
        }
    }
}
