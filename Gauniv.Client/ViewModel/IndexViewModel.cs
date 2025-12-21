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
using Microsoft.Maui.ApplicationModel; // MainThread
using System.Collections.Specialized;
using Microsoft.Maui.Controls; // SelectionChangedEventArgs

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

        [ObservableProperty]
        private bool _CanPrev;
        [ObservableProperty]
        private bool _CanNext;

        public ObservableCollection<Tags> Tags { get; } = new();

        [ObservableProperty]
        private ObservableCollection<Tags> _SelectedTags = new();

        partial void OnSelectedTagsChanged(ObservableCollection<Tags> value)
        {
            // Re-wire CollectionChanged when binding replaces the instance
            if (value != null)
            {
                value.CollectionChanged -= OnSelectedTagsCollectionChanged;
                value.CollectionChanged += OnSelectedTagsCollectionChanged;
            }
            ApplyFilters();
        }

        public IndexViewModel()
        {
            _gameService = new GameService();
            _Games = new ObservableCollection<Game>();
            _SelectedTags.CollectionChanged += OnSelectedTagsCollectionChanged;
        }

        private void OnSelectedTagsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        public bool IsBusy => _IsLoading;
        public bool IsNotBusy => !_IsLoading;
        public bool IsAuthenticated => !string.IsNullOrEmpty(NetworkService.Instance.Token);

        partial void OnIsLoadingChanged(bool value)
        {
            OnPropertyChanged(nameof(IsBusy));
            OnPropertyChanged(nameof(IsNotBusy));
        }

        private int _offset = 0;
        private const int PageSize = 10;

        [RelayCommand]
        private async Task LoadGames()
        {
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                List<Game> gameList;
                var nextOffset = _offset + PageSize;

                if (_SelectedTags.Any())
                {
                    var tagNames = _SelectedTags.Select(t => t.Name).ToList();
                    System.Diagnostics.Debug.WriteLine($"[IndexViewModel] LoadGames with tags: {string.Join(",", tagNames)} (offset={_offset}, limit={PageSize})");
                    gameList = await _gameService.GetGamesAsync(tagNames, _offset, PageSize);
                    CanNext = await _gameService.HasNextGamesAsync(nextOffset, tagNames);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[IndexViewModel] LoadGames without tags (offset={_offset}, limit={PageSize})");
                    gameList = await _gameService.GetGamesAsync(_offset, PageSize);
                    CanNext = await _gameService.HasNextGamesAsync(nextOffset);
                }

                System.Diagnostics.Debug.WriteLine($"[IndexViewModel] Games received: {gameList.Count}");
                _allGames.Clear();
                _allGames.AddRange(gameList);

                // update pagination availability
                CanPrev = _offset > 0;

                if (IsAuthenticated)
                {
                    await LoadOwnedAsync();
                }

                ApplyFilters();
                System.Diagnostics.Debug.WriteLine($"[IndexViewModel] After ApplyFilters: Games displayed={_Games.Count}");
            } 
            finally
            {
                IsLoading = false;
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

        public async Task InitializeAsync()
        {
            await LoadTagsAsync();
            await LoadGames();
        }

        private async Task LoadTagsAsync()
        {
            Debug.WriteLine("LoadTagsAsync appelée");
            try
            {
                var tags = await _gameService.GetTagsAsync();
                Debug.WriteLine($"GetTagsAsync retourné {tags?.Count() ?? 0} tags");

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Tags.Clear();
                    foreach (var tag in tags)
                    {
                        Tags.Add(tag);
                    }
                    Debug.WriteLine($"Tags ObservableCollection remplie: {Tags.Count}");
                });

                foreach (var tag in Tags)
                {
                    Debug.WriteLine($"Tag affiché: {tag.Id} - {tag.Name}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadTagsAsync erreur: {ex}");
            }
        }

        [RelayCommand]
        private void ApplyFilters()
        {
            Debug.WriteLine($"[IndexViewModel] ApplyFilters start: total={_allGames.Count}, selectedTags={_SelectedTags.Count}, search='{_SearchString}', min='{_MinPriceText}', max='{_MaxPriceText}', seeOwned={_SeeOwned}, notOwned={_NotOwned}, isAuth={IsAuthenticated}");
            IEnumerable<Game> local_query = _allGames;

            if (IsAuthenticated)
            {
                if (_SeeOwned && !_NotOwned)
                    local_query = local_query.Where(g => _ownedIds.Contains(g.Id));
                else if (_NotOwned && !_SeeOwned)
                    local_query = local_query.Where(g => !_ownedIds.Contains(g.Id));
                else if (!_SeeOwned && !_NotOwned)
                    local_query = Enumerable.Empty<Game>();
            }

            if (_SelectedTags.Any())
            {
                var selectedTagNames = _SelectedTags.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                local_query = local_query.Where(game =>
                    game.Tags != null &&
                    game.Tags.Any(t => selectedTagNames.Contains(t.Name))
                );
                Debug.WriteLine($"[IndexViewModel] After tag filter: count={local_query.Count()}");
            }

            var local_search = _SearchString;
            if (!string.IsNullOrWhiteSpace(local_search))
            {
                local_search = local_search.Trim();
                local_query = local_query.Where(g => !string.IsNullOrEmpty(g.Name) && g.Name.Contains(local_search, StringComparison.OrdinalIgnoreCase));
                Debug.WriteLine($"[IndexViewModel] After search filter: count={local_query.Count()}");
            }

            if (double.TryParse(_MinPriceText, out var local_min))
            {
                local_query = local_query.Where(g => g.Price >= local_min);
                Debug.WriteLine($"[IndexViewModel] After min price filter: count={local_query.Count()}");
            }
            if (double.TryParse(_MaxPriceText, out var local_max))
            {
                local_query = local_query.Where(g => g.Price <= local_max);
                Debug.WriteLine($"[IndexViewModel] After max price filter: count={local_query.Count()}");
            }

            var local_list = local_query.ToList();
            _Games.Clear();
            foreach (var game in local_list)
                _Games.Add(game);

            Debug.WriteLine($"[IndexViewModel] ApplyFilters end: displayed={_Games.Count}");
        }

        [RelayCommand]
        private void ResetFilters()
        {
            SearchString = string.Empty;
            MinPriceText = string.Empty;
            MaxPriceText = string.Empty;
            SeeOwned = true;
            NotOwned = true;
            SelectedTags = new ObservableCollection<Tags>();
            ApplyFilters();
        }

        private async Task LoadOwnedAsync()
        {
            _ownedIds.Clear();
            if (!IsAuthenticated)
            {
                Debug.WriteLine("LoadOwnedAsync: Not authenticated, skipping owned games load.");
                return;
            }
            try
            {
                var local_owned = await _gameService.GetGamesOwnedAsync();
                if (local_owned == null)
                {
                    Debug.WriteLine("LoadOwnedAsync: GetGamesOwnedAsync returned null.");
                    return;
                }
                foreach (var g in local_owned)
                {
                    if (g == null)
                    {
                        continue;
                    }
                    _ownedIds.Add(g.Id);
                }
                Debug.WriteLine($"LoadOwnedAsync: Owned IDs loaded count={_ownedIds.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadOwnedAsync error: {ex}");
            }
        }

        [RelayCommand]
        private void TagsSelectionChanged(object argsObj)
        {
            var args = argsObj as SelectionChangedEventArgs;
            var selection = args?.CurrentSelection?.OfType<Tags>() ?? Enumerable.Empty<Tags>();
            var list = selection.ToList();
            Debug.WriteLine($"[IndexViewModel] TagsSelectionChanged: currentSelectionCount={list.Count}");
            if (list.Count > 0)
                Debug.WriteLine("[IndexViewModel] TagsSelectionChanged: selections=" + string.Join(",", list.Select(t => t.Name)));

            SelectedTags = new ObservableCollection<Tags>(list);
            Debug.WriteLine($"[IndexViewModel] SelectedTags now: {SelectedTags.Count}");
        }


        partial void OnSearchStringChanged(string value) => ApplyFilters();
        partial void OnMinPriceTextChanged(string value) => ApplyFilters();
        partial void OnMaxPriceTextChanged(string value) => ApplyFilters();
        partial void OnSeeOwnedChanged(bool value) => ApplyFilters();
        partial void OnNotOwnedChanged(bool value) => ApplyFilters();
        partial void OnSelectedTagsChanged(ObservableCollection<Tags>? oldValue, ObservableCollection<Tags> newValue)
        {
            ApplyFilters();
        }
    }
}
