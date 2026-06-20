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
  public partial class CommandPatternsSelectionDialog : Window
  {
    public List<int> SelectedCommandPatternIds { get; private set; } = new List<int>();
    private readonly List<CommandPatternItem> _allItems = new List<CommandPatternItem>();
    private readonly List<CommandPatternItem> _selectedItems = new List<CommandPatternItem>();

    public CommandPatternsSelectionDialog(IList<int> initiallySelected)
    {
      InitializeComponent();
      LoadPatterns(initiallySelected ?? new List<int>());
      ApplyFilter();
      RefreshSelectedList();
    }

    private void LoadPatterns(IList<int> initiallySelected)
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

      var selectedSet = new HashSet<int>(initiallySelected.Where(id => id > 0));
      foreach (var node in commandChannel.PhraseTree.Nodes.Values.OrderBy(n => n.Id))
      {
        if (node.Id <= 0)
          continue;

        string text = commandChannel.GetPhraseFromPhraseId(node.Id) ?? string.Empty;
        _allItems.Add(new CommandPatternItem
        {
          Id = node.Id,
          Text = text,
          DisplayText = node.Id + ": " + text
        });
      }

      foreach (int id in initiallySelected.Where(id => id > 0))
      {
        CommandPatternItem item = _allItems.FirstOrDefault(x => x.Id == id);
        if (item != null && !_selectedItems.Contains(item))
          _selectedItems.Add(item);
      }
    }

    private void ApplyFilter()
    {
      string filter = (FilterTextBox?.Text ?? string.Empty).Trim();
      IEnumerable<CommandPatternItem> items = _allItems.Where(i => !_selectedItems.Contains(i));
      if (!string.IsNullOrEmpty(filter))
      {
        string lowered = filter.ToLowerInvariant();
        items = items.Where(item =>
            item.Id.ToString(CultureInfo.InvariantCulture).Contains(lowered)
            || (item.Text ?? string.Empty).ToLowerInvariant().Contains(lowered));
      }

      AvailableList.ItemsSource = items.ToList();
    }

    private void RefreshSelectedList()
    {
      SelectedList.ItemsSource = null;
      SelectedList.ItemsSource = _selectedItems.ToList();
    }

    private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
      ApplyFilter();
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
      if (AvailableList.SelectedItem is CommandPatternItem item && !_selectedItems.Contains(item))
      {
        _selectedItems.Add(item);
        ApplyFilter();
        RefreshSelectedList();
      }
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
      if (SelectedList.SelectedItem is CommandPatternItem item)
      {
        _selectedItems.Remove(item);
        ApplyFilter();
        RefreshSelectedList();
      }
    }

    private void MoveUpButton_Click(object sender, RoutedEventArgs e)
    {
      if (!(SelectedList.SelectedItem is CommandPatternItem item))
        return;
      int index = _selectedItems.IndexOf(item);
      if (index <= 0)
        return;
      _selectedItems.RemoveAt(index);
      _selectedItems.Insert(index - 1, item);
      RefreshSelectedList();
      SelectedList.SelectedItem = item;
    }

    private void MoveDownButton_Click(object sender, RoutedEventArgs e)
    {
      if (!(SelectedList.SelectedItem is CommandPatternItem item))
        return;
      int index = _selectedItems.IndexOf(item);
      if (index < 0 || index >= _selectedItems.Count - 1)
        return;
      _selectedItems.RemoveAt(index);
      _selectedItems.Insert(index + 1, item);
      RefreshSelectedList();
      SelectedList.SelectedItem = item;
    }

    private void AvailableList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      AddButton_Click(sender, e);
    }

    private void SelectedList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      RemoveButton_Click(sender, e);
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
      SelectedCommandPatternIds = _selectedItems.Select(x => x.Id).ToList();
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
    }

    private sealed class CommandPatternItem
    {
      public int Id { get; set; }
      public string Text { get; set; }
      public string DisplayText { get; set; }
    }
  }
}
