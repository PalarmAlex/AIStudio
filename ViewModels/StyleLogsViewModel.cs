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
            BaseStyles = g.Where(e => e.Stage == "Base")
                           .Select(e => new StyleInfo
                           {
                             StyleId = e.StyleId,
                             StyleName = e.StyleName,
                             Weight = e.Weight
                           })
                           .ToList(),
            AfterAntagonistStyles = g.Where(e => e.Stage == "AfterAntagonists")
                                     .Select(e => new StyleInfo
                                     {
                                       StyleId = e.StyleId,
                                       StyleName = e.StyleName,
                                       Weight = e.Weight
                                     })
                                     .ToList(),
            FinalStyles = g.Where(e => e.Stage == "Final")
                           .Select(e => new StyleInfo
                           {
                             StyleId = e.StyleId,
                             StyleName = e.StyleName,
                             Weight = e.Weight
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
                      StyleName = e.StyleName,
                      Weight = e.Weight
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
      const int baseHeightPerLine = 15;

      // Считаем количество строк в столбце "Начальные стили"
      var baseStylesText = group.DisplayBaseStyles;
      var lineCount = string.IsNullOrEmpty(baseStylesText) ? 1 : baseStylesText.Split('\n').Length;

      // Вычисляем высоту: количество строк * высота строки
      return Math.Max(baseHeightPerLine, lineCount * baseHeightPerLine);
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

      // Базовые стили
      public List<StyleInfo> BaseStyles { get; set; } = new List<StyleInfo>();

      // Стили после антагонистов
      public List<StyleInfo> AfterAntagonistStyles { get; set; } = new List<StyleInfo>();

      // Финальные стили
      public List<StyleInfo> FinalStyles { get; set; } = new List<StyleInfo>();

      // Активации параметров
      public List<ParameterActivationInfo> ParameterActivations { get; set; } = new List<ParameterActivationInfo>();

      public string DisplayTime => Timestamp.ToString("HH:mm:ss");
      public string DisplayPulse => Pulse.ToString();

      // Для отображения в таблице
      public string DisplayAfterAntagonistStyles => string.Join("\n", AfterAntagonistStyles.Select(s => s.ToString()));
      public string DisplayFinalStyles => string.Join("\n", FinalStyles.Select(s => s.ToString()));

      public string DisplayParametersAndZones => GetParametersAndZonesDisplay();
      public string DisplayBaseStyles => GetBaseStylesDisplay();

      private string GetParametersAndZonesDisplay()
      {
        var result = new List<string>();

        // Группируем по параметру и зоне
        var groupedByParamZone = ParameterActivations
            .GroupBy(pa => new { pa.ParameterId, pa.ParameterName, pa.ZoneId, pa.ZoneDescription })
            .OrderBy(g => g.Key.ParameterName)
            .ThenBy(g => g.Key.ZoneId);

        foreach (var paramZoneGroup in groupedByParamZone)
        {
          result.Add($"{paramZoneGroup.Key.ParameterName} (ID:{paramZoneGroup.Key.ParameterId}) | {paramZoneGroup.Key.ZoneDescription} (ID:{paramZoneGroup.Key.ZoneId})");
          for (int i = 1; i < paramZoneGroup.Count(); i++)
          {
            result.Add("");
          }
        }

        return string.Join("\n", result);
      }

      private string GetBaseStylesDisplay()
      {
        var result = new List<string>();

        // Группируем по параметру и зоне
        var groupedByParamZone = ParameterActivations
            .GroupBy(pa => new { pa.ParameterId, pa.ParameterName, pa.ZoneId, pa.ZoneDescription })
            .OrderBy(g => g.Key.ParameterName)
            .ThenBy(g => g.Key.ZoneId);

        foreach (var paramZoneGroup in groupedByParamZone)
        {
          // Для каждой группы параметр-зона выводим все стили
          var stylesInGroup = paramZoneGroup
              .OrderBy(pa => pa.StyleName)
              .Select(pa => $"{pa.StyleName} (ID:{pa.StyleId}) | Вес:{pa.Weight}");

          result.AddRange(stylesInGroup);
        }

        return string.Join("\n", result);
      }
    }

    /// <summary>
    /// Информация о стиле
    /// </summary>
    public class StyleInfo
    {
      public int StyleId { get; set; }
      public string StyleName { get; set; } = string.Empty;
      public int Weight { get; set; }
      public override string ToString()
      {
        return $"{StyleName} (ID:{StyleId}) | Вес:{Weight}";
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
      public int Weight { get; set; }
    }
  }
}