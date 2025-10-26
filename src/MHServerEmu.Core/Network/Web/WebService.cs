using System.Diagnostics;
using System.Net;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Core.Network.Web
{
    public class WebService
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<string, WebHandler> _handlers = new(StringComparer.OrdinalIgnoreCase);

        private HttpListener _listener;
        private CancellationTokenSource _cts;

        public WebServiceSettings Settings { get; }
        public bool IsRunning { get; private set; }

        public int HandlerCount { get => _handlers.Count; }
        public int HandledRequests { get; private set; }

        public WebService(WebServiceSettings settings)
        {
            Settings = settings;
        }

        public override string ToString()
        {
            return Settings.Name;
        }

        /// <summary>
        /// Starts the web service. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool Start()
        {
            if (IsRunning)
                return false;

            Debug.Assert(_listener == null);
            Debug.Assert(_cts == null);

            string url = Settings.ListenUrl;

            _listener = new();
            _listener.Prefixes.Add(url);
            _listener.Start();

            _cts = new();
            Task.Run(HandleRequestsAsync);

            IsRunning = true;
            return true;
        }

        /// <summary>
        /// Stops the currently running REST service. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool Stop()
        {
            if (IsRunning == false)
                return false;

            Debug.Assert(_listener != null);
            Debug.Assert(_cts != null);

            _cts.Cancel();
            _cts.Dispose();
            _cts = null;

            _listener.Stop();
            _listener = null;

            IsRunning = false;
            return true;
        }

        /// <summary>
        /// Returns the currently registered <see cref="WebHandler"/> for the specified local path if available.
        /// Returns the fallback handler if no handler is registered for the local path, which may be <see langword="null"/>.
        /// </summary>
        public WebHandler GetHandler(string localPath)
        {
            if (_handlers.TryGetValue(localPath, out WebHandler handler) == false)
                return Settings.FallbackHandler;

            return handler;
        }

        /// <summary>
        /// Registers the provided <see cref="WebHandler"/> for the specified local path.
        /// Returns <see langword="true"/> if successful.
        /// </summary>
        public bool RegisterHandler(string localPath, WebHandler handler)
        {
            bool added = _handlers.TryAdd(localPath, handler);

            if (added)
                handler.Register(this);
            else
                Logger.Warn($"RegisterHandler(): Local path {localPath} already has a registered handler");

            return added;
        }

        /// <summary>
        /// Removed the currently registered <see cref="WebHandler"/> for the specified local path.
        /// Returns <see langword="true"/> if successful.
        /// </summary>
        public bool RemoveHandler(string localPath)
        {
            bool removed = _handlers.Remove(localPath, out WebHandler handler);

            if (removed)
                handler.Unregister();
            else
                Logger.Warn($"RemoveHandler(): No handler is registered for local path {localPath}");

            return removed;
        }

        /// <summary>
        /// Handles incoming requests asynchronously.
        /// </summary>
        private async Task HandleRequestsAsync()
        {
            Logger.Info($"{this} is listening on {Settings.ListenUrl}...");

            while (_cts.IsCancellationRequested == false)
            {
                try
                {
                    HttpListenerContext httpContext = await _listener.GetContextAsync().WaitAsync(_cts.Token);
                    WebRequestContext requestContext = new(httpContext);

                    // This may be either a registered handler or a fallback handler.
                    WebHandler handler = GetHandler(requestContext.LocalPath);
                    await handler?.HandleAsync(requestContext);

                    httpContext.Response.Close();

                    HandledRequests++;
                }
                catch (TaskCanceledException)
                {
                    return;
                }
                catch (Exception e)
                {
                    // NOTE: HandleRequest() should catch and handle exceptions when processing requests.
                    // If we got to this part, something must be wrong with the listener.
                    Logger.Error($"HandleRequestAsync(): {e}");
                    return;
                }
            }
        }
    }
}
