namespace Gauniv.WebServer.ViewModels
{
    public class CreateGameViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public string ImagePath { get; set; }
        public List<int> SelectedTagIds { get; set; } = new List<int>();
        public List<Gauniv.WebServer.Data.Tags> AvailableTags { get; set; } = new List<Gauniv.WebServer.Data.Tags>();
    }
}