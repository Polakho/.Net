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
        public bool ShowDownload => IsAuthenticated && IsOwned && !IsDownloaded;

        [ObservableProperty] private bool _isDownloaded;
        partial void OnIsDownloadedChanged(bool value)
        {
            OnPropertyChanged(nameof(ShowDelete));
            OnPropertyChanged(nameof(ShowLaunch));
            OnPropertyChanged(nameof(ShowDownload));
        }

        public bool ShowDelete => IsDownloaded;
        public bool ShowLaunch => IsDownloaded;

        [ObservableProperty] private string _launchText = "Lancer";

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
            OnPropertyChanged(nameof(ShowBuy));
            OnPropertyChanged(nameof(ShowDownload));
        }

        public async Task InitializeAsync()
        {
            if (!IsAuthenticated)
            {
                IsOwned = false;
                IsDownloaded = false;
                LaunchText = "Lancer";
                OnPropertyChanged(nameof(ShowBuy));
                OnPropertyChanged(nameof(ShowDownload));
                return;
            }
            try
            {
                IsLoading = true;
                IsOwned = await _service.IsGameOwnedAsync(Id);
                IsDownloaded = _service.IsGameDownloaded(Id);
                LaunchText = "Lancer";
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

        [RelayCommand]
        private async Task Download()
        {
            if (!IsAuthenticated) return;
            IsLoading = true;
            var path = await _service.DownloadGameAsync(Id);
            IsLoading = false;
            IsDownloaded = path != null;
        }

        [RelayCommand]
        private void Delete()
        {
            if (_service.DeleteDownloadedGame(Id))
            {
                IsDownloaded = false;
                LaunchText = "Lancer";
            }
        }

        [RelayCommand]
        private void Launch()
        {
            var path = _service.GetDownloadedPath(Id);
            if (string.IsNullOrEmpty(path)) return;

            var isWindows = OperatingSystem.IsWindows();
            var isExe = System.IO.Path.GetExtension(path).Equals(".exe", StringComparison.OrdinalIgnoreCase);
            if (!isWindows || !isExe)
            {
                System.Diagnostics.Debug.WriteLine($"[Launch] Not supported on this platform or file type. isWindows={isWindows}, isExe={isExe}");
                return;
            }

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true,
                        WorkingDirectory = System.IO.Path.GetDirectoryName(path) ?? NetworkService.Instance.InstallDirectory
                    }
                };
                process.Start();
                // Keep button text as "Lancer"; no stop logic
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Launch error: {ex}");
            }
        }
    }
}
