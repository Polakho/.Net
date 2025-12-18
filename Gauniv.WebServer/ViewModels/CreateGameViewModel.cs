using System.ComponentModel.DataAnnotations;

namespace Gauniv.WebServer.ViewModels
{
    public class CreateGameViewModel
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public string ImagePath { get; set; }
        [Required]
        public IFormFile BinaryFile { get; set; }
        public List<int> SelectedTagIds { get; set; } = new List<int>();
        public List<Gauniv.WebServer.Data.Tags> AvailableTags { get; set; } = new List<Gauniv.WebServer.Data.Tags>();
    }
}