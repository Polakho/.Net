using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Services;
using System.IO;

namespace Gauniv.Client.ViewModel
{
    internal partial class ProfileViewModel : ObservableObject
    {
        private string _installDirectory = NetworkService.Instance.InstallDirectory;
        public string InstallDirectory
        {
            get => _installDirectory;
            set => SetProperty(ref _installDirectory, value);
        }

        [RelayCommand]
        private void SaveInstallDir()
        {
            if (!string.IsNullOrWhiteSpace(InstallDirectory))
            {
                try
                {
                    Directory.CreateDirectory(InstallDirectory);
                    NetworkService.Instance.InstallDirectory = InstallDirectory;
                }
                catch
                {
                }
            }
        }
    }
}
