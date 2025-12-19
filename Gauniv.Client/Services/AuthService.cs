namespace Gauniv.Client.Services
{
    internal partial class NetworkService : ObservableObject
    {
        public void SetToken(string token)
        {
            Token = token;
        }

        public void Authentification(string username, string password)
        {
            var client = HttpClient;
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password)
            });
            var response = await client.PostAsync($"{AppConfig.BaseUrl}/Bearer/Login", content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                SetToken(responseContent);
                OnConnected?.Invoke();
            }
        }
    }
}