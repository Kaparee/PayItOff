using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PayItOff.MauiClient.ViewModels;

// To jest klasa bazowa. Piszesz logikę raz, używasz wszędzie.
public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool IsMenuVisible { get; set; }

    [RelayCommand]
    private void ToggleMenu()
    {
        IsMenuVisible = !IsMenuVisible;
    }

    [RelayCommand]
    private async Task Logout()
    {
        SecureStorage.Default.Remove("auth_token");
        await Shell.Current.GoToAsync("//LoginPage");
    }

    [RelayCommand]
    private async Task Navigate(string route)
    {
        IsMenuVisible = false; // Zamyka menu mobilne przed zmianą strony

        if (!string.IsNullOrWhiteSpace(route))
        {
            // Przechodzi do wybranej strony.
            // Upewnij się, że masz zarejestrowane ścieżki w AppShell.xaml!
            await Shell.Current.GoToAsync(route);
        }
    }
}