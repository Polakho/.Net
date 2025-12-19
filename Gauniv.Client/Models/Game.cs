namespace Gauniv.Client.Models
{
    public class Game
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public ICollection<Tags> Tags { get; set; } = new List<Tags>();
    }
}