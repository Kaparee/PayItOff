using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace PayItOff.MauiClient.Models;

public partial class ReceiptMemberShare : ObservableObject
{
    [ObservableProperty]
    private int _memberId;

    [ObservableProperty]
    private string _memberName = string.Empty;

    [ObservableProperty]
    private string _avatarUrl = string.Empty;

    [ObservableProperty]
    private decimal _owedAmount;

    [ObservableProperty]
    private bool _isRemainderRecipient;
}

public partial class ReceiptItem : ObservableObject
{
    public string Id { get; } = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private int? _payerId;

    [ObservableProperty]
    private string _payerName = string.Empty;

    [ObservableProperty]
    private decimal _quantity = 1;

    [ObservableProperty]
    private decimal _unitPrice = 0;

    [ObservableProperty]
    private string _categoryId = string.Empty;

    public decimal TotalPrice => Quantity * UnitPrice;

    // List of members who are assigned to this item
    public ObservableCollection<ReceiptMemberShare> AssignedMembers { get; } = new();

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(Quantity) || e.PropertyName == nameof(UnitPrice))
        {
            OnPropertyChanged(nameof(TotalPrice));
            RecalculateShares();
        }
    }

    public void AssignMember(int memberId, string memberName, string avatarUrl)
    {
        if (!AssignedMembers.Any(m => m.MemberId == memberId))
        {
            AssignedMembers.Add(new ReceiptMemberShare 
            { 
                MemberId = memberId, 
                MemberName = memberName,
                AvatarUrl = avatarUrl
            });
            RecalculateShares();
        }
    }

    public void RemoveMember(int memberId)
    {
        var member = AssignedMembers.FirstOrDefault(m => m.MemberId == memberId);
        if (member != null)
        {
            AssignedMembers.Remove(member);
            RecalculateShares();
        }
    }

    private void RecalculateShares()
    {
        if (AssignedMembers.Count == 0) return;

        decimal share = Math.Round(TotalPrice / AssignedMembers.Count, 2);
        decimal totalAllocated = 0;

        foreach (var m in AssignedMembers)
        {
            m.OwedAmount = share;
            m.IsRemainderRecipient = false;
            totalAllocated += share;
        }

        // Handle remainder pennies (e.g. 10 / 3 = 3.33 * 3 = 9.99, missing 0.01)
        decimal remainder = TotalPrice - totalAllocated;
        if (remainder != 0 && AssignedMembers.Count > 0)
        {
            var firstMember = AssignedMembers.First();
            firstMember.OwedAmount += remainder;
            firstMember.IsRemainderRecipient = true;
        }
    }
}

public partial class ReceiptCategory : ObservableObject
{
    public string Id { get; } = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _name = string.Empty;

    public ObservableCollection<ReceiptItem> Items { get; } = new();
}

public partial class DisplayGroupMember : ObservableObject
{
    [ObservableProperty]
    private int _userId;

    [ObservableProperty]
    private string _fullName = string.Empty;

    [ObservableProperty]
    private string _avatarUrl = string.Empty;

    [ObservableProperty]
    private bool _isVisible = true;

    // Items specifically assigned to this user
    public ObservableCollection<ReceiptItem> AssignedItems { get; } = new();

    public decimal TotalOwed => AssignedItems.Sum(i => i.AssignedMembers.FirstOrDefault(m => m.MemberId == UserId)?.OwedAmount ?? 0);

    public void RefreshTotal()
    {
        OnPropertyChanged(nameof(TotalOwed));
    }
}
