using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PayItOff.MauiClient.Services;
using PayItOff.Shared.Requests;

namespace PayItOff.MauiClient.ViewModels;

public partial class RegisterViewModel : PopupViewModelBase
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
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial bool IsPasswordHidden { get; set; } = true;

    [ObservableProperty]
    public partial string PasswordIcon { get; set; } = "eye_closed.png";

    public RegisterViewModel(RegisterService registerService)
    {
        _registerService = registerService;
        IsCustomAlertSupported = true;
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
        if (IsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            await ShowAlertAsync("Błąd", "Email i Hasło są wymagane!", "OK");
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
                IBAN = IBAN ?? string.Empty
            };

            var result = await _registerService.RegisterAsync(request, SelectedAvatarFile);

            if (result == true)
            {
                await ShowAlertAsync("Sukces", "Konto zostało założone! Możesz się teraz zalogować.", "OK");
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            await ShowAlertAsync("Błąd", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PickAvatar()
    {
        try
        {
            var photo = await MediaPicker.Default.PickPhotosAsync();
            if (photo == null)
            {
                return;
            }

            SelectedAvatarFile = photo?.FirstOrDefault();

            if (SelectedAvatarFile != null)
            {
                var stream = await SelectedAvatarFile.OpenReadAsync();
                AvatarPreviewSource = ImageSource.FromStream(() => stream);


                IsAvatarPlaceholderVisible = false;
                IsAvatarImageVisible = true;
            }
        }
        catch (Exception ex)
        {
            await ShowAlertAsync("Błąd", ex.Message, "OK");
        }
    }


    [RelayCommand]
    private void CancelAvatar()
    {
        SelectedAvatarFile = null;
        AvatarPreviewSource = "default_avatar.png";
        IsAvatarPlaceholderVisible = true;
        IsAvatarImageVisible = false;
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordHidden = !IsPasswordHidden;
        PasswordIcon = IsPasswordHidden ? "eye_closed.png" : "eye_open.png";
    }

    [RelayCommand]
    private async Task GoToLogin()
    {

        await Shell.Current.GoToAsync("..");
    }
}