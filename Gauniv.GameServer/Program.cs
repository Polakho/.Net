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

          var GameClient = new GameClient("1");
          var GameClient2 = new GameClient("2");
          await GameClient2.ConnectToServer();
          await GameClient.ConnectToServer();
          
          var createGameRequest = new Message.CreateGameRequest
          {
              BoardSize = 19
          };
          var message = new Message.MessageGeneric
          {
              Type = Message.MessageType.CreateGame, 
              Data = MessagePack.MessagePackSerializer.Serialize(createGameRequest)
          };
          await GameClient.SendMessageAsync(message);
          
          // Get the game ID from console output and use it to join the game
          await Task.Delay(1000); // Wait for the game to be created
          var gameId = Console.ReadLine();
          
          var joinGameRequest = new Message.JoinGameRequest
          {
              GameId = gameId,
              AsSpectator = false
          };
          
            var joinMessage = new Message.MessageGeneric
            {
                Type = Message.MessageType.JoinGame,
                Data = MessagePack.MessagePackSerializer.Serialize(joinGameRequest)
            };
            
            await GameClient2.SendMessageAsync(joinMessage);
            await Task.Delay(3000);

            await GameClient.SendMessageAsync(joinMessage);
            
          await Task.Delay(-1);
        }
    }
}
