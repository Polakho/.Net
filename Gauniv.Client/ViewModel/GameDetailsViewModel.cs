using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Models;
using Gauniv.Client.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Gauniv.Client.ViewModel
{
    public partial class GameDetailsViewModel : ObservableObject
    {
        private readonly GameService _service = new();

        [ObservableProperty] private int _Id;
        [ObservableProperty] private string _Name;
        [ObservableProperty] private string _Description;
        [ObservableProperty] private double _Price;

        public ObservableCollection<Tags> Tags { get; } = new();

        public bool IsAuthenticated => NetworkService.Instance.IsConnected || !string.IsNullOrEmpty(NetworkService.Instance.Token);

        [ObservableProperty] private bool _IsOwned;
        partial void OnIsOwnedChanged(bool value)
        {
            OnPropertyChanged(nameof(ShowBuy));
            OnPropertyChanged(nameof(ShowDownload));
        }

        [ObservableProperty] private bool _IsLoading;

        public bool ShowBuy => IsAuthenticated && !IsOwned;
        public bool ShowDownload => IsAuthenticated && IsOwned;

        public GameDetailsViewModel()
        {
            NetworkService.Instance.OnConnected += () =>
            {
                OnPropertyChanged(nameof(IsAuthenticated));
                OnPropertyChanged(nameof(ShowBuy));
                OnPropertyChanged(nameof(ShowDownload));
            };
        }

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
            // Ensure visibility reflects current state
            OnPropertyChanged(nameof(ShowBuy));
            OnPropertyChanged(nameof(ShowDownload));
        }

        public async Task InitializeAsync()
        {
            if (!IsAuthenticated)
            {
                IsOwned = false;
                OnPropertyChanged(nameof(ShowBuy));
                OnPropertyChanged(nameof(ShowDownload));
                return;
            }
            try
            {
                IsLoading = true;
                IsOwned = await _service.IsGameOwnedAsync(Id);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task Buy()
        {
            if (!IsAuthenticated) return;
            try
            {
                IsLoading = true;
                var ok = await _service.BuyGameAsync(Id);
                if (ok)
                {
                    IsOwned = true;
                    OnPropertyChanged(nameof(ShowBuy));
                    OnPropertyChanged(nameof(ShowDownload));
                }
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
