// ParameterLogsViewModel.cs
using AIStudio.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace AIStudio.ViewModels
{
  public class ParameterLogsViewModel : INotifyPropertyChanged, IDisposable
  {
    public event PropertyChangedEventHandler PropertyChanged;

    private readonly DispatcherTimer _refreshTimer;
    private bool _disposed = false;
    private DataGrid _dataGrid;
    private List<int> _knownParamIds = new List<int>();

    public ObservableCollection<ParameterLogGroup> ParameterLogGroups { get; } = new ObservableCollection<ParameterLogGroup>();
    public ICommand ClearLogsCommand { get; }

    // Ссылка на DataGrid для динамического добавления колонок
    public DataGrid ParametersDataGrid
    {
      get => _dataGrid;
      set
      {
        _dataGrid = value;
        InitializeDataGridColumns();
      }
    }

    public ParameterLogsViewModel()
    {
      ClearLogsCommand = new RelayCommand(_ => ClearLogs());

      _refreshTimer = new DispatcherTimer
      {
        Interval = TimeSpan.FromMilliseconds(500) // Увеличиваем интервал для производительности
      };
      _refreshTimer.Tick += (s, e) => RefreshDisplay();
      _refreshTimer.Start();
    }

    private void InitializeDataGridColumns()
    {
      if (_dataGrid == null) return;

      // Очищаем существующие колонки (кроме фиксированных)
      while (_dataGrid.Columns.Count > 2) // Оставляем Время и Пульс
      {
        _dataGrid.Columns.RemoveAt(_dataGrid.Columns.Count - 1);
      }
    }

    private void RefreshDisplay()
    {
      if (_disposed || _dataGrid == null) return;

      // Получаем текущие данные
      var currentEntries = MemoryLogManager.Instance.ParameterLogEntries.ToList();
      if (!currentEntries.Any()) return;

      // Находим все уникальные ID параметров
      var currentParamIds = currentEntries
          .Select(e => e.ParamId)
          .Distinct()
          .OrderBy(id => id)
          .ToList();

      // Проверяем, изменился ли список параметров
      if (!_knownParamIds.SequenceEqual(currentParamIds))
      {
        UpdateDataGridColumns(currentParamIds);
        _knownParamIds = currentParamIds;
      }

      // Обновляем данные
      var groupedData = currentEntries
          .GroupBy(entry => entry.Pulse)
          .OrderByDescending(g => g.Key) // Сначала новые пульсы
          .Select(g => CreateParameterLogGroup(g.Key, g.ToList(), currentParamIds))
          .ToList();

      Application.Current.Dispatcher.Invoke(() =>
      {
        ParameterLogGroups.Clear();
        foreach (var group in groupedData)
        {
          ParameterLogGroups.Add(group);
        }
      });
    }

    private void UpdateDataGridColumns(List<int> paramIds)
    {
      if (_dataGrid == null) return;

      Application.Current.Dispatcher.Invoke(() =>
      {
        // Удаляем старые колонки параметров
        while (_dataGrid.Columns.Count > 2)
        {
          _dataGrid.Columns.RemoveAt(_dataGrid.Columns.Count - 1);
        }

        // Добавляем новые колонки для каждого параметра
        foreach (var paramId in paramIds)
        {
          // Получаем имя параметра для заголовка
          var paramName = GetParameterName(paramId);

          // Создаем колонку для параметра
          var paramColumn = new DataGridTextColumn
          {
            Header = $"{paramName}\n(ID:{paramId})",
            Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells),
            Binding = new System.Windows.Data.Binding($"Parameters[{paramId}]")
          };

          // Устанавливаем стили напрямую в свойства
          paramColumn.HeaderStyle = CreateHeaderStyle();
          paramColumn.CellStyle = CreateCellStyle();

          _dataGrid.Columns.Add(paramColumn);
        }
      });
    }

    private Style CreateHeaderStyle()
    {
      var style = new Style(typeof(DataGridColumnHeader));
      style.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.Yellow));
      style.Setters.Add(new Setter(Control.FontFamilyProperty, new FontFamily("Consolas")));
      style.Setters.Add(new Setter(Control.FontSizeProperty, 12.0));
      style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Bold));
      style.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(16, 16, 16))));
      style.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Lime));
      style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
      style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(4)));
      return style;
    }

    private Style CreateCellStyle()
    {
      var style = new Style(typeof(DataGridCell));
      style.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.Lime));
      style.Setters.Add(new Setter(Control.FontFamilyProperty, new FontFamily("Consolas")));
      style.Setters.Add(new Setter(Control.FontSizeProperty, 12.0));
      style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
      style.Setters.Add(new Setter(Control.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(32, 32, 32))));
      style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
      style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(4)));
      style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));

      var trigger = new Trigger { Property = DataGridCell.IsSelectedProperty, Value = true };
      trigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromArgb(32, 0, 255, 0))));
      trigger.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
      style.Triggers.Add(trigger);

      return style;
    }

    private string GetParameterName(int paramId)
    {
      // Получаем имя параметра из последней записи
      var lastEntry = MemoryLogManager.Instance.ParameterLogEntries
          .LastOrDefault(e => e.ParamId == paramId);
      return lastEntry?.ParamName ?? $"Параметр {paramId}";
    }

    private ParameterLogGroup CreateParameterLogGroup(int pulse, List<ParameterLogEntry> entries, List<int> allParamIds)
    {
      var group = new ParameterLogGroup
      {
        Pulse = pulse,
        Timestamp = entries.First().Timestamp,
        Parameters = new Dictionary<int, string>()
      };

      // Заполняем словарь параметров
      foreach (var paramId in allParamIds)
      {
        var paramEntry = entries.FirstOrDefault(e => e.ParamId == paramId);
        if (paramEntry != null)
        {
          group.Parameters[paramId] = FormatParameterInfo(paramEntry);
        }
        else
        {
          group.Parameters[paramId] = "—"; // Прочерк если данных нет
        }
      }

      return group;
    }

    private string FormatParameterInfo(ParameterLogEntry entry)
    {
      return $"Знач: {entry.Value:F1}\n" +
             $"Вес: {entry.Weight}\n" +
             $"Срочн: {entry.UrgencyFunction:F4}\n" +
             $"Сост: {entry.ParameterState}\n" +
             $"Зона: {entry.ActivationZone}";
    }

    private void ClearLogs()
    {
      if (_disposed) return;

      Application.Current.Dispatcher.Invoke(() =>
      {
        ParameterLogGroups.Clear();
        _knownParamIds.Clear();
        InitializeDataGridColumns();
      });

      MemoryLogManager.Instance.ClearParameterLogs();
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
      if (_disposed) return;
      _refreshTimer?.Stop();
      _disposed = true;
    }

    /// <summary>
    /// Группированные данные по пульсам с горизонтальным отображением
    /// </summary>
    public class ParameterLogGroup
    {
      public DateTime Timestamp { get; set; }
      public int Pulse { get; set; }

      // Словарь: Key = ParamId, Value = форматированная строка с данными
      public Dictionary<int, string> Parameters { get; set; } = new Dictionary<int, string>();

      public string DisplayTime => Timestamp.ToString("HH:mm:ss");
      public string DisplayPulse => Pulse.ToString();

      // Индексатор для привязки данных в колонках
      public string this[int paramId] => Parameters.ContainsKey(paramId) ? Parameters[paramId] : "—";
    }
  }
}