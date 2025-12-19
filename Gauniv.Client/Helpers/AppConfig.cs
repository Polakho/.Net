namespace Gauniv.Client.Helpers
{
    public static class AppConfig
    {
        public const string LocalIpAddress = "https://4b0nuxk7wab0.share.zrok.io";
        public const string ApiPort = "443";

        public static string BaseUrl
        {
            get
            {
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    return $"{LocalIpAddress}:{ApiPort}";
                }       

                return $"{LocalIpAddress}:{ApiPort}";
            }
        }

    }
}