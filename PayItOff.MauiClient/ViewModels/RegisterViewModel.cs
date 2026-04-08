using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PayItOff.MauiClient.Services;
using PayItOff.Shared.Requests;

namespace PayItOff.MauiClient.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    private readonly RegisterService _registerService;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Nickname { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Surname { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PhoneNumber { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string IBAN { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ImageSource? AvatarPreviewSource { get; set; } = "default_avatar.png";

    [ObservableProperty]
    public partial FileResult? SelectedAvatarFile { get; set; }
    [ObservableProperty]
    public partial bool IsAvatarPlaceholderVisible { get; set; } = true;
    [ObservableProperty]
    public partial bool IsAvatarImageVisible { get; set; } = false;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial bool IsPasswordHidden { get; set; } = true;

    [ObservableProperty]
    public partial string PasswordIcon { get; set; } = "👁️";

    public RegisterViewModel(RegisterService registerService)
    {
        _registerService = registerService;
    }

    partial void OnEmailChanged(string value)
    {
        if (!string.IsNullOrEmpty(value)) HasError = false;
    }
    partial void OnPasswordChanged(string value)
    {
        if (!string.IsNullOrEmpty(value)) HasError = false;
    }
    [RelayCommand]
    private async Task Register()
    {
        if (IsBusy) return;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            await Shell.Current.DisplayAlertAsync("Błąd", "Email i Hasło są wymagane!", "OK");
            return;
        }

        try
        {
            IsBusy = true;

            var request = new RegisterRequest
            {
                Email = Email,
                Password = Password,
                Nickname = Nickname,
                Name = Name,
                Surname = Surname,
                PhoneNumber = PhoneNumber ?? string.Empty,
                IBAN = IBAN ?? string.Empty,
                AvatarUrl = string.Empty
            };

            var result = await _registerService.RegisterAsync(request, SelectedAvatarFile);

            if (result == true)
            {
                await Shell.Current.DisplayAlertAsync("Sukces", "Konto założone! Potwierdz rejestrację na mailu aby móc się zalogować..", "OK");
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            await Shell.Current.DisplayAlertAsync("Błąd", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    [Obsolete]
    private async Task PickAvatar()
    {
        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync();
            if (photo == null) return;

            SelectedAvatarFile = photo;

            var stream = await photo.OpenReadAsync();
            AvatarPreviewSource = ImageSource.FromStream(() => stream);

            // KLUCZ: Przełączamy widoczność ręcznie
            IsAvatarPlaceholderVisible = false;
            IsAvatarImageVisible = true;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Błąd", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordHidden = !IsPasswordHidden;
        PasswordIcon = IsPasswordHidden ? "👁️" : "🙈";
    }

    [RelayCommand]
    private async Task GoToLogin()
    {
        // Powrót do poprzedniej strony (LoginPage)
        await Shell.Current.GoToAsync("..");
    }
}