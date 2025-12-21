using System.Text.Json.Serialization;
namespace Gauniv.Client.Dto
{
    public class GameDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        [JsonIgnore]
        public string BinaryFilePath { get; set;}
        
        public double Price { get; set;}

        [JsonIgnore]
        public string ImagePath { get; set; }
        public ICollection<TagsDto> Tags { get; set; } = new List<TagsDto>();
    }
}