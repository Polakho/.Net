using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Pages;
using Gauniv.Client.Services;
using Gauniv.Client.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gauniv.Client.ViewModel
{
    public partial class MyGamesViewModel: ObservableObject
    {
        private readonly GameService _gameService;

        [ObservableProperty]
        private ObservableCollection<Game> _Games;
        [ObservableProperty]
        private bool _IsLoading;

        private int _offset = 0;
        private const int PageSize = 10;

        [ObservableProperty]
        private bool _CanPrev;
        [ObservableProperty]
        private bool _CanNext;

        public ObservableCollection<Tags> Tags { get; } = new();
        [ObservableProperty]
        private ObservableCollection<Tags> _SelectedTags = new();

        public MyGamesViewModel()
        {
            _gameService = new GameService();
            _Games = new ObservableCollection<Game>();
        }

        public async Task InitializeAsync()
        {
            await LoadTagsAsync();
            await LoadGames();
        }

        private async Task LoadTagsAsync()
        {
            try
            {
                var tags = await _gameService.GetTagsAsync();
                Tags.Clear();
                foreach (var t in tags)
                    Tags.Add(t);
                Debug.WriteLine($"[MyGamesViewModel] Tags loaded: {Tags.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MyGamesViewModel] LoadTags error: {ex}");
            }
        }

        [RelayCommand]
        private async Task LoadGames()
        {
            if (_IsLoading) return;

            _IsLoading = true;
            try
            {
                var gameList = await _gameService.GetGamesOwnedAsync(_offset, PageSize);
                // Update pagination availability
                CanPrev = _offset > 0;
                CanNext = await _gameService.HasNextOwnedGamesAsync(_offset + PageSize);

                _allGames = gameList.ToList();
                ApplyFilters();
            } 
            finally
            {
                _IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task NextPage()
        {
            if (!CanNext) return;
            _offset += PageSize;
            await LoadGames();
        }

        [RelayCommand]
        private async Task PrevPage()
        {
            if (!CanPrev) return;
            _offset = Math.Max(0, _offset - PageSize);
            await LoadGames();
        }

        // Simple filters: search, price, tags
        [ObservableProperty]
        private string _SearchString;
        [ObservableProperty]
        private string _MinPriceText;
        [ObservableProperty]
        private string _MaxPriceText;

        partial void OnSearchStringChanged(string value) => ApplyFilters();
        partial void OnMinPriceTextChanged(string value) => ApplyFilters();
        partial void OnMaxPriceTextChanged(string value) => ApplyFilters();
        partial void OnSelectedTagsChanged(ObservableCollection<Tags> value) => ApplyFilters();

        private List<Game> _allGames = new();

        private void ApplyFilters()
        {
            IEnumerable<Game> local_query = _allGames;

            // Tags by name (robust)
            if (_SelectedTags.Any())
            {
                var selectedTagNames = _SelectedTags.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                local_query = local_query.Where(g => g.Tags != null && g.Tags.Any(t => selectedTagNames.Contains(t.Name)));
            }

            // Search
            var local_search = _SearchString;
            if (!string.IsNullOrWhiteSpace(local_search))
            {
                local_search = local_search.Trim();
                local_query = local_query.Where(g => !string.IsNullOrEmpty(g.Name) && g.Name.Contains(local_search, StringComparison.OrdinalIgnoreCase));
            }

            // Price
            if (double.TryParse(_MinPriceText, out var local_min))
            {
                local_query = local_query.Where(g => g.Price >= local_min);
            }
            if (double.TryParse(_MaxPriceText, out var local_max))
            {
                local_query = local_query.Where(g => g.Price <= local_max);
            }

            var local_list = local_query.ToList();
            _Games.Clear();
            foreach (var game in local_list)
            {
                _Games.Add(game);
            }
        }

        [RelayCommand]
        private void ResetFilters()
        {
            SearchString = string.Empty;
            MinPriceText = string.Empty;
            MaxPriceText = string.Empty;
            SelectedTags = new ObservableCollection<Tags>();
            ApplyFilters();
        }
    }
}
