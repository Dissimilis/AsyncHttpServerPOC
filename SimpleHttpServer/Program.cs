using SimpleHttpServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SimpleHttpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var listener = new Listener(IPAddress.Any, 8081)
            {
                //beware that Console.WriteLine will decrease performance drastically
                Logger = Console.WriteLine
            };
            

            var myMiddleware = new MyMiddleware
            {
                GetCurrentConnectionCount = () => listener.OutstandingConnections
            };

            listener.ReponseHandler = myMiddleware.Handle;
            
            Console.WriteLine("Listening on port 8081");
            Console.WriteLine("Press any key to exit");
            Console.ReadKey(); //todo: would be nice to gracefully close all current connections. But generally it is not needed
        }
    }    
}
