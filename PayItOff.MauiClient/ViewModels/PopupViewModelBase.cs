using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace PayItOff.MauiClient.ViewModels;

public partial class PopupViewModelBase : BaseViewModel
{
    private TaskCompletionSource<bool>? _alertTcs;
    private TaskCompletionSource<string>? _actionSheetTcs;

    [ObservableProperty] public partial bool IsCustomAlertVisible { get; set; }
    [ObservableProperty] public partial string CustomAlertTitle { get; set; } = string.Empty;
    [ObservableProperty] public partial string CustomAlertMessage { get; set; } = string.Empty;
    [ObservableProperty] public partial string CustomAlertAcceptText { get; set; } = "OK";
    [ObservableProperty] public partial string CustomAlertCancelText { get; set; } = string.Empty;

    [ObservableProperty] public partial bool IsActionSheetVisible { get; set; }
    [ObservableProperty] public partial string ActionSheetTitle { get; set; } = string.Empty;
    [ObservableProperty] public partial ObservableCollection<string> ActionSheetOptions { get; set; } = new();

    public bool IsCustomAlertCancelVisible => !string.IsNullOrEmpty(CustomAlertCancelText);
    public int AlertAcceptColumn => IsCustomAlertCancelVisible ? 1 : 0;
    public int AlertAcceptColumnSpan => IsCustomAlertCancelVisible ? 1 : 2;

    [ObservableProperty] public partial bool IsCustomAlertSupported { get; set; } = false;

    public async Task<bool> ShowAlertAsync(string title, string message, string accept = "OK", string cancel = "")
    {
        if (!IsCustomAlertSupported)
        {
            if (string.IsNullOrEmpty(cancel))
            {
                await Shell.Current.DisplayAlertAsync(title, message, accept);
                return true;
            }
            else
            {
                return await Shell.Current.DisplayAlertAsync(title, message, accept, cancel);
            }
        }

        bool wasBusy = IsBusy;
        if (wasBusy)
        {
            IsBusy = false;
        }

        CustomAlertTitle = title;
        CustomAlertMessage = message;
        CustomAlertAcceptText = accept;
        CustomAlertCancelText = cancel;

        OnPropertyChanged(nameof(IsCustomAlertCancelVisible));
        OnPropertyChanged(nameof(AlertAcceptColumn));
        OnPropertyChanged(nameof(AlertAcceptColumnSpan));

        _alertTcs = new TaskCompletionSource<bool>();
        IsCustomAlertVisible = true;
        var result = await _alertTcs.Task;

        if (wasBusy)
        {
            IsBusy = true;
        }

        return result;
    }

    [RelayCommand]
    protected void CloseAlert(object result)
    {
        IsCustomAlertVisible = false;

        bool parsedResult = false;
        if (result is bool b)
        {
            parsedResult = b;
        }
        else if (result is string s)
        {
            parsedResult = s.Equals("True", StringComparison.OrdinalIgnoreCase);
        }

        _alertTcs?.TrySetResult(parsedResult);
    }

    public async Task<string> ShowActionSheetAsync(string title, params string[] options)
    {
        bool wasBusy = IsBusy;
        if (wasBusy)
        {
            IsBusy = false;
        }

        ActionSheetTitle = title;
        ActionSheetOptions = new ObservableCollection<string>(options);

        _actionSheetTcs = new TaskCompletionSource<string>();
        IsActionSheetVisible = true;
        var result = await _actionSheetTcs.Task;

        if (wasBusy)
        {
            IsBusy = true;
        }

        return result;
    }

    [RelayCommand]
    protected void SelectActionSheetOption(string option)
    {
        IsActionSheetVisible = false;
        _actionSheetTcs?.TrySetResult(option);
    }

    [RelayCommand]
    protected void CancelActionSheet()
    {
        IsActionSheetVisible = false;
        _actionSheetTcs?.TrySetResult("Anuluj");
    }
}
