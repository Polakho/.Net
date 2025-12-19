using System.Net.Http.Json;
using Gauniv.Client.Models;
using Gauniv.Client.Helpers;

namespace Gauniv.Client.Services
{
    public class GameService()
    {

        public async Task<List<Game>> GetGamesAsync()
        {
            try
            {
                var client = NetworkService.Instance.HttpClient;
                var response = await client.GetFromJsonAsync<List<Game>>($"{AppConfig.BaseUrl}/api/1.0.0/Games/GetGames/game");
                return response ?? new List<Game>();
            } 
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching games: {ex.Message}");
                return new List<Game>();
            }
        }

        public async Task<List<Game>> GetGamesOwnedAsync(string userId)
        {
            try
            {
                var client = NetworkService.Instance.HttpClient;
                var authorizationHeader = new AuthenticationHeaderValue("Bearer", NetworkService.Instance.Token);
                client.DefaultRequestHeaders.Authorization = authorizationHeader;
                var response = await client.GetFromJsonAsync<List<Game>>($"{AppConfig.BaseUrl}/api/1.0.0/Games/GetGamesOwned/", );
                return response ?? new List<Game>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching owned games: {ex.Message}");
                return new List<Game>();
            }
        }
    }
}