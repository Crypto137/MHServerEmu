using System.Buffers;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Web;
using Google.ProtocolBuffers;

namespace MHServerEmu.Core.Network.Web
{
    /// <summary>
    /// Wrapper for <see cref="HttpListenerContext"/>.
    /// </summary>
    public readonly struct WebRequestContext
    {
        private readonly HttpListenerRequest _httpRequest;
        private readonly HttpListenerResponse _httpResponse;

        public string UserAgent { get => _httpRequest.UserAgent; }
        public string LocalPath { get => _httpRequest.Url.LocalPath; }
        public string HttpMethod { get => _httpRequest.HttpMethod; }
        public string XForwardedFor { get => _httpRequest.Headers["X-Forwarded-For"]; }

        public bool IsGameClientRequest { get => UserAgent.Equals("Secret Identity Studios Http Client", StringComparison.InvariantCulture); }

        public int StatusCode { get => _httpResponse.StatusCode; set => _httpResponse.StatusCode = value; }

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

        public string GetIPAddress()
        {
            string forwardedFor = XForwardedFor;
            if (string.IsNullOrWhiteSpace(forwardedFor) == false)
                return forwardedFor;

            return _httpRequest.RemoteEndPoint.Address.ToString();
        }

        public void Redirect(string url)
        {
            _httpResponse.Redirect(url);
        }

        /// <summary>
        /// Asynchronously reads the request input stream as a UTF-8 string.
        /// </summary>
        public async Task<string> ReadUtf8StringAsync()
        {
            const long MaxLength = 1024 * 16;

            int length = (int)_httpRequest.ContentLength64;
            if (length < 0 || length > MaxLength)
                throw new InternalBufferOverflowException();

            byte[] buffer = ArrayPool<byte>.Shared.Rent(length);

            try
            {
                await _httpRequest.InputStream.ReadAsync(buffer.AsMemory(0, length));
                return Encoding.UTF8.GetString(buffer, 0, length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Asynchronously reads the request input stream as <typeparamref name="T"/> serialized using JSON.
        /// </summary>
        public async Task<T> ReadJsonAsync<T>()
        {
            return await JsonSerializer.DeserializeAsync<T>(_httpRequest.InputStream);
        }

        /// <summary>
        /// Asynchronously reads the request input stream as a <see cref="NameValueCollection"/>.
        /// </summary>
        public async Task<NameValueCollection> ReadQueryStringAsync()
        {
            string queryString = await ReadUtf8StringAsync();
            return HttpUtility.ParseQueryString(queryString);
        }

        /// <summary>
        /// Reads the request input stream as an <see cref="IMessage"/> of protocol <typeparamref name="T"/>.
        /// </summary>
        public IMessage ReadProtobuf<T>() where T: Enum
        {
            MessageBuffer messageBuffer = new(_httpRequest.InputStream);
            return messageBuffer.Deserialize<T>();
        }

        /// <summary>
        /// Asynchronously responds to the request with the provided payload.
        /// </summary>
        public async Task SendAsync(byte[] payload, string contentType)
        {
            _httpResponse.ContentType = contentType;
            _httpResponse.ContentLength64 = payload.Length;

            await _httpResponse.OutputStream.WriteAsync(payload);
        }

        /// <summary>
        /// Asynchronously responds to the request with the provided <see cref="string"/> encoded as UTF-8.
        /// </summary>
        public async Task SendAsync(string message, string contentType = "text/plain")
        {
            int maxByteCount = Encoding.UTF8.GetMaxByteCount(message.Length);

            byte[] buffer = ArrayPool<byte>.Shared.Rent(maxByteCount);

            try
            {
                int byteCount = Encoding.UTF8.GetBytes(message, 0, message.Length, buffer, 0);

                _httpResponse.ContentType = contentType;
                _httpResponse.ContentLength64 = byteCount;

                await _httpResponse.OutputStream.WriteAsync(buffer.AsMemory(0, byteCount));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        
        /// <summary>
        /// Asynchronously responds to the request with the provided <see cref="IMessage"/>.
        /// </summary>
        public async Task SendAsync(IMessage message)
        {
            MessagePackageOut payload = new(message);
            int size = payload.GetSerializedSize();

            byte[] buffer = ArrayPool<byte>.Shared.Rent(size);

            try
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(buffer);
                payload.WriteTo(cos);
                cos.Flush();

                _httpResponse.ContentType = "application/octet-stream";
                _httpResponse.ContentLength64 = size;

                await _httpResponse.OutputStream.WriteAsync(buffer.AsMemory(0, size));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Asynchronously responds to the request with the provided <typeparamref name="T"/> instance serialized to JSON.
        /// </summary>
        public async Task SendJsonAsync<T>(T @object)
        {
            await JsonSerializer.SerializeAsync(_httpResponse.OutputStream, @object);
        }
    }
}
