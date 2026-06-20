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
  public partial class CommandPatternRadioSelectionDialog : Window
  {
    public int SelectedCommandPatternId { get; private set; }
    private readonly List<CommandPatternRadioItem> _allItems = new List<CommandPatternRadioItem>();

    public CommandPatternRadioSelectionDialog(int initiallySelectedId)
    {
      InitializeComponent();
      SelectedCommandPatternId = initiallySelectedId;
      LoadPatterns();
      ApplyFilter();
    }

    private void LoadPatterns()
    {
      _allItems.Clear();
      if (!SensorySystem.IsInitialized)
      {
        MessageBox.Show(
            "SensorySystem не инициализирован — CommandPhrases недоступны.",
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
        _allItems.Add(new CommandPatternRadioItem
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
      IEnumerable<CommandPatternRadioItem> items = _allItems;
      if (!string.IsNullOrEmpty(filter))
      {
        string lowered = filter.ToLowerInvariant();
        items = _allItems.Where(item =>
            item.Id.ToString(CultureInfo.InvariantCulture).Contains(lowered)
            || (item.Text ?? string.Empty).ToLowerInvariant().Contains(lowered));
      }

      var filtered = items.ToList();
      PatternsList.ItemsSource = filtered;

      CommandPatternRadioItem selectedItem = filtered.FirstOrDefault(item => item.IsSelected);
      if (selectedItem != null)
      {
        PatternsList.SelectedItem = selectedItem;
        PatternsList.ScrollIntoView(selectedItem);
      }
    }

    private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
      ApplyFilter();
    }

    private void RadioButton_Click(object sender, RoutedEventArgs e)
    {
      if (!(sender is RadioButton radioButton) || !(radioButton.DataContext is CommandPatternRadioItem item))
        return;

      foreach (CommandPatternRadioItem patternItem in _allItems)
        patternItem.IsSelected = patternItem.Id == item.Id;

      SelectedCommandPatternId = item.Id;
      PatternsList.Items.Refresh();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
      CommandPatternRadioItem selectedItem = _allItems.FirstOrDefault(item => item.IsSelected);
      if (selectedItem != null)
        SelectedCommandPatternId = selectedItem.Id;

      DialogResult = true;
      Close();
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
      SelectedCommandPatternId = 0;
      foreach (CommandPatternRadioItem patternItem in _allItems)
        patternItem.IsSelected = false;
      PatternsList.Items.Refresh();
      DialogResult = true;
      Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
      Close();
    }

    private void PatternsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (PatternsList.SelectedItem is CommandPatternRadioItem item)
      {
        SelectedCommandPatternId = item.Id;
        DialogResult = true;
        Close();
      }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Escape)
      {
        DialogResult = false;
        Close();
        e.Handled = true;
      }
    }

    private sealed class CommandPatternRadioItem
    {
      public int Id { get; set; }
      public string Text { get; set; }
      public bool IsSelected { get; set; }
    }
  }
}
