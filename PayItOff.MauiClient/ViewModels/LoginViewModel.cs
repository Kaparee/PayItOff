using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PayItOff.MauiClient.Services;
using PayItOff.MauiClient.Views;
using PayItOff.Shared.Requests;

namespace PayItOff.MauiClient.ViewModels;

public partial class LoginViewModel : PopupViewModelBase
{
    private readonly AuthService _authService;
    private readonly AppUpdateService _updateService;
    private readonly SignalRService _signalRService;
    private static int _updateCheckStarted;

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

    [ObservableProperty]
    public partial bool RememberMe { get; set; } = false;

    public LoginViewModel(
        AuthService authService,
        AppUpdateService updateService,
        SignalRService signalRService
        )
    {
        _authService = authService;
        _updateService = updateService;
        _signalRService = signalRService;
        IsCustomAlertSupported = true;
    }

    public async Task CheckForAppUpdateAsync()
    {
        if (Interlocked.Exchange(ref _updateCheckStarted, 1) == 1)
            return;

        try
        {
            var update = await _updateService.CheckForUpdateAsync();
            if (update == null)
                return;

            var accept = await ShowAlertAsync(
                "Dostępna aktualizacja",
                $"Pojawiła się nowa wersja aplikacji ({update.Version}). Czy chcesz pobrać i zainstalować ją teraz?",
                "Pobierz i zrestartuj",
                "Pomiń");

            if (!accept)
            {
                _updateService.SkipVersion(update.Version);
                return;
            }

            IsBusy = true;
            await _updateService.DownloadAndRestartAsync(update);
        }
        catch (Exception ex)
        {
            await ShowAlertAsync("Błąd aktualizacji", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
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
            var request = new LoginRequest { EmailOrNickname = EmailOrNickname, Password = Password, RememberMe = this.RememberMe };

            await _authService.LoginAsync(request);

            if (_signalRService.IsDisconnected) { await _signalRService.StartAsync(); }

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
    private void TogglePasswordVisibility()
    {
        IsPasswordHidden = !IsPasswordHidden;
        PasswordIcon = IsPasswordHidden ? "eye_closed.png" : "eye_open.png";
    }

    [RelayCommand]
    private async Task GoToRegister()
    {
        string disclaimer =
            "<b>Umowa Licencyjna Użytkownika Końcowego (EULA) i Polityka Prywatności</b><br/><br/>" +
            "Przed założeniem konta w aplikacji <b>PayItOff</b> prosimy o dokładne zapoznanie się z poniższymi warunkami.<br/><br/>" +
            "<b>1. Postanowienia ogólne i charakter aplikacji</b><br/>" +
            "Aplikacja PayItOff jest hobbystycznym, darmowym projektem studenckim. Jej głównym i jedynym celem jest ułatwienie nieformalnych rozliczeń między znajomymi. Aplikacja nie jest produktem komercyjnym i jest dostarczana w modelu \"tak jak jest\" (as-is), bez żadnych gwarancji.<br/><br/>" +
            "<b>2. Polityka Prywatności i Brak Administratora Danych (RODO)</b><br/>" +
            "Aplikacja nie jest zarządzana przez żadną firmę ani sformalizowanego Administratora Danych Osobowych. Wszelkie dane wprowadzane do systemu – w tym nazwy użytkownika, adresy e-mail, awatary i kwoty rozliczeń – są udostępniane przez Użytkownika <b>całkowicie dobrowolnie</b>. Dane te są przechowywane na zewnętrznych serwerach chmurowych, jednak nie są objęte wojskowym szyfrowaniem end-to-end.<br/><br/>" +
            "<b>3. Bezpieczeństwo konta</b><br/>" +
            "Ze względu na niekomercyjny charakter aplikacji, <b>kategorycznie odradza się</b> stosowania haseł, które Użytkownik wykorzystuje do logowania się do usług wrażliwych (np. bankowości elektronicznej, głównych skrzynek e-mail czy mediów społecznościowych). Użytkownik samodzielnie odpowiada za wymyślenie bezpiecznego, ale niepowtarzalnego hasła.<br/><br/>" +
            "<b>4. Wyłączenie odpowiedzialności</b><br/>" +
            "Twórca aplikacji <b>nie ponosi żadnej odpowiedzialności prawno-finansowej za:</b><br/>" +
            "• Ewentualne awarie serwerów i przerwy w dostępie do usługi.<br/>" +
            "• Utratę, uszkodzenie lub wyciek wprowadzonych danych.<br/>" +
            "• Błędy w algorytmach wyliczających długi.<br/>" +
            "• Jakiekolwiek spory finansowe wynikające z używania aplikacji.<br/><br/>" +
            "<b>5. Postanowienia końcowe</b><br/>" +
            "Twórca zastrzega sobie prawo do zamknięcia serwerów, wyczyszczenia bazy danych lub zawieszenia działania aplikacji w dowolnym momencie, bez wcześniejszego powiadomienia.<br/><br/>" +
            "Klikając <b>Akceptuję</b>, oświadczasz, że zrzekasz się roszczeń wobec twórcy, w pełni rozumiesz i zgadzasz się na korzystanie z aplikacji na wyżej wymienionych warunkach, <b>na własne ryzyko</b>.";

        bool accepted = await ShowAlertAsync("Regulamin usługi", disclaimer, "Akceptuję", "Odrzuć");

        if (accepted)
        {
            await Shell.Current.GoToAsync(nameof(RegisterPage));
        }
    }

    public async Task CheckAutoLoginAsync()
    {
        IsBusy = true;
        bool refresh = await _authService.RefreshTokensAsync();

        if (refresh)
        {
            if (_signalRService.IsDisconnected)
            {
                await _signalRService.StartAsync();
            }

            await Shell.Current.GoToAsync("//MainPage");
        }

        IsBusy = false;
    }
}