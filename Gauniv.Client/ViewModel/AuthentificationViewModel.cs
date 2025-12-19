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
    public partial class AuthentificationViewModel: ObservableObject
    {
        private readonly AuthService _authService;

        [ObservableProperty]
        private string _Username;
        [ObservableProperty]
        private string _Password;
        [ObservableProperty]
        private bool _IsAuthenticating;

        public AuthentificationViewModel()
        {
            _authService = new AuthService();
        }

        [RelayCommand]
        private async Task Authenticate()
        {
            if (_IsAuthenticating) return;

            _IsAuthenticating = true;
            try
            {
                var success = await _authService.AuthenticateAsync(_Username, _Password);
                if (success)
                {
                    // We return to the previous page
                    
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    // Show an error message
                    await Application.Current.MainPage.DisplayAlert("Error", "Authentication failed. Please check your credentials.", "OK");
                }
            }
            finally
            {
                _IsAuthenticating = false;
            }
        }
    }
}