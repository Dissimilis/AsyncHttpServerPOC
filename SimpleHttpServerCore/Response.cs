using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SimpleHttpServerCore
{
    public class Response : IDisposable
    {
        private readonly NetworkStream _networkStream;
        private readonly IDictionary<string, string> _headers = new Dictionary<string, string>();
        private bool _disposed = false;

        public Response (NetworkStream networkStream)
        {
            this._networkStream = networkStream;
        }

        public void AddHeader(string key, string value)
        {
            _headers[key] = value; //todo: add duplicate headers handling
        }

        public async Task SendErrorResponseAsync(int statusCode, string errorMessage)
        {
            await SendStringResponseAsync($"Error: {statusCode}; {errorMessage}", "text/plain", statusCode).ConfigureAwait(false);
        }
        public async Task SendStringResponseAsync(string content, string contentType = "text/plain; charset=UTF-8", int statusCode = 200)
        {
            await SendResponseAsync(Encoding.UTF8.GetBytes(content), contentType, statusCode);
        }
        public async Task SendResponseAsync(byte[] data, string contentType, int statusCode = 200)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Response));
            //Do not use AppendLine because we strictly need CRLF endlings
            var sbHeaders = new StringBuilder($"HTTP/1.1 {statusCode}\r\n");
            sbHeaders.Append($"Content-Length: {data.Length}\r\n");
            sbHeaders.Append($"Content-Type: {contentType}\r\n");
            if (_headers != null)
            {
                foreach (var h in _headers) //todo: handle the case when default headers are added by user
                {
                    sbHeaders.Append($"{h.Key}: {h.Value}\r\n"); //todo: deal with value encoding
                }
            }
            sbHeaders.Append("Connection: Close\r\n");
            sbHeaders.Append("\r\n");

            var headerBytes = Encoding.ASCII.GetBytes(sbHeaders.ToString());
            try //todo: handle exceptions when client aborts connection
            {
                await _networkStream.WriteAsync(headerBytes, 0, headerBytes.Length).ConfigureAwait(false);
                await _networkStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                await _networkStream.FlushAsync().ConfigureAwait(false);
            }
            finally
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            _networkStream.Dispose();
            _disposed = true;
        }
    }
}
