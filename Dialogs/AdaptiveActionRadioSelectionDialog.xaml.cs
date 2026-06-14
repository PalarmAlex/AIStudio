using ISIDA.Actions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AIStudio.Dialogs
{
  public partial class AdaptiveActionRadioSelectionDialog : Window
  {
    public int SelectedAdaptiveActionId { get; private set; }
    private readonly List<AdaptiveActionRadioItem> _allItems = new List<AdaptiveActionRadioItem>();

    public AdaptiveActionRadioSelectionDialog(int initiallySelectedId)
    {
      InitializeComponent();
      SelectedAdaptiveActionId = initiallySelectedId;
      LoadActions();
      ApplyFilter();
    }

    private void LoadActions()
    {
      _allItems.Clear();
      if (!AdaptiveActionsSystem.IsInitialized)
      {
        MessageBox.Show(
            "Система моторных действий не инициализирована",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        return;
      }

      foreach (var action in AdaptiveActionsSystem.Instance.GetAllAdaptiveActions().OrderBy(a => a.Id))
      {
        _allItems.Add(new AdaptiveActionRadioItem
        {
          Id = action.Id,
          Name = action.Name ?? string.Empty,
          Description = action.Description ?? string.Empty,
          IsSelected = action.Id == SelectedAdaptiveActionId
        });
      }
    }

    private void ApplyFilter()
    {
      string filter = (FilterTextBox?.Text ?? string.Empty).Trim();
      IEnumerable<AdaptiveActionRadioItem> items = _allItems;
      if (!string.IsNullOrEmpty(filter))
      {
        string lowered = filter.ToLowerInvariant();
        items = _allItems.Where(item =>
            item.Id.ToString(CultureInfo.InvariantCulture).Contains(lowered)
            || (item.Name ?? string.Empty).ToLowerInvariant().Contains(lowered)
            || (item.Description ?? string.Empty).ToLowerInvariant().Contains(lowered));
      }

      var filtered = items.ToList();
      ActionsList.ItemsSource = filtered;

      AdaptiveActionRadioItem selectedItem = filtered.FirstOrDefault(item => item.IsSelected);
      if (selectedItem != null)
      {
        ActionsList.SelectedItem = selectedItem;
        ActionsList.ScrollIntoView(selectedItem);
      }
    }

    private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
      ApplyFilter();
    }

    private void RadioButton_Click(object sender, RoutedEventArgs e)
    {
      if (!(sender is RadioButton radioButton) || !(radioButton.DataContext is AdaptiveActionRadioItem item))
        return;

      foreach (AdaptiveActionRadioItem actionItem in _allItems)
        actionItem.IsSelected = actionItem.Id == item.Id;

      SelectedAdaptiveActionId = item.Id;
      ActionsList.Items.Refresh();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
      AdaptiveActionRadioItem selectedItem = _allItems.FirstOrDefault(item => item.IsSelected);
      if (selectedItem != null)
        SelectedAdaptiveActionId = selectedItem.Id;

      DialogResult = true;
      Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
      Close();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Escape)
      {
        DialogResult = false;
        Close();
        e.Handled = true;
      }
      else if (e.Key == Key.Enter)
      {
        OkButton_Click(sender, e);
        e.Handled = true;
      }
    }

    private void ActionsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (!(ActionsList.SelectedItem is AdaptiveActionRadioItem item))
        return;

      foreach (AdaptiveActionRadioItem actionItem in _allItems)
        actionItem.IsSelected = actionItem.Id == item.Id;

      SelectedAdaptiveActionId = item.Id;
      DialogResult = true;
      Close();
    }
  }

  public sealed class AdaptiveActionRadioItem
  {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
  }
}
