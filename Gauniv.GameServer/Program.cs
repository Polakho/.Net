using System;

namespace Gauniv.GameServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine($"========================================");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DÉMARRAGE DU SERVEUR GAUNIV GAME SERVER");
            Console.WriteLine($"========================================");
            
            var server = new Server(5000);
            _ = Task.Run(() => server.StartAsync());
            /*
            await Task.Delay(1000);

            var GameClient = new GameClient("1");
            var GameClient2 = new GameClient("2");
            var GameClient3 = new GameClient("3");
            await GameClient2.ConnectToServer();
            await GameClient.ConnectToServer();
            await GameClient3.ConnectToServer();
            
            // Set player names
            var setNameRequest1 = new Message.SetPlayerNameRequest
            {
                Name = "Clément"
            };
            var setNameMessage1 = new Message.MessageGeneric
            {
                Type = Message.MessageType.SetPlayerName,
                Data = MessagePack.MessagePackSerializer.Serialize(setNameRequest1)
            };
            await GameClient.SendMessageAsync(setNameMessage1);
            
            var createGameRequest = new Message.CreateGameRequest
            {
                BoardSize = 19,
                GameName = "Test Game"
            };
            var message = new Message.MessageGeneric
            {
                Type = Message.MessageType.CreateGame,
                Data = MessagePack.MessagePackSerializer.Serialize(createGameRequest)
            };
            await GameClient.SendMessageAsync(message);

            // Get the game ID from console output and use it to join the game
            await Task.Delay(1000);
            Console.WriteLine("Enter the Game ID to join:");
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

            // Both clients join the same game
            await GameClient2.SendMessageAsync(joinMessage);
            await Task.Delay(3000);

            await GameClient.SendMessageAsync(joinMessage);

            await Task.Delay(3000);

            var getGamesMessage = new Message.MessageGeneric
            {
                Type = Message.MessageType.GetGameList,
                Data = MessagePack.MessagePackSerializer.Serialize("hello")
            };
            await GameClient3.SendMessageAsync(getGamesMessage);
            
            var joinGameRequestSpectate = new Message.JoinGameRequest
            {
                GameId = gameId,
                AsSpectator = true
            };

            var joinMessageSpectatete = new Message.MessageGeneric
            {
                Type = Message.MessageType.JoinGame,
                Data = MessagePack.MessagePackSerializer.Serialize(joinGameRequestSpectate)
            };

            await GameClient3.SendMessageAsync(joinMessageSpectatete);
            await Task.Delay(3000);

            testCaptures(gameId, GameClient, GameClient2);
            */
            
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Serveur en attente de connexions...");
            Console.WriteLine($"Appuyez sur Ctrl+C pour arrêter le serveur");
            await Task.Delay(-1);
        }

        static async void randomMove(string gameId, GameClient gameClient, GameClient gameClient2)
        {
            var random = new Random();
            //Random move from random client 
            for (int i = 0; i < 1000; i++)
            {
                int client = random.Next(1, 3);
                int x = random.Next(0, 18);
                int y = random.Next(0, 18);
                var makeMoveRequest = new Message.MakeMoveRequest
                {
                    GameId = gameId,
                    X = x,
                    Y = y,
                    IsPass = false
                };
                var makeMoveMessage = new Message.MessageGeneric
                {
                    Type = Message.MessageType.MakeMove,
                    Data = MessagePack.MessagePackSerializer.Serialize(makeMoveRequest)
                };
                if (client == 1)
                {
                    await gameClient.SendMessageAsync(makeMoveMessage);
                }
                else
                {
                    await gameClient2.SendMessageAsync(makeMoveMessage);
                }

                await Task.Delay(1000);
            }
        }

        static async void testCaptures(string gameId, GameClient gameClient, GameClient gameClient2)
        {
            //Moves to capture stones
            var moves = new List<(int x, int y, GameClient client)>
            {
                (3, 4, gameClient),
                (4, 4, gameClient2),
                (4, 3, gameClient),
                (5, 4, gameClient2),
                (4, 5, gameClient),
                (6, 4, gameClient2),
                (5, 3, gameClient),
                (0, 0, gameClient2),
                (5, 5, gameClient),
                (0, 1, gameClient2),
                (6, 3, gameClient),
                (1, 0, gameClient2),
                (6, 5, gameClient),
                (1, 1, gameClient2),
                (7, 4, gameClient),
                (2, 2, gameClient2),
                (3, 4, gameClient),

            };

            foreach (var move in moves)
            {
                var makeMoveRequest = new Message.MakeMoveRequest
                {
                    GameId = gameId,
                    X = move.x,
                    Y = move.y,
                    IsPass = false
                };
                var makeMoveMessage = new Message.MessageGeneric
                {
                    Type = Message.MessageType.MakeMove,
                    Data = MessagePack.MessagePackSerializer.Serialize(makeMoveRequest)
                };
                await move.client.SendMessageAsync(makeMoveMessage);
                await Task.Delay(1000);
            }
        }

        static async void testDoublePass(string gameId, GameClient gameClient, GameClient gameClient2)
        {
            //Both players pass consecutively
            for (int i = 0; i < 4; i++)
            {
                var makeMoveRequest1 = new Message.MakeMoveRequest
                {
                    GameId = gameId,
                    IsPass = true
                };
                var makeMoveMessage1 = new Message.MessageGeneric
                {
                    Type = Message.MessageType.MakeMove,
                    Data = MessagePack.MessagePackSerializer.Serialize(makeMoveRequest1)
                };
                await gameClient.SendMessageAsync(makeMoveMessage1);
                await Task.Delay(1000);

                var makeMoveRequest2 = new Message.MakeMoveRequest
                {
                    GameId = gameId,
                    IsPass = true
                };
                var makeMoveMessage2 = new Message.MessageGeneric
                {
                    Type = Message.MessageType.MakeMove,
                    Data = MessagePack.MessagePackSerializer.Serialize(makeMoveRequest2)
                };
                await gameClient2.SendMessageAsync(makeMoveMessage2);
                await Task.Delay(1000);
            }
        }

        static async void getListOfGames(GameClient gameClient)
        {
            var getGamesMessage = new Message.MessageGeneric
            {
                Type = Message.MessageType.GetGameList,
                Data = MessagePack.MessagePackSerializer.Serialize("hello")
            };
            await gameClient.SendMessageAsync(getGamesMessage);
        }
    }
}
