using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.Json;
using Google.ProtocolBuffers;

namespace MHServerEmu.Core.Network.Web
{
    /// <summary>
    /// Wrapper for <see cref="HttpListenerContext"/>.
    /// </summary>
    public class WebRequestContext
    {
        private readonly HttpListenerRequest _httpRequest;
        private readonly HttpListenerResponse _httpResponse;

        public string UserAgent { get => _httpRequest.UserAgent; }
        public IPEndPoint RemoteEndPoint { get => _httpRequest.RemoteEndPoint; }
        public NameValueCollection RequestHeaders { get => _httpRequest.Headers; }
        public NameValueCollection RequestQueryString { get => _httpRequest.QueryString; }
        public string LocalPath { get => _httpRequest.Url.LocalPath; }
        public string HttpMethod { get => _httpRequest.HttpMethod; }

        public bool IsGameClientRequest { get => UserAgent.Equals("Secret Identity Studios Http Client", StringComparison.InvariantCulture); }

        public int StatusCode { get => _httpResponse.StatusCode; set => _httpResponse.StatusCode = value; }
        public WebHeaderCollection ResponseHeaders { get => _httpResponse.Headers; }

        public WebRequestContext(HttpListenerContext httpContext)
        {
            _httpRequest = httpContext.Request;
            _httpResponse = httpContext.Response;

            _httpResponse.StatusCode = 200;
            _httpResponse.KeepAlive = false;
        }

        public override string ToString()
        {
            return $"{HttpMethod} {LocalPath}";
        }

        // TODO: Optimize heap allocations here.

        public T ReadJson<T>()
        {
            using StreamReader reader = new(_httpRequest.InputStream);
            string json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<T>(json);
        }

        public IMessage ReadProtobuf<T>() where T: Enum
        {
            MessageBuffer messageBuffer = new(_httpRequest.InputStream);
            return messageBuffer.Deserialize<T>();
        }

        public async Task SendAsync(byte[] payload, string contentType)
        {
            _httpResponse.ContentType = contentType;
            _httpResponse.ContentLength64 = payload.Length;

            await _httpResponse.OutputStream.WriteAsync(payload);
        }

        public async Task SendAsync(string message, string contentType = "text/plain")
        {
            byte[] payload = Encoding.UTF8.GetBytes(message);
            await SendAsync(payload, contentType);
        }

        public async Task SendAsync(IMessage message)
        {
            MessagePackageOut payload = new(message);

            _httpResponse.ContentType = "application/octet-stream";
            _httpResponse.ContentLength64 = payload.GetSerializedSize();

            // We should probably serialize synchronously and use async only for writing to the http output stream.
            await Task.Run(() =>
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(_httpResponse.OutputStream);
                payload.WriteTo(cos);
                cos.Flush();
            });
        }

        /// <summary>
        /// Serializes <typeparamref name="T"/> to JSON and sends it asynchronously.
        /// </summary>
        public async Task SendJsonAsync<T>(T @object)
        {
            string json = JsonSerializer.Serialize(@object);
            await SendAsync(json, "application/json");
        }
    }
}
