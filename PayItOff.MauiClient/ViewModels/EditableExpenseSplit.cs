using CommunityToolkit.Mvvm.ComponentModel;

namespace PayItOff.MauiClient.ViewModels;

public partial class EditableExpenseSplit : ObservableObject
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial decimal OwedAmount { get; set; }
}
