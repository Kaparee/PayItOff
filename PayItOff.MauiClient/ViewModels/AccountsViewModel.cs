using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PayItOff.MauiClient.Services;
using PayItOff.Shared.Requests;

namespace PayItOff.MauiClient.ViewModels;

public partial class AccountsViewModel : BaseViewModel
{
    private readonly UserService _userService;

    public AccountsViewModel(UserService userService)
    {
        _userService = userService;
    }

    [ObservableProperty]
    public partial string AvatarUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FullName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Username { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FirstName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LastName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PhoneNumber { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Iban { get; set; } = string.Empty;

    // Notifications
    [ObservableProperty]
    public partial bool ReceiveEmail { get; set; }
    
    [ObservableProperty]
    public partial bool DailySummary { get; set; }

    [ObservableProperty]
    public partial bool NotifyOnGroupJoined { get; set; }

    [ObservableProperty]
    public partial bool NotifyOnExpenseAdded { get; set; }

    [ObservableProperty]
    public partial bool NotifyOnGroupRemoved { get; set; }

    [ObservableProperty]
    public partial bool NotifyOnFriendRemoved { get; set; }

    [ObservableProperty]
    public partial bool NotifyOnExpenseChanged { get; set; }

    [ObservableProperty]
    public partial bool NotifyOnTransferConfirmed { get; set; }

    private bool _isInitialized;
    private bool _suppressNotificationUpdate;

    public async Task LoadProfileAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var info = await _userService.GetUserInformationAsync();
            if (info != null)
            {
                AvatarUrl = info.AvatarUrl;
                FullName = $"{info.Name} {info.Surname}";
                Email = info.Email;
                Username = info.Nickname;
                FirstName = info.Name;
                LastName = info.Surname;
                PhoneNumber = info.PhoneNumber ?? "Brak danych";
                Iban = info.IBAN ?? "Brak danych";

                _suppressNotificationUpdate = true;
                ReceiveEmail = info.Notifications.ReceiveEmail;
                DailySummary = info.Notifications.DailySummary;
                NotifyOnGroupJoined = info.Notifications.NotifyOnGroupJoined;
                NotifyOnExpenseAdded = info.Notifications.NotifyOnExpenseAdded;
                NotifyOnGroupRemoved = info.Notifications.NotifyOnGroupRemoved;
                NotifyOnFriendRemoved = info.Notifications.NotifyOnFriendRemoved;
                NotifyOnExpenseChanged = info.Notifications.NotifyOnExpenseChanged;
                NotifyOnTransferConfirmed = info.Notifications.NotifyOnTransferConfirmed;
                _suppressNotificationUpdate = false;
            }
        }
        catch (Exception ex)
        {
            // Handle error
            Console.WriteLine(ex.Message);
        }
        finally
        {
            IsBusy = false;
            _isInitialized = true;
        }
    }

    partial void OnReceiveEmailChanged(bool value) => TriggerNotificationUpdate();
    partial void OnDailySummaryChanged(bool value) => TriggerNotificationUpdate();
    partial void OnNotifyOnGroupJoinedChanged(bool value) => TriggerNotificationUpdate();
    partial void OnNotifyOnExpenseAddedChanged(bool value) => TriggerNotificationUpdate();
    partial void OnNotifyOnGroupRemovedChanged(bool value) => TriggerNotificationUpdate();
    partial void OnNotifyOnFriendRemovedChanged(bool value) => TriggerNotificationUpdate();
    partial void OnNotifyOnExpenseChangedChanged(bool value) => TriggerNotificationUpdate();
    partial void OnNotifyOnTransferConfirmedChanged(bool value) => TriggerNotificationUpdate();

    private void TriggerNotificationUpdate()
    {
        if (!_isInitialized || _suppressNotificationUpdate) return;
        UpdateNotificationsCommand.Execute(null);
    }

    [RelayCommand]
    private async Task UpdateNotificationsAsync()
    {
        try
        {
            var request = new UserNotificationChangeRequest
            {
                Notifications = new UserNotificationSettingsRequest(
                    ReceiveEmail,
                    DailySummary,
                    NotifyOnGroupJoined,
                    NotifyOnExpenseAdded,
                    NotifyOnGroupRemoved,
                    NotifyOnFriendRemoved,
                    NotifyOnExpenseChanged,
                    NotifyOnTransferConfirmed
                )
            };
            await _userService.UpdateNotificationAsync(request);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    // Popup Logic for Editing (Placeholders for next steps)
    [ObservableProperty]
    public partial bool IsEditDataPopupVisible { get; set; }

    [ObservableProperty]
    public partial bool IsEditEmailPopupVisible { get; set; }

    [ObservableProperty]
    public partial bool IsEditPasswordPopupVisible { get; set; }

    [ObservableProperty]
    public partial bool IsDeleteAccountPopupVisible { get; set; }

    // Forms
    [ObservableProperty]
    public partial string EditFirstName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditLastName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditPhoneNumber { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditIban { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewEmail { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OldPassword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewPassword { get; set; } = string.Empty;

    [RelayCommand]
    private void ShowEditDataPopup()
    {
        EditFirstName = FirstName;
        EditLastName = LastName;
        EditPhoneNumber = PhoneNumber == "Brak danych" ? "" : PhoneNumber;
        EditIban = Iban == "Brak danych" ? "" : Iban;
        IsEditDataPopupVisible = true;
    }

    [RelayCommand]
    private void CloseEditDataPopup() => IsEditDataPopupVisible = false;

    [RelayCommand]
    private async Task SubmitEditDataAsync()
    {
        try
        {
            await _userService.UpdateInfoAsync(new UserInfoUpdateRequest
            {
                Nickname = Username,
                Name = EditFirstName,
                Surname = EditLastName,
                PhoneNumber = EditPhoneNumber,
                IBAN = EditIban
            });
            IsEditDataPopupVisible = false;
            await LoadProfileAsync();
            await App.Current!.MainPage!.DisplayAlert("Sukces", "Dane zostały zaktualizowane", "OK");
        }
        catch (Exception ex)
        {
            await App.Current!.MainPage!.DisplayAlert("Błąd", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private void ShowEditEmailPopup()
    {
        NewEmail = string.Empty;
        IsEditEmailPopupVisible = true;
    }

    [RelayCommand]
    private void CloseEditEmailPopup() => IsEditEmailPopupVisible = false;

    [RelayCommand]
    private async Task SubmitEditEmailAsync()
    {
        try
        {
            await _userService.RequestEmailChangeAsync(NewEmail);
            IsEditEmailPopupVisible = false;
            await App.Current!.MainPage!.DisplayAlert("Sukces", "Link weryfikacyjny został wysłany na podany e-mail", "OK");
        }
        catch (Exception ex)
        {
            await App.Current!.MainPage!.DisplayAlert("Błąd", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private void ShowEditPasswordPopup()
    {
        OldPassword = string.Empty;
        NewPassword = string.Empty;
        IsEditPasswordPopupVisible = true;
    }

    [RelayCommand]
    private void CloseEditPasswordPopup() => IsEditPasswordPopupVisible = false;

    [RelayCommand]
    private async Task SubmitEditPasswordAsync()
    {
        try
        {
            await _userService.ModifyPasswordAsync(new ModifyPasswordRequest
            {
                OldPassword = OldPassword,
                NewPassword = NewPassword
            });
            IsEditPasswordPopupVisible = false;
            await App.Current!.MainPage!.DisplayAlert("Sukces", "Twoje hasło zostało zmienione", "OK");
        }
        catch (Exception ex)
        {
            await App.Current!.MainPage!.DisplayAlert("Błąd", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task ChangeAvatarAsync()
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Wybierz zdjęcie profilowe"
            });

            if (result != null)
            {
                IsBusy = true;
                bool success = await _userService.UpdateAvatarAsync(result);
                if (success)
                {
                    await LoadProfileAsync();
                    await App.Current!.MainPage!.DisplayAlert("Sukces", "Zdjęcie profilowe zostało zaktualizowane", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await App.Current!.MainPage!.DisplayAlert("Błąd", $"Wystąpił problem przy wyborze zdjęcia: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ShowDeleteAccountPopup() => IsDeleteAccountPopupVisible = true;

    [RelayCommand]
    private void CloseDeleteAccountPopup() => IsDeleteAccountPopupVisible = false;

    [RelayCommand]
    private async Task SubmitDeleteAccountAsync()
    {
        try
        {
            await _userService.DeleteAccountAsync();
            IsDeleteAccountPopupVisible = false;
            SecureStorage.Default.Remove("jwt_token");
            await Shell.Current.GoToAsync("//LoginPage");
        }
        catch (Exception)
        {
        }
    }
}