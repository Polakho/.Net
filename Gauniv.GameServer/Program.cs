using System;

namespace Gauniv.GameServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
          var server = new Server(5000);
          _ = Task.Run(() => server.StartAsync());
          await Task.Delay(1000);

          var GameClient = new GameClient();
          GameClient.ConnectToServer();
          await Task.Delay(-1);
        }
    }
}
