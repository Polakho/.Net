using System.Net.Http.Json;
using Gauniv.Client.Models;
using Gauniv.Client.Helpers;
using System.Net.Http.Headers;
using Gauniv.Client.Services;
using System.Linq;

namespace Gauniv.Client.Services
{
    public class GameService()
    {

        public async Task<List<Game>> GetGamesAsync(int offset = 0, int limit = 50)
        {
            try
            {
                var client = NetworkService.Instance.HttpClient;
                var url = $"{AppConfig.BaseUrl}/api/1.0.0/Games/GetGames/game?offset={offset}&limit={limit}";
                Console.WriteLine($"[GameService] GetGamesAsync URL: {url}");
                var response = await client.GetFromJsonAsync<List<Game>>(url);
                Console.WriteLine($"[GameService] GetGamesAsync count: {response?.Count ?? 0}");
                return response ?? new List<Game>();
            } 
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching games: {ex.Message}");
                Console.WriteLine($"Error fetching games: {ex}");
                return new List<Game>();
            }
        }

        public async Task<List<Game>> GetGamesAsync(IEnumerable<string> tagNames, int offset = 0, int limit = 50)
        {
            try
            {
                var client = NetworkService.Instance.HttpClient;
                var baseUrl = $"{AppConfig.BaseUrl}/api/1.0.0/Games/GetGames/game";
                var list = tagNames?.Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? new List<string>();
                var tagQuery = list.Count > 0 ? string.Join("&", list.Select(n => $"TagNames={Uri.EscapeDataString(n)}")) : string.Empty;
                var pagingQuery = $"offset={offset}&limit={limit}";
                var query = string.IsNullOrEmpty(tagQuery) ? pagingQuery : $"{pagingQuery}&{tagQuery}";
                var url = $"{baseUrl}?{query}";
                Console.WriteLine($"[GameService] GetGamesAsync(tags) URL: {url}");
                var response = await client.GetFromJsonAsync<List<Game>>(url);
                Console.WriteLine($"[GameService] GetGamesAsync(tags) count: {response?.Count ?? 0}");
                return response ?? new List<Game>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching games with tags: {ex.Message}");
                Console.WriteLine($"Error fetching games with tags: {ex}");
                return new List<Game>();
            }
        }

        public async Task<List<Game>> GetGamesOwnedAsync()
        {
            try
            {
                var client = NetworkService.Instance.HttpClient;
                var url = $"{AppConfig.BaseUrl}/api/1.0.0/Games/GetOwnedGames/ownedGames";
                Console.WriteLine($"[GameService] GetGamesOwnedAsync URL: {url}");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (!string.IsNullOrEmpty(NetworkService.Instance.Token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", NetworkService.Instance.Token);
                var response = await client.SendAsync(request);
                Console.WriteLine($"[GameService] GetGamesOwnedAsync status: {response.StatusCode}");
                response.EnsureSuccessStatusCode();
                var responseData = await response.Content.ReadFromJsonAsync<List<Game>>();
                Console.WriteLine($"[GameService] GetGamesOwnedAsync count: {responseData?.Count ?? 0}");
                return responseData ?? new List<Game>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching owned games: {ex.Message}");
                Console.WriteLine($"Error fetching owned games: {ex}");
                return new List<Game>();
            }
        }

        public async Task<List<Game>> GetGamesOwnedAsync(int offset, int limit)
        {
            try
            {
                var client = NetworkService.Instance.HttpClient;
                var url = $"{AppConfig.BaseUrl}/api/1.0.0/Games/GetOwnedGames/ownedGames?offset={offset}&limit={limit}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (!string.IsNullOrEmpty(NetworkService.Instance.Token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", NetworkService.Instance.Token);
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseData = await response.Content.ReadFromJsonAsync<List<Game>>();
                return responseData ?? new List<Game>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching owned games (paged): {ex.Message}");
                return new List<Game>();
            }
        }

        public async Task<List<Tags>> GetTagsAsync()
        {
            try
            {
                var client = NetworkService.Instance.HttpClient;
                var url = $"{AppConfig.BaseUrl}/api/1.0.0/Games/GetGameTags/tags";
                Console.WriteLine($"[GameService] GetTagsAsync URL: {url}");
                var response = await client.GetFromJsonAsync<List<Tags>>(url);
                Console.WriteLine($"[GameService] GetTagsAsync count: {response?.Count ?? 0}");
                return response ?? new List<Tags>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching tags: {ex.Message}");
                Console.WriteLine($"Error fetching tags: {ex}");
                return new List<Tags>();
            }
        }

        public async Task<bool> HasNextGamesAsync(int nextOffset, IEnumerable<string>? tagNames = null)
        {
            try
            {
                var client = NetworkService.Instance.HttpClient;
                var baseUrl = $"{AppConfig.BaseUrl}/api/1.0.0/Games/GetGames/game";
                var list = tagNames?.Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? new List<string>();
                var tagQuery = list.Count > 0 ? string.Join("&", list.Select(n => $"TagNames={Uri.EscapeDataString(n)}")) : string.Empty;
                var pagingQuery = $"offset={nextOffset}&limit=1";
                var query = string.IsNullOrEmpty(tagQuery) ? pagingQuery : $"{pagingQuery}&{tagQuery}";
                var url = $"{baseUrl}?{query}";
                var response = await client.GetFromJsonAsync<List<Game>>(url);
                return (response?.Count ?? 0) > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> HasNextOwnedGamesAsync(int nextOffset)
        {
            try
            {
                var client = NetworkService.Instance.HttpClient;
                var url = $"{AppConfig.BaseUrl}/api/1.0.0/Games/GetOwnedGames/ownedGames?offset={nextOffset}&limit=1";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (!string.IsNullOrEmpty(NetworkService.Instance.Token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", NetworkService.Instance.Token);
                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return false;
                var data = await response.Content.ReadFromJsonAsync<List<Game>>();
                return (data?.Count ?? 0) > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsGameOwnedAsync(int gameId)
        {
            try
            {
                var client = NetworkService.Instance.HttpClient;
                var url = $"{AppConfig.BaseUrl}/api/1.0.0/Games/IsGameOwned/isOwned?gameId={gameId}";
                var resp = await client.GetFromJsonAsync<IsOwnedResponse>(url);
                return resp?.isOwned ?? false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IsGameOwned error: {ex}");
                return false;
            }
        }

        public async Task<bool> BuyGameAsync(int gameId)
        {
            try
            {
                var client = NetworkService.Instance.HttpClient;
                var url = $"{AppConfig.BaseUrl}/api/1.0.0/Games/BuyGame/buyGame?gameId={gameId}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (!string.IsNullOrEmpty(NetworkService.Instance.Token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", NetworkService.Instance.Token);
                var response = await client.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BuyGame error: {ex}");
                return false;
            }
        }

        public async Task<string?> DownloadGameAsync(int gameId)
        {
            try
            {
                var client = NetworkService.Instance.HttpClient;
                var url = $"{AppConfig.BaseUrl}/api/1.0.0/Games/DownloadBinaryFile/download?gameId={gameId}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (!string.IsNullOrEmpty(NetworkService.Instance.Token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", NetworkService.Instance.Token);
                var response = await client.SendAsync(request);

                System.Diagnostics.Debug.WriteLine($"[Download] URL: {url}");
                System.Diagnostics.Debug.WriteLine($"[Download] Status: {(int)response.StatusCode} {response.ReasonPhrase}");
                System.Diagnostics.Debug.WriteLine($"[Download] Content-Type: {response.Content.Headers.ContentType}");
                var cdHeader = response.Content.Headers.TryGetValues("Content-Disposition", out var cdVals) ? string.Join(", ", cdVals) : "(none)";
                System.Diagnostics.Debug.WriteLine($"[Download] Content-Disposition: {cdHeader}");

                if (!response.IsSuccessStatusCode)
                    return null;

                var bytes = await response.Content.ReadAsByteArrayAsync();
                System.Diagnostics.Debug.WriteLine($"[Download] Bytes length: {bytes.Length}");
                var head = BitConverter.ToString(bytes.Take(16).ToArray());
                System.Diagnostics.Debug.WriteLine($"[Download] First 16 bytes (hex): {head}");

                // Validate PE signature (Windows EXE starts with 'MZ')
                if (bytes.Length < 2 || bytes[0] != (byte)'M' || bytes[1] != (byte)'Z')
                {
                    System.Diagnostics.Debug.WriteLine("[Download] Invalid EXE content: missing MZ header");
                    return null;
                }

                var dir = NetworkService.Instance.InstallDirectory;
                Directory.CreateDirectory(dir);

                var filePath = Path.Combine(dir, $"game_{gameId}.exe");
                await File.WriteAllBytesAsync(filePath, bytes);
                System.Diagnostics.Debug.WriteLine($"[Download] Saved to: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DownloadGameAsync error: {ex}");
                return null;
            }
        }

        public bool IsGameDownloaded(int gameId)
        {
            var dir = NetworkService.Instance.InstallDirectory;
            var filePath = Path.Combine(dir, $"game_{gameId}.exe");
            return File.Exists(filePath);
        }

        public string? GetDownloadedPath(int gameId)
        {
            var dir = NetworkService.Instance.InstallDirectory;
            var filePath = Path.Combine(dir, $"game_{gameId}.exe");
            return File.Exists(filePath) ? filePath : null;
        }

        public bool DeleteDownloadedGame(int gameId)
        {
            try
            {
                var dir = NetworkService.Instance.InstallDirectory;
                var filePath = Path.Combine(dir, $"game_{gameId}.exe");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteDownloadedGame error: {ex}");
                return false;
            }
        }
    }

    public class IsOwnedResponse
    {
        public bool isOwned { get; set; }
    }
}