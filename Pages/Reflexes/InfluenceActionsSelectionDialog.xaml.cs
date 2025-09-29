using AIStudio.Common;
using ISIDA.Actions;
using ISIDA.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace AIStudio.Dialogs
{
  public partial class InfluenceActionsSelectionDialog : Window
  {
    public List<int> SelectedInfluenceActions { get; private set; }
    private List<InfluenceActionItem> _influenceActions;
    private AntagonistManager _antagonistManager;
    private bool _isManualSelection = false;

    public InfluenceActionsSelectionDialog(List<int> initiallySelected)
    {
      InitializeComponent();
      LoadInfluenceActions(initiallySelected);
    }

    private void LoadInfluenceActions(List<int> initiallySelected)
    {
      _influenceActions = new List<InfluenceActionItem>();

      try
      {
        if (!InfluenceActionSystem.IsInitialized)
        {
          MessageBox.Show("Система внешних воздействий не инициализирована",
              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        var influenceSystem = InfluenceActionSystem.Instance;
        var allActions = influenceSystem.GetAllInfluenceActions();

        foreach (var action in allActions.OrderBy(a => a.Id))
        {
          _influenceActions.Add(new InfluenceActionItem
          {
            Id = action.Id,
            Name = $"{action.Id}: {action.Name}",
            Description = action.Description,
            AntagonistIds = action.AntagonistInfluences ?? new List<int>(),
            IsSelected = initiallySelected != null && initiallySelected.Contains(action.Id)
          });
        }

        // Инициализируем стандартный менеджер антагонистов с callback'ом
        _antagonistManager = new AntagonistManager(
            _influenceActions.Cast<AntagonistItem>().ToList(),
            OnConflictResolutionRequired);

        // АКТИВАЦИЯ ПРОВЕРКИ ПРИ ОТКРЫТИИ ФОРМЫ
        if (_influenceActions.Any(item => item.IsSelected))
        {
          var firstSelectedItem = _influenceActions.FirstOrDefault(item => item.IsSelected);
          if (firstSelectedItem != null)
          {
            bool originalValue = firstSelectedItem.IsSelected;
            firstSelectedItem.IsSelected = false;
            firstSelectedItem.IsSelected = originalValue;
          }
        }

        // Подписываемся на события для обновления сообщения о конфликтах
        foreach (var item in _influenceActions)
        {
          item.OnSelectionChanged += UpdateConflictMessage;
        }

        UpdateConflictMessage(null);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка загрузки внешних воздействий: {ex.Message}",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }

      InfluenceActionsList.ItemsSource = _influenceActions;
    }

    private void CheckBox_Checked(object sender, RoutedEventArgs e)
    {
      _isManualSelection = true;
    }

    private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
      _isManualSelection = true;
    }

    private bool OnConflictResolutionRequired(List<AntagonistConflict> conflicts, AntagonistItem newlySelectedItem)
    {
      if (!_isManualSelection) return true; // Автоматические изменения — разрешаем

      var conflictItems = conflicts
          .Where(c => c.FirstId == newlySelectedItem.Id || c.SecondId == newlySelectedItem.Id)
          .SelectMany(c => new[] { c.FirstId, c.SecondId })
          .Where(id => id != newlySelectedItem.Id)
          .Distinct()
          .ToList();

      if (conflictItems.Any())
      {
        var conflictNames = conflictItems
            .Select(id => _influenceActions.FirstOrDefault(a => a.Id == id)?.Name ?? $"ID {id}")
            .ToList();

        var message = $"Выбор '{newlySelectedItem.Name}' конфликтует с:\n" +
                     string.Join("\n", conflictNames.Select(name => $"• {name}")) +
                     "\n\nЭти воздействия будут автоматически сняты. Продолжить?";

        var result = MessageBox.Show(message, "Конфликт воздействий",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result == MessageBoxResult.No)
        {
          _isManualSelection = false;
        }

        return result == MessageBoxResult.Yes;
      }

      return true;
    }

    private void UpdateConflictMessage(AntagonistItem changedItem)
    {
      var selectedIds = _influenceActions.Where(x => x.IsSelected).Select(x => x.Id).ToList();

      if (!selectedIds.Any())
      {
        ConflictMessage.Visibility = Visibility.Collapsed;
        return;
      }

      var antagonistsMap = _influenceActions.ToDictionary(
          item => item.Id,
          item => item.AntagonistIds);

      var conflicts = AntagonistValidator.ValidateAntagonists(selectedIds, antagonistsMap);

      if (conflicts.Any())
      {
        var conflictDetails = conflicts
            .Select(c =>
            {
              var action1 = _influenceActions.FirstOrDefault(a => a.Id == c.FirstId);
              var action2 = _influenceActions.FirstOrDefault(a => a.Id == c.SecondId);
              return $"• {action1?.Name} (ID:{c.FirstId}) ↔ {action2?.Name} (ID:{c.SecondId})";
            })
            .ToList();

        ConflictMessage.Text = "Обнаружены конфликты:\n" + string.Join("\n", conflictDetails);
        ConflictMessage.Visibility = Visibility.Visible;
      }
      else
      {
        ConflictMessage.Visibility = Visibility.Collapsed;
      }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
      var selectedIds = _influenceActions
          .Where(x => x.IsSelected)
          .Select(x => x.Id)
          .ToList();

      var antagonistsMap = _influenceActions.ToDictionary(
          item => item.Id,
          item => item.AntagonistIds);

      var conflicts = AntagonistValidator.ValidateAntagonists(selectedIds, antagonistsMap);

      if (conflicts.Any())
      {
        var conflictDetails = conflicts
            .Select(c =>
            {
              var action1 = _influenceActions.FirstOrDefault(a => a.Id == c.FirstId);
              var action2 = _influenceActions.FirstOrDefault(a => a.Id == c.SecondId);
              return $"• {action1?.Name} (ID:{c.FirstId}) ↔ {action2?.Name} (ID:{c.SecondId})";
            })
            .ToList();

        var message = "Обнаружены конфликтующие воздействия:\n" +
                     string.Join("\n", conflictDetails) +
                     "\n\nСохранить несмотря на конфликты?";

        var result = MessageBox.Show(message, "Подтверждение сохранения",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result == MessageBoxResult.No)
        {
          return;
        }
      }

      SelectedInfluenceActions = selectedIds;
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

    protected override void OnClosed(EventArgs e)
    {
      foreach (var item in _influenceActions)
      {
        item.OnSelectionChanged -= UpdateConflictMessage;
      }
      _antagonistManager?.Dispose();
      base.OnClosed(e);
    }
  }

  public class InfluenceActionItem : AntagonistItem
  {
    // Дополнительные свойства при необходимости
  }
}