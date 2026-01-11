using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using AIStudio.Common;
using System.Collections.Generic;

namespace AIStudio.ViewModels
{
  public class StyleLogsViewModel : INotifyPropertyChanged, IDisposable
  {
    public event PropertyChangedEventHandler PropertyChanged;

    private readonly DispatcherTimer _refreshTimer;
    private bool _disposed = false;
    private StyleLogGroup _selectedRow;

    public ObservableCollection<StyleLogGroup> StyleLogGroups { get; } = new ObservableCollection<StyleLogGroup>();
    public ICommand ClearLogsCommand { get; }

    public StyleLogGroup SelectedRow
    {
      get => _selectedRow;
      set
      {
        _selectedRow = value;
        OnPropertyChanged(nameof(SelectedRow));
      }
    }

    public StyleLogsViewModel()
    {
      ClearLogsCommand = new RelayCommand(_ => ClearLogs());

      _refreshTimer = new DispatcherTimer
      {
        Interval = TimeSpan.FromMilliseconds(500)
      };
      _refreshTimer.Tick += (s, e) => RefreshDisplay();
      _refreshTimer.Start();
    }

    private void RefreshDisplay()
    {
      if (_disposed) return;

      var previouslySelectedPulse = SelectedRow?.Pulse;

      // Группируем данные по пульсу
      var groupedData = MemoryLogManager.Instance.StyleLogEntries
          .GroupBy(entry => entry.Pulse)
          .OrderByDescending(g => g.Key)
          .Select(g => new StyleLogGroup
          {
            Pulse = g.Key,
            Timestamp = g.First().Timestamp,
            FinalStyles = g.Where(e => e.Stage == "Final")
                             .Select(e => new StyleInfo
                             {
                               StyleId = e.StyleId,
                               StyleName = e.StyleName
                             })
                             .ToList(),
            // Собираем активации параметров
            ParameterActivations = MemoryLogManager.Instance.StyleParameterActivationEntries
                      .Where(e => e.Pulse == g.Key)
                      .Select(e => new ParameterActivationInfo
                      {
                        ParameterId = e.ParameterId,
                        ParameterName = e.ParameterName,
                        ZoneId = e.ZoneId,
                        ZoneDescription = e.ZoneDescription,
                        StyleId = e.StyleId,
                        StyleName = e.StyleName
                      })
                      .ToList()
          })
          .ToList();

      Application.Current.Dispatcher.Invoke(() =>
      {
        StyleLogGroup rowToSelect = null;

        StyleLogGroups.Clear();
        foreach (var group in groupedData)
        {
          // Вычисляем высоту строки на основе количества стилей
          group.RowHeight = CalculateRowHeight(group);
          StyleLogGroups.Add(group);

          if (previouslySelectedPulse.HasValue && group.Pulse == previouslySelectedPulse.Value)
          {
            rowToSelect = group;
          }
        }

        if (rowToSelect != null)
        {
          SelectedRow = rowToSelect;
        }
      });
    }

    private int CalculateRowHeight(StyleLogGroup group)
    {
      // Базовая высота для одной строки
      const int baseHeightPerLine = 20;

      var linesInParameters = string.IsNullOrEmpty(group.DisplayParameters) ? 1 : group.DisplayParameters.Split('\n').Length;
      var linesInZones = string.IsNullOrEmpty(group.DisplayZones) ? 1 : group.DisplayZones.Split('\n').Length;
      var linesInActiveStyles = string.IsNullOrEmpty(group.DisplayActiveStyles) ? 1 : group.DisplayActiveStyles.Split('\n').Length;
      var maxLines = Math.Max(linesInParameters, Math.Max(linesInZones, linesInActiveStyles));

      return Math.Max(baseHeightPerLine, maxLines * baseHeightPerLine);
    }

    private void ClearLogs()
    {
      if (_disposed) return;

      Application.Current.Dispatcher.Invoke(() =>
      {
        StyleLogGroups.Clear();
        SelectedRow = null;
      });

      // Также очищаем исходные данные
      MemoryLogManager.Instance.ClearStyleLogs();
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
    /// Группированные данные по пульсам
    /// </summary>
    public class StyleLogGroup
    {
      public DateTime Timestamp { get; set; }
      public int Pulse { get; set; }
      public int RowHeight { get; set; } = 30; // Базовая высота по умолчанию
      public List<StyleInfo> FinalStyles { get; set; } = new List<StyleInfo>();
      public List<ParameterActivationInfo> ParameterActivations { get; set; } = new List<ParameterActivationInfo>();

      public string DisplayTime => Timestamp.ToString("HH:mm:ss");
      public string DisplayPulse => Pulse.ToString();

      // Для отображения в таблице
      public string DisplayActiveStyles => string.Join(" | ", FinalStyles.Select(s => s.ToString()));

      // Разделяем параметры и зоны на два столбца
      public string DisplayParameters => GetParametersDisplay();
      public string DisplayZones => GetZonesDisplay();

      private string GetParametersDisplay()
      {
        var uniqueParameters = ParameterActivations
            .GroupBy(pa => new { pa.ParameterId, pa.ParameterName })
            .OrderBy(g => g.Key.ParameterName)
            .Select(g => $"{g.Key.ParameterName} (ID:{g.Key.ParameterId})")
            .ToList();

        return string.Join("\n", uniqueParameters);
      }

      private string GetZonesDisplay()
      {
        var uniqueZones = ParameterActivations
            .GroupBy(pa => new { pa.ZoneId, pa.ZoneDescription })
            .OrderBy(g => g.Key.ZoneId)
            .Select(g => $"{g.Key.ZoneDescription} (ID:{g.Key.ZoneId})")
            .ToList();

        return string.Join("\n", uniqueZones);
      }
    }

    /// <summary>
    /// Информация о стиле
    /// </summary>
    public class StyleInfo
    {
      public int StyleId { get; set; }
      public string StyleName { get; set; } = string.Empty;
      public override string ToString()
      {
        return $"{StyleName} (ID:{StyleId})";
      }
    }

    /// <summary>
    /// Информация об активации параметра
    /// </summary>
    public class ParameterActivationInfo
    {
      public int ParameterId { get; set; }
      public string ParameterName { get; set; } = string.Empty;
      public int ZoneId { get; set; }
      public string ZoneDescription { get; set; } = string.Empty;
      public int StyleId { get; set; }
      public string StyleName { get; set; } = string.Empty;
    }

  }
}