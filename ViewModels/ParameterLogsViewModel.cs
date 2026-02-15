using AIStudio.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static AIStudio.Common.MemoryLogManager;

namespace AIStudio.ViewModels
{
  public class ParameterLogsViewModel : INotifyPropertyChanged, IDisposable
  {
    public event PropertyChangedEventHandler PropertyChanged;

    private readonly DispatcherTimer _refreshTimer;
    private bool _disposed = false;
    private DataGrid _dataGrid;
    private List<int> _knownParamIds = new List<int>();
    private ParameterLogGroup _selectedRow;

    public ObservableCollection<ParameterLogGroup> ParameterLogGroups { get; } = new ObservableCollection<ParameterLogGroup>();
    public ICommand ClearLogsCommand { get; }

    public ParameterLogGroup SelectedRow
    {
      get => _selectedRow;
      set
      {
        _selectedRow = value;
        OnPropertyChanged(nameof(SelectedRow));
      }
    }

    public DataGrid ParametersDataGrid
    {
      get => _dataGrid;
      set
      {
        _dataGrid = value;
        if (_dataGrid != null)
          _dataGrid.SelectionChanged += OnDataGridSelectionChanged;
      }
    }

    public ParameterLogsViewModel()
    {
      ClearLogsCommand = new RelayCommand(_ => ClearLogs());

      _refreshTimer = new DispatcherTimer
      {
        Interval = TimeSpan.FromMilliseconds(500)
      };
      _refreshTimer.Tick += (s, e) => RefreshDisplay();
      _refreshTimer.Start();
    }

    private void OnDataGridSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (_dataGrid?.SelectedItem is ParameterLogGroup selectedGroup)
      {
        SelectedRow = selectedGroup;
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

      // Сохраняем текущее выделение перед обновлением
      var previouslySelectedPulse = SelectedRow?.Pulse;

      // Обновляем данные
      var groupedData = currentEntries
          .GroupBy(entry => entry.Pulse)
          .OrderByDescending(g => g.Key) // Сначала новые пульсы
          .Select(g => CreateParameterLogGroup(g.Key, g.ToList(), currentParamIds))
          .ToList();

      Application.Current.Dispatcher.Invoke(() =>
      {
        // Сохраняем выделение
        ParameterLogGroup rowToSelect = null;

        ParameterLogGroups.Clear();
        foreach (var group in groupedData)
        {
          ParameterLogGroups.Add(group);

          // Восстанавливаем выделение если нужно
          if (previouslySelectedPulse.HasValue && group.Pulse == previouslySelectedPulse.Value)
          {
            rowToSelect = group;
          }
        }

        // Восстанавливаем выделение после обновления данных
        if (rowToSelect != null)
        {
          _dataGrid.SelectedItem = rowToSelect;
          SelectedRow = rowToSelect;
        }
      });
    }

    private void UpdateDataGridColumns(List<int> paramIds)
    {
      if (_dataGrid == null) return;

      Application.Current.Dispatcher.Invoke(() =>
      {
        // Сохраняем текущее выделение
        var selectedItem = _dataGrid.SelectedItem;

        _dataGrid.Columns.Clear();

        // Добавляем фиксированные колонки ПЕРВЫМИ
        var timeColumn = new DataGridTextColumn
        {
          Header = "Время",
          Width = 80,
          Binding = new System.Windows.Data.Binding("DisplayTime"),
          HeaderStyle = CreateHeaderStyle(),
          CellStyle = CreateCellStyle(),
          MinWidth = 80
        };
        _dataGrid.Columns.Add(timeColumn);

        var pulseColumn = new DataGridTextColumn
        {
          Header = "Пульс",
          Width = 60,
          Binding = new System.Windows.Data.Binding("DisplayPulse"),
          HeaderStyle = CreateHeaderStyle(),
          CellStyle = CreateCellStyle(),
          MinWidth = 60
        };
        _dataGrid.Columns.Add(pulseColumn);

        // Добавляем динамические колонки параметров
        foreach (var paramId in paramIds)
        {
          var (paramName, weight) = GetParameterNameAndWeight(paramId);
          var paramColumn = new DataGridTextColumn
          {
            Header = $"{paramName}\n(ID:{paramId}, Вес:{weight})",
            Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells),
            Binding = new System.Windows.Data.Binding($"Parameters[{paramId}]"),
            HeaderStyle = CreateHeaderStyle(),
            CellStyle = CreateParameterCellStyle(paramId),
            MinWidth = 80
          };
          _dataGrid.Columns.Add(paramColumn);
        }

        // Восстанавливаем выделение
        if (selectedItem != null)
        {
          _dataGrid.SelectedItem = selectedItem;
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
      style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0, 0, 1, 1)));
      style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(4)));
      style.Setters.Add(new Setter(Control.HeightProperty, 40.0));
      style.Setters.Add(new Setter(Control.MinHeightProperty, 40.0));
      style.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
      style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
      style.Setters.Add(new Setter(Control.ToolTipProperty, CreateParameterToolTip()));
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
      style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0, 0, 1, 1)));
      style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(4)));
      style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
      style.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));

      var trigger = new Trigger { Property = DataGridCell.IsSelectedProperty, Value = true };
      trigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromArgb(64, 0, 255, 0))));
      trigger.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
      trigger.Setters.Add(new Setter(Control.BorderBrushProperty, new SolidColorBrush(Color.FromArgb(128, 0, 255, 0))));
      style.Triggers.Add(trigger);

      return style;
    }

    private Style CreateParameterCellStyle(int paramId)
    {
      var style = new Style(typeof(DataGridCell));
      style.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.Lime));
      style.Setters.Add(new Setter(Control.FontFamilyProperty, new FontFamily("Consolas")));
      style.Setters.Add(new Setter(Control.FontSizeProperty, 12.0));
      style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent)); // По умолчанию прозрачный
      style.Setters.Add(new Setter(Control.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(32, 32, 32))));
      style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0, 0, 1, 1)));
      style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(4)));
      style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
      style.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));

      // DataTrigger для состояния -1 (красный)
      var badTrigger = new DataTrigger
      {
        Binding = new System.Windows.Data.Binding($"ParameterStates[{paramId}]"),
        Value = -1
      };
      badTrigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromArgb(64, 255, 0, 0))));
      style.Triggers.Add(badTrigger);

      // DataTrigger для состояния 1 (зеленый)
      var goodTrigger = new DataTrigger
      {
        Binding = new System.Windows.Data.Binding($"ParameterStates[{paramId}]"),
        Value = 1
      };
      goodTrigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromArgb(64, 0, 255, 0))));
      style.Triggers.Add(goodTrigger);

      // Trigger для выделения
      var selectionTrigger = new Trigger { Property = DataGridCell.IsSelectedProperty, Value = true };
      selectionTrigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromArgb(128, 0, 255, 0))));
      selectionTrigger.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
      selectionTrigger.Setters.Add(new Setter(Control.BorderBrushProperty, new SolidColorBrush(Color.FromArgb(255, 0, 255, 0))));
      style.Triggers.Add(selectionTrigger);

      return style;
    }

    private ToolTip CreateParameterToolTip()
    {
      return new ToolTip
      {
        Content = new TextBlock
        {
          Text = "Данные параметра:\n" +
             "• Знач - Текущее значение (0-100)\n" +
             "• Срочн - Функция потребности\n" +
             "• Сост - Состояние параметра\n" +
             "• Зона - Зона активации стилей\n\n" +
             "Формат зоны отклонения: Zone (Deviation|Range|Percent)\n" +
             "Zone: 0-Выход из нормы, 1-Возврат, 2-Норма, 3-Слабое, 4-Умеренное, 5-Значительное, 6-Критическое\n\n" +
             "• Deviation - величина отклонения\n" +
             "• Range - размер диапазона\n" +
             "• Percent - отношение Deviation/Range",
          TextWrapping = TextWrapping.Wrap,
          MaxWidth = 350
        },
        Background = new SolidColorBrush(Color.FromRgb(32, 32, 32)),
        BorderBrush = Brushes.Lime,
        Foreground = Brushes.Lime,
        FontFamily = new FontFamily("Consolas"),
        FontSize = 12,
        Padding = new Thickness(8)
      };
    }

    private (string name, int weight) GetParameterNameAndWeight(int paramId)
    {
      var lastEntry = MemoryLogManager.Instance.ParameterLogEntries
          .LastOrDefault(e => e.ParamId == paramId);

      if (lastEntry != null)
      {
        return (lastEntry.ParamName ?? $"Параметр {paramId}", lastEntry.Weight);
      }

      return ($"Параметр {paramId}", 0);
    }

    private ParameterLogGroup CreateParameterLogGroup(int pulse, List<ParameterLogEntry> entries, List<int> allParamIds)
    {
      var group = new ParameterLogGroup
      {
        Pulse = pulse,
        Timestamp = entries.First().Timestamp,
        Parameters = new Dictionary<int, string>(),
        ParameterStates = new Dictionary<int, int>()
      };

      // Заполняем словарь параметров и состояний
      foreach (var paramId in allParamIds)
      {
        var paramEntry = entries.FirstOrDefault(e => e.ParamId == paramId);
        if (paramEntry != null)
        {
          var (info, stateCode) = FormatParameterInfo(paramEntry);
          group.Parameters[paramId] = info;
          group.ParameterStates[paramId] = stateCode;
        }
        else
        {
          group.Parameters[paramId] = "—";
          group.ParameterStates[paramId] = 0; // Прозрачный
        }
      }

      return group;
    }
    private (string info, int stateCode) FormatParameterInfo(ParameterLogEntry entry)
    {
      string info = $"Знач: {entry.Value:F3}\n" +
                   $"Срочн: {entry.UrgencyFunction:F3}\n" +
                   $"Сост: {entry.ParameterState}\n" +
                   $"Зона: {entry.ActivationZone}";

      int stateCode = 0; // 0 = прозрачный (норма)

      if (entry.ParameterState != null)
      {
        string state = entry.ParameterState.ToLower();
        if (state.Contains("плохо") || state.Contains("bad") || state.Contains("критич"))
          stateCode = -1; // Красный
        else if (state.Contains("хорошо") || state.Contains("good"))
          stateCode = 1;  // Зеленый
      }

      return (info, stateCode);
    }

    private void ClearLogs()
    {
      if (_disposed) return;

      Application.Current.Dispatcher.Invoke(() =>
      {
        ParameterLogGroups.Clear();
        _knownParamIds.Clear();
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
      if (_dataGrid != null)
      {
        _dataGrid.SelectionChanged -= OnDataGridSelectionChanged;
      }
      _disposed = true;
    }

    /// <summary>
    /// Группированные данные по пульсам с горизонтальным отображением
    /// </summary>
    public class ParameterLogGroup
    {
      public DateTime Timestamp { get; set; }
      public int Pulse { get; set; }
      public Dictionary<int, string> Parameters { get; set; } = new Dictionary<int, string>();
      public Dictionary<int, int> ParameterStates { get; set; } = new Dictionary<int, int>();

      public string DisplayTime => Timestamp.ToString("HH:mm:ss");
      public string DisplayPulse => Pulse.ToString();
      public string this[int paramId] => Parameters.ContainsKey(paramId) ? Parameters[paramId] : "—";

      public int GetStateCode(int paramId) => ParameterStates.ContainsKey(paramId) ? ParameterStates[paramId] : 0;

      public Brush GetBackground(int paramId)
      {
        int stateCode = GetStateCode(paramId);
        
        if (stateCode == -1)
          return new SolidColorBrush(Color.FromArgb(64, 255, 0, 0));   // Красный для плохо
        else if (stateCode == 1)
          return new SolidColorBrush(Color.FromArgb(64, 0, 255, 0));    // Зеленый для хорошо
        else
          return Brushes.Transparent;
      }
    }

  }
}