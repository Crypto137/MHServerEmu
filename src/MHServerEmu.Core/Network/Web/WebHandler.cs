using System.Net;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Core.Network.Web
{
    public abstract class WebHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public virtual WebApiAccessType Access { get => WebApiAccessType.None; }

        public WebService Service { get; private set; }
        public string LocalPath { get; private set; }

        /// <summary>
        /// Adds a reference to the <see cref="WebService"/> this <see cref="WebHandler"/> is registered to.
        /// </summary>
        internal void Register(WebService service, string localPath)
        {
            Service = service;
            LocalPath = localPath;
        }

        /// <summary>
        /// Removes the reference to the <see cref="WebService"/> this <see cref="WebHandler"/> is currently registered to.
        /// </summary>
        internal void Unregister()
        {
            Service = null;
            LocalPath = null;
        }

        /// <summary>
        /// Handles <see cref="WebRequestContext"/> asynchronously.
        /// </summary>
        internal async Task HandleAsync(WebRequestContext context)
        {
            try
            {
                if (Authorize(context) == false)
                {
                    context.StatusCode = (int)HttpStatusCode.Forbidden;
                    return;
                }

                switch (context.HttpMethod)
                {
                    case "GET":
                        await Get(context);
                        break;

                    case "POST":
                        await Post(context);
                        break;

                    case "DELETE":
                        await Delete(context);
                        break;

                    default:
                        await HandleMethodNotAllowed(context);
                        break;
                }
            }
            catch (Exception e)
            {
                context.StatusCode = (int)HttpStatusCode.InternalServerError;
                Logger.Warn($"Error handling {context}: {e}");
            }
        }

        /// <summary>
        /// Handles a GET request asynchronously.
        /// </summary>
        protected virtual Task Get(WebRequestContext context)
        {
            return HandleMethodNotAllowed(context);
        }

        /// <summary>
        /// Handles a POST request asynchronously.
        /// </summary>
        protected virtual Task Post(WebRequestContext context)
        {
            return HandleMethodNotAllowed(context);
        }

        /// <summary>
        /// Handles a DELETE request asynchronously.
        /// </summary>
        protected virtual Task Delete(WebRequestContext context)
        {
            return HandleMethodNotAllowed(context);
        }

        /// <summary>
        /// Fallback for unsupported HTTP method requests.
        /// </summary>
        private static Task HandleMethodNotAllowed(WebRequestContext context)
        {
            Logger.Warn($"Unsupported HTTP method {context.HttpMethod} for local path {context.LocalPath}");
            context.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            return Task.CompletedTask;
        }

        private bool Authorize(WebRequestContext context)
        {
            // NOTE: If we decide to add global rate limiting of some kind, this can be done here.

            WebApiAccessType access = Access;
            if (access == WebApiAccessType.None)
                return true;

            string ipAddressHandle = context.GetIPAddressHandle();
            string webApiKey = context.GetBearerToken();

            WebApiKeyVerificationResult result = WebApiKeyManager.Instance.VerifyKey(webApiKey, access, out string keyName);

            if (result != WebApiKeyVerificationResult.Success)
            {
                Logger.Warn($"Authorize(): Failed to authorize request to {LocalPath} from {ipAddressHandle} using key [{keyName}], result={result}");
                return false;
            }

            Logger.Info($"Authorized request to {LocalPath} from {ipAddressHandle} using key [{keyName}]");
            return true;
        }
    }
}
