using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Models;
using System.Collections.ObjectModel;

namespace Gauniv.Client.ViewModel
{
    public partial class GameDetailsViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _Id;

        [ObservableProperty]
        private string _Name;

        [ObservableProperty]
        private string _Description;

        [ObservableProperty]
        private double _Price;

        public ObservableCollection<Tags> Tags { get; } = new();

        public void SetGame(Game game)
        {
            if (game == null) return;
            Id = game.Id;
            Name = game.Name;
            Description = game.Description;
            Price = game.Price;
            Tags.Clear();
            if (game.Tags != null)
            {
                foreach (var t in game.Tags)
                    Tags.Add(t);
            }
        }
    }
}
