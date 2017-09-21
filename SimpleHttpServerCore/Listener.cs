using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SimpleHttpServerCore
{
    using System.Threading;
    using ReponseHandler = Func<Response, string, IDictionary<string, string>, Task>;
    public class Listener
    {
        public int OutstandingConnections => _outstandingConnections;
        private int _outstandingConnections = 0;
        private const int SocketBacklog = 128;
        private readonly Socket _socket;
        private readonly Task _listenTask;

        public Action<string> Logger { get; set; } //todo: consider proper non blocking logger with severity handling
        public ReponseHandler ReponseHandler { get; set; }

        public Listener(IPAddress ipAddress, int port, ReponseHandler reponseHandler = null)
        {
            ReponseHandler = reponseHandler;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(localEndPoint);
            _socket.Listen(SocketBacklog); 
            _listenTask = Task.Run(Listen);
        }

        private async Task Listen()
        {
            while (true)
            {
                var socket = await AcceptAsyncAwaitable(_socket);

                var handler = new RequestsHandler(socket, ReponseHandler)
                {
                    Logger = this.Logger
                };

                Interlocked.Increment(ref _outstandingConnections);

                //todo: we can add throttling here;
                //Now we are processing all incoming requests immediately
                handler.HandleRequestAsync().ContinueWith((t) =>
                {
                    Interlocked.Decrement(ref _outstandingConnections);
                });
            }
        }

        private Task<Socket> AcceptAsyncAwaitable(Socket socket)
        {
            return Task.Factory.FromAsync(socket.BeginAccept, socket.EndAccept, TaskCreationOptions.None);
        }

    }
}