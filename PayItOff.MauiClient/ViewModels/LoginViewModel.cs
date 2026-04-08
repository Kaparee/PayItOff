using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PayItOff.MauiClient.Services;
using PayItOff.Shared.Requests;

namespace PayItOff.MauiClient.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly AuthService _authService;

    // Te pola podepniemy do okienek tekstowych (Entry) w widoku
    [ObservableProperty]
    public partial string EmailOrNickname { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    // To pole pokaże "kręciołek" ładowania, gdy czekamy na serwer
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    public LoginViewModel(AuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var request = new LoginRequest
            {
                EmailOrNickname = EmailOrNickname,
                Password = Password
            };

            var result = await _authService.LoginAsync(request);

            if (result == true)
            {
                await Shell.Current.DisplayAlertAsync("Sukces", $"Zalogowano!", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Błąd", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}