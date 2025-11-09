using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network.Web;

namespace MHServerEmu.WebFrontend.Handlers
{
    /// <summary>
    /// Serves a static file.
    /// </summary>
    public class StaticFileWebHandler : WebHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly string _filePath;
        private readonly string _contentType;

        private byte[] _data = Array.Empty<byte>();

        public StaticFileWebHandler(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            _filePath = filePath;
            _contentType = GetContentType(filePath);

            Load();
        }

        public void Load()
        {
            if (File.Exists(_filePath) == false)
            {
                Logger.Warn($"Load(): File not found at '{_filePath}'");
                return;
            }

            _data = File.ReadAllBytes(_filePath);
            Logger.Trace($"Loaded '{_filePath}'");
        }

        protected override async Task Get(WebRequestContext context)
        {
            await context.SendAsync(_data, _contentType);
        }

        /// <summary>
        /// Returns the MIME type for the specified file path.
        /// </summary>
        private static string GetContentType(string filePath)
        {
            ReadOnlySpan<char> extension = Path.GetExtension(filePath.AsSpan());

            // Add more extensions as needed.
            return extension switch
            {
                ".txt"  => "text/plain",
                ".html" => "text/html",
                ".css"  => "text/css",
                ".js"   => "text/javascript",
                _       => "application/octet-stream",
            };
        }
    }
}
