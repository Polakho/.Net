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
}