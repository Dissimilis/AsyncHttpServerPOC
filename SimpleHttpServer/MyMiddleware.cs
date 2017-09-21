using SimpleHttpServerCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimpleHttpServer
{

    /// <summary>
    /// This is just simple middleware to demonstrate functionality of web server
    /// </summary>
    class MyMiddleware
    {
        public Func<int> GetCurrentConnectionCount;
        public async Task Handle(Response response, string uri, IDictionary<string, string> headers)
        {
            var statusCode = 200;

            if (uri.Equals("/throw", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception();
            }
            else if (uri.Equals("/delay", StringComparison.OrdinalIgnoreCase))
            {
                await Task.Delay(5000);
            }
            else if (uri.Equals("/ThreadSleep", StringComparison.OrdinalIgnoreCase))
            {
                System.Threading.Thread.Sleep(5000);
            }
            else if (uri != "/")
            {
                statusCode = 404;
            }
            response.AddHeader("X-App", "MyCustomMiddleware");
            var page = GetIndexPage(uri, headers, statusCode);
            await response.SendStringResponseAsync(page, "text/html; charset=UTF-8", statusCode);            
        }

        private string GetIndexPage(string uri, IDictionary<string, string> headers, int statusCode)
        {
            var sbResponse = new StringBuilder();
            int connCount = GetCurrentConnectionCount?.Invoke() ?? -1;
            sbResponse.AppendLine("<html><body>");
            sbResponse.AppendLine("<h1>This is my custom middleware</h1>");
            sbResponse.AppendLine(@"<p>Available pages: 
                                    <ul>
                                        <li><a href='/'>/</a></li>
                                        <li><a href='/throw'>/throw</a></li>
                                        <li><a href='/delay'>/delay</a></li>
                                        <li><a href='/ThreadSleep'>/ThreadSleep</a></li>
                                    </ul>
                                 </p>");
            sbResponse.AppendLine($"<p>Current connections to server: <strong>{connCount}</strong></p>");
            sbResponse.AppendLine($"<p>Reponse status code: <strong>{statusCode}</strong></p>");
            sbResponse.AppendLine("<p>You requested this URI:</p>");
            sbResponse.AppendLine("<pre>");
            sbResponse.AppendLine(uri);
            sbResponse.AppendLine("</pre>");
            sbResponse.AppendLine("<p>Your headers are:</p>");
            sbResponse.AppendLine("<pre>");
            foreach (var h in headers)
            {
                sbResponse.AppendLine($"{h.Key}: {h.Value}");
            }
            sbResponse.AppendLine("</pre>");
            sbResponse.AppendLine("</body></html>");
            return sbResponse.ToString();
        }
    }
}
