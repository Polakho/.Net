using System;

namespace Gauniv.GameServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
          var server = new Server(5000);
            await server.StartAsync();
        }
    }
}
