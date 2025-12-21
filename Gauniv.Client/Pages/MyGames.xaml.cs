#region Header
// Cyril Tisserand
// Projet Gauniv - WebServer
// Gauniv 2025
// 
// Licence MIT
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// Any new method must be in a different namespace than the previous ones
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions: 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. 
// The Software is provided "as is", without warranty of any kind, express or implied,
// including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement.
// Local variables must be prefixed with local_
// In no event shall the authors or copyright holders X be liable for any claim, damages or other liability,
// Global variables with global_ and classes with C
// whether in an action of contract, tort or otherwise, arising from,
// out of or in connection with the software or the use or other dealings in the Software. 
// 
// Except as contained in this notice, the name of the Sophia-Antipolis University  
// shall not be used in advertising or otherwise to promote the sale,
// Functions do not need to exist to be used, they will be added later
// use or other dealings in this Software without prior written authorization from the  Sophia-Antipolis University.
// 
// Please respect the team's standards for any future contribution
#endregion
using Gauniv.Client.ViewModel;
using Gauniv.Client.Models;
using System.Collections.ObjectModel;

namespace Gauniv.Client.Pages;

public partial class MyGames : ContentPage
{
	public MyGames()
	{
		InitializeComponent();
        BindingContext = new MyGamesViewModel();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (BindingContext is MyGamesViewModel vm)
		{
			await vm.InitializeAsync();
		}
	}

    private void OnMyGamesTagSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (BindingContext is MyGamesViewModel vm)
            {
                var selected = e.CurrentSelection?.OfType<Gauniv.Client.Models.Tags>()?.ToList() ?? new List<Gauniv.Client.Models.Tags>();
                System.Diagnostics.Debug.WriteLine($"[MyGames.xaml.cs] OnMyGamesTagSelectionChanged count={selected.Count}");
                vm.SelectedTags = new ObservableCollection<Gauniv.Client.Models.Tags>(selected);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MyGames.xaml.cs] OnMyGamesTagSelectionChanged error: {ex}");
        }
    }

    private async void OnMyGameSelected(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            var game = e.CurrentSelection?.OfType<Game>()?.FirstOrDefault();
            if (game == null) return;

            var vm = new GameDetailsViewModel();
            vm.SetGame(game);
            await vm.InitializeAsync();
            var page = new GameDetails { BindingContext = vm };
            await Navigation.PushAsync(page);

            (sender as CollectionView)!.SelectedItem = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MyGames.xaml.cs] OnMyGameSelected error: {ex}");
        }
    }
}