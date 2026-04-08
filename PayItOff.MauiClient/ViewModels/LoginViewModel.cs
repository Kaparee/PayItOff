using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PayItOff.MauiClient.Services;
using PayItOff.MauiClient.Views;
using PayItOff.Shared.Requests;

namespace PayItOff.MauiClient.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly AuthService _authService;

    [ObservableProperty]
    public partial string EmailOrNickname { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial bool IsPasswordHidden { get; set; } = true;

    [ObservableProperty]
    public partial string PasswordIcon { get; set; } = "👁️";

    public LoginViewModel(AuthService authService)
    {
        _authService = authService;
    }

    partial void OnEmailOrNicknameChanged(string value)
    {
        if (!string.IsNullOrEmpty(value)) HasError = false;
    }
    partial void OnPasswordChanged(string value)
    {
        if (!string.IsNullOrEmpty(value)) HasError = false;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        HasError = false;

        try
        {
            var request = new LoginRequest { EmailOrNickname = EmailOrNickname, Password = Password };
            var result = await _authService.LoginAsync(request);

            if (result == true)
            {
                await Shell.Current.GoToAsync("//MainDashboardPage");
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Password = string.Empty;
                    HasError = true;
                });
            }
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Password = string.Empty;
                HasError = true;
            });
            await Shell.Current.DisplayAlertAsync("Błąd", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordHidden = !IsPasswordHidden;
        PasswordIcon = IsPasswordHidden ? "👁️" : "🙈";
    }

    [RelayCommand]
    private async Task GoToRegister()
    {
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }
}