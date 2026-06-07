using AIStudio.Common.Adapters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AIStudio.Dialogs
{
  public partial class RecipeIdSelectionDialog : Window
  {
    public string SelectedRecipeId { get; private set; }
    private readonly List<RecipeCatalogItem> _allItems = new List<RecipeCatalogItem>();

    public RecipeIdSelectionDialog(string initiallySelected, IEnumerable<AdapterSchemaRecipeCatalogEntry> entries)
    {
      InitializeComponent();
      SelectedRecipeId = initiallySelected ?? string.Empty;
      LoadEntries(entries);
      ApplyFilter();
    }

    private void LoadEntries(IEnumerable<AdapterSchemaRecipeCatalogEntry> entries)
    {
      _allItems.Clear();
      if (entries == null)
        return;

      var knownIds = new HashSet<string>(StringComparer.Ordinal);
      foreach (AdapterSchemaRecipeCatalogEntry entry in entries.OrderBy(e => e?.Id ?? string.Empty, StringComparer.Ordinal))
      {
        if (entry == null)
          continue;
        string id = (entry.Id ?? string.Empty).Trim();
        if (!knownIds.Add(id))
          continue;
        _allItems.Add(new RecipeCatalogItem
        {
          Id = id,
          Label = entry.Label ?? string.Empty,
          Description = entry.Description ?? string.Empty,
          DisplayText = entry.DisplayText,
          IsSelected = string.Equals(id, SelectedRecipeId, StringComparison.Ordinal)
        });
      }
    }

    private void ApplyFilter()
    {
      string filter = (FilterTextBox?.Text ?? string.Empty).Trim();
      IEnumerable<RecipeCatalogItem> items = _allItems;
      if (!string.IsNullOrEmpty(filter))
      {
        string lowered = filter.ToLowerInvariant();
        items = _allItems.Where(item => MatchesFilter(item, lowered));
      }

      var filtered = items.ToList();
      RecipesList.ItemsSource = filtered;

      RecipeCatalogItem selectedItem = filtered.FirstOrDefault(item => item.IsSelected);
      if (selectedItem != null)
      {
        RecipesList.SelectedItem = selectedItem;
        RecipesList.ScrollIntoView(selectedItem);
      }
    }

    private static bool MatchesFilter(RecipeCatalogItem item, string loweredFilter)
    {
      return (item.Id ?? string.Empty).ToLowerInvariant().Contains(loweredFilter)
          || (item.Label ?? string.Empty).ToLowerInvariant().Contains(loweredFilter)
          || (item.Description ?? string.Empty).ToLowerInvariant().Contains(loweredFilter)
          || (item.DisplayText ?? string.Empty).ToLowerInvariant().Contains(loweredFilter);
    }

    private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
      ApplyFilter();
    }

    private void RadioButton_Click(object sender, RoutedEventArgs e)
    {
      if (!(sender is RadioButton radioButton) || !(radioButton.DataContext is RecipeCatalogItem item))
        return;

      foreach (RecipeCatalogItem catalogItem in _allItems)
        catalogItem.IsSelected = string.Equals(catalogItem.Id, item.Id, StringComparison.Ordinal);

      SelectedRecipeId = item.Id;
      RecipesList.Items.Refresh();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
      RecipeCatalogItem selectedItem = _allItems.FirstOrDefault(item => item.IsSelected);
      if (selectedItem != null)
        SelectedRecipeId = selectedItem.Id;

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

    private void RecipesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (!(RecipesList.SelectedItem is RecipeCatalogItem item))
        return;

      foreach (RecipeCatalogItem catalogItem in _allItems)
        catalogItem.IsSelected = string.Equals(catalogItem.Id, item.Id, StringComparison.Ordinal);

      SelectedRecipeId = item.Id;
      DialogResult = true;
      Close();
    }
  }

  public sealed class RecipeCatalogItem
  {
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DisplayText { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
  }
}
