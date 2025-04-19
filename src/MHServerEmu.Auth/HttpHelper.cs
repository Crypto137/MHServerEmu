using System.Net;
using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Network;

namespace MHServerEmu.Auth
{
    public static class HttpHelper
    {
        /// <summary>
        /// Sends a string as a text/plain <see cref="HttpListenerResponse"/>.
        /// </summary>
        public static async Task SendPlainTextAsync(HttpListenerResponse httpResponse, string text, int statusCode = 200)
        {
            await SendTextAsync(httpResponse, text, "text/plain", statusCode);
        }

        /// <summary>
        /// Sends a string as a text/html <see cref="HttpListenerResponse"/>.
        /// </summary>
        public static async Task SendHtmlAsync(HttpListenerResponse httpResponse, string text, int statusCode = 200)
        {
            await SendTextAsync(httpResponse, text, "text/html", statusCode);
        }

        /// <summary>
        /// Sends an <see cref="IMessage"/> instance as an <see cref="HttpListenerResponse"/>.
        /// </summary>
        public static async Task SendProtobufAsync(HttpListenerResponse httpResponse, IMessage message, int statusCode = 200)
        {
            MessagePackageOut messagePackage = new(message);

            httpResponse.StatusCode = statusCode;
            httpResponse.KeepAlive = false;
            httpResponse.ContentType = "application/octet-stream";
            httpResponse.ContentLength64 = messagePackage.GetSerializedSize();

            CodedOutputStream cos = CodedOutputStream.CreateInstance(httpResponse.OutputStream);
            await Task.Run(() => { messagePackage.WriteTo(cos); cos.Flush(); });
        }

        /// <summary>
        /// Sends a string as an <see cref="HttpListenerResponse"/>.
        /// </summary>
        private static async Task SendTextAsync(HttpListenerResponse httpResponse, string text, string contentType, int statusCode = 200)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);

            httpResponse.StatusCode = statusCode;
            httpResponse.KeepAlive = false;
            httpResponse.ContentType = contentType;
            httpResponse.ContentLength64 = buffer.Length;

            await httpResponse.OutputStream.WriteAsync(buffer);
        }
    }
}
