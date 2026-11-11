using ISIDA.Sensors;
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
  /// Форма выбора команды (паттерна Command-канала, CommandPhrases.dat) в виде чек-списка.
  /// Источник справочника — узлы PhraseTree VerbalSensorChannel (CommandChannel).
  /// Поле CommandPatternId одиночное, поэтому выбор монопольный (radio).
  /// В отличие от InfluenceActionSelectionEditor здесь один столбец данных + фильтр.
  /// </summary>
  public partial class CommandSelectionEditor : Window
  {
    public int SelectedCommandPatternId { get; private set; }

    private readonly List<CommandSelectionItem> _allItems = new List<CommandSelectionItem>();

    public CommandSelectionEditor(string title, int initiallySelectedId)
    {
      InitializeComponent();
      Title = title;
      SelectedCommandPatternId = initiallySelectedId;
      LoadCommands();
      ApplyFilter();
    }

    private void LoadCommands()
    {
      _allItems.Clear();
      if (!SensorySystem.IsInitialized)
      {
        MessageBox.Show(
            "Система сенсорики не инициализирована — команды недоступны.",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        return;
      }

      VerbalSensorChannel commandChannel = SensorySystem.Instance.CommandChannel;
      if (commandChannel?.PhraseTree?.Nodes == null)
        return;

      foreach (var node in commandChannel.PhraseTree.Nodes.Values.OrderBy(n => n.Id))
      {
        if (node.Id <= 0)
          continue;

        string text = commandChannel.GetPhraseFromPhraseId(node.Id) ?? string.Empty;
        _allItems.Add(new CommandSelectionItem
        {
          Id = node.Id,
          Text = text,
          IsSelected = node.Id == SelectedCommandPatternId
        });
      }
    }

    private void ApplyFilter()
    {
      string filter = (FilterTextBox?.Text ?? string.Empty).Trim();
      IEnumerable<CommandSelectionItem> items = _allItems;
      if (!string.IsNullOrEmpty(filter))
      {
        string lowered = filter.ToLowerInvariant();
        items = _allItems.Where(item =>
            (item.Text ?? string.Empty).ToLowerInvariant().Contains(lowered));
      }

      var filtered = items.ToList();
      CommandsList.ItemsSource = filtered;

      var selectedItem = filtered.FirstOrDefault(item => item.IsSelected);
      if (selectedItem != null)
      {
        CommandsList.SelectedItem = selectedItem;
        CommandsList.ScrollIntoView(selectedItem);
      }
    }

    private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
      ApplyFilter();
    }

    private void RadioButton_Click(object sender, RoutedEventArgs e)
    {
      if (!(sender is RadioButton radioButton) || !(radioButton.DataContext is CommandSelectionItem item))
        return;

      SelectItem(item);
    }

    private void SelectItem(CommandSelectionItem item)
    {
      foreach (var commandItem in _allItems)
        commandItem.IsSelected = commandItem.Id == item.Id;

      SelectedCommandPatternId = item.Id;
      CommandsList.Items.Refresh();
    }

    // Снимает выбор: CommandPatternId = 0 означает «нет связи».
    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
      foreach (var commandItem in _allItems)
        commandItem.IsSelected = false;

      SelectedCommandPatternId = 0;
      CommandsList.SelectedItem = null;
      CommandsList.Items.Refresh();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
      var selectedItem = _allItems.FirstOrDefault(item => item.IsSelected);
      SelectedCommandPatternId = selectedItem?.Id ?? 0;

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

    private void CommandsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (!(CommandsList.SelectedItem is CommandSelectionItem item))
        return;

      SelectItem(item);
      DialogResult = true;
      Close();
    }
  }

  public sealed class CommandSelectionItem
  {
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsSelected { get; set; }

    /// <summary>Отображаемая строка в единственном столбце: «ID — текст».</summary>
    public string Display => $"{Id} — {Text}";
  }
}
