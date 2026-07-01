using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace PayItOff.MauiClient.Models;

public partial class ReceiptMemberShare : ObservableObject
{
    [ObservableProperty]
    public partial int MemberId { get; set; }

    [ObservableProperty]
    public partial string MemberName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AvatarUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ItemId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ItemName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PayerName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial decimal OwedAmount { get; set; }

    [ObservableProperty]
    public partial bool IsRemainderRecipient { get; set; }
}

public partial class ReceiptItem : ObservableObject
{
    public string Id { get; } = Guid.NewGuid().ToString();

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DisplayGroupMember? Payer { get; set; }

    [ObservableProperty]
    public partial decimal Quantity { get; set; } = 1;

    [ObservableProperty]
    public partial decimal UnitPrice { get; set; } = 0;

    private string _unitPriceInput = "0";
    public string UnitPriceInput
    {
        get => _unitPriceInput;
        set
        {
            if (SetProperty(ref _unitPriceInput, value))
            {
                ParseUnitPrice();
            }
        }
    }

    private void ParseUnitPrice()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_unitPriceInput))
            {
                UnitPrice = 0;
                return;
            }
            string expression = _unitPriceInput.Replace(',', '.');
            var result = new System.Data.DataTable().Compute(expression, null);
            if (result != DBNull.Value && result != null)
            {
                UnitPrice = Math.Round(Convert.ToDecimal(result), 2);
            }
        }
        catch
        {
        }
    }

    [ObservableProperty]
    public partial string CategoryId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ItemGroupId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ItemGroupName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial SolidColorBrush GroupColor { get; set; } = new SolidColorBrush(Colors.Transparent);

    public decimal TotalPrice => Quantity * UnitPrice;

    public string PayerDisplayName => Payer?.FullName ?? "Brak";

    public bool HasNoPayer => Payer == null;

    public ObservableCollection<ReceiptMemberShare> AssignedMembers { get; } = new();

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(Quantity) || e.PropertyName == nameof(UnitPrice))
        {
            OnPropertyChanged(nameof(TotalPrice));
            RecalculateShares();
        }
        else if (e.PropertyName == nameof(Payer))
        {
            OnPropertyChanged(nameof(PayerDisplayName));
            OnPropertyChanged(nameof(HasNoPayer));
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
                AvatarUrl = avatarUrl,
                ItemId = this.Id,
                ItemName = this.Name,
                PayerName = this.Payer?.FullName ?? string.Empty
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

    public void RecalculateShares()
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
    public partial string Name { get; set; } = string.Empty;

    public ObservableCollection<ReceiptItem> Items { get; } = new();
}

public partial class DisplayGroupMember : ObservableObject
{
    [ObservableProperty]
    public partial int UserId { get; set; }

    [ObservableProperty]
    public partial string FullName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AvatarUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsVisible { get; set; } = true;

    public ObservableCollection<ReceiptMemberShare> AssignedShares { get; } = new();

    public DisplayGroupMember()
    {
        AssignedShares.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (ReceiptMemberShare item in e.NewItems)
                {
                    item.PropertyChanged += Share_PropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (ReceiptMemberShare item in e.OldItems)
                {
                    item.PropertyChanged -= Share_PropertyChanged;
                }
            }
            OnPropertyChanged(nameof(TotalOwed));
        };
    }

    private void Share_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ReceiptMemberShare.OwedAmount))
        {
            OnPropertyChanged(nameof(TotalOwed));
        }
    }

    public decimal TotalOwed => AssignedShares.Sum(s => s.OwedAmount);

    public void RefreshTotal()
    {
        OnPropertyChanged(nameof(TotalOwed));
    }

    public override string ToString() => FullName;
}
