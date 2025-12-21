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

        public MyGamesViewModel()
        {
            _gameService = new GameService();
            _Games = new ObservableCollection<Game>();
        }

        [RelayCommand]
        private async Task LoadGames()
        {
            if (_IsLoading) return;

            _IsLoading = true;
            try
            {
                var gameList = await _gameService.GetGamesOwnedAsync();
                _Games.Clear();
                foreach (var game in gameList)
                {
                    _Games.Add(game);
                }
            } 
            finally
            {
                _IsLoading = false;
            }
        }
    }
}
