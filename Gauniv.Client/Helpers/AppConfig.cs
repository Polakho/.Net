namespace Gauniv.Client.Helpers
{
    public static class AppConfig
    {
        public const string LocalIpAddress = "https://localhost";
        public const string ApiPort = "";

        public static string BaseUrl
        {
            get
            {
                if (!string.IsNullOrEmpty(ApiPort))
                {
                    return $"{LocalIpAddress}:{ApiPort}";
                }       

                return $"{LocalIpAddress}";
            }
        }

    }
}