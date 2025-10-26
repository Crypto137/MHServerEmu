using System.Net;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Core.Network.Web
{
    public abstract class WebHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public WebService Service { get; private set; }

        /// <summary>
        /// Adds a reference to the <see cref="WebService"/> this <see cref="WebHandler"/> is registered to.
        /// </summary>
        internal void Register(WebService service)
        {
            Service = service;
        }

        /// <summary>
        /// Removes the reference to the <see cref="WebService"/> this <see cref="WebHandler"/> is currently registered to.
        /// </summary>
        internal void Unregister()
        {
            Service = null;
        }

        /// <summary>
        /// Handles <see cref="WebRequestContext"/> asynchronously.
        /// </summary>
        internal async Task HandleAsync(WebRequestContext context)
        {
            try
            {
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
    }
}
