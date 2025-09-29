using AIStudio.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ISIDA.Gomeostas;
using ISIDA.Common;

namespace AIStudio.Dialogs
{
  public partial class StyleSelectionEditor : Window
  {
    public class StyleItem : AntagonistItem
    {
      public string DisplayText => $"{Name} (ID: {Id})";
    }

    public IEnumerable<int> SelectedStyleIds =>
        _styleItems.Where(x => x.IsSelected).Select(x => x.Id);

    private readonly List<StyleItem> _styleItems;
    private AntagonistManager _antagonistManager;
    private bool _isManualSelection = false;
    private bool _notСheckАntagonists = false;

    public StyleSelectionEditor(
        string title,
        IEnumerable<GomeostasSystem.BehaviorStyle> availableStyles,
        IEnumerable<int> selectedStyleIds,
        bool notСheckАntagonists = false)
    {
      InitializeComponent();
      DataContext = this;
      Title = title;
      _notСheckАntagonists = notСheckАntagonists;

      var selectedIds = new HashSet<int>(selectedStyleIds);
      _styleItems = availableStyles
          .OrderBy(s => s.Id)
          .Select(s => new StyleItem
          {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            AntagonistIds = s.AntagonistStyles?.ToList() ?? new List<int>(),
            IsSelected = selectedIds.Contains(s.Id)
          })
          .ToList();

      StylesListBox.ItemsSource = _styleItems;

      // Если проверка антагонистов отключена, просто показываем список
      if (_notСheckАntagonists)
      {
        ConflictMessage.Visibility = Visibility.Collapsed;
        return;
      }

      // Инициализируем менеджер антагонистов с callback'ом для подтверждения
      _antagonistManager = new AntagonistManager(
          _styleItems.Cast<AntagonistItem>().ToList(),
          OnConflictResolutionRequired);

      if (_styleItems.Any(item => item.IsSelected))
      {
        // Находим первый выбранный элемент и "триггерим" его изменение
        var firstSelectedItem = _styleItems.FirstOrDefault(item => item.IsSelected);
        if (firstSelectedItem != null)
        {
          // Временно снимаем и возвращаем выбор, чтобы активировать проверку
          bool originalValue = firstSelectedItem.IsSelected;
          firstSelectedItem.IsSelected = false;
          firstSelectedItem.IsSelected = originalValue;
        }
      }

      // Подписываемся на события для обновления сообщения о конфликтах
      foreach (var item in _styleItems)
      {
        item.OnSelectionChanged += UpdateConflictMessage;
      }

      UpdateConflictMessage(null);
    }

    private bool OnConflictResolutionRequired(List<AntagonistConflict> conflicts, AntagonistItem newlySelectedItem)
    {
      if (!_isManualSelection) return true; // Автоматические изменения - разрешаем

      // Для ручного выбора показываем подтверждение
      var conflictItems = conflicts
          .Where(c => c.FirstId == newlySelectedItem.Id || c.SecondId == newlySelectedItem.Id)
          .SelectMany(c => new[] { c.FirstId, c.SecondId })
          .Where(id => id != newlySelectedItem.Id)
          .Distinct()
          .ToList();

      if (conflictItems.Any())
      {
        var conflictNames = conflictItems
            .Select(id =>
            {
              var style = _styleItems.FirstOrDefault(s => s.Id == id);
              return style != null ? $"{style.Name} (ID:{style.Id})" : $"ID {id}";
            })
            .ToList();

        var message = $"Выбор '{newlySelectedItem.Name}' (ID:{newlySelectedItem.Id}) конфликтует с:\n" +
                     string.Join("\n", conflictNames.Select(name => $"• {name}")) +
                     "\n\nЭти стили будут автоматически сняты. Продолжить?";

        var result = MessageBox.Show(message, "Конфликт стилей",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result == MessageBoxResult.No)
        {
          // Сбрасываем флаг ручного выбора для этого изменения
          _isManualSelection = false;
        }

        return result == MessageBoxResult.Yes;
      }

      return true;
    }

    private void CheckBox_Checked(object sender, RoutedEventArgs e)
    {
      _isManualSelection = true;
    }

    private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
      _isManualSelection = true;
    }

    private void UpdateConflictMessage(AntagonistItem changedItem)
    {
      // Если проверка антагонистов отключена, скрываем сообщение
      if (_notСheckАntagonists)
      {
        ConflictMessage.Visibility = Visibility.Collapsed;
        return;
      }

      var selectedIds = _styleItems.Where(x => x.IsSelected).Select(x => x.Id).ToList();

      if (!selectedIds.Any())
      {
        ConflictMessage.Visibility = Visibility.Collapsed;
        return;
      }

      var antagonistsMap = _styleItems.ToDictionary(
          item => item.Id,
          item => item.AntagonistIds);

      var conflicts = AntagonistValidator.ValidateAntagonists(selectedIds, antagonistsMap);

      if (conflicts.Any())
      {
        var conflictDetails = conflicts
            .Select(c =>
            {
              var style1 = _styleItems.FirstOrDefault(s => s.Id == c.FirstId);
              var style2 = _styleItems.FirstOrDefault(s => s.Id == c.SecondId);
              return $"• {style1?.Name} (ID:{c.FirstId}) ↔ {style2?.Name} (ID:{c.SecondId})";
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

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
      // Если проверка антагонистов отключена, просто сохраняем без проверок
      if (_notСheckАntagonists)
      {
        DialogResult = true;
        Close();
        return;
      }

      // Проверяем на конфликты перед сохранением
      var selectedIds = _styleItems.Where(x => x.IsSelected).Select(x => x.Id).ToList();
      var antagonistsMap = _styleItems.ToDictionary(
          item => item.Id,
          item => item.AntagonistIds);

      var conflicts = AntagonistValidator.ValidateAntagonists(selectedIds, antagonistsMap);

      if (conflicts.Any())
      {
        var conflictDetails = conflicts
            .Select(c =>
            {
              var style1 = _styleItems.FirstOrDefault(s => s.Id == c.FirstId);
              var style2 = _styleItems.FirstOrDefault(s => s.Id == c.SecondId);
              return $"• {style1?.Name} (ID:{c.FirstId}) ↔ {style2?.Name} (ID:{c.SecondId})";
            })
            .ToList();

        var message = "Обнаружены конфликтующие стили поведения:\n" +
                     string.Join("\n", conflictDetails) +
                     "\n\nСохранить несмотря на конфликты?";

        var result = MessageBox.Show(message, "Подтверждение сохранения",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result == MessageBoxResult.No)
        {
          return;
        }
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
    }

    protected override void OnClosed(EventArgs e)
    {
      if (!_notСheckАntagonists)
      {
        // Отписываемся от событий и освобождаем ресурсы
        foreach (var item in _styleItems)
        {
          item.OnSelectionChanged -= UpdateConflictMessage;
        }
        _antagonistManager?.Dispose();
      }

      base.OnClosed(e);
    }
  }
}