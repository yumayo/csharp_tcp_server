using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp4
{
    class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server();
            Task.Run(async () =>
            {
                server.Open(60128);
                await Task.Delay(100);
            });
            while(true)
            {
                Thread.Sleep(100);
                server.Service();
            }
        }
    }
}
