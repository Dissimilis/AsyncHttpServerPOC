using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SimpleHttpServerCore
{
    using ReponseHandler = Func<Response, string, IDictionary<string, string>, Task>;
    public class RequestsHandler
    {
        public const string DefaultIndexPage = "<html><body>SimpleHttpServer is running!</body></html>";
        private const int NetworkStreamTimeout = 2;
        private readonly StreamReader _streamReader;
        private readonly ReponseHandler _reponseHandler;
        private readonly Response _response;
        public Action<string> Logger;

        public RequestsHandler(Socket socket, ReponseHandler reponseHandler)
        {
            this._reponseHandler = reponseHandler;
            var networkStream = new NetworkStream(socket, true)
            {
                ReadTimeout = NetworkStreamTimeout,
                WriteTimeout = NetworkStreamTimeout
            };            
            _streamReader = new StreamReader(networkStream);
            _response = new Response(networkStream);
        }

        public async Task HandleRequestAsync()
        {
            var firstLine = true;
            string line = null;
            string uri = null;
            var requestHeaders = new Dictionary<string, string>();
            try
            {
                while (line != string.Empty)
                {
                    //Reading of reaquest should be done using buffering since ReadLineAsync does not allow us to add constrains on lenght
                    //But buffered reading would add a lot of additional code, especially for detecting when headers end
                    //We are fully trusting client for now not to abuse server with infinite lines, slow writes, etc.
                    line = await _streamReader.ReadLineAsync().ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(line))
                    {
                        if (firstLine)
                        {
                            firstLine = false;
                            //Safe to just split by space according to https://www.w3.org/Protocols/rfc2616/rfc2616-sec5.html
                            //METHOD URI HTTP_VERSION
                            var requestParts = line.Split(' '); //todo: remove Split which causes unnecessary array allocation
                            if (requestParts.Length != 3)
                            {
                                await _response.SendErrorResponseAsync(400, "Invalid Request-Line");
                                return;
                            }
                            if (requestParts[0] != "GET") //RFC states that method names are case sensitive
                            {
                                await _response.SendErrorResponseAsync(405, "Server supports only GET method");
                                return;
                            }
                            uri = requestParts[1];
                        }
                        else
                        {
                            int separatorPos = line.IndexOf(':');
                            if (separatorPos == 0 || separatorPos == line.Length - 1)
                            {
                                await _response.SendErrorResponseAsync(400, "Request has invalid header");
                                return;
                            }
                            var key = line.Substring(0, separatorPos);
                            var value = line.Substring(separatorPos + 1).Trim();
                            requestHeaders[key] = value; //todo: silently overwriting duplicate headers for now (RFC says to merge)
                        }
                        Logger?.Invoke(line);
                    }
                }
                if (_reponseHandler != null)
                    await _reponseHandler(_response, uri, requestHeaders);
                else
                    await _response.SendStringResponseAsync(DefaultIndexPage, "text/html");
            }
            catch (Exception ex)
            {
                Logger?.Invoke(ex.ToString());
                //todo: check if connection is fine before trying to send error
                await _response.SendErrorResponseAsync(500, "Server error");
                return;
            }
            finally
            {
                _response.Dispose();
            }
        }        
    }
}