using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PayItOff.MauiClient.Services;
using PayItOff.MauiClient.Views;
using PayItOff.Shared.Requests;

namespace PayItOff.MauiClient.ViewModels;

public partial class LoginViewModel : PopupViewModelBase
{
    private readonly AuthService _authService;

    [ObservableProperty]
    public partial string EmailOrNickname { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;



    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial bool IsPasswordHidden { get; set; } = true;

    [ObservableProperty]
    public partial string PasswordIcon { get; set; } = "eye_closed.png";

    public LoginViewModel(AuthService authService)
    {
        _authService = authService;
        IsCustomAlertSupported = true;
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

        if (string.IsNullOrWhiteSpace(EmailOrNickname) || string.IsNullOrWhiteSpace(Password))
        {
            HasError = true;
            return;
        }

        IsBusy = true;
        HasError = false;

        try
        {
            var request = new LoginRequest { EmailOrNickname = EmailOrNickname, Password = Password };

            await _authService.LoginAsync(request);

            await Shell.Current.GoToAsync("//MainPage");
        }
        catch (Exception ex)
        {
            Password = string.Empty;
            HasError = true;

            await ShowAlertAsync("Błąd logowania", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoginAsJakubAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        HasError = false;

        try
        {
            var request = new LoginRequest { EmailOrNickname = "JakubPlocica", Password = "JakubPlocica123!" };

            await _authService.LoginAsync(request);

            await Shell.Current.GoToAsync("//MainPage");
        }
        catch (Exception ex)
        {
            Password = string.Empty;
            HasError = true;

            await ShowAlertAsync("Błąd logowania", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SeedHeavyDataAsync()
    {
        if (IsBusy) return;

        var password = await App.Current.MainPage!.DisplayPromptAsync("Seeder", "Wpisz hasło do seedera", "Uruchom", "Anuluj", "Hasło");
        if (string.IsNullOrWhiteSpace(password)) return;

        IsBusy = true;
        HasError = false;

        try
        {
            await _authService.SeedHeavyLoginDataAsync(password);
            await ShowAlertAsync("Seeder", "Seeder został wykonany. Możesz zalogować się na JakubPlocica.", "OK");
        }
        catch (Exception ex)
        {
            HasError = true;
            await ShowAlertAsync("Błąd seedera", ex.Message, "OK");
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
        PasswordIcon = IsPasswordHidden ? "eye_closed.png" : "eye_open.png";
    }

    [RelayCommand]
    private async Task GoToRegister()
    {
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }
}