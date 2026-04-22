using AIStudio.Common;
using ISIDA.Actions;
using ISIDA.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace AIStudio.Dialogs
{
  public partial class ScenarioInfluenceActionsEditor : Window
  {
    public List<int> SelectedActionIds { get; private set; }

    private readonly List<int> _initialIds;
    private List<ScenarioInfluenceActionItem> _items;
    private AntagonistManager _antagonistManager;
    private bool _isManualSelection;

    public ScenarioInfluenceActionsEditor(string title,
        IEnumerable<InfluenceActionSystem.GomeostasisInfluenceAction> allActions,
        IList<int> currentIds)
    {
      InitializeComponent();
      Title = title;
      if (HeaderTitle != null)
        HeaderTitle.Text = title;
      var cur = currentIds ?? new List<int>();
      _initialIds = cur.OrderBy(x => x).ToList();
      SelectedActionIds = new List<int>(cur);

      _items = allActions
          .OrderBy(a => a.Id)
          .Select(a => new ScenarioInfluenceActionItem
          {
            Id = a.Id,
            Name = $"{a.Id}: {a.Name ?? ""}",
            Description = a.Description,
            AntagonistIds = a.AntagonistInfluences ?? new List<int>(),
            IsSelected = cur.Contains(a.Id)
          })
          .ToList();

      _antagonistManager = new AntagonistManager(
          _items.Cast<AntagonistItem>().ToList(),
          OnConflictResolutionRequired);

      if (_items.Any(item => item.IsSelected))
      {
        var firstSelectedItem = _items.FirstOrDefault(item => item.IsSelected);
        if (firstSelectedItem != null)
        {
          bool originalValue = firstSelectedItem.IsSelected;
          firstSelectedItem.IsSelected = false;
          firstSelectedItem.IsSelected = originalValue;
        }
      }

      foreach (var item in _items)
        item.OnSelectionChanged += UpdateConflictMessage;

      UpdateConflictMessage(null);

      ActionsList.ItemsSource = _items;
    }

    private void CheckBox_Checked(object sender, RoutedEventArgs e) => _isManualSelection = true;

    private void CheckBox_Unchecked(object sender, RoutedEventArgs e) => _isManualSelection = true;

    private bool OnConflictResolutionRequired(List<AntagonistConflict> conflicts, AntagonistItem newlySelectedItem)
    {
      if (!_isManualSelection)
        return true;

      var conflictItems = conflicts
          .Where(c => c.FirstId == newlySelectedItem.Id || c.SecondId == newlySelectedItem.Id)
          .SelectMany(c => new[] { c.FirstId, c.SecondId })
          .Where(id => id != newlySelectedItem.Id)
          .Distinct()
          .ToList();

      if (!conflictItems.Any())
        return true;

      var conflictNames = conflictItems
          .Select(id => _items.FirstOrDefault(a => a.Id == id)?.Name ?? $"ID {id}")
          .ToList();

      var message = $"Выбор '{newlySelectedItem.Name}' конфликтует с:\n" +
                   string.Join("\n", conflictNames.Select(name => $"• {name}")) +
                   "\n\nЭти воздействия будут автоматически сняты. Продолжить?";

      var result = MessageBox.Show(message, "Конфликт воздействий",
          MessageBoxButton.YesNo, MessageBoxImage.Warning);

      if (result == MessageBoxResult.No)
        _isManualSelection = false;

      return result == MessageBoxResult.Yes;
    }

    private void UpdateConflictMessage(AntagonistItem changedItem)
    {
      var selectedIds = _items.Where(x => x.IsSelected).Select(x => x.Id).ToList();

      if (!selectedIds.Any())
      {
        ConflictMessagePanel.Visibility = Visibility.Collapsed;
        return;
      }

      var antagonistsMap = _items.ToDictionary(
          item => item.Id,
          item => item.AntagonistIds);

      var conflicts = AntagonistValidator.ValidateAntagonists(selectedIds, antagonistsMap);

      if (conflicts.Any())
      {
        var conflictDetails = conflicts
            .Select(c =>
            {
              var action1 = _items.FirstOrDefault(a => a.Id == c.FirstId);
              var action2 = _items.FirstOrDefault(a => a.Id == c.SecondId);
              return $"• {action1?.Name} (ID:{c.FirstId}) ↔ {action2?.Name} (ID:{c.SecondId})";
            })
            .ToList();

        ConflictMessage.Text = "Обнаружены конфликты:\n" + string.Join("\n", conflictDetails);
        ConflictMessagePanel.Visibility = Visibility.Visible;
      }
      else
      {
        ConflictMessagePanel.Visibility = Visibility.Collapsed;
      }
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
      var selectedIds = _items
          .Where(x => x.IsSelected)
          .Select(x => x.Id)
          .ToList();

      var antagonistsMap = _items.ToDictionary(
          item => item.Id,
          item => item.AntagonistIds);

      var conflicts = AntagonistValidator.ValidateAntagonists(selectedIds, antagonistsMap);

      if (conflicts.Any())
      {
        var conflictDetails = conflicts
            .Select(c =>
            {
              var action1 = _items.FirstOrDefault(a => a.Id == c.FirstId);
              var action2 = _items.FirstOrDefault(a => a.Id == c.SecondId);
              return $"• {action1?.Name} (ID:{c.FirstId}) ↔ {action2?.Name} (ID:{c.SecondId})";
            })
            .ToList();

        var message = "Обнаружены конфликтующие воздействия:\n" +
                     string.Join("\n", conflictDetails) +
                     "\n\nСохранить несмотря на конфликты?";

        var result = MessageBox.Show(message, "Подтверждение сохранения",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result == MessageBoxResult.No)
          return;
      }

      SelectedActionIds = selectedIds;
      DialogResult = true;
      Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
      Close();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Escape)
      {
        Close();
        e.Handled = true;
      }
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
      if (DialogResult == true)
        return;
      if (!HasSelectionChanged())
        return;
      var r = MessageBox.Show(
          "Закрыть без сохранения выбранных воздействий?",
          Title,
          MessageBoxButton.YesNo,
          MessageBoxImage.Question);
      if (r != MessageBoxResult.Yes)
        e.Cancel = true;
    }

    private bool HasSelectionChanged()
    {
      var cur = _items
          .Where(x => x.IsSelected)
          .Select(x => x.Id)
          .OrderBy(x => x)
          .ToList();
      return !cur.SequenceEqual(_initialIds);
    }

    protected override void OnClosed(EventArgs e)
    {
      if (_items != null)
      {
        foreach (var item in _items)
          item.OnSelectionChanged -= UpdateConflictMessage;
      }
      _antagonistManager?.Dispose();
      base.OnClosed(e);
    }
  }

  internal sealed class ScenarioInfluenceActionItem : AntagonistItem
  {
  }
}
