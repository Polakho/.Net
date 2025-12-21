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
    public partial class IndexViewModel: ObservableObject
    {
        private readonly GameService _gameService;
        private readonly List<Game> _allGames = new();
        private HashSet<int> _ownedIds = new();

        [ObservableProperty]
        private ObservableCollection<Game> _Games;
        [ObservableProperty]
        private bool _IsLoading;


        [ObservableProperty]
        private string _SearchString;

        [ObservableProperty]
        private string _MinPriceText;

        [ObservableProperty]
        private string _MaxPriceText;

        [ObservableProperty]
        private bool _SeeOwned = true;

        [ObservableProperty]
        private bool _NotOwned = true;

        public IndexViewModel()
        {
            _gameService = new GameService();
            _Games = new ObservableCollection<Game>();
            LoadGamesCommand.Execute(null);
        }

        public bool IsBusy => _IsLoading;
        public bool IsNotBusy => !_IsLoading;
        public bool IsAuthenticated => !string.IsNullOrEmpty(NetworkService.Instance.Token);

        partial void OnIsLoadingChanged(bool value)
        {
            OnPropertyChanged(nameof(IsBusy));
            OnPropertyChanged(nameof(IsNotBusy));
        }

        [RelayCommand]
        private async Task LoadGames()
        {
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                var gameList = await _gameService.GetGamesAsync();
                _allGames.Clear();
                _allGames.AddRange(gameList);

                await LoadOwnedAsync();
                ApplyFilters();
            } 
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void ApplyFilters()
        {
            IEnumerable<Game> local_query = _allGames;

            // Ownership
            if (IsAuthenticated)
            {
                if (_SeeOwned && !_NotOwned)
                {
                    local_query = local_query.Where(g => _ownedIds.Contains(g.Id));
                }
                else if (_NotOwned && !_SeeOwned)
                {
                    local_query = local_query.Where(g => !_ownedIds.Contains(g.Id));
                }
                else if (!_SeeOwned && !_NotOwned)
                {
                    local_query = Enumerable.Empty<Game>();
                }
            }

            // Search by name
            var local_search = _SearchString;
            if (!string.IsNullOrWhiteSpace(local_search))
            {
                local_search = local_search.Trim();
                local_query = local_query.Where(g => !string.IsNullOrEmpty(g.Name) && g.Name.Contains(local_search, StringComparison.OrdinalIgnoreCase));
            }

            // Price range
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
            SeeOwned = true;
            NotOwned = true;
            ApplyFilters();
        }

        private async Task LoadOwnedAsync()
        {
            _ownedIds.Clear();
            if (!IsAuthenticated) return;
            try
            {
                var local_owned = await _gameService.GetGamesOwnedAsync();
                foreach (var g in local_owned)
                {
                    _ownedIds.Add(g.Id);
                }
            }
            catch { }
        }

        

        partial void OnSearchStringChanged(string value) => ApplyFilters();
        partial void OnMinPriceTextChanged(string value) => ApplyFilters();
        partial void OnMaxPriceTextChanged(string value) => ApplyFilters();
        partial void OnSeeOwnedChanged(bool value) => ApplyFilters();
        partial void OnNotOwnedChanged(bool value) => ApplyFilters();
    }

    
}
