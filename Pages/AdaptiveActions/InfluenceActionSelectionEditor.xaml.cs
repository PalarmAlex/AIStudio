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
  /// <summary>
  /// Форма выбора воздействия с пульта (триггера) в виде чек-списка.
  /// Источник справочника — Триггеры (Стимулы) из InfluenceActionSystem.
  /// Поле InfluenceActionId одиночное, поэтому выбор монопольный (radio).
  /// </summary>
  public partial class InfluenceActionSelectionEditor : Window
  {
    public int SelectedInfluenceActionId { get; private set; }

    private readonly List<InfluenceActionSelectionItem> _allItems =
        new List<InfluenceActionSelectionItem>();

    public InfluenceActionSelectionEditor(string title, int initiallySelectedId)
    {
      InitializeComponent();
      Title = title;
      SelectedInfluenceActionId = initiallySelectedId;
      LoadActions();
      ApplyFilter();
    }

    private void LoadActions()
    {
      _allItems.Clear();
      if (!InfluenceActionSystem.IsInitialized)
      {
        MessageBox.Show(
            "Система внешних воздействий не инициализирована",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        return;
      }

      foreach (var action in InfluenceActionSystem.Instance.GetAllInfluenceActions().OrderBy(a => a.Id))
      {
        _allItems.Add(new InfluenceActionSelectionItem
        {
          Id = action.Id,
          Name = action.Name ?? $"Воздействие {action.Id}",
          Description = action.Description ?? string.Empty,
          IsSelected = action.Id == SelectedInfluenceActionId
        });
      }
    }

    private void ApplyFilter()
    {
      string filter = (FilterTextBox?.Text ?? string.Empty).Trim();
      IEnumerable<InfluenceActionSelectionItem> items = _allItems;
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

      var selectedItem = filtered.FirstOrDefault(item => item.IsSelected);
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
      if (!(sender is RadioButton radioButton) || !(radioButton.DataContext is InfluenceActionSelectionItem item))
        return;

      SelectItem(item);
    }

    private void SelectItem(InfluenceActionSelectionItem item)
    {
      foreach (var actionItem in _allItems)
        actionItem.IsSelected = actionItem.Id == item.Id;

      SelectedInfluenceActionId = item.Id;
      ActionsList.Items.Refresh();
    }

    // Снимает выбор: InfluenceActionId = 0 означает «нет связи».
    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
      foreach (var actionItem in _allItems)
        actionItem.IsSelected = false;

      SelectedInfluenceActionId = 0;
      ActionsList.SelectedItem = null;
      ActionsList.Items.Refresh();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
      var selectedItem = _allItems.FirstOrDefault(item => item.IsSelected);
      SelectedInfluenceActionId = selectedItem?.Id ?? 0;

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
      if (!(ActionsList.SelectedItem is InfluenceActionSelectionItem item))
        return;

      SelectItem(item);
      DialogResult = true;
      Close();
    }
  }

  public sealed class InfluenceActionSelectionItem
  {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
  }
}