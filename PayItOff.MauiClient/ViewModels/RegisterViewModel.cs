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
    public partial bool IsBusy { get; set; }

    public RegisterViewModel(RegisterService registerService)
    {
        _registerService = registerService;
    }

    [RelayCommand]
    private async Task Register()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            var request = new RegisterRequest
            {
                Email = Email,
                Password = Password,
                Nickname = Nickname,
                Name = Name,
                Surname = Surname

            };

            var result = await _registerService.RegisterAsync(request);

            if (result == true)
            {
                await Shell.Current.DisplayAlertAsync("Sukces", "Użytkownik zarejestrowany!", "OK");
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