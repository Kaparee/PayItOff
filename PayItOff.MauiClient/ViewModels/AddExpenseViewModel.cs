using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PayItOff.MauiClient.Models;
using PayItOff.MauiClient.Services;
using PayItOff.Shared.Requests;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;

namespace PayItOff.MauiClient.ViewModels;

public partial class AddExpenseViewModel : PopupViewModelBase, IQueryAttributable
{
    private readonly GroupService _groupService;
    private readonly ExpenseService _expenseService;
    private readonly SignalRService _signalRService;

    [ObservableProperty]
    public partial int GroupId { get; set; }

    [ObservableProperty]
    public partial string GroupName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewItemName { get; set; } = string.Empty;

    public ObservableCollection<ReceiptPhotoItem> ReceiptPhotos { get; } = [];

    [ObservableProperty]
    public partial ReceiptPhotoItem? SelectedPhoto { get; set; }

    [ObservableProperty]
    public partial bool IsAddCategoryPopupVisible { get; set; }

    [ObservableProperty]
    public partial string NewCategoryName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsAddGroupPopupVisible { get; set; }

    [ObservableProperty]
    public partial string NewGroupName { get; set; } = string.Empty;

    private TaskCompletionSource<string>? _groupNameTcs;

    [ObservableProperty]
    public partial bool IsMissingMembersPopupVisible { get; set; }
    public ObservableCollection<ReceiptItem> BufferItems { get; } = [];
    public ObservableCollection<ReceiptItem> UncategorizedItems { get; } = [];
    public ObservableCollection<ReceiptCategory> Categories { get; } = [];
    public ObservableCollection<DisplayGroupMember> GroupMembers { get; } = [];
    public ObservableCollection<DisplayGroupMember> MissingGroupMembers { get; } = [];

    private int _currentUserId;
    private DisplayGroupMember? _defaultPayer;

    [ObservableProperty]
    public partial ObservableCollection<string> ExistingGroupCategories { get; set; } = new();
    [ObservableProperty]
    public partial ObservableCollection<string> FilteredGroupCategories { get; set; } = new();

    public AddExpenseViewModel(
        GroupService groupService,
        ExpenseService expenseService,
        SignalRService signalRService)
    {
        _groupService = groupService;
        _expenseService = expenseService;
        _signalRService = signalRService;
        IsCustomAlertSupported = true;

        ReceiptPhotos.CollectionChanged += (s, e) => OnPropertyChanged(nameof(IsReceiptPhotosEmpty));
    }

    public bool IsReceiptPhotosEmpty => ReceiptPhotos.Count == 0;

    public ImageSource? SelectedPhotoImageSource => SelectedPhoto?.ImageSource;

    partial void OnSelectedPhotoChanged(ReceiptPhotoItem? value) =>
        OnPropertyChanged(nameof(SelectedPhotoImageSource));

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("groupId", out var raw))
        {
            return;
        }

        var id = raw switch
        {
            int i => i,
            long l => (int)l,
            string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) => n,
            _ => 0
        };

        if (id > 0)
        {
            GroupId = id;
        }
    }

    partial void OnGroupIdChanged(int value)
    {
        if (value > 0)
        {
            _ = LoadGroupDataAsync();
        }
    }

    public async Task LoadGroupDataAsync()
    {
        IsBusy = true;
        try
        {
            await _signalRService.JoinGroupAsync(GroupId);

            var details = await _groupService.GetGroupDetails(GroupId);
            if (details != null)
            {
                GroupName = details.GroupName ?? string.Empty;
                GroupMembers.Clear();
                foreach (var member in details.Members ?? [])
                {
                    GroupMembers.Add(new DisplayGroupMember
                    {
                        UserId = member.UserId,
                        FullName = member.FullName,
                        AvatarUrl = member.AvatarUrl,
                        IsVisible = true
                    });
                }

                if (_currentUserId == 0)
                {
                    var idStr = await SecureStorage.Default.GetAsync("user_id");
                    if (string.IsNullOrEmpty(idStr))
                    {
                        var token = await SecureStorage.Default.GetAsync("jwt_token");
                        if (!string.IsNullOrEmpty(token))
                        {
                            var userIdFromToken = PayItOff.MauiClient.Helpers.JwtHelper.GetClaimValue(token, "nameid");
                            if (!string.IsNullOrEmpty(userIdFromToken))
                            {
                                idStr = userIdFromToken;
                                await SecureStorage.Default.SetAsync("user_id", idStr);
                            }
                        }
                    }
                    _currentUserId = int.TryParse(idStr, out var parsed) ? parsed : 0;
                }

                if (_currentUserId > 0)
                {
                    _defaultPayer = GroupMembers.FirstOrDefault(m => m.UserId == _currentUserId);
                }
            }
            else
            {
                await ShowAlertAsync("Błąd", "Nie udało się wczytać danych grupy.", "OK");
            }
        }
        catch (Exception ex)
        {
            await ShowAlertAsync("Błąd", "Nie udało się załadować danych grupy: " + ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void AddNewItem()
    {
        if (!string.IsNullOrWhiteSpace(NewItemName))
        {
            var item = new ReceiptItem { Name = NewItemName.Trim(), Payer = _defaultPayer };
            BufferItems.Add(item);
            NewItemName = string.Empty;
        }
    }

    [RelayCommand]
    private async Task ConfirmItem(ReceiptItem item)
    {
        if (item != null && BufferItems.Contains(item))
        {
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                await ShowAlertAsync("Błąd", "Nazwa przedmiotu nie może być pusta.", "OK");
                return;
            }
            BufferItems.Remove(item);
            item.CategoryId = "Uncategorized";
            UncategorizedItems.Add(item);
        }
    }

    [RelayCommand]
    private async Task ShowAddCategoryPopup()
    {
        NewCategoryName = string.Empty;

        var categories = await _expenseService.GetGroupCategoriesAsync(GroupId);

        ExistingGroupCategories.Clear();
        FilteredGroupCategories.Clear();

        foreach (var c in categories.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase))
        {
            ExistingGroupCategories.Add(c);
            FilteredGroupCategories.Add(c);
        }

        IsAddCategoryPopupVisible = true;
    }

    partial void OnNewCategoryNameChanged(string value)
    {
        FilteredGroupCategories.Clear();
        if (string.IsNullOrWhiteSpace(value))
        {
            foreach (var c in ExistingGroupCategories) FilteredGroupCategories.Add(c);
            return;
        }
        foreach (var c in ExistingGroupCategories.Where(c => c.Contains(value, StringComparison.OrdinalIgnoreCase)))
            FilteredGroupCategories.Add(c);
    }

    [RelayCommand]
    private void SelectExistingCategory(string name) => NewCategoryName = name;

    [RelayCommand]
    private void CloseAddCategoryPopup() => IsAddCategoryPopupVisible = false;

    [RelayCommand]
    private void AddNewCategory()
    {
        if (!string.IsNullOrWhiteSpace(NewCategoryName))
        {
            Categories.Add(new ReceiptCategory { Name = NewCategoryName.Trim() });
        }
        IsAddCategoryPopupVisible = false;
    }

    [RelayCommand]
    private void EditCategorizedItem(ReceiptItem item)
    {
        if (item != null)
        {
            var category = Categories.FirstOrDefault(c => c.Id == item.CategoryId);
            if (category != null)
            {
                category.Items.Remove(item);
            }
            else if (UncategorizedItems.Contains(item))
            {
                UncategorizedItems.Remove(item);
            }
            item.CategoryId = "Uncategorized";
            BufferItems.Add(item);
        }
    }

    [RelayCommand]
    private void DeleteCategorizedItem(ReceiptItem item)
    {
        if (item != null)
        {
            var category = Categories.FirstOrDefault(c => c.Id == item.CategoryId);
            if (category != null)
            {
                category.Items.Remove(item);
            }
            else if (UncategorizedItems.Contains(item))
            {
                UncategorizedItems.Remove(item);
            }
            else if (BufferItems.Contains(item))
            {
                BufferItems.Remove(item);
            }

            // Usuniecie wszystkich przypisan
            foreach (var member in GroupMembers)
            {
                var share = member.AssignedShares.FirstOrDefault(s => s.ItemId == item.Id);
                if (share != null)
                {
                    member.AssignedShares.Remove(share);
                }
            }
            RefreshAllTotals();
        }
    }

    public async Task GroupItemsAsync(ReceiptItem source, ReceiptItem target)
    {
        if (source == target)
        {
            return;
        }

        if (string.IsNullOrEmpty(target.ItemGroupId))
        {
            var groupName = await PromptForGroupNameAsync();
            if (string.IsNullOrWhiteSpace(groupName))
            {
                return;
            }

            target.ItemGroupId = Guid.NewGuid().ToString();
            target.ItemGroupName = groupName.Trim();
            var random = new Random();
            target.GroupColor = new Microsoft.Maui.Controls.SolidColorBrush(
                Microsoft.Maui.Graphics.Color.FromRgb(random.Next(256), random.Next(256), random.Next(256)));
        }

        source.ItemGroupId = target.ItemGroupId;
        source.ItemGroupName = target.ItemGroupName;
        source.GroupColor = target.GroupColor;
    }

    public async Task<string?> PromptForGroupNameAsync()
    {
        NewGroupName = string.Empty;
        IsAddGroupPopupVisible = true;
        _groupNameTcs = new TaskCompletionSource<string>();
        return await _groupNameTcs.Task;
    }

    [RelayCommand]
    private void ConfirmAddGroup()
    {
        IsAddGroupPopupVisible = false;
        _groupNameTcs?.TrySetResult(NewGroupName);
    }

    [RelayCommand]
    private void CancelAddGroup()
    {
        IsAddGroupPopupVisible = false;
        _groupNameTcs?.TrySetResult(string.Empty);
    }

    [RelayCommand]
    private void RemoveMemberShare(ReceiptMemberShare share)
    {
        if (share != null)
        {
            var member = GroupMembers.FirstOrDefault(m => m.UserId == share.MemberId);
            if (member != null)
            {
                member.AssignedShares.Remove(share);

                var allItems = Categories.SelectMany(c => c.Items).ToList();
                allItems.AddRange(UncategorizedItems);
                allItems.AddRange(BufferItems);
                var item = allItems.FirstOrDefault(i => i.Id == share.ItemId);
                if (item != null)
                {
                    item.RemoveMember(share.MemberId);
                }

                RefreshAllTotals();
            }
        }
    }

    [RelayCommand]
    private void RemoveMember(DisplayGroupMember member)
    {
        if (member != null)
        {
            GroupMembers.Remove(member);
            member.AssignedShares.Clear();
            MissingGroupMembers.Add(member);

            var allItems = Categories.SelectMany(c => c.Items).ToList();
            allItems.AddRange(UncategorizedItems);
            allItems.AddRange(BufferItems);
            foreach (var item in allItems)
            {
                var share = item.AssignedMembers.FirstOrDefault(m => m.MemberId == member.UserId);
                if (share != null)
                {
                    item.RemoveMember(member.UserId);
                }
            }
            RefreshAllTotals();
        }
    }

    [RelayCommand]
    private void ShowMissingMembersPopup() => IsMissingMembersPopupVisible = true;

    [RelayCommand]
    private void CloseMissingMembersPopup() => IsMissingMembersPopupVisible = false;

    [RelayCommand]
    private void RestoreMember(DisplayGroupMember member)
    {
        if (member != null)
        {
            MissingGroupMembers.Remove(member);
            GroupMembers.Add(member);
        }
    }

    [RelayCommand]
    private void ReorderMember(object payload)
    {
    }

    public void MoveMember(DisplayGroupMember source, DisplayGroupMember target)
    {
        if (source != null && target != null && source != target)
        {
            int oldIndex = GroupMembers.IndexOf(source);
            int newIndex = GroupMembers.IndexOf(target);

            if (oldIndex >= 0 && newIndex >= 0)
            {
                GroupMembers.Move(oldIndex, newIndex);
                RefreshAllTotals();
                var allItems = Categories.SelectMany(c => c.Items).ToList();
                allItems.AddRange(UncategorizedItems);
                allItems.AddRange(BufferItems);
                foreach (var item in allItems)
                {
                    item.RecalculateShares();
                }
                RefreshAllTotals();
            }
        }
    }

    [RelayCommand]
    private void DropItemOnMember(object payload)
    {
    }

    public void AssignItemToMember(ReceiptItem item, DisplayGroupMember member)
    {
        item.AssignMember(member.UserId, member.FullName, member.AvatarUrl);
        var share = item.AssignedMembers.First(m => m.MemberId == member.UserId);

        if (!member.AssignedShares.Contains(share))
        {
            member.AssignedShares.Add(share);
        }
        RefreshAllTotals();
    }

    public void AssignCategoryToMember(ReceiptCategory category, DisplayGroupMember member)
    {
        foreach (var item in category.Items)
        {
            AssignItemToMember(item, member);
        }
    }

    private void RefreshAllTotals()
    {
        foreach (var member in GroupMembers)
        {
            member.RefreshTotal();
        }
    }

    [RelayCommand]
    private async Task UploadJsonAsync()
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Wybierz plik JSON paragonu"
            });

            if (result != null && result.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                using var stream = await result.OpenReadAsync();
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();

                var items = JsonSerializer.Deserialize<List<ReceiptItem>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        item.CategoryId = string.Empty;
                        item.Payer = _defaultPayer;
                        BufferItems.Add(item);
                    }
                    await ShowAlertAsync("Sukces", $"Wczytano {items.Count} produktów z pliku JSON.", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await ShowAlertAsync("Błąd", "Błąd podczas wyboru pliku: " + ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task TakePhotoAsync()
    {
        if (MediaPicker.Default.IsCaptureSupported)
        {
            try
            {
                FileResult? photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo != null)
                {
                    using var stream = await photo.OpenReadAsync();
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    var bytes = ms.ToArray();

                    var newItem = new ReceiptPhotoItem
                    {
                        ImageSource = ImageSource.FromStream(() => new MemoryStream(bytes)),
                        UploadedFileName = "" // tymczasowo puste
                    };
                    ReceiptPhotos.Add(newItem);
                    SelectedPhoto = newItem;

                    IsBusy = true;
                    var uploadedFileName = await _expenseService.UploadReceiptAsync(photo);
                    newItem.UploadedFileName = uploadedFileName;
                    IsBusy = false;
                }
            }
            catch (Exception ex)
            {
                IsBusy = false;
                await ShowAlertAsync("Błąd", "Aparat niedostępny: " + ex.Message, "OK");
            }
        }
    }

    [RelayCommand]
    private async Task PickGalleryAsync()
    {
        try
        {
            var options = new PickOptions
            {
                PickerTitle = "Wybierz zdjęcia paragonów",
                FileTypes = FilePickerFileType.Images
            };

            var photos = await FilePicker.Default.PickMultipleAsync(options);
            if (photos == null || !photos.Any())
            {
                return;
            }

            ReceiptPhotoItem? lastItem = null;

            foreach (var photo in photos)
            {
                using var stream = await photo!.OpenReadAsync();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                var bytes = ms.ToArray();

                var newItem = new ReceiptPhotoItem
                {
                    ImageSource = ImageSource.FromStream(() => new MemoryStream(bytes)),
                    UploadedFileName = "" // tymczasowo puste
                };
                ReceiptPhotos.Add(newItem);
                lastItem = newItem;
            }

            if (lastItem != null)
            {
                SelectedPhoto = lastItem;
            }

            // Upload w tle
            IsBusy = true;
            foreach (var photo in photos)
            {
                var matchingItem = ReceiptPhotos.LastOrDefault(p => string.IsNullOrEmpty(p.UploadedFileName));
                if (matchingItem != null)
                {
                    try
                    {
                        var uploadedFileName = await _expenseService.UploadReceiptAsync(new FileResult(photo!.FullPath));
                        matchingItem.UploadedFileName = uploadedFileName;
                    }
                    catch { /* kontynuuj upload pozostałych */ }
                }
            }
            IsBusy = false;
        }
        catch (Exception ex)
        {
            IsBusy = false;
            await ShowAlertAsync("Błąd", "Błąd przesyłania zdjęcia: " + ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task SubmitExpenseAsync()
    {
        if (GroupId <= 0)
        {
            return;
        }

        var allItems = Categories.SelectMany(c => c.Items).ToList();
        allItems.AddRange(UncategorizedItems);
        allItems.AddRange(BufferItems);

        if (allItems.Count == 0)
        {
            await ShowAlertAsync("Błąd", "Brak produktów do rozliczenia.", "OK");
            return;
        }

        if (allItems.Any(i => string.IsNullOrWhiteSpace(i.Name)))
        {
            await ShowAlertAsync("Błąd", "Wszystkie przedmioty muszą mieć podaną nazwę.", "OK");
            return;
        }

        if (allItems.Any(i => i.Payer == null))
        {
            await ShowAlertAsync("Błąd", "Wszystkie przedmioty muszą mieć przypisanego płatnika ('Kto zapłacił').", "OK");
            return;
        }

        if (allItems.Any(i => i.AssignedMembers.Count == 0))
        {
            await ShowAlertAsync("Błąd", "Wszystkie przedmioty muszą mieć przypisaną przynajmniej jedną osobę (przeciągnij produkty na osoby).", "OK");
            return;
        }

        IsBusy = true;
        try
        {
            var subExpenses = new List<SubExpenseDto>();

            var itemsByPayer = allItems.GroupBy(i => i.Payer!.UserId);

            foreach (var group in itemsByPayer)
            {
                var payerId = group.Key;
                var subExpense = new SubExpenseDto
                {
                    PayerId = payerId,
                    Name = $"Rozliczenie z paragonu - " + DateTime.Now.ToString("dd.MM.yyyy"),
                    PurchasedAt = DateTime.UtcNow,
                    ReceiptImageUrls = ReceiptPhotos.Select(p => p.UploadedFileName).ToList(),
                    Items = []
                };

                var singleItems = group.Where(i => string.IsNullOrEmpty(i.ItemGroupId)).ToList();
                var groupedItems = group.Where(i => !string.IsNullOrEmpty(i.ItemGroupId)).GroupBy(i => i.ItemGroupId);

                subExpense.Groups = [];

                foreach (var item in singleItems)
                {
                    var remainderRecipient = item.AssignedMembers.FirstOrDefault(m => m.IsRemainderRecipient)?.MemberId;

                    subExpense.Items.Add(new ExpenseItemDto
                    {
                        Name = item.Name,
                        Category = Categories.FirstOrDefault(c => c.Id == item.CategoryId)?.Name ?? "Bez Kategorii",
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        RemainderRecipientId = remainderRecipient,
                        ParticipantIds = item.AssignedMembers.Select(m => m.MemberId).ToList()
                    });
                }

                foreach (var g in groupedItems)
                {
                    var firstItem = g.First();
                    var allParticipants = g.SelectMany(i => i.AssignedMembers.Select(m => m.MemberId)).Distinct().ToList();

                    var expGroup = new ExpenseGroupDto
                    {
                        Name = firstItem.ItemGroupName ?? "Grupa produktów",
                        ParticipantIds = allParticipants,
                        Items = []
                    };

                    foreach (var item in g)
                    {
                        var itemRemainder = item.AssignedMembers.FirstOrDefault(m => m.IsRemainderRecipient)?.MemberId;
                        if (itemRemainder != null)
                        {
                            expGroup.RemainderRecipientId = itemRemainder;
                        }

                        expGroup.Items.Add(new ExpenseItemDto
                        {
                            Name = item.Name,
                            Category = Categories.FirstOrDefault(c => c.Id == item.CategoryId)?.Name ?? "Bez Kategorii",
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            RemainderRecipientId = itemRemainder,
                            ParticipantIds = item.AssignedMembers.Select(m => m.MemberId).ToList()
                        });
                    }
                    subExpense.Groups.Add(expGroup);
                }

                subExpenses.Add(subExpense);
            }

            var request = new CreateExpenseBatchRequest
            {
                GroupId = GroupId,
                Expenses = subExpenses
            };

            await _expenseService.CreateExpenseBatch(request);
            await ShowAlertAsync("Sukces", "Wydatek został zapisany!", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await ShowAlertAsync("Błąd", "Wystąpił problem podczas zapisu: " + ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private void RemovePhoto(ReceiptPhotoItem photo)
    {
        if (photo != null && ReceiptPhotos.Contains(photo))
        {
            ReceiptPhotos.Remove(photo);
            if (SelectedPhoto == photo)
            {
                SelectedPhoto = ReceiptPhotos.FirstOrDefault();
            }
        }
    }

    [RelayCommand]
    private void SwipeLeft()
    {
        if (ReceiptPhotos.Count <= 1 || SelectedPhoto == null)
        {
            return;
        }

        int currentIndex = ReceiptPhotos.IndexOf(SelectedPhoto);
        if (currentIndex < ReceiptPhotos.Count - 1)
        {
            SelectedPhoto = ReceiptPhotos[currentIndex + 1];
        }
    }

    [RelayCommand]
    private void SwipeRight()
    {
        if (ReceiptPhotos.Count <= 1 || SelectedPhoto == null)
        {
            return;
        }

        int currentIndex = ReceiptPhotos.IndexOf(SelectedPhoto);
        if (currentIndex > 0)
        {
            SelectedPhoto = ReceiptPhotos[currentIndex - 1];
        }
    }

    public void OnDisappearing()
    {
        _ = _signalRService.LeaveGroupAsync(GroupId);
    }
}

public partial class ReceiptPhotoItem : ObservableObject
{
    [ObservableProperty]
    public partial ImageSource? ImageSource { get; set; } = null!;

    public string UploadedFileName { get; set; } = string.Empty;
}
