using AIStudio.Common;
using ISIDA.Common;
using ISIDA.Gomeostas;
using ISIDA.Psychic.Understanding;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace AIStudio.ViewModels
{
  /// <summary>Строка таблицы типов тем (theme_types.dat): ID, описание и вес по умолчанию.</summary>
  public class ThemeTypeItem : INotifyPropertyChanged
  {
    private int _id;
    private string _description;
    private int _defaultWeight = 2;

    public int Id { get => _id; set { if (_id != value) { _id = value; OnPropertyChanged(nameof(Id)); } } }
    public string Description { get => _description; set { if (_description != value) { _description = value; OnPropertyChanged(nameof(Description)); } } }
    public int DefaultWeight { get => _defaultWeight; set { if (_defaultWeight != value) { _defaultWeight = value; OnPropertyChanged(nameof(DefaultWeight)); } } }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
  }

  public class ThemeTypesViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly GomeostasSystem _gomeostas;
    private int _currentAgentStage;

    public bool IsStageFour => _currentAgentStage == 4;
    public string CurrentAgentTitle => "Темы мышления";

    public DescriptionWithLink CurrentAgentDescription => new DescriptionWithLink
    {
      Text = "Темы мышления агента. "
    };

    public ObservableCollection<ThemeTypeItem> ThemeTypes { get; } = new ObservableCollection<ThemeTypeItem>();

    /// <summary>Текст блока «Темы мышления привязанные к событиям агента»: строки вида «ID: 1, Описание».</summary>
    public string ReservedThemeTypesText { get; private set; } = "";

    public ICommand SaveCommand { get; }
    public ICommand ClearCommand { get; }

    public ThemeTypesViewModel(GomeostasSystem gomeostas)
    {
      _gomeostas = gomeostas ?? throw new ArgumentNullException(nameof(gomeostas));
      SaveCommand = new RelayCommand(SaveData);
      ClearCommand = new RelayCommand(ClearData);
      GlobalTimer.PulsationStateChanged += OnPulsationStateChanged;
      LoadData();
    }

    private void OnPulsationStateChanged()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        OnPropertyChanged(nameof(IsEditingEnabled));
        OnPropertyChanged(nameof(PulseWarningMessage));
        OnPropertyChanged(nameof(WarningMessageColor));
        OnPropertyChanged(nameof(IsReadOnlyMode));
      });
    }

    #region Блокировка страницы

    public bool IsEditingEnabled => IsStageFour && !GlobalTimer.IsPulsationRunning;
    public bool IsReadOnlyMode => !IsEditingEnabled;

    public string PulseWarningMessage =>
        !IsStageFour
            ? "[КРИТИЧНО] Редактирование тем мышления доступно только в стадии 4"
            : GlobalTimer.IsPulsationRunning
                ? "Редактирование доступно только при выключенной пульсации"
                : string.Empty;

    public Brush WarningMessageColor =>
        !IsStageFour ? Brushes.Red : Brushes.Gray;

    #endregion

    private void LoadData()
    {
      try
      {
        var agentInfo = _gomeostas.GetAgentState();
        _currentAgentStage = agentInfo?.EvolutionStage ?? 0;

        ThemeTypes.Clear();
        if (ThemeImageSystem.IsInitialized)
        {
          foreach (var t in ThemeImageSystem.Instance.GetEditableThemeTypes())
          {
            ThemeTypes.Add(new ThemeTypeItem { Id = t.Id, Description = t.Description ?? "", DefaultWeight = t.DefaultWeight });
          }
          var protectedIds = ThemeImageSystem.GetThemeTypeIdsProtectedFromRemoval();
          var instance = ThemeImageSystem.Instance;
          ReservedThemeTypesText = string.Join(Environment.NewLine,
            protectedIds.OrderBy(id => id).Select(id => $"ID: {id}, {instance.GetThemeTypeDescription(id)}"));
        }
        else
        {
          ReservedThemeTypesText = "";
        }

        OnPropertyChanged(nameof(ThemeTypes));
        OnPropertyChanged(nameof(ReservedThemeTypesText));
        OnPropertyChanged(nameof(IsStageFour));
        OnPropertyChanged(nameof(IsEditingEnabled));
        OnPropertyChanged(nameof(PulseWarningMessage));
        OnPropertyChanged(nameof(WarningMessageColor));
        OnPropertyChanged(nameof(CurrentAgentTitle));
        OnPropertyChanged(nameof(IsReadOnlyMode));
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка загрузки тем мышления: {ex.Message}", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    /// <summary>Следующий свободный ID: не занят в таблице и не зарезервирован в дефолтных типах ситуаций (EnsureDefaultTypes).</summary>
    public int GetNextId()
    {
      var reserved = new HashSet<int>(ThemeImageSystem.GetThemeTypeIdsProtectedFromRemoval());
      var used = new HashSet<int>(ThemeTypes.Select(x => x.Id));
      int candidate = 1;
      while (reserved.Contains(candidate) || used.Contains(candidate))
        candidate++;
      return candidate;
    }

    public void RemoveRecord(ThemeTypeItem record)
    {
      if (record != null)
        ThemeTypes.Remove(record);
    }

    /// <summary>Удалить выбранные записи из коллекции и сразу сохранить в theme_types.dat (как в AdaptiveActionsView).</summary>
    public void RemoveRecordsAndSave(IReadOnlyList<ThemeTypeItem> toRemove)
    {
      if (toRemove == null || toRemove.Count == 0) return;
      if (!ThemeImageSystem.IsInitialized)
      {
        MessageBox.Show("Система тем мышления не инициализирована.", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }
      try
      {
        foreach (var r in toRemove)
          ThemeTypes.Remove(r);
        var list = ThemeTypes.Select(t => (t.Id, t.Description ?? "", t.DefaultWeight)).ToList();
        var (success, error) = ThemeImageSystem.Instance.UpdateThemeTypesFromEditable(list);
        if (success)
          LoadData();
        else
          MessageBox.Show($"Не удалось сохранить после удаления:\n{error}", "Ошибка сохранения",
              MessageBoxButton.OK, MessageBoxImage.Error);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при удалении записей: {ex.Message}", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void SaveData(object parameter)
    {
      if (!ThemeImageSystem.IsInitialized)
      {
        MessageBox.Show("Система тем мышления не инициализирована.", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      var list = ThemeTypes.Select(t => (t.Id, t.Description ?? "", t.DefaultWeight)).ToList();
      var (success, error) = ThemeImageSystem.Instance.UpdateThemeTypesFromEditable(list);

      if (success)
      {
        LoadData();
        MessageBox.Show("Темы мышления успешно сохранены.", "Сохранение завершено",
            MessageBoxButton.OK, MessageBoxImage.Information);
      }
      else
      {
        MessageBox.Show($"Не удалось сохранить темы мышления:\n{error}",
            "Ошибка сохранения",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void ClearData(object parameter)
    {
      var result = MessageBox.Show(
          "Очистить описания всех тем мышления в таблице (заменить на пустые строки и сохранить в файл)?",
          "Подтверждение очистки",
          MessageBoxButton.YesNo,
          MessageBoxImage.Warning);

      if (result != MessageBoxResult.Yes)
        return;

      if (!ThemeImageSystem.IsInitialized)
      {
        MessageBox.Show("Система тем мышления не инициализирована.", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      var list = ThemeTypes.Select(t => (t.Id, "", t.DefaultWeight)).ToList();
      var (success, error) = ThemeImageSystem.Instance.UpdateThemeTypesFromEditable(list);
      if (success)
      {
        LoadData();
        MessageBox.Show("Описания тем мышления очищены.", "Очистка завершена",
            MessageBoxButton.OK, MessageBoxImage.Information);
      }
      else
      {
        MessageBox.Show($"Ошибка при очистке:\n{error}",
            "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    public class DescriptionWithLink
    {
      public string Text { get; set; }
      public string LinkText { get; set; } = "Подробнее...";
      public string Url { get; set; } = "https://scorcher.ru/isida/iadaptive_agents_guide.php#understanding";
      public ICommand OpenLinkCommand { get; }

      public DescriptionWithLink()
      {
        OpenLinkCommand = new RelayCommand(_ =>
        {
          try
          {
            Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true });
          }
          catch { }
        });
      }
    }
  }
}
