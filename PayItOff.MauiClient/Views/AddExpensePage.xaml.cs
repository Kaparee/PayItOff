using PayItOff.MauiClient.Models;
using PayItOff.MauiClient.ViewModels;

namespace PayItOff.MauiClient.Views;

public partial class AddExpensePage : ContentPage
{
    private readonly AddExpenseViewModel _viewModel;
    private DateTime _lastItemDropTime = DateTime.MinValue;

    public AddExpensePage(AddExpenseViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    private List<ReceiptItem> GetItemsToProcess(ReceiptItem item)
    {
        var list = new List<ReceiptItem>();
        if (string.IsNullOrEmpty(item.ItemGroupId))
        {
            list.Add(item);
        }
        else
        {
            list.AddRange(_viewModel.BufferItems.Where(x => x.ItemGroupId == item.ItemGroupId));
            list.AddRange(_viewModel.UncategorizedItems.Where(x => x.ItemGroupId == item.ItemGroupId));
            foreach (var cat in _viewModel.Categories)
            {
                list.AddRange(cat.Items.Where(x => x.ItemGroupId == item.ItemGroupId));
            }

            list = list.Distinct().ToList();
        }
        return list;
    }

    private async void OnCategoryDrop(object sender, DropEventArgs e)
    {
        if (sender is VisualElement target)
        {
            target.Opacity = 1.0;
            target.BackgroundColor = Colors.Transparent;
        }

        if ((DateTime.UtcNow - _lastItemDropTime).TotalMilliseconds < 200)
            return;

        if (e.Data.Properties.TryGetValue("Item", out var itemObj) && itemObj is ReceiptItem draggedItem)
        {
            if (sender is Element element && element.BindingContext is ReceiptCategory targetCategory)
            {
                if (draggedItem.CategoryId != targetCategory.Id)
                {
                    await Task.Delay(50);

                    _viewModel.UncategorizedItems.Remove(draggedItem);
                    _viewModel.BufferItems.Remove(draggedItem);
                    foreach (var cat in _viewModel.Categories)
                    {
                        cat.Items.Remove(draggedItem);
                    }

                    draggedItem.CategoryId = targetCategory.Id;
                    targetCategory.Items.Add(draggedItem);
                }
            }
        }
    }

    private async void OnUncategorizedDrop(object sender, DropEventArgs e)
    {
        if (sender is VisualElement target)
        {
            target.Opacity = 1.0;
            target.BackgroundColor = Colors.Transparent;
        }

        if ((DateTime.UtcNow - _lastItemDropTime).TotalMilliseconds < 200)
            return;

        if (e.Data.Properties.TryGetValue("Item", out var itemObj) && itemObj is ReceiptItem draggedItem)
        {
            if (draggedItem.CategoryId != "Uncategorized")
            {
                await Task.Delay(50);

                _viewModel.BufferItems.Remove(draggedItem);
                foreach (var cat in _viewModel.Categories)
                {
                    cat.Items.Remove(draggedItem);
                }

                draggedItem.CategoryId = "Uncategorized";
                if (!_viewModel.UncategorizedItems.Contains(draggedItem))
                {
                    _viewModel.UncategorizedItems.Add(draggedItem);
                }
            }
        }
    }

    private async void OnMemberDrop(object sender, DropEventArgs e)
    {
        if (sender is VisualElement target)
        {
            target.Opacity = 1.0;
            target.BackgroundColor = Color.FromArgb("#1E232D");
        }

        if ((DateTime.UtcNow - _lastItemDropTime).TotalMilliseconds < 200)
            return;

        if (sender is Element element && element.BindingContext is DisplayGroupMember targetMember)
        {
            await Task.Delay(50);

            if (e.Data.Properties.TryGetValue("Item", out var itemObj) && itemObj is ReceiptItem draggedItem)
            {
                var items = GetItemsToProcess(draggedItem);
                foreach (var item in items)
                {
                    _viewModel.AssignItemToMember(item, targetMember);
                }
            }
            else if (e.Data.Properties.TryGetValue("Category", out var catObj) && catObj is ReceiptCategory category)
            {
                _viewModel.AssignCategoryToMember(category, targetMember);
            }
            else if (e.Data.Properties.TryGetValue("Member", out var memObj) && memObj is DisplayGroupMember draggedMember)
            {
                _viewModel.MoveMember(draggedMember, targetMember);
            }
        }
    }

    private void OnMemberDragStarting(object sender, DragStartingEventArgs e)
    {
        if (sender is Element element && element.BindingContext is DisplayGroupMember member)
        {
            e.Data.Properties.Add("Member", member);
        }
    }

    private void OnItemDragStarting(object sender, DragStartingEventArgs e)
    {
        if (sender is Element element && element.BindingContext is ReceiptItem item)
        {
            e.Data.Properties.Add("Item", item);
        }
    }

    private void OnCategoryDragStarting(object sender, DragStartingEventArgs e)
    {
        if (sender is Element element && element.BindingContext is ReceiptCategory category)
        {
            e.Data.Properties.Add("Category", category);
        }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (sender is VisualElement target)
        {
            target.Opacity = 0.7;
            target.BackgroundColor = Color.FromArgb("#3C4556");
        }
    }

    private async void OnItemDrop(object sender, DropEventArgs e)
    {
        if (sender is VisualElement target)
        {
            target.Opacity = 1.0;
            target.BackgroundColor = Color.FromArgb("#353D4A");
        }

        if (sender is Element targetElement && targetElement.BindingContext is ReceiptItem targetItem)
        {
            if (e.Data.Properties.TryGetValue("Item", out var sourceObj) && sourceObj is ReceiptItem sourceItem)
            {
                _lastItemDropTime = DateTime.UtcNow;

                await Task.Delay(50);

                await _viewModel.GroupItemsAsync(sourceItem, targetItem);
            }
        }
    }

    private void OnDragLeave(object sender, DragEventArgs e)
    {
        if (sender is VisualElement target)
        {
            target.Opacity = 1.0;

            if (target.BindingContext is DisplayGroupMember)
                target.BackgroundColor = Color.FromArgb("#1E232D");
            else if (target.BindingContext is ReceiptItem)
                target.BackgroundColor = Color.FromArgb("#353D4A");
            else
                target.BackgroundColor = Colors.Transparent;
        }
    }

    private void OnPayerPickerLoaded(object sender, EventArgs e)
    {
        if (sender is Picker picker && picker.BindingContext is ReceiptItem item && item.Payer != null)
        {
            var idx = _viewModel.GroupMembers.IndexOf(item.Payer);
            if (idx >= 0)
            {
                picker.SelectedIndex = idx;
            }
        }
    }

    private void OnPayerSelectedIndexChanged(object sender, EventArgs e)
    {
        if (sender is Picker picker && picker.BindingContext is ReceiptItem item)
        {
            var idx = picker.SelectedIndex;
            if (idx >= 0 && idx < _viewModel.GroupMembers.Count)
            {
                item.Payer = _viewModel.GroupMembers[idx];
            }
        }
    }
}
