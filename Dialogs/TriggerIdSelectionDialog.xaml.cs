using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AIStudio.Dialogs
{
  public partial class TriggerIdSelectionDialog : Window
  {
    public string SelectedTriggerId { get; private set; }
    public string SelectedDisplayName { get; private set; }
    private readonly List<TriggerCatalogItem> _allItems = new List<TriggerCatalogItem>();

    public TriggerIdSelectionDialog(string initiallySelected, IEnumerable<TriggerCatalogItem> entries)
    {
      InitializeComponent();
      SelectedTriggerId = initiallySelected ?? string.Empty;
      LoadEntries(entries);
      ApplyFilter();
    }

    private void LoadEntries(IEnumerable<TriggerCatalogItem> entries)
    {
      _allItems.Clear();
      if (entries == null)
        return;

      var knownIds = new HashSet<string>(StringComparer.Ordinal);
      foreach (TriggerCatalogItem entry in entries.OrderBy(e => e?.Id ?? string.Empty, StringComparer.Ordinal))
      {
        if (entry == null)
          continue;
        string id = (entry.Id ?? string.Empty).Trim();
        if (!knownIds.Add(id))
          continue;
        _allItems.Add(new TriggerCatalogItem
        {
          Id = id,
          Label = entry.Label ?? string.Empty,
          Description = entry.Description ?? string.Empty,
          IsSelected = string.Equals(id, SelectedTriggerId, StringComparison.Ordinal)
        });
      }
    }

    private void ApplyFilter()
    {
      string filter = (FilterTextBox?.Text ?? string.Empty).Trim();
      IEnumerable<TriggerCatalogItem> items = _allItems;
      if (!string.IsNullOrEmpty(filter))
      {
        string lowered = filter.ToLowerInvariant();
        items = _allItems.Where(item =>
            (item.Id ?? string.Empty).ToLowerInvariant().Contains(lowered)
            || (item.Label ?? string.Empty).ToLowerInvariant().Contains(lowered)
            || (item.Description ?? string.Empty).ToLowerInvariant().Contains(lowered));
      }

      var filtered = items.ToList();
      TriggersList.ItemsSource = filtered;

      TriggerCatalogItem selectedItem = filtered.FirstOrDefault(item => item.IsSelected);
      if (selectedItem != null)
      {
        TriggersList.SelectedItem = selectedItem;
        TriggersList.ScrollIntoView(selectedItem);
      }
    }

    private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
      ApplyFilter();
    }

    private void RadioButton_Click(object sender, RoutedEventArgs e)
    {
      if (!(sender is RadioButton radioButton) || !(radioButton.DataContext is TriggerCatalogItem item))
        return;

      foreach (TriggerCatalogItem catalogItem in _allItems)
        catalogItem.IsSelected = string.Equals(catalogItem.Id, item.Id, StringComparison.Ordinal);

      SelectedTriggerId = item.Id;
      SelectedDisplayName = item.Label;
      TriggersList.Items.Refresh();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
      TriggerCatalogItem selectedItem = _allItems.FirstOrDefault(item => item.IsSelected);
      if (selectedItem != null)
      {
        SelectedTriggerId = selectedItem.Id;
        SelectedDisplayName = selectedItem.Label;
      }

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

    private void TriggersList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (!(TriggersList.SelectedItem is TriggerCatalogItem item))
        return;

      foreach (TriggerCatalogItem catalogItem in _allItems)
        catalogItem.IsSelected = string.Equals(catalogItem.Id, item.Id, StringComparison.Ordinal);

      SelectedTriggerId = item.Id;
      SelectedDisplayName = item.Label;
      DialogResult = true;
      Close();
    }
  }

  public sealed class TriggerCatalogItem
  {
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
  }
}
