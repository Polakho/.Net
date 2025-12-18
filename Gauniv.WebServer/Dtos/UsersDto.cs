namespace Gauniv.WebServer.Dtos
{
    public class UsersDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool IsOnline { get; set; }
        public ICollection<GameDto> OwnedGames { get; set; } = new List<GameDto>();
    }

    public class UserWithFriendsDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool IsOnline { get; set; }
        public ICollection<GameDto> OwnedGames { get; set; } = new List<GameDto>();
        public ICollection<UserBasicDto> Friends { get; set; } = new List<UserBasicDto>();
        public ICollection<UserBasicDto> FriendApplications { get; set; } = new List<UserBasicDto>();
    }

    public class UserBasicDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
    }
}