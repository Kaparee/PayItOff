using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PayItOff.MauiClient.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool IsMenuVisible { get; set; }
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [RelayCommand]
    private void ToggleMenu()
    {
        IsMenuVisible = !IsMenuVisible;
    }

    [RelayCommand]
    private async Task Logout()
    {
        SecureStorage.Default.Remove("jwt_token");
        await Shell.Current.GoToAsync("//LoginPage");
    }

    [RelayCommand]
    private async Task Navigate(string route)
    {
        IsMenuVisible = false;

        if (!string.IsNullOrWhiteSpace(route))
        {


            await Shell.Current.GoToAsync(route);
        }
    }
}