using System.Net.Http.Json;
using Gauniv.Client.Models;
using Gauniv.Client.Helpers;
using System.Net.Http.Headers;

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

        public async Task<List<Game>> GetGamesOwnedAsync()
        {
            try
            {
                var client = NetworkService.Instance.HttpClient;
                var request = new HttpRequestMessage(HttpMethod.Get, $"{AppConfig.BaseUrl}/api/1.0.0/Games/GetOwnedGames/ownedGames");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", NetworkService.Instance.Token);
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseData = await response.Content.ReadFromJsonAsync<List<Game>>();
                return responseData ?? new List<Game>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching owned games: {ex.Message}");
                return new List<Game>();
            }
        }

        public async Task<List<Tags>> GetTagsAsync()
        {
            try
            {
                var client = NetworkService.Instance.HttpClient;
                var response = await client.GetFromJsonAsync<List<Tags>>($"{AppConfig.BaseUrl}/api/1.0.0/Tags/GetGamesTags/tags");
                return response ?? new List<Tags>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching tags: {ex.Message}");
                return new List<Tags>();
            }
        }

        
    }
}