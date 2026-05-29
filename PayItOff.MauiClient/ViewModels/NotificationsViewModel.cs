using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PayItOff.Domain.Enums;
using PayItOff.MauiClient.Models;
using PayItOff.MauiClient.Services;
using PayItOff.Shared.Requests;
using System.Collections.ObjectModel;

namespace PayItOff.MauiClient.ViewModels;

public partial class NotificationsViewModel : PopupViewModelBase
{
    private readonly NotificationService _notificationService;
    private readonly FriendService _friendService;
    private readonly GroupMemberService _groupMemberService;
    private readonly SettlementService _settlementService;

    [ObservableProperty]
    public partial ObservableCollection<NotificationDisplayItem> Notifications { get; set; } = new();

    public NotificationsViewModel(
        NotificationService notificationService,
        FriendService friendService,
        GroupMemberService groupMemberService,
        SettlementService settlementService)
    {
        _notificationService = notificationService;
        _friendService = friendService;
        _groupMemberService = groupMemberService;
        _settlementService = settlementService;
        IsCustomAlertSupported = true;
    }

    [ObservableProperty]
    public partial bool IsUnreadFilterActive { get; set; }

    [ObservableProperty]
    public partial bool IsActionRequiredFilterActive { get; set; }

    public bool IsAllFilterActive => !IsUnreadFilterActive && !IsActionRequiredFilterActive;

    public Color FilterAllColor => IsAllFilterActive ? Color.FromArgb("#2A3648") : Color.FromArgb("#1E232D");
    public Color FilterUnreadColor => IsUnreadFilterActive ? Color.FromArgb("#2A3648") : Color.FromArgb("#1E232D");
    public Color FilterActionColor => IsActionRequiredFilterActive ? Color.FromArgb("#2A3648") : Color.FromArgb("#1E232D");

    private int _totalCount;
    public int TotalCount { get => _totalCount; set => SetProperty(ref _totalCount, value); }

    private int _unreadCount;
    public int UnreadCount { get => _unreadCount; set => SetProperty(ref _unreadCount, value); }

    private int _actionRequiredCount;
    public int ActionRequiredCount { get => _actionRequiredCount; set => SetProperty(ref _actionRequiredCount, value); }

    [RelayCommand]
    public void ToggleFilter(string filter)
    {
        if (filter == "All")
        {
            IsUnreadFilterActive = false;
            IsActionRequiredFilterActive = false;
        }
        else if (filter == "Unread")
        {
            IsUnreadFilterActive = !IsUnreadFilterActive;
        }
        else if (filter == "ActionRequired")
        {
            IsActionRequiredFilterActive = !IsActionRequiredFilterActive;
        }

        OnPropertyChanged(nameof(FilterAllColor));
        OnPropertyChanged(nameof(FilterUnreadColor));
        OnPropertyChanged(nameof(FilterActionColor));
        _ = LoadNotificationsAsync(false);
    }

    [RelayCommand]
    public async Task LoadNotificationsAsync()
    {
        await LoadNotificationsAsync(true);
    }

    private async Task LoadNotificationsAsync(bool updateCounts)
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            if (updateCounts)
            {
                var allData = await _notificationService.GetAllNotifications();
                TotalCount = allData?.Count ?? 0;
                UnreadCount = allData?.Count(n => n.NotificationStatus != PayItOff.Domain.Enums.NotificationStatus.Read) ?? 0;
                ActionRequiredCount = allData?.Count(n => n.NotificationType == PayItOff.Domain.Enums.NotificationType.NeedAction) ?? 0;
            }

            var type1 = IsUnreadFilterActive ? "Unread" : null;
            var type2 = IsActionRequiredFilterActive ? "NeedAction" : null;
            if (type1 == null && type2 != null)
            {
                type1 = type2;
                type2 = null;
            }

            var data = await _notificationService.GetAllNotifications(type1, type2);

            Notifications = new ObservableCollection<NotificationDisplayItem>(
                (data ?? new List<PayItOff.Shared.Responses.NotificationResponse>()).Select(NotificationDisplayItem.FromResponse)
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load notifications: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task MarkAsReadAsync(NotificationDisplayItem item)
    {
        if (item == null || item.IsRead) return;

        try
        {
            await _notificationService.MarkAsReadAsync(item.NotificationId);
            await LoadNotificationsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to mark as read: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task AcceptActionAsync(NotificationDisplayItem item)
    {
        if (item == null || !item.IsActionRequired) return;

        bool confirm = await ShowAlertAsync("Akceptacja", "Czy na pewno chcesz zaakceptować tę akcję?", "Tak", "Nie");

        if (!confirm) return;

        try
        {
            if (item.EntityType == EntityType.Friends)
            {
                await _friendService.AcceptInviteAsync(new UpdateInviteRequest { InviteId = item.EntityId });
            }
            else if (item.EntityType == EntityType.Groups || item.EntityType == EntityType.GroupMembers)
            {
                await _groupMemberService.AcceptInviteAsync(item.EntityId);
            }
            else if (item.EntityType == EntityType.Settlements || item.EntityType == EntityType.GroupDebts)
            {
                await _settlementService.AcceptSettlementAsync(item.EntityId);
            }
            else if (item.EntityType == EntityType.NetSettlements)
            {
                await _settlementService.AcceptNetSettlementsAsync(item.EntityId);
            }

            await LoadNotificationsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to accept action: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task DeclineActionAsync(NotificationDisplayItem item)
    {
        if (item == null || !item.IsActionRequired) return;

        bool confirm = await ShowAlertAsync("Odrzucenie", "Czy na pewno chcesz odrzucić tę akcję?", "Tak", "Nie");

        if (!confirm) return;

        try
        {
            if (item.EntityType == EntityType.Friends)
            {
                await _friendService.DeclineInviteAsync(new UpdateInviteRequest { InviteId = item.EntityId });
            }
            else if (item.EntityType == EntityType.Groups || item.EntityType == EntityType.GroupMembers)
            {
                await _groupMemberService.DeclineInviteAsync(item.EntityId);
            }
            else if (item.EntityType == EntityType.Settlements || item.EntityType == EntityType.GroupDebts)
            {
                await _settlementService.RejectSettlementAsync(item.EntityId);
            }
            else if (item.EntityType == EntityType.NetSettlements)
            {
                await _settlementService.RejectNetSettlementsAsync(item.EntityId);
            }

            await LoadNotificationsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to decline action: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task DeleteNotificationAsync(NotificationDisplayItem item)
    {
        if (item == null) return;
        try
        {
            await _notificationService.DeleteNotificationAsync(item.NotificationId);
            await LoadNotificationsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to delete notification: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task MarkAllAsReadAsync()
    {
        try
        {
            await _notificationService.MarkAllAsReadAsync();
            await LoadNotificationsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to mark all as read: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task DeleteAllNotificationsAsync()
    {
        try
        {
            await _notificationService.DeleteAllNotificationsAsync();
            await LoadNotificationsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to delete all notifications: {ex.Message}");
        }
    }
}